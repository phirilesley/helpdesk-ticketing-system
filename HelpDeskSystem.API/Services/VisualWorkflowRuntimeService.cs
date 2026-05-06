using System.Text.Json;
using HelpDeskSystem.Application.DTOs.Messages;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Services;

public interface IVisualWorkflowRuntimeService
{
    Task<VisualWorkflowRunResult> ExecuteAsync(int workflowDefinitionId, int ticketId, int actorUserId, bool dryRun, CancellationToken cancellationToken = default);
}

public class VisualWorkflowRunResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public int TicketId { get; set; }
    public int WorkflowDefinitionId { get; set; }
    public List<VisualWorkflowStepResult> Steps { get; set; } = [];
}

public class VisualWorkflowStepResult
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}

internal class VisualWorkflowGraph
{
    public List<VisualWorkflowNode> Nodes { get; set; } = [];
    public List<VisualWorkflowEdge> Edges { get; set; } = [];
}

internal class VisualWorkflowNode
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionValue { get; set; } = string.Empty;
    public int DelayMinutes { get; set; }
}

internal class VisualWorkflowEdge
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class VisualWorkflowRuntimeService : IVisualWorkflowRuntimeService
{
    private readonly HelpDeskDbContext _context;
    private readonly ITicketService _ticketService;
    private readonly ITicketMessageService _ticketMessageService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<VisualWorkflowRuntimeService> _logger;

    public VisualWorkflowRuntimeService(
        HelpDeskDbContext context,
        ITicketService ticketService,
        ITicketMessageService ticketMessageService,
        INotificationService notificationService,
        ILogger<VisualWorkflowRuntimeService> logger)
    {
        _context = context;
        _ticketService = ticketService;
        _ticketMessageService = ticketMessageService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<VisualWorkflowRunResult> ExecuteAsync(int workflowDefinitionId, int ticketId, int actorUserId, bool dryRun, CancellationToken cancellationToken = default)
    {
        var result = new VisualWorkflowRunResult
        {
            TicketId = ticketId,
            WorkflowDefinitionId = workflowDefinitionId
        };

        var definition = await _context.WorkflowDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == workflowDefinitionId && x.IsPublished && !x.IsDeleted, cancellationToken);
        if (definition == null)
        {
            result.Error = "Workflow definition not found or not published.";
            return result;
        }

        var ticket = await _context.Tickets
            .Include(x => x.Priority)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == ticketId && !x.IsDeleted, cancellationToken);
        if (ticket == null)
        {
            result.Error = "Ticket not found.";
            return result;
        }

        VisualWorkflowGraph graph;
        try
        {
            graph = JsonSerializer.Deserialize<VisualWorkflowGraph>(definition.GraphJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new VisualWorkflowGraph();
        }
        catch (Exception ex)
        {
            result.Error = $"Invalid workflow graph JSON: {ex.Message}";
            return result;
        }

        if (graph.Nodes.Count == 0)
        {
            result.Error = "Workflow graph has no nodes.";
            return result;
        }

        var nodeMap = graph.Nodes.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var edgeMap = graph.Edges
            .GroupBy(x => x.From, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var currentNodeId = nodeMap.ContainsKey("start") ? "start" : graph.Nodes[0].Id;
        var visitCounter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var maxLoopCount = definition.MaxLoopCount <= 0 ? 3 : definition.MaxLoopCount;
        var guardrail = ParseGuardrail(definition.GuardrailJson);
        var startedAt = DateTime.UtcNow;

        while (!string.IsNullOrWhiteSpace(currentNodeId))
        {
            if (!nodeMap.TryGetValue(currentNodeId, out var current))
            {
                result.Error = $"Unknown node '{currentNodeId}'.";
                return result;
            }

            if (!visitCounter.TryAdd(currentNodeId, 1))
            {
                visitCounter[currentNodeId]++;
                if (visitCounter[currentNodeId] > maxLoopCount)
                {
                    result.Error = $"Loop limit exceeded at node '{currentNodeId}'.";
                    return result;
                }
            }

            if (guardrail.maxExecutionMinutes > 0 && DateTime.UtcNow > startedAt.AddMinutes(guardrail.maxExecutionMinutes))
            {
                result.Error = $"Workflow execution exceeded maxExecutionMinutes={guardrail.maxExecutionMinutes}.";
                return result;
            }

            var stepResult = new VisualWorkflowStepResult
            {
                NodeId = current.Id,
                NodeType = current.Type
            };

            switch (current.Type.Trim().ToLowerInvariant())
            {
                case "start":
                    stepResult.Result = "start";
                    currentNodeId = ResolveNext(edgeMap, current.Id, ticket, defaultToFirst: true);
                    break;
                case "end":
                    stepResult.Result = "end";
                    currentNodeId = string.Empty;
                    break;
                case "delay":
                    stepResult.Result = $"delay:{current.DelayMinutes}m";
                    if (!dryRun && current.DelayMinutes > 0 && current.DelayMinutes <= 5)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(current.DelayMinutes), cancellationToken);
                    }
                    currentNodeId = ResolveNext(edgeMap, current.Id, ticket, defaultToFirst: true);
                    break;
                case "guard":
                    {
                        var passed = EvaluateCondition(current.Condition, ticket);
                        stepResult.Result = passed ? "guard:pass" : "guard:fail";
                        currentNodeId = ResolveNext(edgeMap, current.Id, ticket, preferConditional: passed);
                        break;
                    }
                case "branch":
                    stepResult.Result = "branch";
                    currentNodeId = ResolveNext(edgeMap, current.Id, ticket, preferConditional: true);
                    break;
                case "action":
                    stepResult.Result = await ExecuteActionNodeAsync(current, ticket, actorUserId, dryRun, cancellationToken);
                    currentNodeId = ResolveNext(edgeMap, current.Id, ticket, defaultToFirst: true);
                    break;
                default:
                    stepResult.Result = "no-op";
                    currentNodeId = ResolveNext(edgeMap, current.Id, ticket, defaultToFirst: true);
                    break;
            }

            result.Steps.Add(stepResult);
            if (result.Steps.Count > 500)
            {
                result.Error = "Workflow step limit exceeded.";
                return result;
            }
        }

        result.Success = true;
        return result;
    }

    private async Task<string> ExecuteActionNodeAsync(VisualWorkflowNode node, Ticket ticket, int actorUserId, bool dryRun, CancellationToken cancellationToken)
    {
        var action = node.ActionType.Trim().ToLowerInvariant();
        var value = node.ActionValue.Trim();

        if (dryRun)
            return $"dryrun:{action}:{value}";

        switch (action)
        {
            case "set_status":
                if (Enum.TryParse<TicketStatus>(value, true, out var status))
                {
                    await _ticketService.ChangeTicketStatusAsync(ticket.Id, status, actorUserId, "Status set by visual workflow");
                    return $"set_status:{status}";
                }
                return "set_status:invalid";

            case "assign_user":
                if (int.TryParse(value, out var userId))
                {
                    await _ticketService.AssignTicketAsync(ticket.Id, userId, "Assigned by visual workflow");
                    return $"assign_user:{userId}";
                }
                return "assign_user:invalid";

            case "notify_role":
                {
                    var userIds = await _context.UserRoles
                        .Where(x => x.Role != null && x.Role.Name == value)
                        .Where(x => x.User != null && x.User.TenantId == ticket.TenantId && x.User.IsActive)
                        .Select(x => x.UserId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    foreach (var recipientId in userIds)
                    {
                        await _notificationService.NotifyAsync(
                            recipientId,
                            "Workflow Notification",
                            $"Ticket {ticket.TicketNumber} matched workflow action.",
                            NotificationType.Info);
                    }

                    return $"notify_role:{value}:{userIds.Count}";
                }

            case "add_internal_note":
                await _ticketMessageService.SendMessageAsync(new CreateTicketMessageDto
                {
                    TicketId = ticket.Id,
                    SenderUserId = actorUserId,
                    Message = value,
                    IsInternalNote = true,
                    MessageType = TicketMessageType.Note
                });
                return "add_internal_note";

            default:
                _logger.LogWarning("Unsupported visual workflow action {ActionType} on ticket {TicketId}", action, ticket.Id);
                return $"unsupported:{action}";
        }
    }

    private static string ResolveNext(
        IReadOnlyDictionary<string, List<VisualWorkflowEdge>> edgeMap,
        string nodeId,
        Ticket ticket,
        bool preferConditional = false,
        bool defaultToFirst = false)
    {
        if (!edgeMap.TryGetValue(nodeId, out var edges) || edges.Count == 0)
            return string.Empty;

        if (preferConditional)
        {
            foreach (var edge in edges)
            {
                if (string.IsNullOrWhiteSpace(edge.Condition) || edge.IsDefault)
                    continue;
                if (EvaluateCondition(edge.Condition, ticket))
                    return edge.To;
            }

            var fallback = edges.FirstOrDefault(x => x.IsDefault);
            if (fallback != null)
                return fallback.To;
        }

        if (defaultToFirst)
            return edges[0].To;

        return string.Empty;
    }

    private static bool EvaluateCondition(string condition, Ticket ticket)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return true;

        // Minimal expression DSL: status=New;priority=High;categoryId=2;assigned=true
        var rules = condition.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var rule in rules)
        {
            var parts = rule.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var key = parts[0].ToLowerInvariant();
            var expected = parts[1];

            var isMatch = key switch
            {
                "status" => string.Equals(ticket.Status.ToString(), expected, StringComparison.OrdinalIgnoreCase),
                "priority" => string.Equals(ticket.Priority?.Name, expected, StringComparison.OrdinalIgnoreCase),
                "priorityid" => int.TryParse(expected, out var pid) && ticket.PriorityId == pid,
                "category" => string.Equals(ticket.Category?.Name, expected, StringComparison.OrdinalIgnoreCase),
                "categoryid" => int.TryParse(expected, out var cid) && ticket.CategoryId == cid,
                "assigned" => bool.TryParse(expected, out var assigned) && (ticket.AssignedToUserId.HasValue == assigned),
                _ => true
            };

            if (!isMatch)
                return false;
        }

        return true;
    }

    private static (int maxExecutionMinutes, bool requireGuard) ParseGuardrail(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (0, false);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var max = root.TryGetProperty("maxExecutionMinutes", out var maxEl) && maxEl.TryGetInt32(out var maxVal)
                ? maxVal
                : 0;
            var req = root.TryGetProperty("requiresGuard", out var reqEl) && reqEl.ValueKind == JsonValueKind.True;
            return (max, req);
        }
        catch
        {
            return (0, false);
        }
    }
}
