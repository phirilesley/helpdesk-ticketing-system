using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiTriageService _aiService;

    public AiController(IAiTriageService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// Analyze a ticket and suggest category, priority, assignee, and sentiment.
    /// Matches Zendesk AI triage and Freshdesk Freddy Intelligence.
    /// </summary>
    [HttpPost("tickets/{ticketId}/triage")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> TriageTicket(int ticketId)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var result = await _aiService.TriageTicketAsync(ticketId, tenantId ?? 0);
        return Ok(result);
    }

    /// <summary>
    /// Generate a suggested reply for a ticket based on its content and category.
    /// </summary>
    [HttpPost("tickets/{ticketId}/suggest-reply")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> SuggestReply(int ticketId)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var result = await _aiService.SuggestReplyAsync(ticketId, tenantId ?? 0);
        return Ok(result);
    }

    /// <summary>
    /// Check for potential duplicate tickets using Jaccard similarity on titles.
    /// </summary>
    [HttpGet("tickets/{ticketId}/duplicates")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> FindDuplicates(int ticketId)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var result = await _aiService.FindDuplicatesAsync(ticketId, tenantId ?? 0);
        return Ok(result);
    }

    /// <summary>
    /// Suggest knowledge base articles for given text (used in widget).
    /// </summary>
    [HttpPost("kb/suggest")]
    public async Task<IActionResult> SuggestArticles([FromBody] AiKbSuggestRequest request)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var result = await _aiService.SuggestKnowledgeArticlesAsync(request.Title, request.Description, tenantId ?? 0);
        return Ok(result);
    }

    /// <summary>
    /// Analyze sentiment of any text snippet — useful in live chat integrations.
    /// </summary>
    [HttpPost("sentiment")]
    public async Task<IActionResult> AnalyzeSentiment([FromBody] AiSentimentRequest request)
    {
        var result = await _aiService.AnalyzeSentimentAsync(request.Text);
        return Ok(result);
    }
}

public record AiKbSuggestRequest(string Title, string Description);
public record AiSentimentRequest(string Text);
