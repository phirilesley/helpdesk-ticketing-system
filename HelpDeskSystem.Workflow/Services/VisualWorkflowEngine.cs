using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using HelpDeskSystem.Workflow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HelpDeskSystem.Workflow.Services;

public class VisualWorkflowEngine : IWorkflowEngine
{
    private readonly HelpDeskDbContext _context;
    private readonly ITicketService _ticketService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<VisualWorkflowEngine> _logger;
    private readonly Dictionary<string, IWorkflowNodeExecutor> _nodeExecutors;
    private readonly Dictionary<string, IWorkflowConditionEvaluator> _conditionEvaluators;
    private readonly Dictionary<string, IWorkflowActionExecutor> _actionExecutors;

    public VisualWorkflowEngine(
        HelpDeskDbContext context,
        ITicketService ticketService,
        INotificationService notificationService,
        IAuditService auditService,
        ILogger<VisualWorkflowEngine> logger,
        IEnumerable<IWorkflowNodeExecutor> nodeExecutors,
        IEnumerable<IWorkflowConditionEvaluator> conditionEvaluators,
        IEnumerable<IWorkflowActionExecutor> actionExecutors)
    {
        _context = context;
        _ticketService = ticketService;
        _notificationService = notificationService;
        _auditService = auditService;
        _logger = logger;
        
        _nodeExecutors = nodeExecutors.ToDictionary(e => e.NodeType, e => e);
        _conditionEvaluators = conditionEvaluators.ToDictionary(e => e.ConditionType, e => e);
        _actionExecutors = actionExecutors.ToDictionary(e => e.ActionType, e => e);
    }

    public async Task<WorkflowDefinition> CreateWorkflowAsync(WorkflowDefinition workflow)
    {
        try
        {
            var entity = new WorkflowDefinitionEntity
            {
                TenantId = workflow.TenantId,
                Name = workflow.Name,
                Description = workflow.Description,
                Category = workflow.Category,
                IsActive = workflow.IsActive,
                TriggerJson = JsonSerializer.Serialize(workflow.Trigger),
                NodesJson = JsonSerializer.Serialize(workflow.Nodes),
                ConnectionsJson = JsonSerializer.Serialize(workflow.Connections),
                VariablesJson = JsonSerializer.Serialize(workflow.Variables),
                SettingsJson = JsonSerializer.Serialize(workflow.Settings),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = workflow.CreatedBy,
                UpdatedBy = workflow.UpdatedBy,
                Version = 1
            };

            _context.WorkflowDefinitions.Add(entity);
            await _context.SaveChangesAsync();

            workflow.Id = entity.Id;
            workflow.CreatedAt = entity.CreatedAt;
            workflow.UpdatedAt = entity.UpdatedAt;
            workflow.Version = entity.Version;

            _logger.LogInformation("Created visual workflow: {WorkflowName}", workflow.Name);
            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create visual workflow: {WorkflowName}", workflow.Name);
            throw;
        }
    }

    public async Task<WorkflowDefinition> UpdateWorkflowAsync(WorkflowDefinition workflow)
    {
        try
        {
            var entity = await _context.WorkflowDefinitions.FindAsync(workflow.Id);
            if (entity == null)
                throw new ArgumentException($"Workflow with ID {workflow.Id} not found");

            entity.Name = workflow.Name;
            entity.Description = workflow.Description;
            entity.Category = workflow.Category;
            entity.IsActive = workflow.IsActive;
            entity.TriggerJson = JsonSerializer.Serialize(workflow.Trigger);
            entity.NodesJson = JsonSerializer.Serialize(workflow.Nodes);
            entity.ConnectionsJson = JsonSerializer.Serialize(workflow.Connections);
            entity.VariablesJson = JsonSerializer.Serialize(workflow.Variables);
            entity.SettingsJson = JsonSerializer.Serialize(workflow.Settings);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = workflow.UpdatedBy;
            entity.Version++;

            await _context.SaveChangesAsync();

            workflow.UpdatedAt = entity.UpdatedAt;
            workflow.Version = entity.Version;

            _logger.LogInformation("Updated visual workflow: {WorkflowName}", workflow.Name);
            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update visual workflow: {WorkflowName}", workflow.Name);
            throw;
        }
    }

