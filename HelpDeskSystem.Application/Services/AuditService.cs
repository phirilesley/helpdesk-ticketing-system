using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;

namespace HelpDeskSystem.Application.Services;

public class AuditService : IAuditService
{
    private readonly HelpDeskDbContext _context;

    public AuditService(HelpDeskDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        int? userId,
        string action,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
            return;

        if (string.IsNullOrWhiteSpace(entityName))
            return;

        if (string.IsNullOrWhiteSpace(entityId))
            return;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId ?? 0,
            Action = action.Trim(),
            EntityName = entityName.Trim(),
            EntityId = entityId.Trim(),
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "system" : ipAddress.Trim()
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
