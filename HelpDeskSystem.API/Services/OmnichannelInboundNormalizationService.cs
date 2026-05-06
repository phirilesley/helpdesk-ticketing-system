using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HelpDeskSystem.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;

namespace HelpDeskSystem.API.Services;

public interface IOmnichannelInboundNormalizationService
{
    Task<InboundNormalizationResult> NormalizeAsync(
        OmnichannelConnector connector,
        HttpRequest request,
        JsonElement payload,
        string rawBody,
        CancellationToken cancellationToken = default);
}

public class InboundNormalizationResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; } = string.Empty;
    public string ExternalConversationId { get; set; } = string.Empty;
    public string ExternalMessageId { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? ExternalTimestampUtc { get; set; }
    public string NormalizedPayloadJson { get; set; } = "{}";
}

public class OmnichannelInboundNormalizationService : IOmnichannelInboundNormalizationService
{
    private readonly ILogger<OmnichannelInboundNormalizationService> _logger;

    public OmnichannelInboundNormalizationService(ILogger<OmnichannelInboundNormalizationService> logger)
    {
        _logger = logger;
    }

    public async Task<InboundNormalizationResult> NormalizeAsync(
        OmnichannelConnector connector,
        HttpRequest request,
        JsonElement payload,
        string rawBody,
        CancellationToken cancellationToken = default)
    {
        var provider = string.IsNullOrWhiteSpace(connector.ProviderKey)
            ? "generic"
            : connector.ProviderKey.Trim().ToLowerInvariant();

        return provider switch
        {
            "slack" => await NormalizeSlackAsync(connector, request, payload, rawBody, cancellationToken),
            "meta_whatsapp" or "whatsapp_meta" => await NormalizeMetaWhatsappAsync(connector, request, payload, rawBody, cancellationToken),
            "twilio_whatsapp" or "twilio_voice" or "twilio" => await NormalizeTwilioAsync(connector, request, payload, rawBody, cancellationToken),
            "webchat" or "chat_widget" => NormalizeWebchat(payload),
            "cti" => NormalizeCti(payload),
            _ => NormalizeGeneric(payload)
        };
    }

    private async Task<InboundNormalizationResult> NormalizeSlackAsync(
        OmnichannelConnector connector,
        HttpRequest request,
        JsonElement payload,
        string rawBody,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var signingSecret = GetConfigValue(connector.ConfigJson, "signingSecret");
        if (!string.IsNullOrWhiteSpace(signingSecret))
        {
            var timestamp = request.Headers["X-Slack-Request-Timestamp"].ToString();
            var signature = request.Headers["X-Slack-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature))
            {
                return Invalid("Missing Slack signature headers.");
            }

            if (!long.TryParse(timestamp, out var ts))
                return Invalid("Invalid Slack timestamp.");

            var delta = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts);
            if (delta > 60 * 5)
                return Invalid("Slack request timestamp is outside the 5-minute window.");

            var baseString = $"v0:{timestamp}:{rawBody}";
            var expected = "v0=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingSecret), Encoding.UTF8.GetBytes(baseString))).ToLowerInvariant();
            if (!SecureEquals(expected, signature))
                return Invalid("Invalid Slack signature.");
        }

        if (payload.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "url_verification")
        {
            return new InboundNormalizationResult
            {
                IsValid = false,
                Error = "Slack URL verification challenge request."
            };
        }

        var ev = payload.TryGetProperty("event", out var eventEl) ? eventEl : payload;
        var messageText = GetString(ev, "text");
        var channel = GetString(ev, "channel");
        var eventTs = GetString(ev, "ts");
        var user = GetString(ev, "user");

        return Valid(
            externalConversationId: channel,
            externalMessageId: eventTs,
            sender: user,
            subject: "Slack message",
            content: messageText,
            timestamp: TryParseUnixTimestamp(eventTs),
            normalized: new
            {
                provider = "slack",
                channel,
                messageId = eventTs,
                user,
                text = messageText
            });
    }

    private async Task<InboundNormalizationResult> NormalizeMetaWhatsappAsync(
        OmnichannelConnector connector,
        HttpRequest request,
        JsonElement payload,
        string rawBody,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var appSecret = GetConfigValue(connector.ConfigJson, "appSecret");
        if (!string.IsNullOrWhiteSpace(appSecret))
        {
            var signature = request.Headers["X-Hub-Signature-256"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
                return Invalid("Missing Meta signature header.");

            var expectedHash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), Encoding.UTF8.GetBytes(rawBody));
            var expected = "sha256=" + Convert.ToHexString(expectedHash).ToLowerInvariant();
            if (!SecureEquals(expected, signature))
                return Invalid("Invalid Meta signature.");
        }

        var message = payload
            .GetProperty("entry")[0]
            .GetProperty("changes")[0]
            .GetProperty("value")
            .GetProperty("messages")[0];

        var from = GetString(message, "from");
        var messageId = GetString(message, "id");
        var timestamp = GetString(message, "timestamp");
        var text = message.TryGetProperty("text", out var textEl) ? GetString(textEl, "body") : string.Empty;
        var conversationId = from;

