using HelpDeskSystem.Application.DTOs.Tickets;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using HelpDeskSystem.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Application.Services;

public class TicketService : ITicketService
{
    private readonly HelpDeskDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAutomationRuleService _automationRuleService;
    private readonly IBusinessTimeService _businessTimeService;
    private readonly IAuditService _auditService;

    public TicketService(
        HelpDeskDbContext context,
        INotificationService notificationService,
        IAutomationRuleService automationRuleService,
        IBusinessTimeService businessTimeService,
        IAuditService auditService)
    {
        _context = context;
        _notificationService = notificationService;
        _automationRuleService = automationRuleService;
        _businessTimeService = businessTimeService;
        _auditService = auditService;
    }

    public async Task<TicketDto> CreateTicketAsync(CreateTicketDto dto)
    {
        var creator = await _context.Users
            .Where(u => u.Id == dto.CreatedByUserId && u.IsActive)
            .Select(u => new { u.Id, u.TenantId })
            .FirstOrDefaultAsync();

        if (creator == null)
        {
            throw new InvalidOperationException("Creator user not found or inactive.");
        }
        if (!creator.TenantId.HasValue)
        {
            throw new InvalidOperationException("Creator user is not assigned to a tenant.");
        }

        var ticket = new Ticket
        {
            TicketNumber = TicketNumberGenerator.Generate(),
            Title = dto.Title,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            PriorityId = dto.PriorityId,
            CreatedByUserId = dto.CreatedByUserId,
            TenantId = creator.TenantId.Value,
            DueAtUtc = dto.DueAtUtc
        };

        if (!ticket.DueAtUtc.HasValue)
        {
            var slaRule = await _context.TicketSlaRules
                .AsNoTracking()
                .Where(r => r.CategoryId == ticket.CategoryId && r.PriorityId == ticket.PriorityId && r.IsActive && !r.IsDeleted)
                .FirstOrDefaultAsync();

            if (slaRule != null)
            {
                ticket.DueAtUtc = await _businessTimeService.AddBusinessMinutesAsync(
                    ticket.TenantId,
                    DateTime.UtcNow,
                    slaRule.ResolutionTimeMinutes);
            }
        }

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(
            ticket.CreatedByUserId,
            "Ticket Created",
            $"Ticket {ticket.TicketNumber} has been created.",
            NotificationType.Info);

        await _automationRuleService.ApplyRulesAsync(
            ticket.Id,
            AutomationTriggerType.TicketCreated,
            ticket.CreatedByUserId);

        await _auditService.LogAsync(
            ticket.CreatedByUserId,
            "PORTAL_TICKET_CREATED",
            "Ticket",
            ticket.Id.ToString(),
            newValues: $"{{\"ticketNumber\":\"{ticket.TicketNumber}\",\"tenantId\":{ticket.TenantId}}}");

        return await MapToDtoAsync(ticket);
    }

    public async Task<TicketDto?> GetTicketByIdAsync(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        return ticket == null ? null : await MapToDtoAsync(ticket);
    }

    public async Task<IEnumerable<TicketDto>> GetAllTicketsAsync()
    {
        var tickets = await _context.Tickets
            .Where(t => !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .ToListAsync();

        var dtos = new List<TicketDto>();
        foreach (var ticket in tickets)
        {
            dtos.Add(await MapToDtoAsync(ticket));
        }
        return dtos;
    }

    public async Task<IEnumerable<TicketDto>> GetTicketsForCreatorAsync(int tenantId, int creatorUserId)
    {
        var tickets = await _context.Tickets
            .Where(t => !t.IsDeleted && t.TenantId == tenantId && t.CreatedByUserId == creatorUserId)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();

        var dtos = new List<TicketDto>();
        foreach (var ticket in tickets)
        {
            dtos.Add(await MapToDtoAsync(ticket));
        }
        return dtos;
    }

    public async Task UpdateTicketAsync(int id, UpdateTicketDto dto)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null || ticket.IsDeleted) return;

        ticket.Title = dto.Title;
        ticket.Description = dto.Description;
        ticket.CategoryId = dto.CategoryId;
        ticket.PriorityId = dto.PriorityId;
        ticket.DueAtUtc = dto.DueAtUtc;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteTicketAsync(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null) return;

