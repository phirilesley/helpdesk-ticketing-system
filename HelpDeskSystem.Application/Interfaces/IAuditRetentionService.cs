namespace HelpDeskSystem.Application.Interfaces;

public interface IAuditRetentionService
{
    Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
