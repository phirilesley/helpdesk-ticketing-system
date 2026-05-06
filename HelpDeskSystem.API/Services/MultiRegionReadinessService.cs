using System.Diagnostics;
using System.Text.Json;
using HelpDeskSystem.API.Setup;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Services;

public interface IMultiRegionReadinessService
{
    Task<TenantRegionPolicy> UpsertPolicyAsync(UpsertTenantRegionPolicyRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RegionSyntheticCheck>> RunSyntheticChecksAsync(int? tenantId = null, CancellationToken cancellationToken = default);
}

public class UpsertTenantRegionPolicyRequest
{
    public int TenantId { get; set; }
    public string PrimaryRegion { get; set; } = "af-south";
    public string SecondaryRegion { get; set; } = "eu-west";
    public TenantFailoverMode FailoverMode { get; set; } = TenantFailoverMode.Manual;
    public bool AutoFailbackEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public string RunbookUrl { get; set; } = string.Empty;
    public string MonitoringConfigJson { get; set; } = "{}";
}

public class MultiRegionReadinessService : IMultiRegionReadinessService
{
    private readonly HelpDeskDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MultiRegionOptions _options;
    private readonly ILogger<MultiRegionReadinessService> _logger;

    public MultiRegionReadinessService(
        HelpDeskDbContext context,
        IHttpClientFactory httpClientFactory,
        MultiRegionOptions options,
        ILogger<MultiRegionReadinessService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<TenantRegionPolicy> UpsertPolicyAsync(UpsertTenantRegionPolicyRequest request, CancellationToken cancellationToken = default)
    {
        var policy = await _context.TenantRegionPolicies
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken)
            ?? new TenantRegionPolicy
            {
                TenantId = request.TenantId
            };

        policy.PrimaryRegion = request.PrimaryRegion.Trim().ToLowerInvariant();
        policy.SecondaryRegion = request.SecondaryRegion.Trim().ToLowerInvariant();
        policy.FailoverMode = request.FailoverMode;
        policy.AutoFailbackEnabled = request.AutoFailbackEnabled;
        policy.IsActive = request.IsActive;
        policy.RunbookUrl = request.RunbookUrl.Trim();
        policy.MonitoringConfigJson = string.IsNullOrWhiteSpace(request.MonitoringConfigJson) ? "{}" : request.MonitoringConfigJson.Trim();
        policy.UpdatedAtUtc = DateTime.UtcNow;

        if (policy.Id == 0)
            _context.TenantRegionPolicies.Add(policy);

        await _context.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<IReadOnlyCollection<RegionSyntheticCheck>> RunSyntheticChecksAsync(int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var policies = await _context.TenantRegionPolicies
            .Where(x => x.IsActive && !x.IsDeleted && (!tenantId.HasValue || x.TenantId == tenantId.Value))
            .ToListAsync(cancellationToken);

        var checks = new List<RegionSyntheticCheck>();
        foreach (var policy in policies)
        {
            checks.Add(await CheckDatabaseAsync(policy.TenantId, policy.PrimaryRegion, cancellationToken));
            checks.Add(await CheckHealthEndpointAsync(policy.TenantId, policy.PrimaryRegion, cancellationToken));

            var urls = ParseSyntheticUrls(policy.MonitoringConfigJson);
            foreach (var url in urls)
            {
                checks.Add(await CheckExternalEndpointAsync(policy.TenantId, policy.PrimaryRegion, url, cancellationToken));
            }
        }

        if (checks.Count > 0)
        {
            _context.RegionSyntheticChecks.AddRange(checks);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return checks;
    }

    private async Task<RegionSyntheticCheck> CheckDatabaseAsync(int tenantId, string region, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var connected = await _context.Database.CanConnectAsync(cancellationToken);
            sw.Stop();
            return new RegionSyntheticCheck
            {
                TenantId = tenantId,
                Region = region,
                CheckType = "database_connectivity",
                Passed = connected,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Detail = connected ? "Database connectivity OK." : "Database connectivity failed.",
                CheckedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RegionSyntheticCheck
            {
                TenantId = tenantId,
                Region = region,
                CheckType = "database_connectivity",
                Passed = false,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Detail = ex.Message,
                CheckedAtUtc = DateTime.UtcNow
            };
        }
    }

    private async Task<RegionSyntheticCheck> CheckHealthEndpointAsync(int tenantId, string region, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(2, _options.SyntheticCheckTimeoutSeconds)));
            var healthReadyUrl = string.IsNullOrWhiteSpace(_options.LocalHealthReadyUrl)
                ? "http://localhost:5229/health/ready"
                : _options.LocalHealthReadyUrl;
            using var response = await client.GetAsync(healthReadyUrl, cts.Token);
            sw.Stop();

            return new RegionSyntheticCheck
            {
                TenantId = tenantId,
                Region = region,
                CheckType = "local_health_ready",
                Passed = response.IsSuccessStatusCode,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Detail = $"{healthReadyUrl} => HTTP {(int)response.StatusCode}",
                CheckedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RegionSyntheticCheck
            {
                TenantId = tenantId,
                Region = region,
                CheckType = "local_health_ready",
                Passed = false,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Detail = ex.Message,
                CheckedAtUtc = DateTime.UtcNow
            };
        }
    }

    private async Task<RegionSyntheticCheck> CheckExternalEndpointAsync(int tenantId, string region, string url, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(2, _options.SyntheticCheckTimeoutSeconds)));
            using var response = await client.GetAsync(url, cts.Token);
            sw.Stop();

            return new RegionSyntheticCheck
            {
                TenantId = tenantId,
                Region = region,
                CheckType = "external_endpoint",
                Passed = response.IsSuccessStatusCode,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Detail = $"{url} => HTTP {(int)response.StatusCode}",
                CheckedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Synthetic external endpoint check failed for {Url}", url);
            return new RegionSyntheticCheck
            {
                TenantId = tenantId,
                Region = region,
                CheckType = "external_endpoint",
                Passed = false,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Detail = $"{url} => {ex.Message}",
                CheckedAtUtc = DateTime.UtcNow
            };
        }
    }

    private static IReadOnlyCollection<string> ParseSyntheticUrls(string monitoringConfigJson)
    {
        if (string.IsNullOrWhiteSpace(monitoringConfigJson))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(monitoringConfigJson);
            if (!doc.RootElement.TryGetProperty("syntheticUrls", out var urlsEl) || urlsEl.ValueKind != JsonValueKind.Array)
                return [];

            var urls = new List<string>();
            foreach (var item in urlsEl.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        urls.Add(value.Trim());
                }
            }

            return urls;
        }
        catch
        {
            return [];
        }
    }
}

public class MultiRegionSyntheticWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MultiRegionOptions _options;
    private readonly ILogger<MultiRegionSyntheticWorker> _logger;

    public MultiRegionSyntheticWorker(IServiceProvider serviceProvider, MultiRegionOptions options, ILogger<MultiRegionSyntheticWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableSyntheticBackgroundChecks)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IMultiRegionReadinessService>();
                await service.RunSyntheticChecksAsync(null, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Multi-region synthetic background check iteration failed.");
            }

            var minutes = _options.SyntheticCheckIntervalMinutes <= 0 ? 10 : _options.SyntheticCheckIntervalMinutes;
            await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
        }
    }
}

public static class MultiRegionReadinessExtensions
{
    public static IServiceCollection AddMultiRegionReadiness(this IServiceCollection services)
    {
        services.AddScoped<IMultiRegionReadinessService, MultiRegionReadinessService>();
        services.AddHostedService<MultiRegionSyntheticWorker>();
        return services;
    }
}