    public async Task<WorkflowDefinition> GetWorkflowAsync(int workflowId)
    {
        try
        {
            var entity = await _context.WorkflowDefinitions.FindAsync(workflowId);
            if (entity == null)
                throw new ArgumentException($"Workflow with ID {workflowId} not found");

            return new WorkflowDefinition
            {
                Id = entity.Id,
                TenantId = entity.TenantId,
                Name = entity.Name,
                Description = entity.Description,
                Category = entity.Category,
                IsActive = entity.IsActive,
                Trigger = JsonSerializer.Deserialize<WorkflowTrigger>(entity.TriggerJson),
                Nodes = JsonSerializer.Deserialize<WorkflowNode[]>(entity.NodesJson),
                Connections = JsonSerializer.Deserialize<WorkflowConnection[]>(entity.ConnectionsJson),
                Variables = JsonSerializer.Deserialize<WorkflowVariable[]>(entity.VariablesJson),
                Settings = JsonSerializer.Deserialize<WorkflowSettings>(entity.SettingsJson),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy,
                Version = entity.Version
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get visual workflow: {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<WorkflowDefinition[]> GetWorkflowsAsync(int? tenantId = null)
    {
        try
        {
            var query = _context.WorkflowDefinitions.AsQueryable();
            
            if (tenantId.HasValue)
                query = query.Where(w => w.TenantId == tenantId);

            var entities = await query.OrderByDescending(w => w.UpdatedAt).ToListAsync();

            return entities.Select(entity => new WorkflowDefinition
            {
                Id = entity.Id,
                TenantId = entity.TenantId,
                Name = entity.Name,
                Description = entity.Description,
                Category = entity.Category,
                IsActive = entity.IsActive,
                Trigger = JsonSerializer.Deserialize<WorkflowTrigger>(entity.TriggerJson),
                Nodes = JsonSerializer.Deserialize<WorkflowNode[]>(entity.NodesJson),
                Connections = JsonSerializer.Deserialize<WorkflowConnection[]>(entity.ConnectionsJson),
                Variables = JsonSerializer.Deserialize<WorkflowVariable[]>(entity.VariablesJson),
                Settings = JsonSerializer.Deserialize<WorkflowSettings>(entity.SettingsJson),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy,
                Version = entity.Version
            }).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get visual workflows");
            throw;
        }
    }

    public async Task<bool> DeleteWorkflowAsync(int workflowId)
    {
        try
        {
            var entity = await _context.WorkflowDefinitions.FindAsync(workflowId);
            if (entity == null)
                return false;

            _context.WorkflowDefinitions.Remove(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted visual workflow: {WorkflowId}", workflowId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete visual workflow: {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<WorkflowExecution> ExecuteWorkflowAsync(int workflowId, WorkflowExecutionContext context)
    {
        try
        {
            var workflow = await GetWorkflowAsync(workflowId);
            if (!workflow.IsActive)
                throw new InvalidOperationException("Workflow is not active");

            var executionId = Guid.NewGuid().ToString();
            var execution = new WorkflowExecution
            {
                Id = executionId,
                WorkflowId = workflowId,
                TenantId = context.TenantId,
                Status = WorkflowExecutionStatus.Running,
                TriggeredBy = context.TriggeredBy,
                Context = context,
                StartedAt = DateTime.UtcNow,
                Steps = new WorkflowExecutionStep[0],
                Metrics = new Dictionary<string, object>(),
                CurrentNodeId = workflow.Nodes.FirstOrDefault(n => n.IsStartNode)?.Id
            };

            // Save execution to database
            var executionEntity = new WorkflowExecutionEntity
            {
                Id = executionId,
                WorkflowId = workflowId,
                TenantId = context.TenantId,
                Status = execution.Status,
                TriggeredBy = context.TriggeredBy,
                ContextJson = JsonSerializer.Serialize(context),
                StartedAt = execution.StartedAt,
                StepsJson = JsonSerializer.Serialize(execution.Steps),
                MetricsJson = JsonSerializer.Serialize(execution.Metrics),
                CurrentNodeId = execution.CurrentNodeId
            };

            _context.WorkflowExecutions.Add(executionEntity);
            await _context.SaveChangesAsync();

            // Execute workflow asynchronously
            _ = Task.Run(() => ExecuteWorkflowInternalAsync(executionId, workflow));

            return execution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute visual workflow: {WorkflowId}", workflowId);
            throw;
        }
    }

    private async Task ExecuteWorkflowInternalAsync(string executionId, WorkflowDefinition workflow)
    {
        try
        {
            var execution = await GetExecutionAsync(executionId);
            var context = execution.Context;
            var currentNode = workflow.Nodes.FirstOrDefault(n => n.Id == execution.CurrentNodeId);

            while (currentNode != null && execution.Status == WorkflowExecutionStatus.Running)
            {
                // Execute current node
                var nodeResult = await ExecuteNodeAsync(currentNode, context);
                
                // Record step
                var step = new WorkflowExecutionStep
                {
                    Id = Guid.NewGuid().ToString(),
                    NodeId = currentNode.Id,
                    NodeType = currentNode.Type,
                    Status = nodeResult.Success ? WorkflowStepStatus.Completed : WorkflowStepStatus.Failed,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    OutputData = JsonSerializer.Serialize(nodeResult.Output),
                    ErrorMessage = nodeResult.ErrorMessage
                };

                // Update execution
                execution.Steps = execution.Steps.Append(step).ToArray();
                execution.CurrentNodeId = nodeResult.NextNodes?.FirstOrDefault();

                if (!nodeResult.Success || !nodeResult.ShouldContinue)
                {
                    execution.Status = nodeResult.Success ? WorkflowExecutionStatus.Completed : WorkflowExecutionStatus.Failed;
                    break;
                }

                // Find next node
                currentNode = workflow.Nodes.FirstOrDefault(n => n.Id == execution.CurrentNodeId);
            }

            // Update execution in database
            await UpdateExecutionAsync(execution);

            _logger.LogInformation("Completed visual workflow execution: {ExecutionId}", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute visual workflow: {ExecutionId}", executionId);
            
            var execution = await GetExecutionAsync(executionId);
            execution.Status = WorkflowExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt = DateTime.UtcNow;
            
            await UpdateExecutionAsync(execution);
        }
    }

    private async Task<WorkflowNodeResult> ExecuteNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
    {
        try
        {
            if (_nodeExecutors.TryGetValue(node.Type, out var executor))
            {
                return await executor.ExecuteAsync(node, context);
            }

            // Default execution for simple nodes
            var result = new WorkflowNodeResult
            {
                Success = true,
                Status = "completed",
                Output = new { Message = $"Executed node {node.Type}" },
                ShouldContinue = true
            };

            // Execute actions
            if (node.Actions != null)
            {
                foreach (var action in node.Actions)
                {
                    if (_actionExecutors.TryGetValue(action.Type, out var actionExecutor))
                    {
                        var actionResult = await actionExecutor.ExecuteAsync(action, context);
                        if (!actionResult.Success)
                        {
                            result.Success = false;
                            result.ErrorMessage = actionResult.ErrorMessage;
                            break;
                        }
                    }
                }
            }

            // Find next nodes based on connections
            var nextNodes = await GetNextNodesAsync(node, context);

            return new WorkflowNodeResult
            {
                Success = result.Success,
                Status = result.Status,
                Output = result.Output,
                NextNodes = nextNodes,
                ShouldContinue = result.Success && nextNodes.Any()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow node: {NodeId}", node.Id);
            return new WorkflowNodeResult
            {
                Success = false,
                Status = "failed",
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<string[]> GetNextNodesAsync(WorkflowNode currentNode, WorkflowExecutionContext context)
    {
        try
        {
            var workflow = await GetWorkflowAsync(context.WorkflowId);
            var connections = workflow.Connections.Where(c => c.SourceNodeId == currentNode.Id);

            var nextNodes = new List<string>();

            foreach (var connection in connections)
            {
                if (connection.Condition == null)
                {
                    nextNodes.Add(connection.TargetNodeId);
                }
                else
                {
                    if (_conditionEvaluators.TryGetValue(connection.Condition.Type, out var evaluator))
                    {
                        var conditionResult = await evaluator.EvaluateAsync(connection.Condition, context);
                        if (conditionResult)
                        {
                            nextNodes.Add(connection.TargetNodeId);
                        }
                    }
                }
            }

            return nextNodes.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get next nodes for: {NodeId}", currentNode.Id);
            return Array.Empty<string>();
        }
    }

    public async Task<WorkflowExecution> GetExecutionAsync(string executionId)
    {
        try
        {
            var entity = await _context.WorkflowExecutions.FindAsync(executionId);
            if (entity == null)
                throw new ArgumentException($"Execution with ID {executionId} not found");

            return new WorkflowExecution
            {
                Id = entity.Id,
                WorkflowId = entity.WorkflowId,
                TenantId = entity.TenantId,
                Status = entity.Status,
                TriggeredBy = entity.TriggeredBy,
                Context = JsonSerializer.Deserialize<WorkflowExecutionContext>(entity.ContextJson),
                Steps = JsonSerializer.Deserialize<WorkflowExecutionStep[]>(entity.StepsJson),
                StartedAt = entity.StartedAt,
                CompletedAt = entity.CompletedAt,
                PausedAt = entity.PausedAt,
                ErrorMessage = entity.ErrorMessage,
                ErrorCode = entity.ErrorCode,
                Metrics = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.MetricsJson),
                CurrentNodeId = entity.CurrentNodeId,
                ExecutionCount = entity.ExecutionCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow execution: {ExecutionId}", executionId);
            throw;
        }
    }

    public async Task<WorkflowExecution[]> GetExecutionsAsync(int workflowId, int? status = null)
    {
        try
        {
            var query = _context.WorkflowExecutions.Where(e => e.WorkflowId == workflowId);
            
            if (status.HasValue)
                query = query.Where(e => e.Status == status.ToString());

            var entities = await query.OrderByDescending(e => e.StartedAt).ToListAsync();

            return entities.Select(entity => new WorkflowExecution
            {
                Id = entity.Id,
                WorkflowId = entity.WorkflowId,
                TenantId = entity.TenantId,
                Status = entity.Status,
                TriggeredBy = entity.TriggeredBy,
                Context = JsonSerializer.Deserialize<WorkflowExecutionContext>(entity.ContextJson),
                Steps = JsonSerializer.Deserialize<WorkflowExecutionStep[]>(entity.StepsJson),
                StartedAt = entity.StartedAt,
                CompletedAt = entity.CompletedAt,
                PausedAt = entity.PausedAt,
                ErrorMessage = entity.ErrorMessage,
                ErrorCode = entity.ErrorCode,
                Metrics = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.MetricsJson),
                CurrentNodeId = entity.CurrentNodeId,
                ExecutionCount = entity.ExecutionCount
            }).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow executions for: {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<bool> PauseExecutionAsync(string executionId)
    {
        try
        {
            var entity = await _context.WorkflowExecutions.FindAsync(executionId);
            if (entity == null)
                return false;

            entity.Status = WorkflowExecutionStatus.Paused;
            entity.PausedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Paused workflow execution: {ExecutionId}", executionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause workflow execution: {ExecutionId}", executionId);
            throw;
        }
    }

    public async Task<bool> ResumeExecutionAsync(string executionId)
    {
        try
        {
            var entity = await _context.WorkflowExecutions.FindAsync(executionId);
            if (entity == null)
                return false;

            entity.Status = WorkflowExecutionStatus.Running;
            entity.PausedAt = null;
            await _context.SaveChangesAsync();

            // Resume execution asynchronously
            _ = Task.Run(() => ResumeExecutionInternalAsync(executionId));

            _logger.LogInformation("Resumed workflow execution: {ExecutionId}", executionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume workflow execution: {ExecutionId}", executionId);
            throw;
        }
    }

    private async Task ResumeExecutionInternalAsync(string executionId)
    {
        try
        {
            var execution = await GetExecutionAsync(executionId);
            var workflow = await GetWorkflowAsync(execution.WorkflowId);
            
            await ExecuteWorkflowInternalAsync(executionId, workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume workflow execution: {ExecutionId}", executionId);
        }
    }

    public async Task<bool> CancelExecutionAsync(string executionId)
    {
        try
        {
            var entity = await _context.WorkflowExecutions.FindAsync(executionId);
            if (entity == null)
                return false;

            entity.Status = WorkflowExecutionStatus.Cancelled;
            entity.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled workflow execution: {ExecutionId}", executionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel workflow execution: {ExecutionId}", executionId);
            throw;
        }
    }

    public async Task<WorkflowNode[]> GetAvailableNodesAsync()
    {
        try
        {
            return new[]
            {
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Start,
                    Name = "Start",
                    Category = "Control",
                    Icon = "play",
                    Color = "#4CAF50",
                    IsStartNode = true,
                    Description = "Start of the workflow"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.End,
                    Name = "End",
                    Category = "Control",
                    Icon = "stop",
                    Color = "#F44336",
                    IsEndNode = true,
                    Description = "End of the workflow"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Condition,
                    Name = "Condition",
                    Category = "Logic",
                    Icon = "git-branch",
                    Color = "#2196F3",
                    Description = "Conditional logic branching"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Action,
                    Name = "Action",
                    Category = "Action",
                    Icon = "play-circle",
                    Color = "#FF9800",
                    Description = "Execute an action"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Email,
                    Name = "Send Email",
                    Category = "Communication",
                    Icon = "mail",
                    Color = "#9C27B0",
                    Description = "Send email notification"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Notification,
                    Name = "Send Notification",
                    Category = "Communication",
                    Icon = "bell",
                    Color = "#E91E63",
                    Description = "Send system notification"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Assignment,
                    Name = "Assign Ticket",
                    Category = "Ticket",
                    Icon = "user-plus",
                    Color = "#00BCD4",
                    Description = "Assign ticket to user"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Approval,
                    Name = "Approval",
                    Category = "Approval",
                    Icon = "check-circle",
                    Color = "#8BC34A",
                    Description = "Require approval"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Integration,
                    Name = "Integration",
                    Category = "Integration",
                    Icon = "link",
                    Color = "#795548",
                    Description = "External system integration"
                },
                new WorkflowNode
                {
                    Type = WorkflowNodeTypes.Delay,
                    Name = "Delay",
                    Category = "Timing",
                    Icon = "clock",
                    Color = "#607D8B",
                    Description = "Wait for specified time"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available workflow nodes");
            throw;
        }
    }

    public async Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow)
    {
        try
        {
            var result = new WorkflowValidationResult
            {
                IsValid = true,
                Errors = Array.Empty<string>(),
                Warnings = Array.Empty<string>(),
                NodeErrors = new Dictionary<string, string[]>(),
                ConnectionErrors = new Dictionary<string, string[]>()
            };

            // Validate basic properties
            if (string.IsNullOrEmpty(workflow.Name))
            {
                result.IsValid = false;
                result.Errors = result.Errors.Append("Workflow name is required").ToArray();
            }

            // Validate nodes
            if (workflow.Nodes == null || !workflow.Nodes.Any())
            {
                result.IsValid = false;
                result.Errors = result.Errors.Append("Workflow must have at least one node").ToArray();
            }
            else
            {
                var startNodes = workflow.Nodes.Where(n => n.IsStartNode).ToArray();
                var endNodes = workflow.Nodes.Where(n => n.IsEndNode).ToArray();

                if (!startNodes.Any())
                {
                    result.IsValid = false;
                    result.Errors = result.Errors.Append("Workflow must have a start node").ToArray();
                }

                if (!endNodes.Any())
                {
                    result.IsValid = false;
                    result.Errors = result.Errors.Append("Workflow must have an end node").ToArray();
                }

                if (startNodes.Length > 1)
                {
                    result.Warnings = result.Warnings.Append("Workflow has multiple start nodes").ToArray();
                }
            }

            // Validate connections
            if (workflow.Connections != null)
            {
                foreach (var connection in workflow.Connections)
                {
                    var sourceNode = workflow.Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                    var targetNode = workflow.Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

                    if (sourceNode == null)
                    {
                        result.IsValid = false;
                        result.ConnectionErrors[connection.Id] = result.ConnectionErrors.GetValueOrDefault(connection.Id, Array.Empty<string>())
                            .Append("Source node not found").ToArray();
                    }

                    if (targetNode == null)
                    {
                        result.IsValid = false;
                        result.ConnectionErrors[connection.Id] = result.ConnectionErrors.GetValueOrDefault(connection.Id, Array.Empty<string>())
                            .Append("Target node not found").ToArray();
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate workflow: {WorkflowId}", workflow.Id);
            throw;
        }
    }

    private async Task UpdateExecutionAsync(WorkflowExecution execution)
    {
        try
        {
            var entity = await _context.WorkflowExecutions.FindAsync(execution.Id);
            if (entity == null)
                return;

            entity.Status = execution.Status;
            entity.CurrentNodeId = execution.CurrentNodeId;
            entity.StepsJson = JsonSerializer.Serialize(execution.Steps);
            entity.MetricsJson = JsonSerializer.Serialize(execution.Metrics);
            entity.CompletedAt = execution.CompletedAt;
            entity.PausedAt = execution.PausedAt;
            entity.ErrorMessage = execution.ErrorMessage;
            entity.ErrorCode = execution.ErrorCode;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workflow execution: {ExecutionId}", execution.Id);
        }
    }
}

// Entity classes for database storage
public class WorkflowDefinitionEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public bool IsActive { get; set; }
    public string TriggerJson { get; set; }
    public string NodesJson { get; set; }
    public string ConnectionsJson { get; set; }
    public string VariablesJson { get; set; }
    public string SettingsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public int Version { get; set; }
}

public class WorkflowExecutionEntity
{
    public string Id { get; set; }
    public int WorkflowId { get; set; }
    public int TenantId { get; set; }
    public string Status { get; set; }
    public string TriggeredBy { get; set; }
    public string ContextJson { get; set; }
    public string StepsJson { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorCode { get; set; }
    public string MetricsJson { get; set; }
    public string CurrentNodeId { get; set; }
    public int ExecutionCount { get; set; }
}
