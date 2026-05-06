using HelpDeskSystem.API.Security;
using HelpDeskSystem.API.Services;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/regions")]
public class RegionReadinessController : ControllerBase
{
    private readonly HelpDeskDbContext _context;
    private readonly IMultiRegionReadinessService _multiRegionReadinessService;

    public RegionReadinessController(HelpDeskDbContext context, IMultiRegionReadinessService multiRegionReadinessService)
    {
        _context = context;
        _multiRegionReadinessService = multiRegionReadinessService;
    }

    [HttpGet("policies")]
    public async Task<ActionResult<IEnumerable<TenantRegionPolicy>>> GetPolicies([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var policies = await _context.TenantRegionPolicies
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(policies);
    }

    [HttpPost("policies")]
    public async Task<ActionResult<TenantRegionPolicy>> UpsertPolicy([FromBody] UpsertTenantRegionPolicyApiRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId) ?? request.TenantId;
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var policy = await _multiRegionReadinessService.UpsertPolicyAsync(new UpsertTenantRegionPolicyRequest
        {
            TenantId = resolvedTenantId.Value,
            PrimaryRegion = request.PrimaryRegion,
            SecondaryRegion = request.SecondaryRegion,
            FailoverMode = request.FailoverMode,
            AutoFailbackEnabled = request.AutoFailbackEnabled,
            IsActive = request.IsActive,
            RunbookUrl = request.RunbookUrl,
            MonitoringConfigJson = request.MonitoringConfigJson
        }, HttpContext.RequestAborted);

        return Ok(policy);
    }

    [HttpPost("synthetic/run")]
    public async Task<ActionResult<IEnumerable<RegionSyntheticCheck>>> RunSyntheticChecks([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var checks = await _multiRegionReadinessService.RunSyntheticChecksAsync(resolvedTenantId.Value, HttpContext.RequestAborted);
        return Ok(checks);
    }

    [HttpGet("synthetic/checks")]
    public async Task<ActionResult<IEnumerable<RegionSyntheticCheck>>> GetRecentChecks([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var checks = await _context.RegionSyntheticChecks
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CheckedAtUtc)
            .Take(200)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(checks);
    }

    [HttpGet("runbook-template")]
    public ActionResult<object> GetRunbookTemplate()
    {
        return Ok(new
        {
            title = "Multi-Region Failover Runbook",
            steps = new[]
            {
                "1. Verify synthetic checks are failing in primary region for at least 5 consecutive intervals.",
                "2. Confirm dependency impact: outbound messaging queue latency, webhook failures, and health endpoint degradation.",
                "3. Switch active region routing to secondary region and freeze non-critical deployments.",
                "4. Validate queue processors in secondary region and confirm message delivery receipts recover.",
                "5. Run post-failover smoke checks for ticket CRUD, inbound/outbound messaging, and authentication.",
                "6. Begin incident communications and attach dashboard snapshots + timestamps.",
                "7. After primary recovers, evaluate auto-failback policy and execute controlled return."
            }
        });
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }
}

public class UpsertTenantRegionPolicyApiRequest
{
    public int? TenantId { get; set; }
    public string PrimaryRegion { get; set; } = "af-south";
    public string SecondaryRegion { get; set; } = "eu-west";
    public HelpDeskSystem.Domain.Enums.TenantFailoverMode FailoverMode { get; set; } = HelpDeskSystem.Domain.Enums.TenantFailoverMode.Manual;
    public bool AutoFailbackEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public string RunbookUrl { get; set; } = string.Empty;
    public string MonitoringConfigJson { get; set; } = "{}";
}
