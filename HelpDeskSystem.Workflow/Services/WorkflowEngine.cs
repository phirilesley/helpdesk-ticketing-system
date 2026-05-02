using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using HelpDeskSystem.Workflow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HelpDeskSystem.Workflow.Services;

public interface IWorkflowEngine
{
    Task TriggerWorkflowsAsync(WorkflowTriggerType triggerType, int ticketId, int userId, object? triggerData = null);
    Task ExecuteWorkflowAsync(int workflowExecutionId);
    Task<IEnumerable<WorkflowRule>> GetActiveRulesAsync(int tenantId);
    Task<bool> CreateWorkflowRuleAsync(WorkflowRule rule);
    Task<bool> UpdateWorkflowRuleAsync(WorkflowRule rule);
    Task<bool> DeleteWorkflowRuleAsync(int ruleId, int tenantId);
}

public class WorkflowEngine : IWorkflowEngine
{
    private readonly HelpDeskDbContext _context;
    private readonly ITicketService _ticketService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        HelpDeskDbContext context,
        ITicketService ticketService,
        INotificationService notificationService,
        IAuditService auditService,
        ILogger<WorkflowEngine> logger)
    {
        _context = context;
        _ticketService = ticketService;
        _notificationService = notificationService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task TriggerWorkflowsAsync(WorkflowTriggerType triggerType, int ticketId, int userId, object? triggerData = null)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return;

        var activeRules = await GetActiveRulesAsync(ticket.TenantId);
        var matchingRules = activeRules
            .Where(r => r.TriggerType == triggerType)
            .Where(r => EvaluateConditions(r, ticket, triggerData))
            .OrderByDescending(r => r.Priority);

        foreach (var rule in matchingRules)
        {
            var execution = new WorkflowExecution
            {
                WorkflowRuleId = rule.Id,
                TicketId = ticketId,
                TriggeredByUserId = userId,
                Status = WorkflowStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _context.WorkflowExecutions.Add(execution);
            await _context.SaveChangesAsync();

            // Execute workflow asynchronously
            _ = Task.Run(() => ExecuteWorkflowAsync(execution.Id));
        }
    }

    public async Task ExecuteWorkflowAsync(int workflowExecutionId)
    {
        var execution = await _context.WorkflowExecutions
            .Include(e => e.WorkflowRule)
            .Include(e => e.Ticket)
            .FirstOrDefaultAsync(e => e.Id == workflowExecutionId);

        if (execution == null)
            return;

        try
        {
            execution.Status = WorkflowStatus.Running;
            execution.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var actions = JsonSerializer.Deserialize<List<WorkflowAction>>(execution.WorkflowRule!.ActionsJson);
            var results = new List<object>();

            foreach (var action in actions!)
            {
                var result = await ExecuteActionAsync(action, execution.TicketId, execution.TriggeredByUserId);
                results.Add(result);
            }

            execution.Status = WorkflowStatus.Completed;
            execution.ExecutedAtUtc = DateTime.UtcNow;
            execution.ExecutionResultJson = JsonSerializer.Serialize(results);
            execution.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                execution.TriggeredByUserId,
                "WORKFLOW_EXECUTED",
                "WorkflowRule",
                execution.WorkflowRuleId.ToString(),
                newValues: JsonSerializer.Serialize(new
                {
                    RuleName = execution.WorkflowRule.Name,
                    TicketId = execution.TicketId,
                    Actions = actions.Select(a => a.Type),
                    ExecutionTime = execution.ExecutedAtUtc
                }),
                cancellationToken: CancellationToken.None);

            _logger.LogInformation("Workflow {RuleName} executed successfully for ticket {TicketId}", 
                execution.WorkflowRule.Name, execution.TicketId);
        }
        catch (Exception ex)
        {
            execution.Status = WorkflowStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Workflow execution failed for execution {ExecutionId}", workflowExecutionId);
        }
    }

    private async Task<object> ExecuteActionAsync(WorkflowAction action, int ticketId, int userId)
    {
        return action.Type.ToLowerInvariant() switch
        {
            "assign_ticket" => await AssignTicketAction(action, ticketId, userId),
            "change_status" => await ChangeStatusAction(action, ticketId, userId),
            "send_notification" => await SendNotificationAction(action, ticketId, userId),
            "create_task" => await CreateTaskAction(action, ticketId, userId),
            "escalate" => await EscalateAction(action, ticketId, userId),
            "add_tag" => await AddTagAction(action, ticketId, userId),
            "set_priority" => await SetPriorityAction(action, ticketId, userId),
            "delay" => await DelayAction(action),
            _ => throw new NotSupportedException($"Action type '{action.Type}' is not supported")
        };
    }

    private async Task<object> AssignTicketAction(WorkflowAction action, int ticketId, int userId)
    {
        var assignToUserId = action.Parameters.GetValueOrDefault("userId") as int?;
        var reason = action.Parameters.GetValueOrDefault("reason") as string ?? "Auto-assigned by workflow";

        if (assignToUserId.HasValue)
        {
            await _ticketService.AssignTicketAsync(ticketId, assignToUserId.Value, reason);
            return new { Action = "assigned", UserId = assignToUserId.Value, Reason = reason };
        }

        // Auto-assign to least busy agent
        var leastBusyAgent = await GetLeastBusyAgentAsync(ticketId);
        if (leastBusyAgent != null)
        {
            await _ticketService.AssignTicketAsync(ticketId, leastBusyAgent.Id, reason);
            return new { Action = "auto_assigned", UserId = leastBusyAgent.Id, Reason = reason };
        }

        return new { Action = "assign_failed", Reason = "No suitable agent found" };
    }

    private async Task<object> ChangeStatusAction(WorkflowAction action, int ticketId, int userId)
    {
        var statusStr = action.Parameters.GetValueOrDefault("status") as string;
        var comment = action.Parameters.GetValueOrDefault("comment") as string ?? "Status changed by workflow";

        if (Enum.TryParse<TicketStatus>(statusStr, out var status))
        {
            await _ticketService.ChangeTicketStatusAsync(ticketId, status, userId, comment);
            return new { Action = "status_changed", Status = status.ToString(), Comment = comment };
        }

        return new { Action = "status_change_failed", Reason = "Invalid status" };
    }

    private async Task<object> SendNotificationAction(WorkflowAction action, int ticketId, int userId)
    {
        var message = action.Parameters.GetValueOrDefault("message") as string;
        var recipientType = action.Parameters.GetValueOrDefault("recipientType") as string;
        var notificationTypeStr = action.Parameters.GetValueOrDefault("type") as string ?? "Info";

        if (string.IsNullOrEmpty(message))
            return new { Action = "notification_failed", Reason = "Message is required" };

        var notificationType = Enum.TryParse<NotificationType>(notificationTypeStr, out var nt) ? nt : NotificationType.Info;

        switch (recipientType?.ToLowerInvariant())
        {
            case "assignee":
                var ticket = await _context.Tickets.FindAsync(ticketId);
                if (ticket?.AssignedToUserId.HasValue == true)
                {
                    await _notificationService.NotifyAsync(ticket.AssignedToUserId.Value, "Workflow Notification", message, notificationType);
                    return new { Action = "notification_sent", Recipient = "assignee", Message = message };
                }
                break;

            case "creator":
                var creatorTicket = await _context.Tickets.FindAsync(ticketId);
                if (creatorTicket != null)
                {
                    await _notificationService.NotifyAsync(creatorTicket.CreatedByUserId, "Workflow Notification", message, notificationType);
                    return new { Action = "notification_sent", Recipient = "creator", Message = message };
                }
                break;

            case "role":
                var roleName = action.Parameters.GetValueOrDefault("role") as string;
                if (!string.IsNullOrEmpty(roleName))
                {
                    await NotifyRoleAsync(ticketId, roleName, message, notificationType);
                    return new { Action = "notification_sent", Recipient = $"role_{roleName}", Message = message };
                }
                break;
        }

        return new { Action = "notification_failed", Reason = "Invalid recipient type" };
    }

    private async Task<object> CreateTaskAction(WorkflowAction action, int ticketId, int userId)
    {
        var title = action.Parameters.GetValueOrDefault("title") as string;
        var description = action.Parameters.GetValueOrDefault("description") as string;
        var assignToUserId = action.Parameters.GetValueOrDefault("userId") as int?;

        if (string.IsNullOrEmpty(title))
            return new { Action = "task_creation_failed", Reason = "Title is required" };

        // This would integrate with a task management system
        // For now, we'll create a message as a task placeholder
        var taskMessage = $"TASK: {title}\n{description ?? ""}";
        
        return new { Action = "task_created", Title = title, AssignedTo = assignToUserId };
    }

    private async Task<object> EscalateAction(WorkflowAction action, int ticketId, int userId)
    {
        var escalateToRole = action.Parameters.GetValueOrDefault("role") as string;
        var reason = action.Parameters.GetValueOrDefault("reason") as string ?? "Escalated by workflow";

        await _ticketService.ChangeTicketStatusAsync(ticketId, TicketStatus.Escalated, userId, reason);

        if (!string.IsNullOrEmpty(escalateToRole))
        {
            await NotifyRoleAsync(ticketId, escalateToRole, $"Ticket escalated: {reason}", NotificationType.Warning);
        }

        return new { Action = "escalated", Role = escalateToRole, Reason = reason };
    }

    private async Task<object> AddTagAction(WorkflowAction action, int ticketId, int userId)
    {
        var tag = action.Parameters.GetValueOrDefault("tag") as string;
        
        if (string.IsNullOrEmpty(tag))
            return new { Action = "tag_failed", Reason = "Tag is required" };

        // This would integrate with a tagging system
        return new { Action = "tag_added", Tag = tag };
    }

    private async Task<object> SetPriorityAction(WorkflowAction action, int ticketId, int userId)
    {
        if (action.Type == "SetPriority" && action.Parameters.TryGetValue("priorityId", out var priorityObj))
        {
            if (int.TryParse(priorityObj.ToString(), out var priorityId))
            {
                var ticket = await _context.Tickets.FindAsync(ticketId);
                if (ticket != null)
                {
                    ticket.PriorityId = priorityId;
                    await _context.SaveChangesAsync();
                }
            }
        }
        else
        {
            var priorityStr = action.Parameters.GetValueOrDefault("priority") as string;
            
            if (int.TryParse(priorityStr, out var priorityId))
            {
                var ticket = await _context.Tickets.FindAsync(ticketId);
                if (ticket != null)
                {
                    ticket.PriorityId = priorityId;
                    ticket.UpdatedAtUtc = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return new { Action = "priority_set", Priority = priorityId };
                }
            }
        }

        return new { Action = "priority_set_failed", Reason = "Invalid priority" };
    }

    private async Task<object> DelayAction(WorkflowAction action)
    {
        var delaySeconds = action.Parameters.GetValueOrDefault("seconds") as int? ?? 0;
        
        if (delaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        return new { Action = "delayed", Seconds = delaySeconds };
    }

    private bool EvaluateConditions(WorkflowRule rule, Ticket ticket, object? triggerData)
    {
        if (string.IsNullOrEmpty(rule.TriggerConditionJson))
            return true;

        try
        {
            var conditions = JsonSerializer.Deserialize<List<WorkflowCondition>>(rule.TriggerConditionJson);
            if (conditions == null || !conditions.Any())
                return true;

            var ticketData = new Dictionary<string, object>
            {
                ["Status"] = ticket.Status.ToString(),
                ["Priority"] = ticket.PriorityId,
                ["Category"] = ticket.CategoryId,
                ["AssignedTo"] = ticket.AssignedToUserId ?? 0,
                ["CreatedBy"] = ticket.CreatedByUserId,
                ["TenantId"] = ticket.TenantId
            };

            bool result = true;
            string? currentLogicalOperator = null;

            foreach (var condition in conditions)
            {
                var fieldValue = GetFieldValue(condition.Field, ticketData, triggerData);
                var conditionResult = EvaluateCondition(condition.Operator, fieldValue, condition.Value);

                if (currentLogicalOperator == "OR")
                {
                    result = result || conditionResult;
                }
                else // Default is AND
                {
                    result = result && conditionResult;
                }

                currentLogicalOperator = condition.LogicalOperator;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating workflow conditions for rule {RuleId}", rule.Id);
            return false;
        }
    }

    private object? GetFieldValue(string field, Dictionary<string, object> ticketData, object? triggerData)
    {
        if (ticketData.TryGetValue(field, out var value))
            return value;

        // Check trigger data for additional fields
        if (triggerData != null)
        {
            var triggerJson = JsonSerializer.Serialize(triggerData);
            var triggerDict = JsonSerializer.Deserialize<Dictionary<string, object>>(triggerJson);
            if (triggerDict?.TryGetValue(field, out var triggerValue) == true)
                return triggerValue;
        }

        return null;
    }

    private bool EvaluateCondition(string op, object? fieldValue, object conditionValue)
    {
        return op.ToLowerInvariant() switch
        {
            "equals" => Equals(fieldValue, conditionValue),
            "not_equals" => !Equals(fieldValue, conditionValue),
            "contains" => fieldValue?.ToString()?.Contains(conditionValue.ToString() ?? "") == true,
            "not_contains" => fieldValue?.ToString()?.Contains(conditionValue.ToString() ?? "") != true,
            "greater_than" => CompareTo(fieldValue, conditionValue) > 0,
            "less_than" => CompareTo(fieldValue, conditionValue) < 0,
            "greater_equal" => CompareTo(fieldValue, conditionValue) >= 0,
            "less_equal" => CompareTo(fieldValue, conditionValue) <= 0,
            "is_null" => fieldValue == null,
            "is_not_null" => fieldValue != null,
            _ => false
        };
    }

    private int CompareTo(object? left, object? right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        if (left is IComparable lc && right is IComparable rc)
        {
            try
            {
                return lc.CompareTo(Convert.ChangeType(rc, lc.GetType()));
            }
            catch
            {
                return string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        return string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task<User?> GetLeastBusyAgentAsync(int ticketId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return null;

        return await _context.Users
            .Where(u => u.TenantId == ticket.TenantId && u.IsActive)
            .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.Role!.Name == "Agent"))
            .OrderBy(u => _context.Tickets.Count(t => t.AssignedToUserId == u.Id && t.Status != TicketStatus.Closed))
            .FirstOrDefaultAsync();
    }

    private async Task NotifyRoleAsync(int ticketId, string roleName, string message, NotificationType notificationType)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null) return;

        var userIds = await _context.UserRoles
            .Where(ur => ur.Role!.Name == roleName && ur.User!.TenantId == ticket.TenantId && ur.User!.IsActive)
            .Select(ur => ur.UserId)
            .ToListAsync();

        foreach (var userId in userIds)
        {
            await _notificationService.NotifyAsync(userId, "Workflow Notification", message, notificationType);
        }
    }

    public async Task<IEnumerable<WorkflowRule>> GetActiveRulesAsync(int tenantId)
    {
        return await _context.WorkflowRules
            .Where(r => r.TenantId == tenantId && r.IsActive && !r.IsDeleted)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
    }

    public async Task<bool> CreateWorkflowRuleAsync(WorkflowRule rule)
    {
        _context.WorkflowRules.Add(rule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateWorkflowRuleAsync(WorkflowRule rule)
    {
        _context.WorkflowRules.Update(rule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkflowRuleAsync(int ruleId, int tenantId)
    {
        var rule = await _context.WorkflowRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.TenantId == tenantId);

        if (rule == null)
            return false;

        rule.IsDeleted = true;
        rule.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