        return Valid(
            externalConversationId: conversationId,
            externalMessageId: messageId,
            sender: from,
            subject: "WhatsApp message",
            content: text,
            timestamp: TryParseUnixTimestamp(timestamp),
            normalized: new
            {
                provider = "meta_whatsapp",
                conversationId,
                messageId,
                from,
                text
            });
    }

    private async Task<InboundNormalizationResult> NormalizeTwilioAsync(
        OmnichannelConnector connector,
        HttpRequest request,
        JsonElement payload,
        string rawBody,
        CancellationToken cancellationToken)
    {
        var authToken = GetConfigValue(connector.ConfigJson, "authToken");
        if (!string.IsNullOrWhiteSpace(authToken))
        {
            var signature = request.Headers["X-Twilio-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
                return Invalid("Missing Twilio signature.");

            var uri = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(cancellationToken);
                foreach (var key in form.Keys)
                {
                    values[key] = form[key].ToString();
                }
            }
            else
            {
                var query = QueryHelpers.ParseQuery(request.QueryString.Value ?? string.Empty);
                foreach (var key in query.Keys)
                {
                    values[key] = query[key].ToString();
                }
            }

            var signatureBase = new StringBuilder(uri);
            foreach (var pair in values)
            {
                signatureBase.Append(pair.Key).Append(pair.Value);
            }

            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken));
            var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureBase.ToString())));
            if (!SecureEquals(expected, signature))
                return Invalid("Invalid Twilio signature.");
        }

        var messageId = FirstNotEmpty(
            GetString(payload, "MessageSid"),
            GetString(payload, "SmsSid"),
            GetString(payload, "CallSid"));
        var from = FirstNotEmpty(GetString(payload, "From"), GetString(payload, "Caller"));
        var body = FirstNotEmpty(GetString(payload, "Body"), GetString(payload, "SpeechResult"), GetString(payload, "TranscriptionText"));
        var conversation = FirstNotEmpty(GetString(payload, "ConversationSid"), from);

        return Valid(
            externalConversationId: conversation,
            externalMessageId: messageId,
            sender: from,
            subject: "Twilio inbound",
            content: body,
            timestamp: DateTime.UtcNow,
            normalized: new
            {
                provider = "twilio",
                conversation,
                messageId,
                from,
                body
            });
    }

    private static InboundNormalizationResult NormalizeWebchat(JsonElement payload)
    {
        return Valid(
            externalConversationId: FirstNotEmpty(GetString(payload, "conversationId"), GetString(payload, "sessionId")),
            externalMessageId: FirstNotEmpty(GetString(payload, "messageId"), Guid.NewGuid().ToString("N")),
            sender: FirstNotEmpty(GetString(payload, "sender"), GetString(payload, "email"), "anonymous"),
            subject: FirstNotEmpty(GetString(payload, "subject"), "Web chat message"),
            content: FirstNotEmpty(GetString(payload, "message"), GetString(payload, "content")),
            timestamp: TryParseDate(GetString(payload, "timestamp")) ?? DateTime.UtcNow,
            normalized: payload);
    }

    private static InboundNormalizationResult NormalizeCti(JsonElement payload)
    {
        var callId = FirstNotEmpty(GetString(payload, "callId"), GetString(payload, "CallSid"));
        var from = FirstNotEmpty(GetString(payload, "from"), GetString(payload, "From"), GetString(payload, "caller"));
        var transcript = FirstNotEmpty(GetString(payload, "transcript"), GetString(payload, "SpeechResult"), GetString(payload, "notes"));
        return Valid(
            externalConversationId: FirstNotEmpty(GetString(payload, "conversationId"), callId),
            externalMessageId: FirstNotEmpty(GetString(payload, "messageId"), callId, Guid.NewGuid().ToString("N")),
            sender: from,
            subject: FirstNotEmpty(GetString(payload, "subject"), "Voice/CTI interaction"),
            content: transcript,
            timestamp: TryParseDate(GetString(payload, "timestamp")) ?? DateTime.UtcNow,
            normalized: payload);
    }

    private static InboundNormalizationResult NormalizeGeneric(JsonElement payload)
    {
        return Valid(
            externalConversationId: GetString(payload, "externalConversationId"),
            externalMessageId: FirstNotEmpty(GetString(payload, "externalMessageId"), Guid.NewGuid().ToString("N")),
            sender: FirstNotEmpty(GetString(payload, "senderAddress"), GetString(payload, "sender")),
            subject: GetString(payload, "subject"),
            content: FirstNotEmpty(GetString(payload, "content"), GetString(payload, "message")),
            timestamp: TryParseDate(GetString(payload, "externalTimestampUtc")),
            normalized: payload);
    }

    private static InboundNormalizationResult Valid(
        string externalConversationId,
        string externalMessageId,
        string sender,
        string subject,
        string content,
        DateTime? timestamp,
        object normalized)
    {
        return new InboundNormalizationResult
        {
            IsValid = true,
            ExternalConversationId = externalConversationId.Trim(),
            ExternalMessageId = externalMessageId.Trim(),
            SenderAddress = sender.Trim().ToLowerInvariant(),
            Subject = subject.Trim(),
            Content = content.Trim(),
            ExternalTimestampUtc = timestamp,
            NormalizedPayloadJson = JsonSerializer.Serialize(normalized)
        };
    }

    private static InboundNormalizationResult Invalid(string error)
    {
        return new InboundNormalizationResult
        {
            IsValid = false,
            Error = error
        };
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
            return string.Empty;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    private static string FirstNotEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private static DateTime? TryParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(value, out var dt) ? dt.ToUniversalTime() : null;
    }

    private static DateTime? TryParseUnixTimestamp(string value)
    {
        if (!long.TryParse(value, out var unix))
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
    }

    private static bool SecureEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string GetConfigValue(string configJson, string key)
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
