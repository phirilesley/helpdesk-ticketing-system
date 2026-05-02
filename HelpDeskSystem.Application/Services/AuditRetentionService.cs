using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Application.Services;

public class AuditRetentionService : IAuditRetentionService
{
    private readonly HelpDeskDbContext _context;

    public AuditRetentionService(HelpDeskDbContext context)
    {
        _context = context;
    }

    public async Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var deletedCount = await _context.AuditLogs
            .Where(a => a.CreatedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = 0,
            Action = "AUDIT_RETENTION_PURGE_EXECUTED",
            EntityName = "AuditLog",
            EntityId = "*",
            NewValues = $"{{\"cutoffUtc\":\"{cutoffUtc:O}\",\"deletedCount\":{deletedCount}}}",
            IpAddress = "system"
        });

        await _context.SaveChangesAsync(cancellationToken);
        return deletedCount;
    }
}
