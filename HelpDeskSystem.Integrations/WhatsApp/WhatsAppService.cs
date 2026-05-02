using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelpDeskSystem.Integrations.WhatsApp;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppService> _logger;

    private const string ApiBaseUrl = "https://graph.facebook.com/v18.0";

    public WhatsAppService(IOptions<WhatsAppOptions> options, ILogger<WhatsAppService> logger)
    {
        _options = options;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);
    }

    public async Task<string> SendMessageAsync(string to, string message, string templateName = null, Dictionary<string, string> templateVariables = null)
    {
        try
        {
            var payload = new WhatsAppMessagePayload
            {
                MessagingProduct = "whatsapp",
                To = to,
                RecipientType = "individual"
            };

            if (!string.IsNullOrEmpty(templateName))
            {
                payload.Template = new WhatsAppTemplate
                {
                    Name = templateName,
                    Language = new WhatsAppLanguage { Code = "en_US" },
                    Components = templateVariables?.Any() == true ? new[]
                    {
                        new WhatsAppTemplateComponent
                        {
                            Type = "body",
                            Parameters = templateVariables.Select(v => new WhatsAppTemplateParameter
                            {
                                Type = "text",
                                Text = v.Value
                            }).ToArray()
                        }
                    } : null
                };
            }
            else
            {
                payload.Text = new WhatsAppText { Body = message };
            }

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/{_options.PhoneNumberId}/messages", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WhatsAppMessageResponse>(responseContent);
            
            _logger.LogInformation("WhatsApp message sent successfully. Message ID: {MessageId}", result?.Messages?.FirstOrDefault()?.Id);
            return result?.Messages?.FirstOrDefault()?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {To}", to);
            throw;
        }
    }

    public async Task<string> SendMediaMessageAsync(string to, string mediaUrl, string mediaType, string caption = null)
    {
        try
        {
            var payload = new WhatsAppMessagePayload
            {
                MessagingProduct = "whatsapp",
                To = to,
                RecipientType = "individual",
                Media = new WhatsAppMediaMessage
                {
                    Link = mediaUrl,
                    Caption = caption
                }
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/{_options.PhoneNumberId}/messages", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WhatsAppMessageResponse>(responseContent);
            
            _logger.LogInformation("WhatsApp media message sent successfully. Message ID: {MessageId}", result?.Messages?.FirstOrDefault()?.Id);
            return result?.Messages?.FirstOrDefault()?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp media message to {To}", to);
            throw;
        }
    }

    public async Task<string> SendInteractiveMessageAsync(string to, WhatsAppInteractiveMessage interactiveMessage)
    {
        try
        {
            var payload = new WhatsAppMessagePayload
            {
                MessagingProduct = "whatsapp",
                To = to,
                RecipientType = "individual",
                Interactive = interactiveMessage
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/{_options.PhoneNumberId}/messages", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WhatsAppMessageResponse>(responseContent);
            
            _logger.LogInformation("WhatsApp interactive message sent successfully. Message ID: {MessageId}", result?.Messages?.FirstOrDefault()?.Id);
            return result?.Messages?.FirstOrDefault()?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp interactive message to {To}", to);
            throw;
        }
    }

    public async Task<WhatsAppWebhookEvent> ProcessIncomingWebhookAsync(WhatsAppWebhookPayload payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (!webhookData.ContainsKey("entry") || !webhookData["entry"].Equals(null))
            {
                return null;
            }

            var entries = JsonSerializer.Deserialize<JsonElement[]>(webhookData["entry"].ToString());
            var firstEntry = entries[0];
            
            if (!firstEntry.TryGetProperty("changes", out var changes))
            {
                return null;
            }

            var firstChange = changes[0];
            if (!firstChange.TryGetProperty("value", out var value))
            {
                return null;
            }

            var webhookEvent = new WhatsAppWebhookEvent
            {
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>()
            };

            // Process messages
            if (value.TryGetProperty("messages", out var messages))
            {
                var messageArray = messages.EnumerateArray().FirstOrDefault();
                if (messageArray.ValueKind != JsonValueKind.Undefined)
                {
                    webhookEvent.EventType = "message";
                    webhookEvent.MessageId = messageArray.GetProperty("id").GetString();
                    webhookEvent.From = messageArray.GetProperty("from").GetString();
                    webhookEvent.To = messageArray.GetProperty("to").GetString();
                    webhookEvent.Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(messageArray.GetProperty("timestamp").GetString())).DateTime;
                    webhookEvent.MessageType = messageArray.GetProperty("type").GetString();

                    switch (webhookEvent.MessageType)
                    {
                        case "text":
                            webhookEvent.MessageText = messageArray.GetProperty("text").GetProperty("body").GetString();
                            break;
                        case "image":
                        case "audio":
                        case "video":
                        case "document":
                            webhookEvent.Media = JsonSerializer.Deserialize<WhatsAppMedia>(messageArray.GetProperty(webhookEvent.MessageType).GetRawText());
                            break;
                        case "interactive":
                            webhookEvent.Interactive = JsonSerializer.Deserialize<WhatsAppInteractive>(messageArray.GetProperty("interactive").GetRawText());
                            break;
                        case "location":
                            webhookEvent.Location = JsonSerializer.Deserialize<WhatsAppLocation>(messageArray.GetProperty("location").GetRawText());
                            break;
                        case "contacts":
                            webhookEvent.Contact = JsonSerializer.Deserialize<WhatsAppContact>(messageArray.GetProperty("contacts").GetRawText());
                            break;
                    }
                }
            }
            // Process message status updates
            else if (value.TryGetProperty("statuses", out var statuses))
            {
                var statusArray = statuses.EnumerateArray().FirstOrDefault();
                if (statusArray.ValueKind != JsonValueKind.Undefined)
                {
                    webhookEvent.EventType = "status";
                    webhookEvent.MessageId = statusArray.GetProperty("id").GetString();
                    webhookEvent.From = statusArray.GetProperty("recipient_id").GetString();
                    webhookEvent.Status = JsonSerializer.Deserialize<WhatsAppMessageStatus>(statusArray.GetRawText());
                }
            }

            _logger.LogInformation("Processed WhatsApp webhook event: {EventType} from {From}", webhookEvent.EventType, webhookEvent.From);
            return webhookEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process WhatsApp webhook");
            throw;
        }
    }

    public async Task<bool> VerifyWebhookAsync(string hubMode, string hubVerifyToken, string hubChallenge)
    {
        try
        {
            if (hubMode == "subscribe" && hubVerifyToken == _options.WebhookVerifyToken)
            {
                _logger.LogInformation("WhatsApp webhook verified successfully");
                return true;
            }

            _logger.LogWarning("WhatsApp webhook verification failed. Mode: {HubMode}, Token: {HubVerifyToken}", hubMode, hubVerifyToken);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify WhatsApp webhook");
            return false;
        }
    }

    public async Task<WhatsAppPhoneNumber[]> GetConnectedPhoneNumbersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/{_options.BusinessAccountId}/phone_numbers");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WhatsAppPhoneNumbersResponse>(responseContent);
            
            return result?.Data ?? Array.Empty<WhatsAppPhoneNumber>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connected phone numbers");
            throw;
        }
    }

    public async Task<WhatsAppMessageTemplate[]> GetMessageTemplatesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/{_options.BusinessAccountId}/message_templates");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WhatsAppTemplatesResponse>(responseContent);
            
            return result?.Data ?? Array.Empty<WhatsAppMessageTemplate>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message templates");
            throw;
        }
    }

    public async Task<WhatsAppAnalytics> GetAnalyticsAsync(DateTime from, DateTime to)
    {
        try
        {
            // This would require implementing Meta's Business Analytics API
            // For now, return basic analytics structure
            return new WhatsAppAnalytics
            {
                TotalMessages = 0,
                SentMessages = 0,
                DeliveredMessages = 0,
                ReadMessages = 0,
                FailedMessages = 0,
                MessageTypes = new Dictionary<string, int>(),
                DailyStats = new Dictionary<string, int>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WhatsApp analytics");
            throw;
        }
    }

    public async Task<bool> MarkMessageAsReadAsync(string messageId)
    {
        try
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                status = "read",
                message_id = messageId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/{_options.PhoneNumberId}/messages", content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Marked WhatsApp message as read: {MessageId}", messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark WhatsApp message as read: {MessageId}", messageId);
            throw;
        }
    }
}

