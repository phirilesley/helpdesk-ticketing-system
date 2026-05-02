namespace HelpDeskSystem.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        int? userId,
        string action,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
