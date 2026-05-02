using HelpDeskSystem.Application.DTOs.Tickets;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HelpDeskSystem.AI.Services;

public interface ITicketCategorizationService
{
    Task<TicketCategorizationResult> CategorizeTicketAsync(CreateTicketDto ticketDto);
    Task<AgentSuggestionResult> SuggestAgentAsync(int ticketId);
    Task<ResponseSuggestionResult> GenerateResponseSuggestionAsync(int ticketId);
    Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string content);
    Task<bool> AutoAssignTicketAsync(int ticketId);
}

public class TicketCategorizationService : ITicketCategorizationService
{
    private readonly HelpDeskDbContext _context;
    private readonly ILogger<TicketCategorizationService> _logger;

    // Keywords for categorization
    private readonly Dictionary<string, string> _categoryKeywords = new()
    {
        // Technical Support
        ["password"] = "Technical Support",
        ["login"] = "Technical Support",
        ["access"] = "Technical Support",
        ["error"] = "Technical Support",
        ["bug"] = "Technical Support",
        ["crash"] = "Technical Support",
        ["slow"] = "Technical Support",
        ["performance"] = "Technical Support",
        
        // Billing
        ["payment"] = "Billing",
        ["invoice"] = "Billing",
        ["charge"] = "Billing",
        ["refund"] = "Billing",
        ["billing"] = "Billing",
        ["price"] = "Billing",
        ["cost"] = "Billing",
        
        // Account
        ["account"] = "Account",
        ["profile"] = "Account",
        ["settings"] = "Account",
        ["subscription"] = "Account",
        ["cancel"] = "Account",
        ["upgrade"] = "Account",
        
        // Feature Request
        ["feature"] = "Feature Request",
        ["request"] = "Feature Request",
        ["suggestion"] = "Feature Request",
        ["improvement"] = "Feature Request",
        ["new"] = "Feature Request",
        ["add"] = "Feature Request"
    };

    // Priority keywords
    private readonly Dictionary<string, int> _priorityKeywords = new()
    {
        ["urgent"] = 1, // Critical
        ["emergency"] = 1,
        ["critical"] = 1,
        ["production"] = 1,
        ["down"] = 1,
        
        ["high"] = 2, // High
        ["important"] = 2,
        ["priority"] = 2,
        ["asap"] = 2,
        
        ["medium"] = 3, // Medium
        ["normal"] = 3,
        ["regular"] = 3,
        
        ["low"] = 4, // Low
        ["minor"] = 4,
        ["later"] = 4,
        ["when"] = 4
    };

