using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HelpDeskSystem.API.Setup;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace HelpDeskSystem.API.Services;

public interface IOutboundMessagingService
{
    Task<OutboundChannelMessage> QueueMessageAsync(QueueOutboundMessageRequest request, CancellationToken cancellationToken = default);
    Task ProcessQueueAsync(CancellationToken cancellationToken = default);
    Task RecordReceiptAsync(int connectorId, string providerMessageId, DeliveryReceiptStatus status, string payloadJson, CancellationToken cancellationToken = default);
}

public class QueueOutboundMessageRequest
{
    public int ConnectorId { get; set; }
    public int TicketId { get; set; }
    public int RequestedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public int? MaxAttempts { get; set; }
}

public class OutboundSendResult
{
    public bool Success { get; set; }
    public string ProviderMessageId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public interface IOutboundChannelAdapter
{
    string ProviderKey { get; }
    Task<OutboundSendResult> SendAsync(OmnichannelConnector connector, OutboundChannelMessage message, CancellationToken cancellationToken);
}

public class OutboundMessagingService : IOutboundMessagingService
{
    private readonly HelpDeskDbContext _context;
    private readonly OutboundMessagingOptions _options;
    private readonly IDistributedCache _cache;
    private readonly ILogger<OutboundMessagingService> _logger;
    private readonly Dictionary<string, IOutboundChannelAdapter> _adapters;

    public OutboundMessagingService(
        HelpDeskDbContext context,
        OutboundMessagingOptions options,
        IDistributedCache cache,
        ILogger<OutboundMessagingService> logger,
        IEnumerable<IOutboundChannelAdapter> adapters)
    {
        _context = context;
        _options = options;
        _cache = cache;
        _logger = logger;
        _adapters = adapters.ToDictionary(x => x.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<OutboundChannelMessage> QueueMessageAsync(QueueOutboundMessageRequest request, CancellationToken cancellationToken = default)
    {
        var connector = await _context.OmnichannelConnectors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ConnectorId && !x.IsDeleted, cancellationToken);
        if (connector == null)
            throw new InvalidOperationException("Connector not found.");
        if (connector.Status != ConnectorStatus.Enabled)
            throw new InvalidOperationException("Connector is not enabled.");

        var normalizedKey = BuildIdempotencyKey(request, connector.TenantId);
        var existing = await _context.OutboundChannelMessages
            .FirstOrDefaultAsync(x => x.ConnectorId == request.ConnectorId && x.IdempotencyKey == normalizedKey && !x.IsDeleted, cancellationToken);
        if (existing != null)
            return existing;

        var message = new OutboundChannelMessage
        {
            TenantId = connector.TenantId,
            ConnectorId = connector.Id,
            TicketId = request.TicketId,
            RequestedByUserId = request.RequestedByUserId,
            IdempotencyKey = normalizedKey,
            RecipientAddress = request.RecipientAddress.Trim(),
            Subject = request.Subject.Trim(),
            Content = request.Content.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson.Trim(),
            Status = OutboundMessageStatus.Pending,
            AttemptCount = 0,
            MaxAttempts = request.MaxAttempts.GetValueOrDefault(_options.MaxAttemptsDefault <= 0 ? 5 : _options.MaxAttemptsDefault),
            NextAttemptAtUtc = DateTime.UtcNow,
            PartitionKey = connector.TenantId
        };

        _context.OutboundChannelMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        await CacheMessageStateAsync(message, cancellationToken);
        return message;
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        var now = DateTime.UtcNow;
        var partitionCount = _options.PartitionCount <= 0 ? 1 : _options.PartitionCount;
        var partitionId = Math.Clamp(_options.PartitionId, 0, partitionCount - 1);
        var batchSize = _options.BatchSize <= 0 ? 50 : _options.BatchSize;

        var queue = await _context.OutboundChannelMessages
            .Where(x =>
                !x.IsDeleted &&
                (x.Status == OutboundMessageStatus.Pending || x.Status == OutboundMessageStatus.Retrying) &&
                x.NextAttemptAtUtc <= now)
            .Where(x => Math.Abs(x.PartitionKey % partitionCount) == partitionId)
            .OrderBy(x => x.NextAttemptAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in queue)
        {
            await ProcessSingleMessageAsync(message, cancellationToken);
        }
    }

    public async Task RecordReceiptAsync(int connectorId, string providerMessageId, DeliveryReceiptStatus status, string payloadJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId))
            return;

