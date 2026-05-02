using HelpDeskSystem.API.Security;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin,Agent")]
[Route("api/projects")]
public class ProjectPlanningController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public ProjectPlanningController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceProject>>> GetProjects([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var items = await _context.ServiceProjects
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderBy(x => x.Key)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<ServiceProject>> UpsertProject([FromBody] UpsertProjectRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        ServiceProject entity;
        if (request.Id.HasValue)
        {
            entity = await _context.ServiceProjects
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new ServiceProject { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new ServiceProject { TenantId = resolvedTenantId.Value };
            _context.ServiceProjects.Add(entity);
        }

        entity.Key = request.Key.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.WorkflowConfigJson = request.WorkflowConfigJson.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.ServiceProjects.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("dependencies")]
    public async Task<ActionResult<IEnumerable<IssueDependency>>> GetDependencies([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var deps = await _context.IssueDependencies
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(deps);
    }

    [HttpGet("dependencies/graph")]
    public async Task<ActionResult> GetDependencyGraph([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var deps = await _context.IssueDependencies
            .AsNoTracking()
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .Select(x => new { x.SourceTicketId, x.DependsOnTicketId, x.DependencyType })
            .ToListAsync(HttpContext.RequestAborted);

        var nodeIds = deps
            .SelectMany(x => new[] { x.SourceTicketId, x.DependsOnTicketId })
            .Distinct()
            .ToList();

        var ticketMap = await _context.Tickets
            .AsNoTracking()
            .Where(x => nodeIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.TicketNumber, Status = x.Status.ToString() })
            .ToDictionaryAsync(x => x.Id, HttpContext.RequestAborted);

        var nodes = nodeIds.Select(id => ticketMap.TryGetValue(id, out var t)
            ? new { id, label = t.TicketNumber, status = t.Status }
            : new { id, label = $"TKT-{id}", status = "Unknown" });

        var edges = deps.Select(x => new { from = x.SourceTicketId, to = x.DependsOnTicketId, type = x.DependencyType });
        return Ok(new { nodes, edges });
    }

    [HttpPost("dependencies")]
    public async Task<ActionResult<IssueDependency>> UpsertDependency([FromBody] UpsertIssueDependencyRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var entity = new IssueDependency
        {
            TenantId = resolvedTenantId.Value,
            SourceTicketId = request.SourceTicketId,
            DependsOnTicketId = request.DependsOnTicketId,
            DependencyType = request.DependencyType.Trim().ToLowerInvariant()
        };

        _context.IssueDependencies.Add(entity);
        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("releases")]
    public async Task<ActionResult<IEnumerable<ReleasePlan>>> GetReleases([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var releases = await _context.ReleasePlans
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.TargetDateUtc)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(releases);
    }

    [HttpPost("releases")]
    public async Task<ActionResult<ReleasePlan>> UpsertRelease([FromBody] UpsertReleaseRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        ReleasePlan entity;
        if (request.Id.HasValue)
        {
            entity = await _context.ReleasePlans
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new ReleasePlan { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new ReleasePlan { TenantId = resolvedTenantId.Value };
            _context.ReleasePlans.Add(entity);
        }

        entity.ProjectId = request.ProjectId;
        entity.Name = request.Name.Trim();
        entity.StartDateUtc = request.StartDateUtc;
        entity.TargetDateUtc = request.TargetDateUtc;
        entity.ScopeTicketIdsJson = request.ScopeTicketIdsJson.Trim();
        entity.DependencyGraphJson = request.DependencyGraphJson.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.ReleasePlans.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("agile-metrics")]
    public async Task<ActionResult<IEnumerable<SprintMetric>>> GetAgileMetrics([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var metrics = await _context.SprintMetrics
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.EndDateUtc)
            .Take(50)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(metrics);
    }

    [HttpGet("agile-metrics/summary")]
    public async Task<ActionResult> GetAgileSummary([FromQuery] int? tenantId = null, [FromQuery] int? projectId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var query = _context.SprintMetrics
            .AsNoTracking()
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted);

        if (projectId.HasValue)
            query = query.Where(x => x.ProjectId == projectId.Value);

        var items = await query
            .OrderByDescending(x => x.EndDateUtc)
            .Take(24)
            .ToListAsync(HttpContext.RequestAborted);

        if (items.Count == 0)
            return Ok(new { sprintCount = 0 });

        return Ok(new
        {
            sprintCount = items.Count,
            avgVelocity = Math.Round(items.Average(x => (double)x.Velocity), 2),
            avgBurnup = Math.Round(items.Average(x => (double)x.Burnup), 2),
            avgBurndown = Math.Round(items.Average(x => (double)x.Burndown), 2),
            avgCycleTimeHours = Math.Round(items.Average(x => (double)x.CycleTimeHours), 2),
            completionRate = Math.Round(items.Average(x =>
                x.PlannedIssues <= 0 ? 0 : (double)x.CompletedIssues / x.PlannedIssues) * 100, 2)
        });
    }

    [HttpPost("agile-metrics")]
    public async Task<ActionResult<SprintMetric>> UpsertAgileMetric([FromBody] UpsertSprintMetricRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var entity = new SprintMetric
        {
            TenantId = resolvedTenantId.Value,
            ProjectId = request.ProjectId,
            SprintName = request.SprintName.Trim(),
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            PlannedStoryPoints = request.PlannedStoryPoints,
            CompletedStoryPoints = request.CompletedStoryPoints,
            PlannedIssues = request.PlannedIssues,
            CompletedIssues = request.CompletedIssues,
            Velocity = request.Velocity > 0 ? request.Velocity : request.CompletedStoryPoints,
            Burnup = request.Burnup > 0 ? request.Burnup : request.CompletedStoryPoints,
            Burndown = request.Burndown > 0 ? request.Burndown : Math.Max(0, request.PlannedStoryPoints - request.CompletedStoryPoints),
            CycleTimeHours = request.CycleTimeHours > 0
                ? request.CycleTimeHours
                : request.CompletedIssues <= 0 ? 0 : (decimal)Math.Round((request.EndDateUtc - request.StartDateUtc).TotalHours / request.CompletedIssues, 2)
        };

        _context.SprintMetrics.Add(entity);
        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }
}

public class UpsertProjectRequest
{
    public int? Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WorkflowConfigJson { get; set; } = "{}";
}

public class UpsertIssueDependencyRequest
{
    public int SourceTicketId { get; set; }
    public int DependsOnTicketId { get; set; }
    public string DependencyType { get; set; } = "blocks";
}

public class UpsertReleaseRequest
{
    public int? Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime TargetDateUtc { get; set; }
    public string ScopeTicketIdsJson { get; set; } = "[]";
    public string DependencyGraphJson { get; set; } = "{}";
}

public class UpsertSprintMetricRequest
{
    public int ProjectId { get; set; }
    public string SprintName { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int PlannedStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int PlannedIssues { get; set; }
    public int CompletedIssues { get; set; }
    public decimal Velocity { get; set; }
    public decimal Burnup { get; set; }
    public decimal Burndown { get; set; }
    public decimal CycleTimeHours { get; set; }
}
