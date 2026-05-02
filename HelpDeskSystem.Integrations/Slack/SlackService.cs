using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HelpDeskSystem.Integrations.Slack;

public class SlackService : ISlackService
{
    private readonly HttpClient _httpClient;
    private readonly SlackOptions _options;
    private readonly ILogger<SlackService> _logger;

    private const string ApiBaseUrl = "https://slack.com/api";

    public SlackService(IOptions<SlackOptions> options, ILogger<SlackService> logger)
    {
        _options = options;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.BotToken);
    }

    public async Task<string> SendMessageAsync(string channelId, string message, SlackMessageFormat format = SlackMessageFormat.Plain)
    {
        try
        {
            var payload = new SlackMessagePayload
            {
                Channel = channelId,
                Text = message
            };

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", channelId),
                new KeyValuePair<string, string>("text", message),
                new KeyValuePair<string, string>("token", _options.BotToken)
            });

            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/chat.postMessage", formContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackMessageResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                _logger.LogInformation("Slack message sent successfully. Message ID: {MessageId}", result.Ts);
                return result.Ts;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack message to channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<string> SendBlockMessageAsync(string channelId, SlackBlock[] blocks)
    {
        try
        {
            var payload = new SlackBlockMessagePayload
            {
                Channel = channelId,
                Blocks = blocks
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/chat.postMessage", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackMessageResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                _logger.LogInformation("Slack block message sent successfully. Message ID: {MessageId}", result.Ts);
                return result.Ts;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack block message to channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<string> SendEphemeralMessageAsync(string channelId, string userId, string message)
    {
        try
        {
            var payload = new SlackEphemeralMessagePayload
            {
                Channel = channelId,
                User = userId,
                Text = message
            };

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", channelId),
                new KeyValuePair<string, string>("user", userId),
                new KeyValuePair<string, string>("text", message),
                new KeyValuePair<string, string>("token", _options.BotToken)
            });

            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/chat.postEphemeral", formContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackMessageResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                _logger.LogInformation("Slack ephemeral message sent successfully. Message ID: {MessageId}", result.Ts);
                return result.Ts;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack ephemeral message to user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(string channelId, string filePath, string title, string initialComment = null)
    {
        try
        {
            var formContent = new MultipartFormDataContent();
            formContent.Add(new StringContent(channelId), "channels");
            formContent.Add(new StringContent(title), "title");
            
            if (!string.IsNullOrEmpty(initialComment))
            {
                formContent.Add(new StringContent(initialComment), "initial_comment");
            }

            var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            formContent.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/files.upload", formContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackFileUploadResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                _logger.LogInformation("Slack file uploaded successfully. File ID: {FileId}", result.File?.Id);
                return result.File?.Id;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Slack channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<SlackWebhookEvent> ProcessIncomingWebhookAsync(SlackWebhookPayload payload)
    {
        try
        {
            var webhookEvent = new SlackWebhookEvent
            {
                EventType = payload.Type,
                Timestamp = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>()
            };

            if (payload.Event != null)
            {
                webhookEvent.ChannelId = payload.Event.Channel;
                webhookEvent.UserId = payload.Event.User;
                webhookEvent.MessageText = payload.Event.Text;
                webhookEvent.MessageType = payload.Event.Type;
                webhookEvent.Blocks = payload.Event.Blocks;
                webhookEvent.Attachments = payload.Event.Attachments;
                webhookEvent.Files = payload.Event.Files;
                webhookEvent.Reactions = payload.Event.Reactions;
                webhookEvent.ThreadTs = payload.Event.ThreadTs;
                webhookEvent.IsThreaded = !string.IsNullOrEmpty(payload.Event.ThreadTs);

                if (payload.Event.Ts != null)
                {
                    webhookEvent.Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(payload.Event.Ts)).DateTime;
                }

                if (webhookEvent.IsThreaded)
                {
                    webhookEvent.ThreadInfo = new SlackThreadInfo
                    {
                        ThreadTs = payload.Event.ThreadTs
                    };
                }
            }

            _logger.LogInformation("Processed Slack webhook event: {EventType}", webhookEvent.EventType);
            return webhookEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Slack webhook");
            throw;
        }
    }

    public async Task<bool> VerifyWebhookSignatureAsync(string signature, string timestamp, string requestBody)
    {
        try
        {
            var timeSinceRequest = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - long.Parse(timestamp);
            if (timeSinceRequest > 300) // 5 minutes
            {
                _logger.LogWarning("Slack webhook timestamp is too old");
                return false;
            }

            var baseString = $"v0:{timestamp}:{requestBody}";
            var computedSignature = ComputeHmacSha256(_options.SigningSecret, baseString);
            var expectedSignature = $"v0={computedSignature}";

            return signature == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Slack webhook signature");
            return false;
        }
    }

    private string ComputeHmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    public async Task<SlackUser[]> GetTeamMembersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/users.list?token={_options.BotToken}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackUsersResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                return result.Members;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team members");
            throw;
        }
    }

    public async Task<SlackChannel[]> GetChannelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/conversations.list?token={_options.BotToken}&types=public_channel,private_channel");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackChannelsResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                return result.Channels;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channels");
            throw;
        }
    }

    public async Task<SlackUser> GetUserProfileAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/users.info?token={_options.BotToken}&user={userId}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackUserInfoResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                return result.User;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user profile for {UserId}", userId);
            throw;
        }
    }

    public async Task<SlackPresence> GetUserPresenceAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/users.getPresence?token={_options.BotToken}&user={userId}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackPresenceResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                return new SlackPresence
                {
                    Presence = result.Presence,
                    Online = result.Presence == "active",
                    LastActivity = DateTime.UtcNow
                };
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user presence for {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> SetUserPresenceAsync(string userId, SlackPresenceStatus presence)
    {
        try
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", _options.BotToken),
                new KeyValuePair<string, string>("user", userId),
                new KeyValuePair<string, string>("presence", presence)
            });

            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/users.setPresence", formContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackApiResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                _logger.LogInformation("Set user presence for {UserId} to {Presence}", userId, presence);
                return true;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user presence for {UserId}", userId);
            throw;
        }
    }

    public async Task<SlackConversationHistory> GetConversationHistoryAsync(string channelId, string cursor = null, int limit = 100)
    {
        try
        {
            var queryParams = $"token={_options.BotToken}&channel={channelId}&limit={limit}";
            if (!string.IsNullOrEmpty(cursor))
            {
                queryParams += $"&cursor={cursor}";
            }

            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/conversations.history?{queryParams}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackConversationHistoryResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                return new SlackConversationHistory
                {
                    Messages = result.Messages,
                    HasMore = result.HasMore,
                    NextCursor = result.ResponseMetadata?.NextCursor
                };
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conversation history for channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<string> OpenDirectMessageAsync(string userId)
    {
        try
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", _options.BotToken),
                new KeyValuePair<string, string>("users", userId)
            });

            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/conversations.open", formContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackConversationOpenResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                _logger.LogInformation("Opened direct message channel for user {UserId}", userId);
                return result.Channel?.Id;
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open direct message for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SlackMessage[]> SearchMessagesAsync(string query, int count = 25)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/search.messages?token={_options.BotToken}&query={Uri.EscapeDataString(query)}&count={count}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SlackSearchResponse>(responseContent);
            
            if (result?.Ok == true)
            {
                return result.Messages?.Matches ?? Array.Empty<SlackMessage>();
            }
            else
            {
                throw new Exception($"Slack API error: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search messages with query: {Query}", query);
            throw;
        }
    }
}

