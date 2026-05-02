using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HelpDeskSystem.Application.Services;

public class AutomationRuleService : IAutomationRuleService
{
    private readonly HelpDeskDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;

    public AutomationRuleService(
        HelpDeskDbContext context,
        INotificationService notificationService,
        IAuditService auditService)
    {
        _context = context;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    public async Task ApplyRulesAsync(int ticketId, AutomationTriggerType trigger, int actorUserId, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && !t.IsDeleted, cancellationToken);
        if (ticket == null)
            return;

        var rules = await _context.AutomationRules
            .Where(r => r.IsActive && r.TriggerType == trigger)
            .Where(r => !r.TenantId.HasValue || r.TenantId.Value == ticket.TenantId)
            .Where(r => !r.ConditionCategoryId.HasValue || r.ConditionCategoryId.Value == ticket.CategoryId)
            .Where(r => !r.ConditionPriorityId.HasValue || r.ConditionPriorityId.Value == ticket.PriorityId)
            .Where(r => !r.ConditionStatus.HasValue || r.ConditionStatus.Value == ticket.Status)
            .OrderBy(r => r.ExecutionOrder)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            switch (rule.ActionType)
            {
                case AutomationActionType.AssignRole:
                    await ApplyAssignRoleAsync(ticket, rule, actorUserId, cancellationToken);
                    break;
                case AutomationActionType.NotifyRole:
                    await ApplyNotifyRoleAsync(ticket, rule, actorUserId, cancellationToken);
                    break;
                case AutomationActionType.SetPriority:
                    await ApplySetPriorityAsync(ticket, rule, actorUserId, cancellationToken);
                    break;
                case AutomationActionType.SetStatus:
                    await ApplySetStatusAsync(ticket, rule, actorUserId, cancellationToken);
                    break;
                case AutomationActionType.SetDueHours:
                    await ApplySetDueHoursAsync(ticket, rule, actorUserId, cancellationToken);
                    break;
            }
        }

        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAssignRoleAsync(Ticket ticket, AutomationRule rule, int actorUserId, CancellationToken cancellationToken)
    {
        var roleName = rule.ActionValue?.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
            return;

        var targetUserId = await _context.UserRoles
            .Where(ur => ur.Role != null && ur.Role.Name == roleName)
            .Where(ur => ur.User != null && ur.User.IsActive && ur.User.TenantId == ticket.TenantId)
            .OrderBy(ur => ur.UserId)
            .Select(ur => (int?)ur.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!targetUserId.HasValue || ticket.AssignedToUserId == targetUserId.Value)
            return;

        var oldAssignedUserId = ticket.AssignedToUserId;
        ticket.AssignedToUserId = targetUserId.Value;
        _context.TicketAssignments.Add(new TicketAssignment
        {
            TicketId = ticket.Id,
            AssignedToUserId = targetUserId.Value,
            AssignedByUserId = actorUserId,
            Reason = $"Automation rule '{rule.Name}'"
        });

        await _notificationService.NotifyAsync(
            targetUserId.Value,
            "Ticket Auto-Assigned",
            $"Ticket {ticket.TicketNumber} was automatically assigned to you by rule '{rule.Name}'.",
            NotificationType.Info,
            cancellationToken);

        await _auditService.LogAsync(
            actorUserId,
            "AUTOMATION_ASSIGN_ROLE_APPLIED",
            "Ticket",
            ticket.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { AssignedToUserId = oldAssignedUserId }),
            newValues: JsonSerializer.Serialize(new
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                AssignedToUserId = targetUserId.Value,
                RoleName = roleName
            }),
            cancellationToken: cancellationToken);
    }

    private async Task ApplyNotifyRoleAsync(Ticket ticket, AutomationRule rule, int actorUserId, CancellationToken cancellationToken)
    {
        var roleName = rule.ActionValue?.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
            return;

        var userIds = await _context.UserRoles
            .Where(ur => ur.Role != null && ur.Role.Name == roleName)
            .Where(ur => ur.User != null && ur.User.IsActive && ur.User.TenantId == ticket.TenantId)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await _notificationService.NotifyAsync(
                userId,
                "Automation Rule Notification",
                $"Rule '{rule.Name}' triggered for ticket {ticket.TicketNumber}.",
                NotificationType.Info,
                cancellationToken);
        }

        if (userIds.Count == 0)
            return;

        await _auditService.LogAsync(
            actorUserId,
            "AUTOMATION_NOTIFY_ROLE_APPLIED",
            "Ticket",
            ticket.Id.ToString(),
            newValues: JsonSerializer.Serialize(new
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                RoleName = roleName,
                NotifiedUsers = userIds
            }),
            cancellationToken: cancellationToken);
    }

    private async Task ApplySetPriorityAsync(Ticket ticket, AutomationRule rule, int actorUserId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(rule.ActionValue, out var newPriorityId))
            return;

        var exists = await _context.TicketPriorities.AnyAsync(p => p.Id == newPriorityId, cancellationToken);
        if (!exists || ticket.PriorityId == newPriorityId)
            return;

        var oldPriorityId = ticket.PriorityId;
        ticket.PriorityId = newPriorityId;

        await _auditService.LogAsync(
            actorUserId,
            "AUTOMATION_SET_PRIORITY_APPLIED",
            "Ticket",
            ticket.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { PriorityId = oldPriorityId }),
            newValues: JsonSerializer.Serialize(new
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                PriorityId = newPriorityId
            }),
            cancellationToken: cancellationToken);
    }

    private async Task ApplySetStatusAsync(Ticket ticket, AutomationRule rule, int actorUserId, CancellationToken cancellationToken)
    {
        if (!TryParseStatus(rule.ActionValue, out var newStatus))
            return;

        if (ticket.Status == newStatus)
            return;

        var oldStatus = ticket.Status;
        ticket.Status = newStatus;
        if (newStatus == TicketStatus.Closed)
        {
            ticket.ClosedAtUtc = DateTime.UtcNow;
        }

        _context.TicketStatusHistories.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = actorUserId,
            Comment = $"Automation rule '{rule.Name}'"
        });

        await _notificationService.NotifyAsync(
            ticket.CreatedByUserId,
            "Ticket Status Updated",
            $"Ticket {ticket.TicketNumber} status changed to {newStatus} by rule '{rule.Name}'.",
            NotificationType.Info,
            cancellationToken);

        await _auditService.LogAsync(
            actorUserId,
            "AUTOMATION_SET_STATUS_APPLIED",
            "Ticket",
            ticket.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { Status = oldStatus.ToString() }),
            newValues: JsonSerializer.Serialize(new
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Status = newStatus.ToString()
            }),
            cancellationToken: cancellationToken);
    }

    private async Task ApplySetDueHoursAsync(Ticket ticket, AutomationRule rule, int actorUserId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(rule.ActionValue, out var dueHours))
            return;

        if (dueHours <= 0)
            return;

        var oldDueAt = ticket.DueAtUtc;
        ticket.DueAtUtc = DateTime.UtcNow.AddHours(dueHours);

        await _auditService.LogAsync(
            actorUserId,
            "AUTOMATION_SET_DUE_HOURS_APPLIED",
            "Ticket",
            ticket.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { DueAtUtc = oldDueAt }),
            newValues: JsonSerializer.Serialize(new
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                DueAtUtc = ticket.DueAtUtc
            }),
            cancellationToken: cancellationToken);
    }

    private static bool TryParseStatus(string raw, out TicketStatus status)
    {
        if (int.TryParse(raw, out var numeric) && Enum.IsDefined(typeof(TicketStatus), numeric))
        {
            status = (TicketStatus)numeric;
            return true;
        }

        return Enum.TryParse(raw, true, out status);
    }
}
