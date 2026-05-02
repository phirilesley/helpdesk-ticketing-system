namespace HelpDeskSystem.Application.Interfaces;

public interface IBusinessTimeService
{
    Task<DateTime> AddBusinessMinutesAsync(int tenantId, DateTime startUtc, int minutesToAdd, CancellationToken cancellationToken = default);
}