        var message = await _context.OutboundChannelMessages
            .FirstOrDefaultAsync(x => x.ConnectorId == connectorId && x.ProviderMessageId == providerMessageId && !x.IsDeleted, cancellationToken);
        if (message == null)
            return;

        var receipt = new OutboundDeliveryReceipt
        {
            TenantId = message.TenantId,
            OutboundChannelMessageId = message.Id,
            ProviderMessageId = providerMessageId.Trim(),
            Status = status,
            RawPayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson.Trim(),
            ReceivedAtUtc = DateTime.UtcNow
        };
        _context.OutboundDeliveryReceipts.Add(receipt);

        if (status == DeliveryReceiptStatus.Delivered || status == DeliveryReceiptStatus.Read)
        {
            message.Status = OutboundMessageStatus.Delivered;
            message.UpdatedAtUtc = DateTime.UtcNow;
        }
        else if (status == DeliveryReceiptStatus.Failed)
        {
            message.Status = OutboundMessageStatus.Failed;
            message.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await CacheMessageStateAsync(message, cancellationToken);
    }

    private async Task ProcessSingleMessageAsync(OutboundChannelMessage message, CancellationToken cancellationToken)
    {
        var connector = await _context.OmnichannelConnectors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == message.ConnectorId && !x.IsDeleted, cancellationToken);
        if (connector == null || connector.Status != ConnectorStatus.Enabled)
        {
            await MarkFailedAsync(message, "Connector is missing or disabled.", cancellationToken);
            return;
        }

        if (!_adapters.TryGetValue(connector.ProviderKey, out var adapter))
        {
            await MarkFailedAsync(message, $"No outbound adapter registered for provider '{connector.ProviderKey}'.", cancellationToken);
            return;
        }

