namespace HelpDeskSystem.Workflow.Visual.Models;

public class WorkflowDefinition
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public bool IsActive { get; set; }
    public WorkflowTrigger Trigger { get; set; }
    public WorkflowNode[] Nodes { get; set; }
    public WorkflowConnection[] Connections { get; set; }
    public WorkflowVariable[] Variables { get; set; }
    public WorkflowSettings Settings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public int Version { get; set; }
}

public class WorkflowTrigger
{
    public string Type { get; set; }
    public string EventType { get; set; }
    public Dictionary<string, object> Configuration { get; set; }
    public bool IsActive { get; set; }
}

public class WorkflowNode
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public WorkflowNodeData Data { get; set; }
    public WorkflowCondition[] Conditions { get; set; }
    public WorkflowAction[] Actions { get; set; }
    public bool IsStartNode { get; set; }
    public bool IsEndNode { get; set; }
    public string Category { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}

public class WorkflowNodeData
{
    public object Input { get; set; }
    public object Output { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public Dictionary<string, string> Mappings { get; set; }
}

public class WorkflowConnection
{
    public string Id { get; set; }
    public string SourceNodeId { get; set; }
    public string TargetNodeId { get; set; }
    public string SourceHandle { get; set; }
    public string TargetHandle { get; set; }
    public WorkflowCondition Condition { get; set; }
    public string Type { get; set; }
    public bool IsActive { get; set; }
}

public class WorkflowCondition
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Operator { get; set; }
    public string LeftOperand { get; set; }
    public string RightOperand { get; set; }
    public object Value { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public bool IsNegated { get; set; }
}

public class WorkflowAction
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public bool IsAsync { get; set; }
    public int TimeoutSeconds { get; set; }
    public int RetryCount { get; set; }
}

public class WorkflowVariable
{
    public string Name { get; set; }
    public string Type { get; set; }
    public object Value { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public string DefaultValue { get; set; }
}

public class WorkflowSettings
{
    public bool EnableLogging { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public int MaxExecutionTimeMinutes { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
    public string Timezone { get; set; } = "UTC";
    public Dictionary<string, object> CustomSettings { get; set; }
}

public class WorkflowExecution
{
    public string Id { get; set; }
    public int WorkflowId { get; set; }
    public int TenantId { get; set; }
    public string Status { get; set; }
    public string TriggeredBy { get; set; }
    public WorkflowExecutionContext Context { get; set; }
    public WorkflowExecutionStep[] Steps { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorCode { get; set; }
    public Dictionary<string, object> Metrics { get; set; }
    public string CurrentNodeId { get; set; }
    public int ExecutionCount { get; set; }
}

public class WorkflowExecutionContext
{
    public string ExecutionId { get; set; }
    public int WorkflowId { get; set; }
    public int TenantId { get; set; }
    public Dictionary<string, object> Variables { get; set; }
    public Dictionary<string, object> InputData { get; set; }
    public Dictionary<string, object> OutputData { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public string TriggeredBy { get; set; }
    public DateTime StartedAt { get; set; }
    public WorkflowUser User { get; set; }
    public WorkflowTicket Ticket { get; set; }
}

public class WorkflowUser
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}

public class WorkflowTicket
{
    public int Id { get; set; }
    public string Number { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
    public string Category { get; set; }
    public int? AssignedToId { get; set; }
    public int CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, object> CustomFields { get; set; }
}

public class WorkflowExecutionStep
{
    public string Id { get; set; }
    public string NodeId { get; set; }
    public string NodeType { get; set; }
    public string Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string InputData { get; set; }
    public string OutputData { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorCode { get; set; }
    public int ExecutionTimeMs { get; set; }
    public int RetryCount { get; set; }
}

public class WorkflowNodeResult
{
    public bool Success { get; set; }
    public string Status { get; set; }
    public object Output { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorCode { get; set; }
    public Dictionary<string, object> Variables { get; set; }
    public string[] NextNodes { get; set; }
    public bool ShouldContinue { get; set; }
}

public class WorkflowActionResult
{
    public bool Success { get; set; }
    public string Status { get; set; }
    public object Result { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorCode { get; set; }
    public Dictionary<string, object> Output { get; set; }
    public int ExecutionTimeMs { get; set; }
}

public class WorkflowValidationResult
{
    public bool IsValid { get; set; }
    public string[] Errors { get; set; }
    public string[] Warnings { get; set; }
    public Dictionary<string, string[]> NodeErrors { get; set; }
    public Dictionary<string, string[]> ConnectionErrors { get; set; }
}

// Node Types
public static class WorkflowNodeTypes
{
    public const string Start = "start";
    public const string End = "end";
    public const string Condition = "condition";
    public const string Action = "action";
    public const string Timer = "timer";
    public const string Email = "email";
    public const string Notification = "notification";
    public const string Assignment = "assignment";
    public const string Approval = "approval";
    public const string Integration = "integration";
    public const string Transform = "transform";
    public const string Script = "script";
    public const string ApiCall = "api_call";
    public const string Database = "database";
    public const string Decision = "decision";
    public const string Parallel = "parallel";
    public const string Merge = "merge";
    public const string Delay = "delay";
    public const string Loop = "loop";
}

// Execution Status
public static class WorkflowExecutionStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Paused = "paused";
    public const string Timeout = "timeout";
}

// Step Status
public static class WorkflowStepStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
    public const string Timeout = "timeout";
}

// Condition Operators
public static class WorkflowConditionOperators
{
    public const string Equals = "equals";
    public const string NotEquals = "not_equals";
    public const string GreaterThan = "greater_than";
    public const string LessThan = "less_than";
    public const string GreaterThanOrEqual = "greater_than_or_equal";
    public const string LessThanOrEqual = "less_than_or_equal";
    public const string Contains = "contains";
    public const string NotContains = "not_contains";
    public const string StartsWith = "starts_with";
    public const string EndsWith = "ends_with";
    public const string IsEmpty = "is_empty";
    public const string IsNotEmpty = "is_not_empty";
    public const string In = "in";
    public const string NotIn = "not_in";
    public const string Regex = "regex";
}

// Action Types
public static class WorkflowActionTypes
{
    public const string SendEmail = "send_email";
    public const string SendNotification = "send_notification";
    public const string AssignTicket = "assign_ticket";
    public const string UpdateTicket = "update_ticket";
    public const string CreateTicket = "create_ticket";
    public const string CloseTicket = "close_ticket";
    public const string ReopenTicket = "reopen_ticket";
    public const string SetVariable = "set_variable";
    public const string CallApi = "call_api";
    public const string ExecuteScript = "execute_script";
    public const string TransformData = "transform_data";
    public const string LogMessage = "log_message";
    public const string SetPriority = "set_priority";
    public const string SetStatus = "set_status";
    public const string AddComment = "add_comment";
    public const string AddAttachment = "add_attachment";
    public const string CreateTask = "create_task";
    public const string ScheduleMeeting = "schedule_meeting";
    public const string TriggerWebhook = "trigger_webhook";
}
