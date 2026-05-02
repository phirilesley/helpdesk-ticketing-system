using HelpDeskSystem.AI.Services;
using HelpDeskSystem.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly ITicketCategorizationService _categorizationService;
    private readonly ILogger<AIController> _logger;

    public AIController(
        ITicketCategorizationService categorizationService,
        ILogger<AIController> logger)
    {
        _categorizationService = categorizationService;
        _logger = logger;
    }

    [HttpPost("categorize")]
    public async Task<ActionResult<TicketCategorizationResult>> CategorizeTicket([FromBody] Application.DTOs.Tickets.CreateTicketDto ticketDto)
    {
        try
        {
            var result = await _categorizationService.CategorizeTicketAsync(ticketDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error categorizing ticket");
            return StatusCode(500, "Error categorizing ticket");
        }
    }

    [HttpPost("suggest-agent/{ticketId}")]
    public async Task<ActionResult<AgentSuggestionResult>> SuggestAgent(int ticketId)
    {
        try
        {
            var result = await _categorizationService.SuggestAgentAsync(ticketId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting agent for ticket {TicketId}", ticketId);
            return StatusCode(500, "Error suggesting agent");
        }
    }

    [HttpPost("suggest-response/{ticketId}")]
    public async Task<ActionResult<ResponseSuggestionResult>> SuggestResponse(int ticketId)
    {
        try
        {
            var result = await _categorizationService.GenerateResponseSuggestionAsync(ticketId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response suggestion for ticket {TicketId}", ticketId);
            return StatusCode(500, "Error generating response suggestion");
        }
    }

    [HttpPost("analyze-sentiment")]
    public async Task<ActionResult<SentimentAnalysisResult>> AnalyzeSentiment([FromBody] string content)
    {
        try
        {
            var result = await _categorizationService.AnalyzeSentimentAsync(content);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            return StatusCode(500, "Error analyzing sentiment");
        }
    }

    [HttpPost("auto-assign/{ticketId}")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<ActionResult<bool>> AutoAssignTicket(int ticketId)
    {
        try
        {
            var result = await _categorizationService.AutoAssignTicketAsync(ticketId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-assigning ticket {TicketId}", ticketId);
            return StatusCode(500, "Error auto-assigning ticket");
        }
    }
}