        message.AttemptCount += 1;
        message.Status = OutboundMessageStatus.Sending;
        message.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var send = await adapter.SendAsync(connector, message, cancellationToken);
            if (send.Success)
            {
                message.Status = OutboundMessageStatus.Delivered;
                message.ProviderMessageId = send.ProviderMessageId.Trim();
                message.SentAtUtc = DateTime.UtcNow;
                message.LastError = string.Empty;
                message.UpdatedAtUtc = DateTime.UtcNow;

                _context.OutboundDeliveryReceipts.Add(new OutboundDeliveryReceipt
                {
                    TenantId = message.TenantId,
                    OutboundChannelMessageId = message.Id,
                    ProviderMessageId = message.ProviderMessageId,
                    Status = DeliveryReceiptStatus.Sent,
                    RawPayloadJson = "{}",
                    ReceivedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                ApplyRetryPolicy(message, send.Error);
            }
        }
        catch (Exception ex)
        {
            ApplyRetryPolicy(message, ex.Message);
            _logger.LogWarning(ex, "Outbound send failed for message {MessageId}", message.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await CacheMessageStateAsync(message, cancellationToken);
    }

    private async Task MarkFailedAsync(OutboundChannelMessage message, string error, CancellationToken cancellationToken)
    {
        message.Status = OutboundMessageStatus.Failed;
        message.LastError = error;
        message.UpdatedAtUtc = DateTime.UtcNow;
        _context.OutboundDeliveryReceipts.Add(new OutboundDeliveryReceipt
        {
            TenantId = message.TenantId,
            OutboundChannelMessageId = message.Id,
            ProviderMessageId = message.ProviderMessageId,
            Status = DeliveryReceiptStatus.Failed,
            RawPayloadJson = JsonSerializer.Serialize(new { error }),
            ReceivedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        await CacheMessageStateAsync(message, cancellationToken);
    }

    private static string BuildIdempotencyKey(QueueOutboundMessageRequest request, int tenantId)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return request.IdempotencyKey.Trim();

        var raw = $"{tenantId}|{request.ConnectorId}|{request.TicketId}|{request.RecipientAddress}|{request.Subject}|{request.Content}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static void ApplyRetryPolicy(OutboundChannelMessage message, string error)
    {
        message.LastError = error;
        if (message.AttemptCount >= Math.Max(1, message.MaxAttempts))
        {
            message.Status = OutboundMessageStatus.Failed;
            message.NextAttemptAtUtc = null;
            message.UpdatedAtUtc = DateTime.UtcNow;
            return;
        }

        message.Status = OutboundMessageStatus.Retrying;
        var backoffSeconds = Math.Min(3600, (int)(Math.Pow(2, Math.Min(10, message.AttemptCount)) * 15));
        var jitter = Random.Shared.Next(0, 10);
        message.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(backoffSeconds + jitter);
        message.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task CacheMessageStateAsync(OutboundChannelMessage message, CancellationToken cancellationToken)
    {
        var cacheKey = $"outbound:message:{message.Id}";
        var cacheValue = JsonSerializer.Serialize(new
        {
            message.Id,
            message.ConnectorId,
            message.IdempotencyKey,
            Status = message.Status.ToString(),
            message.AttemptCount,
            message.ProviderMessageId,
            message.LastError,
            message.NextAttemptAtUtc
        });
        await _cache.SetStringAsync(
            cacheKey,
            cacheValue,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            },
            cancellationToken);
    }
}

public class OutboundMessageWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboundMessagingOptions _options;
    private readonly ILogger<OutboundMessageWorker> _logger;

    public OutboundMessageWorker(IServiceProvider serviceProvider, OutboundMessagingOptions options, ILogger<OutboundMessageWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outbound = scope.ServiceProvider.GetRequiredService<IOutboundMessagingService>();
                await outbound.ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbound message worker iteration failed.");
            }

            var delaySeconds = _options.PollIntervalSeconds <= 0 ? 10 : _options.PollIntervalSeconds;
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }
}

public static class OutboundMessagingExtensions
{
    public static IServiceCollection AddOutboundMessaging(this IServiceCollection services)
    {
        services.AddScoped<IOutboundMessagingService, OutboundMessagingService>();
        services.AddScoped<IOutboundChannelAdapter, SlackOutboundAdapter>();
        services.AddScoped<IOutboundChannelAdapter, MetaWhatsappOutboundAdapter>();
        services.AddScoped<IOutboundChannelAdapter, TwilioOutboundAdapter>();
        services.AddHostedService<OutboundMessageWorker>();
        return services;
    }
}

internal static class ConnectorConfigReader
{
    public static string GetValue(string configJson, string key)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(configJson);
            if (!doc.RootElement.TryGetProperty(key, out var value))
                return string.Empty;
            return value.GetString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

public class SlackOutboundAdapter : IOutboundChannelAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    public string ProviderKey => "slack";

    public SlackOutboundAdapter(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OutboundSendResult> SendAsync(OmnichannelConnector connector, OutboundChannelMessage message, CancellationToken cancellationToken)
    {
        var botToken = ConnectorConfigReader.GetValue(connector.ConfigJson, "botToken");
        var defaultChannel = ConnectorConfigReader.GetValue(connector.ConfigJson, "defaultChannel");
        var channel = string.IsNullOrWhiteSpace(message.RecipientAddress) ? defaultChannel : message.RecipientAddress;

        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(channel))
            return new OutboundSendResult { Success = false, Error = "Slack config missing botToken/defaultChannel." };

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                channel,
                text = string.IsNullOrWhiteSpace(message.Subject) ? message.Content : $"*{message.Subject}*\n{message.Content}"
            }), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", botToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new OutboundSendResult { Success = false, Error = $"Slack HTTP {(int)response.StatusCode}: {raw}" };

        using var doc = JsonDocument.Parse(raw);
        var ok = doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.ValueKind == JsonValueKind.True;
        if (!ok)
            return new OutboundSendResult { Success = false, Error = $"Slack API failed: {raw}" };

        var ts = doc.RootElement.TryGetProperty("ts", out var tsEl) ? tsEl.GetString() ?? string.Empty : string.Empty;
        return new OutboundSendResult { Success = true, ProviderMessageId = string.IsNullOrWhiteSpace(ts) ? Guid.NewGuid().ToString("N") : ts };
    }
}

public class MetaWhatsappOutboundAdapter : IOutboundChannelAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    public string ProviderKey => "meta_whatsapp";

