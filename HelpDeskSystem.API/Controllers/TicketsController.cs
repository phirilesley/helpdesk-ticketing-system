using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.DTOs.Messages;
using HelpDeskSystem.Application.DTOs.Tickets;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ITicketMessageService _messageService;
    private readonly HelpDeskDbContext _context;

    public TicketsController(ITicketService ticketService, ITicketMessageService messageService, HelpDeskDbContext context)
    {
        _ticketService = ticketService;
        _messageService = messageService;
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> CreateTicket(CreateTicketDto dto)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();
        if (!IsSuperAdmin() && !User.GetTenantId().HasValue)
            return Forbid();

        dto.CreatedByUserId = userId.Value;
        var result = await _ticketService.CreateTicketAsync(dto);

        if (!CanAccessTicket(result))
            return Forbid();

        return CreatedAtAction(nameof(GetTicket), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        return ticket;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets()
    {
        var tickets = await _ticketService.GetAllTicketsAsync();
        if (IsSuperAdmin())
            return Ok(tickets);

        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        return Ok(tickets.Where(t => t.TenantId == tenantId.Value));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(int id, UpdateTicketDto dto)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        await _ticketService.UpdateTicketAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        await _ticketService.DeleteTicketAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketRequest request)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        await _ticketService.AssignTicketAsync(id, request.UserId, request.Reason);
        return NoContent();
    }

    [HttpPost("{id}/status")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        await _ticketService.ChangeTicketStatusAsync(id, request.Status, userId.Value, request.Comment);
        return NoContent();
    }

    [HttpGet("kanban")]
    public async Task<ActionResult<Dictionary<string, IEnumerable<TicketDto>>>> GetKanban()
    {
        var tickets = await _ticketService.GetAllTicketsAsync();

        if (!IsSuperAdmin())
        {
            var tenantId = User.GetTenantId();
            if (!tenantId.HasValue)
                return Forbid();

            tickets = tickets.Where(t => t.TenantId == tenantId.Value);
        }

        var grouped = tickets
            .GroupBy(t => t.Status.ToString())
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        return Ok(grouped);
    }

    [HttpGet("metadata/categories")]
    public async Task<ActionResult<IEnumerable<TicketLookupDto>>> GetCategories()
    {
        var categories = await _context.TicketCategories
            .AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new TicketLookupDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("metadata/priorities")]
    public async Task<ActionResult<IEnumerable<TicketLookupDto>>> GetPriorities()
    {
        var priorities = await _context.TicketPriorities
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Level)
            .Select(p => new TicketLookupDto
            {
                Id = p.Id,
                Name = p.Name
            })
            .ToListAsync();

        return Ok(priorities);
    }

    [HttpPost("{id}/sla/pause")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> PauseSla(int id, [FromBody] SlaPauseResumeRequest request)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        await _ticketService.PauseSlaAsync(id, userId.Value, request.Reason);
        return NoContent();
    }

    [HttpPost("{id}/sla/resume")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> ResumeSla(int id, [FromBody] SlaPauseResumeRequest request)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        await _ticketService.ResumeSlaAsync(id, userId.Value, request.Reason);
        return NoContent();
    }

    [HttpPost("{id}/messages")]
    public async Task<ActionResult<TicketMessageDto>> SendMessage(int id, CreateTicketMessageDto dto)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        if (dto.IsInternalNote && !User.IsInRole("Agent") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        dto.TicketId = id;
        dto.SenderUserId = userId.Value;
        var result = await _messageService.SendMessageAsync(dto);
        return CreatedAtAction(nameof(GetMessages), new { id }, result);
    }

    [HttpGet("{id}/messages")]
    public async Task<ActionResult<IEnumerable<TicketMessageDto>>> GetMessages(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null)
            return NotFound();

        if (!CanAccessTicket(ticket))
            return Forbid();

        var messages = await _messageService.GetMessagesAsync(id);
        return Ok(messages);
    }

    [HttpPost("{id}/comments")]
    public Task<ActionResult<TicketMessageDto>> AddComment(int id, [FromBody] AddTicketCommentRequest request)
    {
        return SendMessage(id, new CreateTicketMessageDto
        {
            Message = request.Content,
            MessageType = TicketMessageType.Message,
            IsInternalNote = request.IsInternal
        });
    }

    [HttpGet("{id}/comments")]
    public Task<ActionResult<IEnumerable<TicketMessageDto>>> GetComments(int id)
    {
        return GetMessages(id);
    }

    private bool CanAccessTicket(TicketDto ticket)
    {
        if (IsSuperAdmin())
            return true;

        var tenantId = User.GetTenantId();
        return tenantId.HasValue && ticket.TenantId == tenantId.Value;
    }

    private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");
}

public class AssignTicketRequest
{
    public int UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ChangeStatusRequest
{
    public TicketStatus Status { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class SlaPauseResumeRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class AddTicketCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public class TicketLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