        ticket.IsDeleted = true;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task AssignTicketAsync(int ticketId, int userId, string reason)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null || ticket.IsDeleted) return;

        var assignee = await _context.Users
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new { u.Id, u.TenantId })
            .FirstOrDefaultAsync();

        if (assignee == null) return;
        if (!assignee.TenantId.HasValue) return;
        if (ticket.TenantId != assignee.TenantId.Value) return;

        ticket.AssignedToUserId = userId;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        var assignment = new TicketAssignment
        {
            TicketId = ticketId,
            AssignedToUserId = userId,
            AssignedByUserId = 0, // TODO: get current user
            Reason = reason
        };

        _context.TicketAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(
            userId,
            "Ticket Assigned",
            $"Ticket {ticket.TicketNumber} was assigned to you.",
            NotificationType.Info);
    }

    public async Task ChangeTicketStatusAsync(int ticketId, TicketStatus status, int userId, string comment)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null || ticket.IsDeleted) return;

        var oldStatus = ticket.Status;
        ticket.Status = status;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        if (status == TicketStatus.Closed)
        {
            ticket.ClosedAtUtc = DateTime.UtcNow;
        }

        var history = new TicketStatusHistory
        {
            TicketId = ticketId,
            OldStatus = oldStatus,
            NewStatus = status,
            ChangedByUserId = userId,
            Comment = comment
        };

        _context.TicketStatusHistories.Add(history);
        await _context.SaveChangesAsync();

        await _notificationService.NotifyAsync(
            ticket.CreatedByUserId,
            "Ticket Status Updated",
            $"Ticket {ticket.TicketNumber} changed from {oldStatus} to {status}.",
            NotificationType.Info);

        if (ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId.Value != ticket.CreatedByUserId)
        {
            await _notificationService.NotifyAsync(
                ticket.AssignedToUserId.Value,
                "Ticket Status Updated",
                $"Ticket {ticket.TicketNumber} changed from {oldStatus} to {status}.",
                NotificationType.Info);
        }

        await _automationRuleService.ApplyRulesAsync(
            ticketId,
            AutomationTriggerType.TicketStatusChanged,
            userId);
    }

    public async Task PauseSlaAsync(int ticketId, int userId, string reason)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && !t.IsDeleted);
        if (ticket == null || ticket.IsSlaPaused)
            return;

        ticket.IsSlaPaused = true;
        ticket.SlaPausedAtUtc = DateTime.UtcNow;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            userId,
            "SLA_PAUSED",
            "Ticket",
            ticketId.ToString(),
            newValues: $"{{\"reason\":\"{reason}\"}}");
    }

    public async Task ResumeSlaAsync(int ticketId, int userId, string reason)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && !t.IsDeleted);
        if (ticket == null || !ticket.IsSlaPaused || !ticket.SlaPausedAtUtc.HasValue)
            return;

        var pausedMinutes = (int)Math.Max(0, Math.Floor((DateTime.UtcNow - ticket.SlaPausedAtUtc.Value).TotalMinutes));
        ticket.SlaPausedTotalMinutes += pausedMinutes;
        ticket.IsSlaPaused = false;
        ticket.SlaPausedAtUtc = null;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            userId,
            "SLA_RESUMED",
            "Ticket",
            ticketId.ToString(),
            newValues: $"{{\"reason\":\"{reason}\",\"pausedMinutes\":{pausedMinutes}}}");
    }

    private async Task<TicketDto> MapToDtoAsync(Ticket ticket)
    {
        return new TicketDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = ticket.Category?.Name ?? string.Empty,
            PriorityId = ticket.PriorityId,
            PriorityName = ticket.Priority?.Name ?? string.Empty,
            Status = ticket.Status,
            CreatedByUserId = ticket.CreatedByUserId,
            AssignedToUserId = ticket.AssignedToUserId,
            CreatedAtUtc = ticket.CreatedAtUtc,
            UpdatedAtUtc = ticket.UpdatedAtUtc,
            ClosedAtUtc = ticket.ClosedAtUtc,
            DueAtUtc = ticket.DueAtUtc,
            IsSlaPaused = ticket.IsSlaPaused,
            TenantId = ticket.TenantId,
            IsDeleted = ticket.IsDeleted
        };
    }
}
