using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class WorkflowRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0; // Higher number = higher priority
    
    // Trigger conditions
    public WorkflowTriggerType TriggerType { get; set; }
    public string? TriggerConditionJson { get; set; } // JSON serialized condition
    
    // Actions to execute
    public string ActionsJson { get; set; } = string.Empty; // JSON serialized actions
    
    // Navigation
    public Tenant? Tenant { get; set; }
    public ICollection<WorkflowExecution> Executions { get; set; } = new List<WorkflowExecution>();
}

public class WorkflowExecution : BaseEntity
{
    public int WorkflowRuleId { get; set; }
    public int TicketId { get; set; }
    public int TriggeredByUserId { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public string? ExecutionResultJson { get; set; }
    
    // Navigation
    public WorkflowRule? WorkflowRule { get; set; }
    public Ticket? Ticket { get; set; }
}

public enum WorkflowTriggerType
{
    TicketCreated = 1,
    TicketStatusChanged = 2,
    TicketAssigned = 3,
    TicketUnassigned = 4,
    SlaBreached = 5,
    MessageReceived = 6,
    TimeBased = 7
}

public enum WorkflowStatus
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