    public TicketCategorizationService(HelpDeskDbContext context, ILogger<TicketCategorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TicketCategorizationResult> CategorizeTicketAsync(CreateTicketDto ticketDto)
    {
        var result = new TicketCategorizationResult();
        
        // Combine title and description for analysis
        var content = $"{ticketDto.Title} {ticketDto.Description}".ToLower();
        
        // Categorize based on keywords
        result.SuggestedCategory = CategorizeByKeywords(content);
        result.SuggestedPriority = DeterminePriorityByKeywords(content);
        result.Confidence = CalculateConfidence(content, result.SuggestedCategory);
        
        // Use historical data for better categorization
        var historicalMatch = await FindHistoricalMatchAsync(content);
        if (historicalMatch != null)
        {
            result.SuggestedCategory = historicalMatch.Category?.Name ?? result.SuggestedCategory;
            result.SuggestedPriority = historicalMatch.PriorityId;
            result.Confidence = Math.Max(result.Confidence, 0.8); // Higher confidence with historical match
        }
        
        _logger.LogInformation("AI categorization: Category={Category}, Priority={Priority}, Confidence={Confidence}%", 
            result.SuggestedCategory, result.SuggestedPriority, result.Confidence * 100);
        
        return result;
    }

    public async Task<AgentSuggestionResult> SuggestAgentAsync(int ticketId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return new AgentSuggestionResult { Success = false, Reason = "Ticket not found" };

        var result = new AgentSuggestionResult();
        
        // Find agents with relevant expertise
        var suitableAgents = await _context.Users
            .Where(u => u.IsActive && 
                       u.UserRoles.Any(r => r.Role.Name == "Agent") &&
                       !u.IsSuperAdmin)
            .ToListAsync();

        var agentScores = new List<AgentScore>();

        foreach (var agent in suitableAgents)
        {
            var score = CalculateAgentScore(agent, ticket);
            if (score > 0)
            {
                agentScores.Add(new AgentScore
                {
                    AgentId = agent.Id,
                    AgentName = $"{agent.FirstName} {agent.LastName}",
                    Score = score,
                    Reasons = GetAgentScoreReasons(agent, ticket)
                });
            }
        }

        result.SuggestedAgents = agentScores.OrderByDescending(a => a.Score).Take(3).ToList();
        result.Success = result.SuggestedAgents.Any();
        
        if (!result.Success)
            result.Reason = "No suitable agents found";

        return result;
    }

    public async Task<ResponseSuggestionResult> GenerateResponseSuggestionAsync(int ticketId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return new ResponseSuggestionResult { Success = false, Reason = "Ticket not found" };

        var result = new ResponseSuggestionResult();
        
        // Find similar resolved tickets
        var similarTickets = await FindSimilarTicketsAsync(ticket);
        
        if (similarTickets.Any())
        {
            // Generate response based on similar tickets
            result.SuggestedResponse = GenerateResponseFromSimilarTickets(similarTickets);
            result.Confidence = Math.Min(0.9, 0.5 + (similarTickets.Count * 0.1));
            result.Success = true;
        }
        else
        {
            // Generate generic response based on category
            result.SuggestedResponse = GenerateGenericResponse(ticket.Category?.Name ?? "");
            result.Confidence = 0.3;
            result.Success = true;
        }

        return result;
    }

    public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string content)
    {
        var result = new SentimentAnalysisResult();
        
        // Simple sentiment analysis based on keywords
        var positiveWords = new[] { "thank", "great", "good", "excellent", "appreciate", "helpful", "resolved", "working" };
        var negativeWords = new[] { "angry", "frustrated", "terrible", "awful", "broken", "not working", "issue", "problem", "error" };
        
        var words = content.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var positiveCount = words.Count(w => positiveWords.Any(pw => w.Contains(pw)));
        var negativeCount = words.Count(w => negativeWords.Any(nw => w.Contains(nw)));
        
        var totalSentimentWords = positiveCount + negativeCount;
        
        if (totalSentimentWords == 0)
        {
            result.Sentiment = "Neutral";
            result.Confidence = 0.5;
        }
        else
        {
            var positiveRatio = (double)positiveCount / totalSentimentWords;
            
            if (positiveRatio > 0.6)
            {
                result.Sentiment = "Positive";
                result.Confidence = 0.7 + (positiveRatio - 0.6);
            }
            else if (positiveRatio < 0.4)
            {
                result.Sentiment = "Negative";
                result.Confidence = 0.7 + (0.4 - positiveRatio);
            }
            else
            {
                result.Sentiment = "Neutral";
                result.Confidence = 0.6;
            }
        }

        result.SuggestedPriority = result.Sentiment == "Negative" ? 2 : 3; // High priority for negative sentiment
        
        return await Task.FromResult(result);
    }

    public async Task<bool> AutoAssignTicketAsync(int ticketId)
    {
        var suggestion = await SuggestAgentAsync(ticketId);
        
        if (suggestion.Success && suggestion.SuggestedAgents.Any())
        {
            var topAgent = suggestion.SuggestedAgents.First();
            
            // Assign ticket to the best agent
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.AssignedToUserId = topAgent.AgentId;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Auto-assigned ticket {TicketId} to agent {AgentId}", ticketId, topAgent.AgentId);
                return true;
            }
        }
        
