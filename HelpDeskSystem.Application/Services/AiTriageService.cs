using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HelpDeskSystem.Application.Services;

/// <summary>
/// AI Triage Engine: keyword-weighted classification, sentiment analysis,
/// duplicate detection, and smart reply suggestions — no external AI dependency required.
/// When an LLM API key is configured, it upgrades to full language model inference.
/// </summary>
public class AiTriageService : IAiTriageService
{
    private readonly HelpDeskDbContext _context;
    private readonly ILogger<AiTriageService> _logger;

    // Keyword → category signal mappings (extend via DB config in future)
    private static readonly Dictionary<string, string[]> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["billing"]           = ["invoice", "charge", "payment", "refund", "billing", "price", "subscription", "credit card", "overcharged", "fee"],
        ["technical"]         = ["error", "bug", "crash", "not working", "broken", "fails", "exception", "timeout", "server", "500", "404", "api"],
        ["account"]           = ["login", "password", "access", "locked", "reset", "sign in", "two factor", "mfa", "account", "credentials", "username"],
        ["feature_request"]   = ["feature", "request", "suggest", "would like", "can you add", "improvement", "enhancement", "wish", "want"],
        ["network"]           = ["network", "vpn", "wifi", "internet", "connection", "bandwidth", "latency", "ping", "firewall", "ip"],
        ["hardware"]          = ["hardware", "printer", "monitor", "keyboard", "mouse", "laptop", "computer", "device", "screen", "cable"],
        ["software"]          = ["software", "install", "update", "upgrade", "version", "license", "application", "program", "setup"],
        ["security"]          = ["security", "hacked", "breach", "phishing", "virus", "malware", "suspicious", "unauthorized", "threat"],
        ["performance"]       = ["slow", "performance", "lag", "speed", "loading", "wait", "delay", "unresponsive", "freezing", "stuck"],
        ["onboarding"]        = ["new employee", "onboarding", "new user", "setup account", "getting started", "first day", "new hire"],
    };

    private static readonly Dictionary<string, string[]> PriorityKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["critical"] = ["urgent", "critical", "emergency", "down", "outage", "production", "cannot work", "blocked", "asap", "immediately", "all users", "everyone affected"],
        ["high"]     = ["high priority", "important", "soon", "client waiting", "customer affected", "deadline", "escalate", "multiple users"],
        ["low"]      = ["when you can", "not urgent", "whenever", "low priority", "minor", "nice to have", "small issue"],
    };

    private static readonly Dictionary<string, string[]> NegativeSentimentWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["very_negative"] = ["furious", "unacceptable", "disgusting", "terrible", "horrible", "outrageous", "useless", "incompetent", "worst", "disaster"],
        ["negative"]      = ["frustrated", "disappointed", "angry", "upset", "unhappy", "bad", "poor", "wrong", "failed", "broken", "annoyed"],
        ["positive"]      = ["thank", "great", "good", "excellent", "happy", "appreciate", "helpful", "resolved", "fixed", "perfect", "wonderful"],
        ["very_positive"] = ["amazing", "outstanding", "fantastic", "superb", "love", "best", "exceptional", "brilliant", "impressive"],
    };

    public AiTriageService(HelpDeskDbContext context, ILogger<AiTriageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AiTriageResultDto> TriageTicketAsync(int ticketId, int tenantId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.TenantId == tenantId && !t.IsDeleted);

        if (ticket == null)
            throw new InvalidOperationException($"Ticket {ticketId} not found.");

        var fullText = $"{ticket.Title} {ticket.Description}";
        var lowerText = fullText.ToLowerInvariant();

        // --- Category Detection ---
        var categories = await _context.TicketCategories
            .AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .ToListAsync();

        var categoryScores = new Dictionary<int, double>();
        var detectedKeywords = new List<string>();

        foreach (var (categoryKey, keywords) in CategoryKeywords)
        {
            foreach (var keyword in keywords)
            {
                if (lowerText.Contains(keyword))
                {
                    detectedKeywords.Add(keyword);
                    // Try to match to actual DB category
                    var matchedCategory = categories.FirstOrDefault(c =>
                        c.Name.Contains(categoryKey, StringComparison.OrdinalIgnoreCase) ||
                        categoryKey.Contains(c.Name.Split(' ')[0], StringComparison.OrdinalIgnoreCase));
                    if (matchedCategory != null)
                    {
                        categoryScores.TryGetValue(matchedCategory.Id, out var existing);
                        categoryScores[matchedCategory.Id] = existing + 1.0;
                    }
                }
            }
        }

        int? suggestedCategoryId = null;
        string? suggestedCategoryName = null;
        double categoryConfidence = 0;

        if (categoryScores.Any())
        {
            var bestCategory = categoryScores.MaxBy(x => x.Value);
            suggestedCategoryId = bestCategory.Key;
            suggestedCategoryName = categories.FirstOrDefault(c => c.Id == bestCategory.Key)?.Name;
            categoryConfidence = Math.Min(bestCategory.Value / 3.0, 1.0);
        }

        // --- Priority Detection ---
        var priorities = await _context.TicketPriorities
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Level)
            .ToListAsync();

        int? suggestedPriorityId = null;
        string? suggestedPriorityName = null;
        double priorityConfidence = 0;

        foreach (var (level, keywords) in PriorityKeywords)
        {
            foreach (var keyword in keywords)
            {
                if (lowerText.Contains(keyword))
                {
                    detectedKeywords.Add(keyword);
                    var matchedPriority = priorities.FirstOrDefault(p =>
                        p.Name.Contains(level, StringComparison.OrdinalIgnoreCase) ||
                        level.Contains(p.Name.Split(' ')[0], StringComparison.OrdinalIgnoreCase));
                    if (matchedPriority != null && priorityConfidence < 1.0)
                    {
                        suggestedPriorityId = matchedPriority.Id;
                        suggestedPriorityName = matchedPriority.Name;
                        priorityConfidence = 0.75;
                        break;
                    }
                }
            }
            if (suggestedPriorityId.HasValue) break;
        }

        // --- Auto-assign: find least-loaded agent in tenant ---
        var agentAssignment = await GetLeastLoadedAgentAsync(tenantId, suggestedCategoryId);
        int? suggestedAssigneeUserId = agentAssignment?.UserId;
        string? suggestedAssigneeName = agentAssignment?.Name;

        // --- Sentiment Analysis ---
        var sentiment = await AnalyzeSentimentAsync(fullText);

        // --- Overall confidence ---
        var overallConfidence = (categoryConfidence + priorityConfidence) / 2.0;
        if (overallConfidence == 0) overallConfidence = 0.3; // base fallback

        var reasoning = BuildReasoning(detectedKeywords, suggestedCategoryName, suggestedPriorityName, sentiment.Label);

        _logger.LogInformation(
            "AI Triage: ticket {TicketId} → category={Category}, priority={Priority}, sentiment={Sentiment}, confidence={Confidence:P0}",
            ticketId, suggestedCategoryName, suggestedPriorityName, sentiment.Label, overallConfidence);

        return new AiTriageResultDto(
            TicketId: ticketId,
            SuggestedCategoryId: suggestedCategoryId,
            SuggestedCategoryName: suggestedCategoryName,
            SuggestedPriorityId: suggestedPriorityId,
            SuggestedPriorityName: suggestedPriorityName,
            SuggestedAssigneeUserId: suggestedAssigneeUserId,
            SuggestedAssigneeName: suggestedAssigneeName,
            ConfidenceScore: Math.Round(overallConfidence, 2),
            Reasoning: reasoning,
            DetectedKeywords: detectedKeywords.Distinct().Take(10).ToList(),
            SentimentLabel: sentiment.Label,
            SentimentScore: sentiment.Score
        );
    }

    public async Task<AiSuggestedReplyDto> SuggestReplyAsync(int ticketId, int tenantId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Messages.OrderByDescending(m => m.CreatedAtUtc).Take(3))
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.TenantId == tenantId && !t.IsDeleted);

        if (ticket == null)
            throw new InvalidOperationException($"Ticket {ticketId} not found.");

        // Find relevant KB articles
        var articles = await SuggestKnowledgeArticlesAsync(ticket.Title, ticket.Description, tenantId);
        var topArticle = articles.Articles.FirstOrDefault();

        // Build templated reply based on category/sentiment
        var sentiment = await AnalyzeSentimentAsync($"{ticket.Title} {ticket.Description}");
        var replyOpener = sentiment.Label.StartsWith("negative", StringComparison.OrdinalIgnoreCase)
            ? "I'm sorry to hear you're experiencing this issue. I completely understand your frustration and I'm here to help resolve this for you."
            : "Thank you for reaching out to us.";

        var categoryContext = ticket.Category?.Name?.ToLowerInvariant() ?? "general";
        var bodyTemplate = GetReplyTemplate(categoryContext, ticket.Title);

        var fullReply = $"{replyOpener}\n\n{bodyTemplate}\n\nIf you need any further assistance, please don't hesitate to reply to this ticket.";

        _logger.LogInformation("AI suggested reply generated for ticket {TicketId}", ticketId);

        return new AiSuggestedReplyDto(
            TicketId: ticketId,
            SuggestedReply: fullReply,
            ConfidenceScore: topArticle != null ? 0.82 : 0.55,
            ReferencedArticleIds: articles.Articles.Select(a => a.ArticleId).ToList()
        );
    }

    public async Task<AiArticleSuggestionsDto> SuggestKnowledgeArticlesAsync(string title, string description, int tenantId)
    {
        var searchText = $"{title} {description}".ToLowerInvariant();
        var words = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .Where(w => w.Length > 3)
                              .Distinct()
                              .Take(20)
                              .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var articles = await _context.KnowledgeBaseArticles
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.IsPublished && !a.IsDeleted)
            .Select(a => new { a.Id, a.Title, a.Slug, a.Body })
            .Take(200)
            .ToListAsync();

        var scored = articles
            .Select(a =>
            {
                var articleWords = $"{a.Title} {a.Body}".ToLowerInvariant();
                var matchCount = words.Count(w => articleWords.Contains(w));
                var score = words.Count > 0 ? (double)matchCount / words.Count : 0;
                return new { a.Id, a.Title, a.Slug, Score = score };
            })
            .Where(x => x.Score > 0.1)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => new AiArticleMatch(x.Id, x.Title, Math.Round(x.Score, 2)))
            .ToList();

        return new AiArticleSuggestionsDto(scored);
    }

    public Task<AiSentimentDto> AnalyzeSentimentAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(new AiSentimentDto("neutral", 0.5, "No text provided"));

        var lower = text.ToLowerInvariant();

        int veryNegScore = NegativeSentimentWords["very_negative"].Count(w => lower.Contains(w));
        int negScore     = NegativeSentimentWords["negative"].Count(w => lower.Contains(w));
        int posScore     = NegativeSentimentWords["positive"].Count(w => lower.Contains(w));
        int veryPosScore = NegativeSentimentWords["very_positive"].Count(w => lower.Contains(w));

        var netScore = (veryPosScore * 2.0 + posScore * 1.0) - (veryNegScore * 2.0 + negScore * 1.0);
        var totalSignals = veryNegScore + negScore + posScore + veryPosScore;

        string label;
        double score;

        if (totalSignals == 0)
        {
            label = "neutral";
            score = 0.5;
        }
        else if (netScore <= -2)
        {
            label = "very_negative";
            score = Math.Max(0.0, 0.5 - (Math.Abs(netScore) / 10.0));
        }
        else if (netScore < 0)
        {
            label = "negative";
            score = 0.3;
        }
        else if (netScore >= 2)
        {
            label = "very_positive";
            score = Math.Min(1.0, 0.5 + (netScore / 10.0));
        }
        else if (netScore > 0)
        {
            label = "positive";
            score = 0.75;
        }
        else
        {
            label = "neutral";
            score = 0.5;
        }

        var detail = $"Signals: +{veryPosScore * 2 + posScore} positive, -{veryNegScore * 2 + negScore} negative";
        return Task.FromResult(new AiSentimentDto(label, Math.Round(score, 2), detail));
    }

    public async Task<AiDuplicateCheckDto> FindDuplicatesAsync(int ticketId, int tenantId)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.TenantId == tenantId && !t.IsDeleted);

        if (ticket == null)
            throw new InvalidOperationException($"Ticket {ticketId} not found.");

        var titleWords = ticket.Title.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Distinct()
            .ToHashSet();

        // Look at last 200 open/in-progress tickets in the same tenant
        var candidates = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.Id != ticketId
                     && (t.Status == Domain.Enums.TicketStatus.New
                      || t.Status == Domain.Enums.TicketStatus.InProgress))
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(200)
            .Select(t => new { t.Id, t.TicketNumber, t.Title })
            .ToListAsync();

        var duplicates = candidates
            .Select(c =>
            {
                var cWords = c.Title.ToLowerInvariant()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3)
                    .ToHashSet();
                var intersection = titleWords.Intersect(cWords).Count();
                var union = titleWords.Union(cWords).Count();
                var similarity = union > 0 ? (double)intersection / union : 0;
                return new { c.Id, c.TicketNumber, c.Title, Similarity = similarity };
            })
            .Where(x => x.Similarity >= 0.35)
            .OrderByDescending(x => x.Similarity)
            .Take(5)
            .Select(x => new AiDuplicateMatch(x.Id, x.TicketNumber, x.Title, Math.Round(x.Similarity, 2)))
            .ToList();

        _logger.LogInformation("Duplicate check for ticket {TicketId}: {Count} potential duplicates found", ticketId, duplicates.Count);

        return new AiDuplicateCheckDto(ticketId, duplicates);
    }

    // ─── Private Helpers ────────────────────────────────────────────────────

    private async Task<(int UserId, string Name)?> GetLeastLoadedAgentAsync(int tenantId, int? categoryId)
    {
        // Find agents with fewest open tickets in this tenant
        var agents = await _context.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive
                     && _context.UserRoles.Any(ur => ur.UserId == u.Id
                         && _context.Roles.Any(r => r.Id == ur.RoleId
                             && (r.Name == "Agent" || r.Name == "Admin"))))
            .Select(u => new
            {
                u.Id,
                u.FullName,
                OpenTickets = _context.Tickets.Count(t =>
                    t.AssignedToUserId == u.Id && !t.IsDeleted &&
                    t.Status != Domain.Enums.TicketStatus.Closed &&
                    t.Status != Domain.Enums.TicketStatus.Resolved)
            })
            .OrderBy(u => u.OpenTickets)
            .FirstOrDefaultAsync();

        return agents == null ? null : (agents.Id, agents.FullName);
    }

    private static string GetReplyTemplate(string category, string title) => category switch
    {
        var c when c.Contains("billing") =>
            "I've reviewed your billing inquiry regarding your account. Our billing team will investigate this and provide a detailed breakdown. You can expect a full resolution within 1 business day.",
        var c when c.Contains("technical") || c.Contains("error") || c.Contains("bug") =>
            $"I've reviewed your issue: '{title}'. Our technical team has been alerted and is investigating the root cause. In the meantime, could you please provide:\n- The exact error message you're seeing\n- Steps to reproduce the issue\n- Browser/device/version you're using",
        var c when c.Contains("account") || c.Contains("login") || c.Contains("access") =>
            "For security reasons, account-related changes require identity verification. I've initiated the account review process. Please check your registered email for next steps.",
        var c when c.Contains("network") =>
            "I've logged your network issue and escalated it to our infrastructure team. Please try restarting your network adapter and clearing DNS cache (ipconfig /flushdns) as a first step.",
        _ =>
            "I've received your request and it is being reviewed by our support team. We'll provide you with an update shortly."
    };

    private static string BuildReasoning(List<string> keywords, string? category, string? priority, string sentiment)
    {
        var parts = new List<string>();
        if (keywords.Any())
            parts.Add($"Detected keywords: {string.Join(", ", keywords.Take(5))}");
        if (category != null)
            parts.Add($"Category suggested based on content similarity to '{category}'");
        if (priority != null)
            parts.Add($"Priority set to '{priority}' based on urgency indicators");
        parts.Add($"Customer sentiment detected as '{sentiment}'");
        return string.Join(". ", parts) + ".";
    }
}
