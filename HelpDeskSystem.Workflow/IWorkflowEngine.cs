using HelpDeskSystem.Workflow.Visual.Models;

namespace HelpDeskSystem.Workflow.Visual;

public interface IVisualWorkflowEngine
{
    Task<WorkflowDefinition> CreateWorkflowAsync(WorkflowDefinition workflow);
    Task<WorkflowDefinition> UpdateWorkflowAsync(WorkflowDefinition workflow);
    Task<WorkflowDefinition> GetWorkflowAsync(int workflowId);
    Task<WorkflowDefinition[]> GetWorkflowsAsync(int? tenantId = null);
    Task<bool> DeleteWorkflowAsync(int workflowId);
    Task<WorkflowExecution> ExecuteWorkflowAsync(int workflowId, WorkflowExecutionContext context);
    Task<WorkflowExecution> GetExecutionAsync(string executionId);
    Task<WorkflowExecution[]> GetExecutionsAsync(int workflowId, int? status = null);
    Task<bool> PauseExecutionAsync(string executionId);
    Task<bool> ResumeExecutionAsync(string executionId);
    Task<bool> CancelExecutionAsync(string executionId);
    Task<WorkflowNode[]> GetAvailableNodesAsync();
    Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow);
}

public interface IWorkflowNodeExecutor
{
    Task<WorkflowNodeResult> ExecuteAsync(WorkflowNode node, WorkflowExecutionContext context);
    bool CanExecute(WorkflowNode node, WorkflowExecutionContext context);
    string NodeType { get; }
}

public interface IWorkflowConditionEvaluator
{
    Task<bool> EvaluateAsync(WorkflowCondition condition, WorkflowExecutionContext context);
    string ConditionType { get; }
}

public interface IWorkflowActionExecutor
{
    Task<WorkflowActionResult> ExecuteAsync(WorkflowAction action, WorkflowExecutionContext context);
    string ActionType { get; }
}
