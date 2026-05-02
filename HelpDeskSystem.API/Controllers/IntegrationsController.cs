using System.Security.Cryptography;
using System.Text;
using HelpDeskSystem.API.Security;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public IntegrationsController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet("apps")]
    public async Task<ActionResult<IEnumerable<IntegrationApp>>> GetApps([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var apps = await _context.IntegrationApps
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.Name)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(apps);
    }

    [HttpPost("apps")]
    public async Task<ActionResult<IntegrationApp>> UpsertApp([FromBody] UpsertIntegrationAppRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        IntegrationApp entity;
        if (request.Id.HasValue)
        {
            entity = await _context.IntegrationApps
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new IntegrationApp { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new IntegrationApp { TenantId = resolvedTenantId.Value };
            _context.IntegrationApps.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Provider = request.Provider.Trim();
        entity.ConfigJson = request.ConfigJson.Trim();
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.IntegrationApps.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("marketplace/catalog")]
    public async Task<ActionResult<IEnumerable<MarketplaceApp>>> GetMarketplaceCatalog()
    {
        var apps = await _context.MarketplaceApps
            .Where(x => x.IsActive && x.IsPublic && !x.IsDeleted)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(apps);
    }

    [HttpPost("marketplace/catalog")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<MarketplaceApp>> UpsertMarketplaceCatalogItem([FromBody] UpsertMarketplaceAppRequest request)
    {
        MarketplaceApp entity;
        if (request.Id.HasValue)
        {
            entity = await _context.MarketplaceApps
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new MarketplaceApp();
        }
        else
        {
            entity = new MarketplaceApp();
            _context.MarketplaceApps.Add(entity);
        }

        entity.AppKey = request.AppKey.Trim().ToLowerInvariant();
        entity.Name = request.Name.Trim();
        entity.Category = request.Category.Trim().ToLowerInvariant();
        entity.Provider = request.Provider.Trim().ToLowerInvariant();
        entity.ManifestJson = request.ManifestJson.Trim();
        entity.MinPlanName = request.MinPlanName.Trim();
        entity.IsPublic = request.IsPublic;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.MarketplaceApps.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("marketplace/installs")]
    public async Task<ActionResult<IEnumerable<TenantAppInstall>>> GetMarketplaceInstalls([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var installs = await _context.TenantAppInstalls
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(installs);
    }

    [HttpPost("marketplace/installs")]
    public async Task<ActionResult<TenantAppInstall>> UpsertMarketplaceInstall([FromBody] UpsertTenantAppInstallRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var app = await _context.MarketplaceApps
            .FirstOrDefaultAsync(x => x.Id == request.MarketplaceAppId && x.IsActive && !x.IsDeleted, HttpContext.RequestAborted);
        if (app == null)
            return BadRequest(new { error = "Marketplace app not found or inactive." });

        TenantAppInstall entity;
        if (request.Id.HasValue)
        {
            entity = await _context.TenantAppInstalls
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new TenantAppInstall { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new TenantAppInstall { TenantId = resolvedTenantId.Value };
            _context.TenantAppInstalls.Add(entity);
        }

        entity.MarketplaceAppId = app.Id;
        entity.Status = request.Status;
        entity.InstalledVersion = string.IsNullOrWhiteSpace(request.InstalledVersion) ? "1.0.0" : request.InstalledVersion.Trim();
        entity.ConfigJson = request.ConfigJson.Trim();
        entity.LastError = request.LastError.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.TenantAppInstalls.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("webhooks")]
    public async Task<ActionResult<IEnumerable<WebhookSubscription>>> GetWebhooks([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var hooks = await _context.WebhookSubscriptions
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(hooks);
    }

    [HttpPost("webhooks")]
    public async Task<ActionResult<WebhookSubscription>> UpsertWebhook([FromBody] UpsertWebhookRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        WebhookSubscription entity;
        if (request.Id.HasValue)
        {
            entity = await _context.WebhookSubscriptions
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new WebhookSubscription { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new WebhookSubscription { TenantId = resolvedTenantId.Value };
            _context.WebhookSubscriptions.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.EndpointUrl = request.EndpointUrl.Trim();
        entity.EventFiltersJson = request.EventFiltersJson.Trim();
        entity.IsEnabled = request.IsEnabled;
        entity.MaxAttempts = request.MaxAttempts <= 0 ? 5 : request.MaxAttempts;
        entity.RetryBackoffSeconds = request.RetryBackoffSeconds <= 0 ? 30 : request.RetryBackoffSeconds;
        entity.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 20 : request.TimeoutSeconds;
        if (!string.IsNullOrWhiteSpace(request.Secret))
            entity.SecretHash = HashSecret(request.Secret);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.WebhookSubscriptions.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }

    private static string HashSecret(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes);
    }
}

public class UpsertIntegrationAppRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
}

public class UpsertWebhookRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string EventFiltersJson { get; set; } = "[]";
    public string Secret { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int MaxAttempts { get; set; } = 5;
    public int RetryBackoffSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 20;
}

public class UpsertMarketplaceAppRequest
{
    public int? Id { get; set; }
    public string AppKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ManifestJson { get; set; } = "{}";
    public string MinPlanName { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class UpsertTenantAppInstallRequest
{
    public int? Id { get; set; }
    public int MarketplaceAppId { get; set; }
    public AppInstallStatus Status { get; set; } = AppInstallStatus.Installed;
    public string InstalledVersion { get; set; } = "1.0.0";
    public string ConfigJson { get; set; } = "{}";
    public string LastError { get; set; } = string.Empty;
}