    public MetaWhatsappOutboundAdapter(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OutboundSendResult> SendAsync(OmnichannelConnector connector, OutboundChannelMessage message, CancellationToken cancellationToken)
    {
        var phoneNumberId = ConnectorConfigReader.GetValue(connector.ConfigJson, "phoneNumberId");
        var accessToken = ConnectorConfigReader.GetValue(connector.ConfigJson, "accessToken");
        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken))
            return new OutboundSendResult { Success = false, Error = "Meta WhatsApp config missing phoneNumberId/accessToken." };

        var endpoint = $"https://graph.facebook.com/v19.0/{phoneNumberId}/messages";
        var payload = new
        {
            messaging_product = "whatsapp",
            to = message.RecipientAddress,
            type = "text",
            text = new
            {
                body = string.IsNullOrWhiteSpace(message.Subject) ? message.Content : $"{message.Subject}\n{message.Content}"
            }
        };

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new OutboundSendResult { Success = false, Error = $"Meta HTTP {(int)response.StatusCode}: {raw}" };

        using var doc = JsonDocument.Parse(raw);
        var messageId = doc.RootElement.TryGetProperty("messages", out var messagesEl)
            && messagesEl.ValueKind == JsonValueKind.Array
            && messagesEl.GetArrayLength() > 0
            && messagesEl[0].TryGetProperty("id", out var idEl)
                ? idEl.GetString() ?? string.Empty
                : string.Empty;

        return new OutboundSendResult
        {
            Success = true,
            ProviderMessageId = string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString("N") : messageId
        };
    }
}

public class TwilioOutboundAdapter : IOutboundChannelAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    public string ProviderKey => "twilio";

    public TwilioOutboundAdapter(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OutboundSendResult> SendAsync(OmnichannelConnector connector, OutboundChannelMessage message, CancellationToken cancellationToken)
    {
        var accountSid = ConnectorConfigReader.GetValue(connector.ConfigJson, "accountSid");
        var authToken = ConnectorConfigReader.GetValue(connector.ConfigJson, "authToken");
        var from = ConnectorConfigReader.GetValue(connector.ConfigJson, "fromNumber");
        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(from))
            return new OutboundSendResult { Success = false, Error = "Twilio config missing accountSid/authToken/fromNumber." };

        var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
        var form = new Dictionary<string, string>
        {
            ["From"] = from,
            ["To"] = message.RecipientAddress,
            ["Body"] = string.IsNullOrWhiteSpace(message.Subject) ? message.Content : $"{message.Subject}\n{message.Content}"
        };

        var client = _httpClientFactory.CreateClient();
        var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", creds);

        using var response = await client.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new OutboundSendResult { Success = false, Error = $"Twilio HTTP {(int)response.StatusCode}: {raw}" };

        using var doc = JsonDocument.Parse(raw);
        var sid = doc.RootElement.TryGetProperty("sid", out var sidEl) ? sidEl.GetString() ?? string.Empty : string.Empty;
        return new OutboundSendResult { Success = true, ProviderMessageId = string.IsNullOrWhiteSpace(sid) ? Guid.NewGuid().ToString("N") : sid };
    }
}