// Supporting DTOs
public class WhatsAppOptions
{
    public string AccessToken { get; set; }
    public string PhoneNumberId { get; set; }
    public string BusinessAccountId { get; set; }
    public string WebhookVerifyToken { get; set; }
    public string WebhookSecret { get; set; }
}

public class WhatsAppMessagePayload
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; }
    
    [JsonPropertyName("to")]
    public string To { get; set; }
    
    [JsonPropertyName("recipient_type")]
    public string RecipientType { get; set; }
    
    [JsonPropertyName("text")]
    public WhatsAppText Text { get; set; }
    
    [JsonPropertyName("template")]
    public WhatsAppTemplate Template { get; set; }
    
    [JsonPropertyName("media")]
    public WhatsAppMediaMessage Media { get; set; }
    
    [JsonPropertyName("interactive")]
    public WhatsAppInteractiveMessage Interactive { get; set; }
}

public class WhatsAppText
{
    [JsonPropertyName("body")]
    public string Body { get; set; }
}

public class WhatsAppTemplate
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("language")]
    public WhatsAppLanguage Language { get; set; }
    
    [JsonPropertyName("components")]
    public WhatsAppTemplateComponent[] Components { get; set; }
}

public class WhatsAppLanguage
{
    [JsonPropertyName("code")]
    public string Code { get; set; }
}

public class WhatsAppMediaMessage
{
    [JsonPropertyName("link")]
    public string Link { get; set; }
    
    [JsonPropertyName("caption")]
    public string Caption { get; set; }
}

public class WhatsAppMessageResponse
{
    [JsonPropertyName("messages")]
    public WhatsAppMessageResult[] Messages { get; set; }
}

public class WhatsAppMessageResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class WhatsAppPhoneNumbersResponse
{
    [JsonPropertyName("data")]
    public WhatsAppPhoneNumber[] Data { get; set; }
}

public class WhatsAppTemplatesResponse
{
    [JsonPropertyName("data")]
    public WhatsAppMessageTemplate[] Data { get; set; }
}
