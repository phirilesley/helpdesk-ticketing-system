using HelpDeskSystem.Application.Interfaces;

namespace HelpDeskSystem.SLA.Jobs;

public class CheckSlaBreachesJob
{
    private readonly ISlaService _slaService;

    public CheckSlaBreachesJob(ISlaService slaService)
    {
        _slaService = slaService;
    }

    public async Task ExecuteAsync()
    {
        await _slaService.CheckAndHandleSlaBreachesAsync();
        await _slaService.CheckEscalationPoliciesAsync();
    }
}
