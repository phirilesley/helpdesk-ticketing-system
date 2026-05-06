using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

/// <summary>
/// Customer self-service portal — most endpoints are intentionally unauthenticated
/// so end-users can submit and track tickets without creating an account first.
/// </summary>
[ApiController]
[Route("api/portal")]
public class CustomerPortalController : ControllerBase
{
    private readonly ICustomerPortalService _portalService;

    public CustomerPortalController(ICustomerPortalService portalService)
    {
        _portalService = portalService;
    }

    /// <summary>Submit a new support ticket (no login required)</summary>
    [HttpPost("tickets")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitTicket([FromBody] PortalCreateTicketDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RequesterEmail) || string.IsNullOrWhiteSpace(dto.Subject))
            return BadRequest("Email and subject are required.");

        var ticket = await _portalService.SubmitTicketAsync(dto);
        return CreatedAtAction(nameof(GetTicketStatus), new { ticketNumber = ticket.TicketNumber }, ticket);
    }

    /// <summary>Get a ticket by number + requester email (public lookup)</summary>
    [HttpGet("tickets/{ticketNumber}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTicket(string ticketNumber, [FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required.");

        var ticket = await _portalService.GetTicketByNumberAsync(ticketNumber, email);
        if (ticket == null) return NotFound();
        return Ok(ticket);
    }

    /// <summary>Get all tickets for a given email address</summary>
    [HttpGet("tickets")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyTickets([FromQuery] string email, [FromQuery] int tenantId)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("Email is required.");
        var tickets = await _portalService.GetMyTicketsAsync(email, tenantId);
        return Ok(tickets);
    }

    /// <summary>Customer reply to an existing ticket</summary>
    [HttpPost("tickets/{ticketNumber}/messages")]
    [AllowAnonymous]
    public async Task<IActionResult> AddMessage(string ticketNumber, [FromBody] PortalMessageRequest request)
    {
        var result = await _portalService.AddMessageAsync(ticketNumber, request.Email, request.Message);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    /// <summary>Submit a CSAT rating (1–5 stars) after ticket is resolved</summary>
    [HttpPost("tickets/{ticketNumber}/rate")]
    [AllowAnonymous]
    public async Task<IActionResult> RateTicket(string ticketNumber, [FromBody] PortalRateRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest("Rating must be between 1 and 5.");

        var success = await _portalService.RateTicketAsync(ticketNumber, request.Email, request.Rating, request.Comment);
        if (!success) return BadRequest("Unable to submit rating. Ticket may not be resolved or already rated.");
        return Ok(new { message = "Thank you for your feedback!" });
    }

    /// <summary>Public ticket status check (no email required)</summary>
    [HttpGet("tickets/{ticketNumber}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTicketStatus(string ticketNumber)
    {
        var status = await _portalService.GetTicketStatusAsync(ticketNumber);
        return Ok(status);
    }

    /// <summary>Search the knowledge base (public)</summary>
    [HttpGet("kb/search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchKnowledgeBase([FromQuery] string query, [FromQuery] int tenantId)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query is required.");
        var articles = await _portalService.SearchKnowledgeBaseAsync(query, tenantId);
        return Ok(articles);
    }

    /// <summary>Get CSAT summary (agent-authenticated)</summary>
    [HttpGet("csat")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> GetCsatSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var summary = await _portalService.GetCsatSummaryAsync(
            tenantId ?? 0,
            from ?? DateTime.UtcNow.AddDays(-30),
            to ?? DateTime.UtcNow);
        return Ok(summary);
    }
}

public record PortalMessageRequest(string Email, string Message);
public record PortalRateRequest(string Email, int Rating, string? Comment = null);
