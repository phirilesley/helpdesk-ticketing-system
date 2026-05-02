using HelpDeskSystem.API.Setup;
using HelpDeskSystem.Application.Interfaces;

namespace HelpDeskSystem.API.Jobs;

public class PurgeAuditLogsJob
{
    private readonly IAuditRetentionService _auditRetentionService;
    private readonly AuditRetentionOptions _options;

    public PurgeAuditLogsJob(
        IAuditRetentionService auditRetentionService,
        AuditRetentionOptions options)
    {
        _auditRetentionService = auditRetentionService;
        _options = options;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_options.RetentionDays <= 0)
            return;

        var cutoffUtc = DateTime.UtcNow.AddDays(-_options.RetentionDays);
        await _auditRetentionService.PurgeOlderThanAsync(cutoffUtc, cancellationToken);
    }
}
