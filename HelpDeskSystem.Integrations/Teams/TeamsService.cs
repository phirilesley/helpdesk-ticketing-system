using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HelpDeskSystem.Integrations.Teams;

public class TeamsService : ITeamsService
{
    private readonly HttpClient _httpClient;
    private readonly TeamsOptions _options;
    private readonly ILogger<TeamsService> _logger;

    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
    private const string TeamsBaseUrl = "https://graph.microsoft.com/beta";

    public TeamsService(IOptions<TeamsOptions> options, ILogger<TeamsService> logger)
    {
        _options = options;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> SendMessageAsync(string channelId, string message, TeamsMessageFormat format = TeamsMessageFormat.Text)
    {
        try
        {
            var payload = new TeamsMessagePayload
            {
                Body = new TeamsMessageBody
                {
                    ContentType = format,
                    Content = message
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{TeamsBaseUrl}/teams/channels/{channelId}/messages", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TeamsMessageResponse>(responseContent);
            
            _logger.LogInformation("Teams message sent successfully. Message ID: {MessageId}", result?.Id);
            return result?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams message to channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<string> SendAdaptiveCardAsync(string channelId, TeamsAdaptiveCard adaptiveCard)
    {
        try
        {
            var payload = new TeamsAdaptiveCardPayload
            {
                Attachments = new[]
                {
                    new TeamsAttachmentPayload
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        ContentUrl = null,
                        Content = adaptiveCard
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{TeamsBaseUrl}/teams/channels/{channelId}/messages", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TeamsMessageResponse>(responseContent);
            
            _logger.LogInformation("Teams adaptive card sent successfully. Message ID: {MessageId}", result?.Id);
            return result?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams adaptive card to channel {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<string> SendProactiveMessageAsync(string userId, string message)
    {
        try
        {
            var payload = new TeamsProactiveMessagePayload
            {
                Recipient = new TeamsUserReference { Id = userId },
                Message = new TeamsMessagePayload
                {
                    Body = new TeamsMessageBody
                    {
                        ContentType = "text",
                        Content = message
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{TeamsBaseUrl}/users/{userId}/chats", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TeamsChatResponse>(responseContent);
            
            _logger.LogInformation("Teams proactive message sent successfully. Chat ID: {ChatId}", result?.Id);
            return result?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams proactive message to user {UserId}", userId);
            throw;
        }
    }

    public async Task<TeamsMeeting> CreateMeetingAsync(TeamsMeetingRequest meetingRequest)
    {
        try
        {
            var payload = new TeamsOnlineMeetingPayload
            {
                Subject = meetingRequest.Subject,
                Start = new TeamsDateTime { DateTime = meetingRequest.StartTime, TimeZone = "UTC" },
                End = new TeamsDateTime { DateTime = meetingRequest.EndTime, TimeZone = "UTC" },
                Participants = new TeamsParticipants
                {
                    Attendees = meetingRequest.AttendeeIds?.Select(id => new TeamsParticipant
                    {
                        Upn = id,
                        Role = "attendee"
                    }).ToArray()
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{GraphBaseUrl}/me/onlineMeetings", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TeamsOnlineMeetingResponse>(responseContent);
            
            var meeting = new TeamsMeeting
            {
                Id = result?.Id,
                Subject = result?.Subject,
                JoinUrl = result?.JoinUrl,
                StartTime = DateTime.Parse(result?.Start?.DateTime),
                EndTime = DateTime.Parse(result?.End?.DateTime),
                OrganizerId = meetingRequest.OrganizerId,
                AttendeeIds = meetingRequest.AttendeeIds,
                IsOnlineMeeting = true,
                MeetingType = meetingRequest.MeetingType
            };

            _logger.LogInformation("Teams meeting created successfully. Meeting ID: {MeetingId}", meeting.Id);
            return meeting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Teams meeting");
            throw;
        }
    }

    public async Task<bool> JoinMeetingAsync(string meetingId, string userId)
    {
        try
        {
            var payload = new TeamsJoinMeetingPayload
            {
                ParticipantId = userId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{GraphBaseUrl}/onlineMeetings/{meetingId}/participants", content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("User {UserId} joined Teams meeting {MeetingId}", userId, meetingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join Teams meeting {MeetingId} for user {UserId}", meetingId, userId);
            throw;
        }
    }

    public async Task<TeamsCall> InitiateCallAsync(string userId, TeamsCallRequest callRequest)
    {
        try
        {
            var payload = new TeamsCallPayload
            {
                CallType = callRequest.CallType,
                TargetId = callRequest.TargetId,
                CallDirection = callRequest.CallDirection,
                MediaConfig = callRequest.MediaConfig
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{GraphBaseUrl}/communications/calls", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TeamsCallResponse>(responseContent);
            
            var call = new TeamsCall
            {
                Id = result?.Id,
                CallType = result?.CallType,
                InitiatorId = userId,
                TargetId = callRequest.TargetId,
                StartTime = DateTime.UtcNow,
                Status = "ringing",
                CallDirection = callRequest.CallDirection
            };

            _logger.LogInformation("Teams call initiated successfully. Call ID: {CallId}", call.Id);
            return call;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate Teams call");
            throw;
        }
    }

    public async Task<TeamsWebhookEvent> ProcessIncomingWebhookAsync(TeamsWebhookPayload payload)
    {
        try
        {
            var webhookEvent = new TeamsWebhookEvent
            {
                EventType = payload.Type,
                Timestamp = payload.Timestamp,
                AdditionalData = new Dictionary<string, object>()
            };

            if (payload.Data != null)
            {
                webhookEvent.MessageId = payload.Data.MessageId;
                webhookEvent.ChannelId = payload.Data.ChannelId;
                webhookEvent.TeamId = payload.Data.TeamId;
                webhookEvent.UserId = payload.Data.UserId;
                webhookEvent.MessageText = payload.Data.Text;
                webhookEvent.AdaptiveCard = payload.Data.AdaptiveCard;
                webhookEvent.Mentions = payload.Data.Mentions;
                webhookEvent.Attachments = payload.Data.Attachments;
            }

            _logger.LogInformation("Processed Teams webhook event: {EventType}", webhookEvent.EventType);
            return webhookEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Teams webhook");
            throw;
        }
    }

    public async Task<bool> ValidateWebhookSignatureAsync(string signature, string payload)
    {
        try
        {
            var expectedSignature = ComputeHmacSha256(_options.WebhookSecret, payload);
            return signature == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Teams webhook signature");
            return false;
        }
    }

    private string ComputeHmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    public async Task<TeamsUser[]> GetTeamMembersAsync(string teamId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{GraphBaseUrl}/groups/{teamId}/members");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TeamsUsersResponse>(responseContent);
            
            return result?.Value ?? Array.Empty<TeamsUser>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team members for team {TeamId}", teamId);
            throw;
        }
    }

    public async Task<TeamsChannel[]> GetChannelsAsync(string teamId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{GraphBaseUrl}/teams/{teamId}/channels");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TeamsChannelsResponse>(responseContent);
            
            return result?.Value ?? Array.Empty<TeamsChannel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channels for team {TeamId}", teamId);
            throw;
        }
    }

    public async Task<TeamsPresence[]> GetPresenceAsync(string[] userIds)
    {
        try
        {
            var presenceRequests = userIds.Select(id => new { id }).ToArray();
            var json = JsonSerializer.Serialize(new { ids = userIds });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{GraphBaseUrl}/communications/getPresencesByUserId", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TeamsPresenceResponse>(responseContent);
            
            return result?.Value ?? Array.Empty<TeamsPresence>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get presence for users");
            throw;
        }
    }

    public async Task<bool> UpdatePresenceAsync(string userId, TeamsPresence presence)
    {
        try
        {
            var payload = new TeamsPresenceUpdatePayload
            {
                Presence = presence.Availability,
                Activity = presence.Activity
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PatchAsync($"{GraphBaseUrl}/users/{userId}/presence", content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Updated presence for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update presence for user {UserId}", userId);
            throw;
        }
    }
}

// Supporting DTOs and Options
public class TeamsOptions
{
    public string AccessToken { get; set; }
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string WebhookSecret { get; set; }
    public string CallbackUrl { get; set; }
}

// Message Payloads
public class TeamsMessagePayload
{
    public TeamsMessageBody Body { get; set; }
}

public class TeamsMessageBody
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class TeamsAdaptiveCardPayload
{
    public TeamsAttachmentPayload[] Attachments { get; set; }
}

public class TeamsAttachmentPayload
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }
    
    [JsonPropertyName("contentUrl")]
    public string ContentUrl { get; set; }
    
    [JsonPropertyName("content")]
    public TeamsAdaptiveCard Content { get; set; }
}

public class TeamsProactiveMessagePayload
{
    public TeamsUserReference Recipient { get; set; }
    public TeamsMessagePayload Message { get; set; }
}

public class TeamsUserReference
{
    public string Id { get; set; }
}

// Meeting Payloads
public class TeamsOnlineMeetingPayload
{
    public string Subject { get; set; }
    public TeamsDateTime Start { get; set; }
    public TeamsDateTime End { get; set; }
    public TeamsParticipants Participants { get; set; }
}

public class TeamsDateTime
{
    public DateTime DateTime { get; set; }
    public string TimeZone { get; set; }
}

public class TeamsParticipants
{
    public TeamsParticipant[] Attendees { get; set; }
}

public class TeamsParticipant
{
    public string Upn { get; set; }
    public string Role { get; set; }
}

// Call Payloads
public class TeamsCallPayload
{
    public string CallType { get; set; }
    public string TargetId { get; set; }
    public string CallDirection { get; set; }
    public TeamsMediaConfig MediaConfig { get; set; }
}

public class TeamsJoinMeetingPayload
{
    public string ParticipantId { get; set; }
}

// Response DTOs
public class TeamsMessageResponse
{
    public string Id { get; set; }
}

public class TeamsChatResponse
{
    public string Id { get; set; }
}

public class TeamsOnlineMeetingResponse
{
    public string Id { get; set; }
    public string Subject { get; set; }
    public string JoinUrl { get; set; }
    public TeamsDateTime Start { get; set; }
    public TeamsDateTime End { get; set; }
}

public class TeamsCallResponse
{
    public string Id { get; set; }
    public string CallType { get; set; }
}

public class TeamsUsersResponse
{
    public TeamsUser[] Value { get; set; }
}

public class TeamsChannelsResponse
{
    public TeamsChannel[] Value { get; set; }
}

public class TeamsPresenceResponse
{
    public TeamsPresence[] Value { get; set; }
}

public class TeamsPresenceUpdatePayload
{
    public string Presence { get; set; }
    public string Activity { get; set; }
}
