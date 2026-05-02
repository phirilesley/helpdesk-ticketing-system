using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Application.Interfaces;

public interface IAutomationRuleService
{
    Task ApplyRulesAsync(int ticketId, AutomationTriggerType trigger, int actorUserId, CancellationToken cancellationToken = default);
}
