using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Application.DTOs.Workflow;

public class AutomationRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public AutomationTriggerType TriggerType { get; set; }
    public int? ConditionCategoryId { get; set; }
    public int? ConditionPriorityId { get; set; }
    public TicketStatus? ConditionStatus { get; set; }
    public AutomationActionType ActionType { get; set; }
    public string ActionValue { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public bool IsActive { get; set; }
}
