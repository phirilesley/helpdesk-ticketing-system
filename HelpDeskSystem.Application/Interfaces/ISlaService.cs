using HelpDeskSystem.Application.DTOs.Sla;

namespace HelpDeskSystem.Application.Interfaces;

public interface ISlaService
{
    Task<SlaResult> CalculateSlaForTicketAsync(int ticketId);
    Task CheckAndHandleSlaBreachesAsync();
    Task CheckEscalationPoliciesAsync();
}
