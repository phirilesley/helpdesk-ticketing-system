using HelpDeskSystem.API.Security;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public BillingController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet("plans")]
    public async Task<ActionResult<IEnumerable<BillingPlan>>> GetPlans()
    {
        var plans = await _context.BillingPlans
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.MonthlyPriceUsd)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(plans);
    }

    [HttpPost("plans")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<BillingPlan>> UpsertPlan([FromBody] UpsertBillingPlanRequest request)
    {
        BillingPlan entity;
        if (request.Id.HasValue)
        {
            entity = await _context.BillingPlans
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new BillingPlan();
        }
        else
        {
            entity = new BillingPlan();
            _context.BillingPlans.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.MonthlyPriceUsd = request.MonthlyPriceUsd;
        entity.IncludedAgentSeats = request.IncludedAgentSeats;
        entity.IncludedTicketsPerMonth = request.IncludedTicketsPerMonth;
        entity.EntitlementsJson = request.EntitlementsJson.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.BillingPlans.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("subscriptions")]
    public async Task<ActionResult<IEnumerable<TenantSubscription>>> GetSubscriptions([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var subscriptions = await _context.TenantSubscriptions
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CurrentPeriodEndUtc)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(subscriptions);
    }

    [HttpPost("subscriptions")]
    public async Task<ActionResult<TenantSubscription>> UpsertSubscription([FromBody] UpsertSubscriptionRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        TenantSubscription entity;
        if (request.Id.HasValue)
        {
            entity = await _context.TenantSubscriptions
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new TenantSubscription { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new TenantSubscription { TenantId = resolvedTenantId.Value };
            _context.TenantSubscriptions.Add(entity);
        }

        entity.BillingPlanId = request.BillingPlanId;
        entity.Status = request.Status;
        entity.CurrentPeriodStartUtc = request.CurrentPeriodStartUtc;
        entity.CurrentPeriodEndUtc = request.CurrentPeriodEndUtc;
        entity.AutoRenew = request.AutoRenew;
        entity.EntitlementOverridesJson = request.EntitlementOverridesJson.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.TenantSubscriptions.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var invoices = await _context.Invoices
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(invoices);
    }

    [HttpPost("invoices")]
    public async Task<ActionResult<Invoice>> UpsertInvoice([FromBody] UpsertInvoiceRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        Invoice entity;
        if (request.Id.HasValue)
        {
            entity = await _context.Invoices
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new Invoice { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new Invoice
            {
                TenantId = resolvedTenantId.Value,
                InvoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber)
                    ? $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    : request.InvoiceNumber.Trim().ToUpperInvariant()
            };
            _context.Invoices.Add(entity);
        }

        entity.TenantSubscriptionId = request.TenantSubscriptionId;
        entity.PeriodStartUtc = request.PeriodStartUtc;
        entity.PeriodEndUtc = request.PeriodEndUtc;
        entity.SubtotalUsd = request.SubtotalUsd;
        entity.TaxUsd = request.TaxUsd;
        entity.TotalUsd = request.TotalUsd;
        entity.DueAtUtc = request.DueAtUtc;
        entity.PaidAtUtc = request.PaidAtUtc;
        entity.Status = request.Status;
        entity.LineItemsJson = request.LineItemsJson.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.Invoices.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("entitlements")]
    public async Task<ActionResult> GetEffectiveEntitlements([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var subscription = await _context.TenantSubscriptions
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CurrentPeriodEndUtc)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
        if (subscription == null)
            return NotFound(new { error = "No active subscription found for tenant." });

        var plan = await _context.BillingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == subscription.BillingPlanId && !x.IsDeleted, HttpContext.RequestAborted);
        if (plan == null)
            return NotFound(new { error = "Billing plan not found." });

        var merged = MergeJsonObjects(plan.EntitlementsJson, subscription.EntitlementOverridesJson);
        return Ok(new
        {
            subscription.Id,
            plan.Name,
            subscription.Status,
            entitlements = merged
        });
    }

    [HttpGet("usage")]
    public async Task<ActionResult<IEnumerable<UsageMeter>>> GetUsage(
        [FromQuery] int? tenantId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var start = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var end = to ?? DateTime.UtcNow.Date.AddDays(1);

        var usage = await _context.UsageMeters
            .Where(x => x.TenantId == resolvedTenantId.Value && x.UsageDateUtc >= start && x.UsageDateUtc < end && !x.IsDeleted)
            .OrderByDescending(x => x.UsageDateUtc)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(usage);
    }

    [HttpPost("usage")]
    public async Task<ActionResult<UsageMeter>> AddUsage([FromBody] AddUsageRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var meter = new UsageMeter
        {
            TenantId = resolvedTenantId.Value,
            MetricName = request.MetricName.Trim(),
            UsageDateUtc = request.UsageDateUtc == default ? DateTime.UtcNow : request.UsageDateUtc,
            Quantity = request.Quantity
        };

        _context.UsageMeters.Add(meter);
        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(meter);
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }

    private static Dictionary<string, object> MergeJsonObjects(string baseJson, string overrideJson)
    {
        var result = ParseObject(baseJson);
        var overrides = ParseObject(overrideJson);
        foreach (var kv in overrides)
        {
            result[kv.Key] = kv.Value;
        }

        return result;
    }

    private static Dictionary<string, object> ParseObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.TryGetInt64(out var i64) ? i64 : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.GetRawText()
                };
            }

            return dict;
        }
        catch
        {
            return [];
        }
    }
}

public class UpsertBillingPlanRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPriceUsd { get; set; }
    public int IncludedAgentSeats { get; set; }
    public int IncludedTicketsPerMonth { get; set; }
    public string EntitlementsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public class UpsertSubscriptionRequest
{
    public int? Id { get; set; }
    public int BillingPlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string EntitlementOverridesJson { get; set; } = "{}";
}

public class AddUsageRequest
{
    public string MetricName { get; set; } = string.Empty;
    public DateTime UsageDateUtc { get; set; }
    public decimal Quantity { get; set; }
}

public class UpsertInvoiceRequest
{
    public int? Id { get; set; }
    public int? TenantSubscriptionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public decimal SubtotalUsd { get; set; }
    public decimal TaxUsd { get; set; }
    public decimal TotalUsd { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string LineItemsJson { get; set; } = "[]";
}
