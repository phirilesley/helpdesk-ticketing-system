using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Workflow.Models;

// Workflow action models
public class WorkflowAction
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class WorkflowCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object Value { get; set; } = string.Empty;
    public string? LogicalOperator { get; set; } // AND, OR for multiple conditions
}
