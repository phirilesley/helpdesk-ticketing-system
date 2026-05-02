using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class AutomationRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public AutomationTriggerType TriggerType { get; set; }
    public int? ConditionCategoryId { get; set; }
    public int? ConditionPriorityId { get; set; }
    public TicketStatus? ConditionStatus { get; set; }
    public AutomationActionType ActionType { get; set; }
    public string ActionValue { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; } = 100;
    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }
}