        return false;
    }

    private string CategorizeByKeywords(string content)
    {
        var scores = new Dictionary<string, int>();
        
        foreach (var keyword in _categoryKeywords)
        {
            if (content.Contains(keyword.Key))
            {
                var category = keyword.Value;
                scores[category] = scores.GetValueOrDefault(category, 0) + 1;
            }
        }
        
        return scores.Any() ? scores.OrderByDescending(kvp => kvp.Value).First().Key : "Technical Support";
    }

    private int DeterminePriorityByKeywords(string content)
    {
        foreach (var keyword in _priorityKeywords)
        {
            if (content.Contains(keyword.Key))
            {
                return keyword.Value;
            }
        }
        
        return 3; // Default to Medium
    }

    private double CalculateConfidence(string content, string category)
    {
        var categoryKeywords = _categoryKeywords.Where(kvp => kvp.Value == category).ToList();
        var matchCount = categoryKeywords.Count(kvp => content.Contains(kvp.Key));
        
        return Math.Min(0.9, matchCount * 0.3);
    }

    private async Task<Ticket?> FindHistoricalMatchAsync(string content)
    {
        // Simple similarity check using keyword matching
        var keywords = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Take(5)
            .ToList();

        if (!keywords.Any()) return null;

        var query = _context.Tickets.AsQueryable();
        
        foreach (var keyword in keywords)
        {
            query = query.Where(t => t.Title.Contains(keyword) || t.Description.Contains(keyword));
        }

        return await query
            .Include(t => t.Category)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    private double CalculateAgentScore(User agent, Ticket ticket)
    {
        var score = 0.0;
        
        // Category expertise (based on historical assignments)
        var categoryExpertise = _context.Tickets
            .Count(t => t.AssignedToUserId == agent.Id && t.CategoryId == ticket.CategoryId);
        score += categoryExpertise * 0.3;
        
        // Current workload (lower is better)
        var currentWorkload = _context.Tickets
            .Count(t => t.AssignedToUserId == agent.Id && t.Status != Domain.Enums.TicketStatus.Closed);
        score += Math.Max(0, (10 - currentWorkload) * 0.2);
        
        // Historical performance
        var resolvedTickets = _context.Tickets
            .Count(t => t.AssignedToUserId == agent.Id && t.Status == Domain.Enums.TicketStatus.Closed);
        score += resolvedTickets * 0.1;
        
        // Priority matching
        if (ticket.PriorityId <= 2) // High priority tickets
        {
            score += agent.IsActive ? 0.2 : 0;
        }
        
        return score;
    }

    private List<string> GetAgentScoreReasons(User agent, Ticket ticket)
    {
        var reasons = new List<string>();
        
        var categoryExpertise = _context.Tickets
            .Count(t => t.AssignedToUserId == agent.Id && t.CategoryId == ticket.CategoryId);
        if (categoryExpertise > 0)
            reasons.Add($"Has {categoryExpertise} tickets in this category");
        
        var currentWorkload = _context.Tickets
            .Count(t => t.AssignedToUserId == agent.Id && t.Status != Domain.Enums.TicketStatus.Closed);
        if (currentWorkload < 5)
            reasons.Add("Low current workload");
        
        return reasons;
    }

    private async Task<List<Ticket>> FindSimilarTicketsAsync(Ticket ticket)
    {
        var keywords = $"{ticket.Title} {ticket.Description}"
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Take(3)
            .ToList();

        if (!keywords.Any()) return new List<Ticket>();

        var query = _context.Tickets
            .Where(t => t.Id != ticket.Id && t.Status == Domain.Enums.TicketStatus.Closed);

        foreach (var keyword in keywords)
        {
            query = query.Where(t => t.Title.Contains(keyword) || t.Description.Contains(keyword));
        }

        return await query
            .Include(t => t.Messages)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(3)
            .ToListAsync();
    }

    private string GenerateResponseFromSimilarTickets(List<Ticket> similarTickets)
    {
        var responses = similarTickets
            .SelectMany(t => t.Messages)
            .Where(c => !c.IsInternalNote)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(2)
            .Select(c => c.Message)
            .ToList();

        if (responses.Any())
        {
            return string.Join("\n\n", responses.Select((r, i) => $"Suggestion {i + 1}:\n{r}"));
        }

        return "Based on similar tickets, I recommend checking the following common solutions...";
    }

    private string GenerateGenericResponse(string category)
    {
        var responses = new Dictionary<string, string>
        {
            ["Technical Support"] = "I understand you're experiencing a technical issue. Let me help you troubleshoot this step by step. First, could you please provide more details about when this issue started and any error messages you're seeing?",
            ["Billing"] = "I'll be happy to help you with your billing inquiry. To assist you better, could you please provide your account details and describe the specific billing issue you're experiencing?",
            ["Account"] = "I can help you with your account-related request. Please let me know what specific changes you need to make to your account, and I'll guide you through the process.",
            ["Feature Request"] = "Thank you for your suggestion! We appreciate feedback from our users and will consider this for future development. Could you provide more details about how this feature would benefit your workflow?"
        };

        return responses.GetValueOrDefault(category, "Thank you for contacting us. I understand your request and will work to provide you with the best possible assistance. Could you please provide any additional details that might help me resolve this more quickly?");
    }
}

// Supporting DTOs
public class TicketCategorizationResult
{
    public string SuggestedCategory { get; set; } = string.Empty;
    public int SuggestedPriority { get; set; }
    public double Confidence { get; set; }
}

public class AgentSuggestionResult
{
    public bool Success { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<AgentScore> SuggestedAgents { get; set; } = new();
}

public class AgentScore
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> Reasons { get; set; } = new();
}

public class ResponseSuggestionResult
{
    public bool Success { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SuggestedResponse { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class SentimentAnalysisResult
{
    public string Sentiment { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public int SuggestedPriority { get; set; }
}
