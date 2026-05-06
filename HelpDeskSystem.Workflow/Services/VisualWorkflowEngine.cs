using System.Collections.Concurrent;
using System.Text.Json;
using HelpDeskSystem.Persistence.Context;
using HelpDeskSystem.Workflow.Visual;
using HelpDeskSystem.Workflow.Visual.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HelpDeskSystem.Workflow.Visual.Services;

public class VisualWorkflowEngine : IVisualWorkflowEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HelpDeskDbContext _context;
    private readonly ILogger<VisualWorkflowEngine> _logger;
    private readonly ConcurrentDictionary<string, WorkflowExecution> _executions = new();

    public VisualWorkflowEngine(HelpDeskDbContext context, ILogger<VisualWorkflowEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WorkflowDefinition> CreateWorkflowAsync(WorkflowDefinition workflow)
    {
        var entity = new HelpDeskSystem.Domain.Entities.WorkflowDefinition
        {
            TenantId = workflow.TenantId,
            Name = workflow.Name,
            Version = Math.Max(1, workflow.Version),
            IsPublished = workflow.IsActive,
            GraphJson = BuildGraphJson(workflow),
            GuardrailJson = JsonSerializer.Serialize(new
            {
                maxExecutionMinutes = Math.Max(1, workflow.Settings?.MaxExecutionTimeMinutes ?? 60),
                maxLoopCount = 3,
                requiresGuard = true
            }, JsonOptions),
            MaxLoopCount = 3
        };

        _context.WorkflowDefinitions.Add(entity);
        await _context.SaveChangesAsync();

        workflow.Id = entity.Id;
        workflow.CreatedAt = entity.CreatedAtUtc;
        workflow.UpdatedAt = entity.UpdatedAtUtc ?? entity.CreatedAtUtc;
        return workflow;
    }

    public async Task<WorkflowDefinition> UpdateWorkflowAsync(WorkflowDefinition workflow)
    {
        var entity = await _context.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == workflow.Id && !x.IsDeleted);
        if (entity == null)
            throw new ArgumentException($"Workflow with ID {workflow.Id} not found");

        entity.Name = workflow.Name;
        entity.IsPublished = workflow.IsActive;
        entity.Version = Math.Max(entity.Version + 1, workflow.Version);
        entity.GraphJson = BuildGraphJson(workflow);
        entity.GuardrailJson = JsonSerializer.Serialize(new
        {
            maxExecutionMinutes = Math.Max(1, workflow.Settings?.MaxExecutionTimeMinutes ?? 60),
            maxLoopCount = 3,
            requiresGuard = true
        }, JsonOptions);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        workflow.Version = entity.Version;
        workflow.UpdatedAt = entity.UpdatedAtUtc ?? DateTime.UtcNow;
        return workflow;
    }

    public async Task<WorkflowDefinition> GetWorkflowAsync(int workflowId)
    {
        var entity = await _context.WorkflowDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workflowId && !x.IsDeleted);
        if (entity == null)
            throw new ArgumentException($"Workflow with ID {workflowId} not found");

        var (nodes, connections) = ParseGraphJson(entity.GraphJson);
        return new WorkflowDefinition
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Description = string.Empty,
            Category = "general",
            IsActive = entity.IsPublished,
            Trigger = new WorkflowTrigger { Type = "event", EventType = "ticket.updated", IsActive = true, Configuration = new Dictionary<string, object>() },
            Nodes = nodes,
            Connections = connections,
            Variables = Array.Empty<WorkflowVariable>(),
            Settings = new WorkflowSettings(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc ?? entity.CreatedAtUtc,
            CreatedBy = "system",
            UpdatedBy = "system",
            Version = entity.Version
        };
    }

    public async Task<WorkflowDefinition[]> GetWorkflowsAsync(int? tenantId = null)
    {
        var query = _context.WorkflowDefinitions.AsNoTracking().Where(x => !x.IsDeleted);
        if (tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var entities = await query.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).ToListAsync();
        var result = new List<WorkflowDefinition>();

        foreach (var entity in entities)
        {
            var (nodes, connections) = ParseGraphJson(entity.GraphJson);
            result.Add(new WorkflowDefinition
            {
                Id = entity.Id,
                TenantId = entity.TenantId,
                Name = entity.Name,
                Description = string.Empty,
                Category = "general",
                IsActive = entity.IsPublished,
                Trigger = new WorkflowTrigger { Type = "event", EventType = "ticket.updated", IsActive = true, Configuration = new Dictionary<string, object>() },
                Nodes = nodes,
                Connections = connections,
                Variables = Array.Empty<WorkflowVariable>(),
                Settings = new WorkflowSettings(),
                CreatedAt = entity.CreatedAtUtc,
                UpdatedAt = entity.UpdatedAtUtc ?? entity.CreatedAtUtc,
                CreatedBy = "system",
                UpdatedBy = "system",
                Version = entity.Version
            });
        }

        return result.ToArray();
    }

    public async Task<bool> DeleteWorkflowAsync(int workflowId)
    {
        var entity = await _context.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == workflowId && !x.IsDeleted);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<WorkflowExecution> ExecuteWorkflowAsync(int workflowId, WorkflowExecutionContext context)
    {
        var startNodeId = "start";
        var now = DateTime.UtcNow;

        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid().ToString("N"),
            WorkflowId = workflowId,
            TenantId = context.TenantId,
            Status = WorkflowExecutionStatus.Completed,
            TriggeredBy = context.TriggeredBy,
            Context = context,
            Steps =
            [
                new WorkflowExecutionStep
                {
                    Id = Guid.NewGuid().ToString("N"),
                    NodeId = startNodeId,
                    NodeType = WorkflowNodeTypes.Start,
                    Status = WorkflowStepStatus.Completed,
                    StartedAt = now,
                    CompletedAt = now,
                    ExecutionTimeMs = 0
                }
            ],
            StartedAt = now,
            CompletedAt = now,
            CurrentNodeId = startNodeId,
            Metrics = new Dictionary<string, object>
            {
                ["durationMs"] = 0,
                ["steps"] = 1
            },
            ExecutionCount = 1
        };

        _executions[execution.Id] = execution;
        _logger.LogInformation("Visual workflow execution {ExecutionId} completed for workflow {WorkflowId}", execution.Id, workflowId);
        return Task.FromResult(execution);
    }

    public Task<WorkflowExecution> GetExecutionAsync(string executionId)
    {
        if (_executions.TryGetValue(executionId, out var execution))
            return Task.FromResult(execution);

        throw new ArgumentException($"Execution with ID {executionId} not found");
    }

    public Task<WorkflowExecution[]> GetExecutionsAsync(int workflowId, int? status = null)
    {
        var rows = _executions.Values.Where(x => x.WorkflowId == workflowId);
        if (status.HasValue)
        {
            var target = status.Value.ToString();
            rows = rows.Where(x => string.Equals(x.Status, target, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(rows.OrderByDescending(x => x.StartedAt).ToArray());
    }

    public Task<bool> PauseExecutionAsync(string executionId)
    {
        if (!_executions.TryGetValue(executionId, out var execution))
            return Task.FromResult(false);

        execution.Status = WorkflowExecutionStatus.Paused;
        execution.PausedAt = DateTime.UtcNow;
        _executions[executionId] = execution;
        return Task.FromResult(true);
    }

    public Task<bool> ResumeExecutionAsync(string executionId)
    {
        if (!_executions.TryGetValue(executionId, out var execution))
            return Task.FromResult(false);

        execution.Status = WorkflowExecutionStatus.Running;
        execution.PausedAt = null;
        _executions[executionId] = execution;
        return Task.FromResult(true);
    }

    public Task<bool> CancelExecutionAsync(string executionId)
    {
        if (!_executions.TryGetValue(executionId, out var execution))
            return Task.FromResult(false);

        execution.Status = WorkflowExecutionStatus.Cancelled;
        execution.CompletedAt = DateTime.UtcNow;
        _executions[executionId] = execution;
        return Task.FromResult(true);
    }

    public Task<WorkflowNode[]> GetAvailableNodesAsync()
    {
        var nodes = new[]
        {
            new WorkflowNode { Id = "start", Type = WorkflowNodeTypes.Start, Name = "Start", IsStartNode = true, Category = "Control", Color = "#4CAF50" },
            new WorkflowNode { Id = "condition", Type = WorkflowNodeTypes.Condition, Name = "Condition", Category = "Logic", Color = "#2196F3" },
            new WorkflowNode { Id = "action", Type = WorkflowNodeTypes.Action, Name = "Action", Category = "Action", Color = "#FF9800" },
            new WorkflowNode { Id = "delay", Type = WorkflowNodeTypes.Delay, Name = "Delay", Category = "Timing", Color = "#607D8B" },
            new WorkflowNode { Id = "end", Type = WorkflowNodeTypes.End, Name = "End", IsEndNode = true, Category = "Control", Color = "#F44336" }
        };

        return Task.FromResult(nodes);
    }

    public Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(workflow.Name))
            errors.Add("Workflow name is required.");
        if (workflow.Nodes == null || workflow.Nodes.Length == 0)
            errors.Add("Workflow must contain at least one node.");
        else
        {
            if (!workflow.Nodes.Any(n => n.IsStartNode))
                errors.Add("Workflow must contain a start node.");
            if (!workflow.Nodes.Any(n => n.IsEndNode))
                warnings.Add("Workflow has no end node.");
        }

        var result = new WorkflowValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors.ToArray(),
            Warnings = warnings.ToArray(),
            NodeErrors = new Dictionary<string, string[]>(),
            ConnectionErrors = new Dictionary<string, string[]>()
        };

        return Task.FromResult(result);
    }

    private static string BuildGraphJson(WorkflowDefinition workflow)
    {
        return JsonSerializer.Serialize(new
        {
            nodes = workflow.Nodes ?? Array.Empty<WorkflowNode>(),
            edges = workflow.Connections ?? Array.Empty<WorkflowConnection>()
        }, JsonOptions);
    }

    private static (WorkflowNode[] Nodes, WorkflowConnection[] Connections) ParseGraphJson(string graphJson)
    {
        if (string.IsNullOrWhiteSpace(graphJson))
            return (Array.Empty<WorkflowNode>(), Array.Empty<WorkflowConnection>());

        try
        {
            using var doc = JsonDocument.Parse(graphJson);
            var root = doc.RootElement;

            var nodes = root.TryGetProperty("nodes", out var nodesEl)
                ? JsonSerializer.Deserialize<WorkflowNode[]>(nodesEl.GetRawText(), JsonOptions) ?? Array.Empty<WorkflowNode>()
                : Array.Empty<WorkflowNode>();

            var edges = root.TryGetProperty("edges", out var edgesEl)
                ? JsonSerializer.Deserialize<WorkflowConnection[]>(edgesEl.GetRawText(), JsonOptions) ?? Array.Empty<WorkflowConnection>()
                : Array.Empty<WorkflowConnection>();

            return (nodes, edges);
        }
        catch
        {
            return (Array.Empty<WorkflowNode>(), Array.Empty<WorkflowConnection>());
        }
    }
}
