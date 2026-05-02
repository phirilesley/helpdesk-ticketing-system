using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.DTOs.Messages;
using HelpDeskSystem.Application.DTOs.Tickets;
using HelpDeskSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/customer")]
public class CustomerController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ITicketMessageService _messageService;

    public CustomerController(ITicketService ticketService, ITicketMessageService messageService)
    {
        _ticketService = ticketService;
        _messageService = messageService;
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<TicketDto>> CreateTicket(CreateTicketDto dto)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        dto.CreatedByUserId = userId.Value;
        var result = await _ticketService.CreateTicketAsync(dto);
        return CreatedAtAction(nameof(GetTicket), new { id = result.Id }, result);
    }

    [HttpGet("tickets")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetMyTickets()
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        if (!userId.HasValue || !tenantId.HasValue)
            return Unauthorized();

        var tickets = await _ticketService.GetAllTicketsAsync();
        var visible = tickets.Where(t => t.TenantId == tenantId.Value && t.CreatedByUserId == userId.Value);
        return Ok(visible);
    }

    [HttpGet("tickets/{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(int id)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        if (!userId.HasValue || !tenantId.HasValue)
            return Unauthorized();

        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (ticket.TenantId != tenantId.Value || ticket.CreatedByUserId != userId.Value)
            return Forbid();

        return ticket;
    }

    [HttpPost("tickets/{id}/messages")]
    public async Task<ActionResult<TicketMessageDto>> SendMessage(int id, CreateTicketMessageDto dto)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        if (!userId.HasValue || !tenantId.HasValue)
            return Unauthorized();

        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (ticket.TenantId != tenantId.Value || ticket.CreatedByUserId != userId.Value)
            return Forbid();

        dto.TicketId = id;
        dto.SenderUserId = userId.Value;
        dto.IsInternalNote = false;

        var result = await _messageService.SendMessageAsync(dto);
        return CreatedAtAction(nameof(GetMessages), new { id }, result);
    }

    [HttpGet("tickets/{id}/messages")]
    public async Task<ActionResult<IEnumerable<TicketMessageDto>>> GetMessages(int id)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        if (!userId.HasValue || !tenantId.HasValue)
            return Unauthorized();

        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (ticket.TenantId != tenantId.Value || ticket.CreatedByUserId != userId.Value)
            return Forbid();

        var messages = await _messageService.GetMessagesAsync(id);
        return Ok(messages);
    }
}