// Supporting DTOs and Options
public class SlackOptions
{
    public string BotToken { get; set; }
    public string SigningSecret { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RedirectUri { get; set; }
}

// Response DTOs
public class SlackApiResponse
{
    public bool Ok { get; set; }
    public string Error { get; set; }
    public bool Warning { get; set; }
    public string ResponseMetadata { get; set; }
}

public class SlackMessageResponse : SlackApiResponse
{
    public string Ts { get; set; }
    public string Channel { get; set; }
}

public class SlackFileUploadResponse : SlackApiResponse
{
    public SlackFile File { get; set; }
}

public class SlackUsersResponse : SlackApiResponse
{
    public SlackUser[] Members { get; set; }
}

public class SlackChannelsResponse : SlackApiResponse
{
    public SlackChannel[] Channels { get; set; }
}

public class SlackUserInfoResponse : SlackApiResponse
{
    public SlackUser User { get; set; }
}

public class SlackPresenceResponse : SlackApiResponse
{
    public string Presence { get; set; }
}

public class SlackConversationHistoryResponse : SlackApiResponse
{
    public SlackMessage[] Messages { get; set; }
    public bool HasMore { get; set; }
    public SlackResponseMetadata ResponseMetadata { get; set; }
}

public class SlackResponseMetadata
{
    public string NextCursor { get; set; }
}

public class SlackConversationOpenResponse : SlackApiResponse
{
    public SlackChannel Channel { get; set; }
}

public class SlackSearchResponse : SlackApiResponse
{
    public SlackSearchMessages Messages { get; set; }
}

public class SlackSearchMessages
{
    public SlackMessage[] Matches { get; set; }
    public int Total { get; set; }
    public int Paging { get; set; }
}

// Payload DTOs
public class SlackMessagePayload
{
    public string Channel { get; set; }
    public string Text { get; set; }
}

public class SlackBlockMessagePayload
{
    public string Channel { get; set; }
    public SlackBlock[] Blocks { get; set; }
}

public class SlackEphemeralMessagePayload
{
    public string Channel { get; set; }
    public string User { get; set; }
    public string Text { get; set; }
}
