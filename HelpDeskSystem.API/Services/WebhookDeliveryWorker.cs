using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Services
{
    public interface IWebhookService
    {
        Task QueueWebhookAsync(int subscriptionId, string eventType, object payload, CancellationToken cancellationToken = default);
        Task ProcessWebhookQueueAsync(CancellationToken cancellationToken = default);
    }

    public class WebhookService : IWebhookService
    {
        private const int DefaultMaxAttempts = 5;
        private const int DefaultBackoffSeconds = 30;
        private const int DefaultTimeoutSeconds = 20;
        private readonly HelpDeskDbContext _context;
        private readonly ILogger<WebhookService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WebhookService(
            HelpDeskDbContext context,
            ILogger<WebhookService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task QueueWebhookAsync(int subscriptionId, string eventType, object payload, CancellationToken cancellationToken = default)
        {
            var subscription = await _context.WebhookSubscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.IsEnabled && !s.IsDeleted, cancellationToken);

            if (subscription == null)
                return;

            var filters = ParseEventFilters(subscription.EventFiltersJson);
            if (filters.Count > 0 && !filters.Contains(eventType, StringComparer.OrdinalIgnoreCase))
                return;

            var payloadJson = JsonSerializer.Serialize(payload);
            var delivery = new WebhookDelivery
            {
                SubscriptionId = subscriptionId,
                EventType = eventType,
                Payload = payloadJson,
                Signature = BuildPayloadSignature(payloadJson, subscription.SecretHash),
                AttemptCount = 0,
                NextAttemptAtUtc = DateTime.UtcNow,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.WebhookDeliveries.Add(delivery);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ProcessWebhookQueueAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _context.WebhookDeliveries
                .Include(d => d.Subscription)
                .Where(d => (d.Status == "Pending" || d.Status == "Retrying")
                            && d.NextAttemptAtUtc <= DateTime.UtcNow
                            && d.Subscription.IsEnabled
                            && !d.Subscription.IsDeleted)
                .OrderBy(d => d.CreatedAtUtc)
                .Take(20)
                .ToListAsync(cancellationToken);

            foreach (var delivery in pending)
            {
                await ProcessDeliveryAsync(delivery, cancellationToken);
            }
        }

        private async Task ProcessDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
        {
            delivery.AttemptCount++;
            delivery.Status = "Processing";
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var endpoint = delivery.Subscription.EndpointUrl;
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    delivery.Status = "Failed";
                    delivery.LastError = "Subscription endpoint URL is empty.";
                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-HelpDesk-Event", delivery.EventType);
                if (!string.IsNullOrWhiteSpace(delivery.Signature))
                    request.Headers.Add("X-HelpDesk-Signature", delivery.Signature);

                var client = _httpClientFactory.CreateClient();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var timeoutSeconds = delivery.Subscription.TimeoutSeconds <= 0 ? DefaultTimeoutSeconds : delivery.Subscription.TimeoutSeconds;
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var response = await client.SendAsync(request, timeoutCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    delivery.Status = "Delivered";
                    delivery.DeliveredAtUtc = DateTime.UtcNow;
                    delivery.LastError = null;
                    delivery.Subscription.LastDeliveryAtUtc = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                await SetRetryOrFailureAsync(delivery, $"HTTP {(int)response.StatusCode}: {body}", cancellationToken);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                await SetRetryOrFailureAsync(delivery, $"Timeout: {ex.Message}", cancellationToken);
            }
            catch (Exception ex)
            {
                await SetRetryOrFailureAsync(delivery, ex.Message, cancellationToken);
            }
        }

        private async Task SetRetryOrFailureAsync(WebhookDelivery delivery, string error, CancellationToken cancellationToken)
        {
            delivery.LastError = error;

            var maxAttempts = delivery.Subscription.MaxAttempts <= 0 ? DefaultMaxAttempts : delivery.Subscription.MaxAttempts;
            if (delivery.AttemptCount >= maxAttempts)
            {
                delivery.Status = "Failed";
                delivery.NextAttemptAtUtc = null;
            }
            else
            {
                delivery.Status = "Retrying";
                var baseDelay = delivery.Subscription.RetryBackoffSeconds <= 0 ? DefaultBackoffSeconds : delivery.Subscription.RetryBackoffSeconds;
                var exponential = Math.Pow(2, Math.Min(10, delivery.AttemptCount)) * baseDelay;
                var jitter = Random.Shared.Next(0, Math.Max(2, baseDelay));
                delivery.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(exponential + jitter);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Webhook delivery {DeliveryId} attempt {Attempt} status {Status}: {Error}", delivery.Id, delivery.AttemptCount, delivery.Status, error);
        }

        private static HashSet<string> ParseEventFilters(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];

            try
            {
                var values = JsonSerializer.Deserialize<List<string>>(json) ?? [];
                return values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return [];
            }
        }

        private static string? BuildPayloadSignature(string payload, string secretHash)
        {
            if (string.IsNullOrWhiteSpace(secretHash))
                return null;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretHash));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash);
        }
    }

    public class WebhookDeliveryWorker : BackgroundService
    {
        private readonly ILogger<WebhookDeliveryWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public WebhookDeliveryWorker(ILogger<WebhookDeliveryWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
                    await webhookService.ProcessWebhookQueueAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Webhook delivery worker iteration failed.");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    public static class WebhookServiceExtensions
    {
        public static IServiceCollection AddWebhookServices(this IServiceCollection services)
        {
            services.AddHostedService<WebhookDeliveryWorker>();
            services.AddScoped<IWebhookService, WebhookService>();
            return services;
        }
    }
}
