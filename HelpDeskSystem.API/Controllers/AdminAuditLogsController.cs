using HelpDeskSystem.API.Setup;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/audit-logs")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly HelpDeskDbContext _context;
    private readonly IAuditRetentionService _auditRetentionService;
    private readonly AuditRetentionOptions _options;

    public AdminAuditLogsController(
        HelpDeskDbContext context,
        IAuditRetentionService auditRetentionService,
        AuditRetentionOptions options)
    {
        _context = context;
        _auditRetentionService = auditRetentionService;
        _options = options;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogSearchResponse>> Search(
        [FromQuery] string? action = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action.Trim());

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(x => x.EntityName == entityName.Trim());

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(x => x.EntityId == entityId.Trim());

        if (fromUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= toUtc.Value);

        var totalCount = await query.CountAsync(HttpContext.RequestAborted);
        var records = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogItemDto
            {
                Id = x.Id,
                CreatedAtUtc = x.CreatedAtUtc,
                UserId = x.UserId,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                IpAddress = x.IpAddress
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(new AuditLogSearchResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = records
        });
    }

    [HttpPost("purge")]
    public async Task<ActionResult<AuditPurgeResponse>> Purge([FromBody] AuditPurgeRequest? request = null)
    {
        var retentionDays = request?.RetentionDays ?? _options.RetentionDays;
        if (retentionDays <= 0)
            return BadRequest("RetentionDays must be greater than zero.");

        var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);
        var deletedCount = await _auditRetentionService.PurgeOlderThanAsync(cutoffUtc, HttpContext.RequestAborted);

        return Ok(new AuditPurgeResponse
        {
            RetentionDays = retentionDays,
            CutoffUtc = cutoffUtc,
            DeletedCount = deletedCount
        });
    }
}

public class AuditLogSearchResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<AuditLogItemDto> Items { get; set; } = [];
}

public class AuditLogItemDto
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public class AuditPurgeRequest
{
    public int? RetentionDays { get; set; }
}

public class AuditPurgeResponse
{
    public int RetentionDays { get; set; }
    public DateTime CutoffUtc { get; set; }
    public int DeletedCount { get; set; }
}
