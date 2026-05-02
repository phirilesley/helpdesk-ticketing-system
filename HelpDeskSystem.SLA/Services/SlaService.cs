using HelpDeskSystem.Application.DTOs.Sla;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HelpDeskSystem.SLA.Services;

public class SlaService : ISlaService
{
    private readonly HelpDeskDbContext _context;
    private readonly ITicketService _ticketService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IBusinessTimeService _businessTimeService;

    public SlaService(
        HelpDeskDbContext context,
        ITicketService ticketService,
        INotificationService notificationService,
        IAuditService auditService,
        IBusinessTimeService businessTimeService)
    {
        _context = context;
        _ticketService = ticketService;
        _notificationService = notificationService;
        _auditService = auditService;
        _businessTimeService = businessTimeService;
    }

    public async Task<SlaResult> CalculateSlaForTicketAsync(int ticketId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Priority)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null) return new SlaResult { IsBreached = false };

        var slaRule = await _context.TicketSlaRules
            .FirstOrDefaultAsync(r => r.PriorityId == ticket.PriorityId && r.CategoryId == ticket.CategoryId);

        if (slaRule == null) return new SlaResult { IsBreached = false };

        var now = DateTime.UtcNow;
        var responseDeadline = await _businessTimeService.AddBusinessMinutesAsync(ticket.TenantId, ticket.CreatedAtUtc, slaRule.ResponseTimeMinutes);
        var resolutionDeadline = await _businessTimeService.AddBusinessMinutesAsync(ticket.TenantId, ticket.CreatedAtUtc, slaRule.ResolutionTimeMinutes);

        var pausedMinutes = ticket.SlaPausedTotalMinutes;
        if (ticket.IsSlaPaused && ticket.SlaPausedAtUtc.HasValue)
        {
            pausedMinutes += (int)Math.Max(0, Math.Floor((now - ticket.SlaPausedAtUtc.Value).TotalMinutes));
        }
        if (pausedMinutes > 0)
        {
            responseDeadline = responseDeadline.AddMinutes(pausedMinutes);
            resolutionDeadline = resolutionDeadline.AddMinutes(pausedMinutes);
        }

        var isResponseBreached = !ticket.IsSlaPaused && now > responseDeadline && ticket.Status == TicketStatus.New;
        var isResolutionBreached = !ticket.IsSlaPaused && now > resolutionDeadline && ticket.Status != TicketStatus.Closed;

        return new SlaResult
        {
            TicketId = ticketId,
            ResponseTimeMinutes = slaRule.ResponseTimeMinutes,
            ResolutionTimeMinutes = slaRule.ResolutionTimeMinutes,
            ResponseDeadline = responseDeadline,
            ResolutionDeadline = resolutionDeadline,
            IsResponseBreached = isResponseBreached,
            IsResolutionBreached = isResolutionBreached,
            IsBreached = isResponseBreached || isResolutionBreached
        };
    }

    public async Task CheckAndHandleSlaBreachesAsync()
    {
        var activeTickets = await _context.Tickets
            .Where(t => t.Status != TicketStatus.Closed && !t.IsDeleted)
            .ToListAsync();

        foreach (var ticket in activeTickets)
        {
            var slaResult = await CalculateSlaForTicketAsync(ticket.Id);
            if (slaResult.IsBreached)
            {
                await HandleSlaBreachAsync(ticket.Id, slaResult);
            }
        }
    }

    public async Task CheckEscalationPoliciesAsync()
    {
        var now = DateTime.UtcNow;

        var rules = await _context.EscalationRules
            .Where(r => r.IsActive && !r.IsDeleted)
            .ToListAsync();

        if (rules.Count == 0)
            return;

        var activeTickets = await _context.Tickets
            .Where(t => !t.IsDeleted && t.Status != TicketStatus.Closed)
            .ToListAsync();

        foreach (var ticket in activeTickets)
        {
            var rule = rules.FirstOrDefault(r => r.PriorityId == ticket.PriorityId);
            if (rule == null)
                continue;

            var ageMinutes = (now - ticket.CreatedAtUtc).TotalMinutes;
            if (ageMinutes < rule.EscalateAfterMinutes)
                continue;

            var oldStatus = ticket.Status;
            var escalatedByPolicy = false;
            if (ticket.Status != TicketStatus.Escalated)
            {
                ticket.Status = TicketStatus.Escalated;
                ticket.UpdatedAtUtc = now;
                escalatedByPolicy = true;

                _context.TicketStatusHistories.Add(new TicketStatusHistory
                {
                    TicketId = ticket.Id,
                    OldStatus = oldStatus,
                    NewStatus = TicketStatus.Escalated,
                    ChangedByUserId = 0,
                    Comment = $"Escalation policy: {rule.EscalateToRole} after {rule.EscalateAfterMinutes} minutes"
                });
            }

            var userIds = await _context.UserRoles
                .Where(ur => ur.Role != null && ur.Role.Name == rule.EscalateToRole)
                .Where(ur => ur.User != null && ur.User.IsActive && ur.User.TenantId == ticket.TenantId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIds)
            {
                await _notificationService.NotifyAsync(
                    userId,
                    "Ticket Escalation Alert",
                    $"Ticket {ticket.TicketNumber} met escalation policy for role {rule.EscalateToRole}.",
                    NotificationType.Warning);
            }

            if (escalatedByPolicy)
            {
                await _auditService.LogAsync(
                    null,
                    "ESCALATION_POLICY_TRIGGERED",
                    "Ticket",
                    ticket.Id.ToString(),
                    oldValues: JsonSerializer.Serialize(new { Status = oldStatus.ToString() }),
                    newValues: JsonSerializer.Serialize(new
                    {
                        Status = TicketStatus.Escalated.ToString(),
                        RuleId = rule.Id,
                        RuleRole = rule.EscalateToRole,
                        RuleAfterMinutes = rule.EscalateAfterMinutes,
                        NotifiedUsers = userIds
                    }),
                    cancellationToken: CancellationToken.None);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task HandleSlaBreachAsync(int ticketId, SlaResult slaResult)
    {
        // Escalate ticket
        await _ticketService.ChangeTicketStatusAsync(ticketId, TicketStatus.Escalated, 0, "SLA Breached");

        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket?.AssignedToUserId is int assignedUserId && assignedUserId > 0)
        {
            await _notificationService.NotifyAsync(
                assignedUserId,
                "SLA Breach Alert",
                $"Ticket {ticket.TicketNumber} has breached SLA.",
                NotificationType.Error);
        }

        if (ticket is not null)
        {
            await _notificationService.NotifyAsync(
                ticket.CreatedByUserId,
                "SLA Breach Alert",
                $"Ticket {ticket.TicketNumber} has breached SLA.",
                NotificationType.Error);
        }

        var breachLog = new SlaBreachLog
        {
            TicketId = ticketId,
            BreachType = slaResult.IsResponseBreached ? "Response" : "Resolution",
            BreachedAtUtc = DateTime.UtcNow
        };

        _context.SlaBreachLogs.Add(breachLog);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            null,
            "SLA_BREACH_HANDLED",
            "Ticket",
            ticketId.ToString(),
            newValues: JsonSerializer.Serialize(new
            {
                BreachType = breachLog.BreachType,
                IsResponseBreached = slaResult.IsResponseBreached,
                IsResolutionBreached = slaResult.IsResolutionBreached
            }),
            cancellationToken: CancellationToken.None);
    }
}
