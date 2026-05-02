namespace HelpDeskSystem.Integrations.Teams;

public interface ITeamsService
{
    Task<string> SendMessageAsync(string channelId, string message, TeamsMessageFormat format = TeamsMessageFormat.Text);
    Task<string> SendAdaptiveCardAsync(string channelId, TeamsAdaptiveCard adaptiveCard);
    Task<string> SendProactiveMessageAsync(string userId, string message);
    Task<TeamsMeeting> CreateMeetingAsync(TeamsMeetingRequest meetingRequest);
    Task<bool> JoinMeetingAsync(string meetingId, string userId);
    Task<TeamsCall> InitiateCallAsync(string userId, TeamsCallRequest callRequest);
    Task<TeamsWebhookEvent> ProcessIncomingWebhookAsync(TeamsWebhookPayload payload);
    Task<bool> ValidateWebhookSignatureAsync(string signature, string payload);
    Task<TeamsUser[]> GetTeamMembersAsync(string teamId);
    Task<TeamsChannel[]> GetChannelsAsync(string teamId);
    Task<TeamsPresence[]> GetPresenceAsync(string[] userIds);
    Task<bool> UpdatePresenceAsync(string userId, TeamsPresence presence);
}

public class TeamsWebhookEvent
{
    public string EventType { get; set; }
    public string MessageId { get; set; }
    public string ChannelId { get; set; }
    public string TeamId { get; set; }
    public string UserId { get; set; }
    public string MessageText { get; set; }
    public DateTime Timestamp { get; set; }
    public TeamsMessageFormat Format { get; set; }
    public TeamsAdaptiveCard AdaptiveCard { get; set; }
    public TeamsMention[] Mentions { get; set; }
    public TeamsAttachment[] Attachments { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}

public class TeamsWebhookPayload
{
    public string Type { get; set; }
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public TeamsWebhookData Data { get; set; }
}

public class TeamsWebhookData
{
    public string MessageType { get; set; }
    public string MessageId { get; set; }
    public string ChannelId { get; set; }
    public string TeamId { get; set; }
    public string UserId { get; set; }
    public string Text { get; set; }
    public TeamsAdaptiveCard AdaptiveCard { get; set; }
    public TeamsMention[] Mentions { get; set; }
    public TeamsAttachment[] Attachments { get; set; }
}

public class TeamsMessageFormat
{
    public const string Text = "text";
    public const string Html = "html";
    public const string Markdown = "markdown";
}

public class TeamsAdaptiveCard
{
    public string Type { get; set; } = "AdaptiveCard";
    public string Version { get; set; } = "1.5";
    public TeamsCardBody[] Body { get; set; }
    public TeamsCardAction[] Actions { get; set; }
    public string Speak { get; set; }
    public bool FallbackText { get; set; }
}

public class TeamsCardBody
{
    public string Type { get; set; }
    public string Text { get; set; }
    public string Size { get; set; }
    public string Weight { get; set; }
    public string Color { get; set; }
    public TeamsCardBody[] Items { get; set; }
    public TeamsFact[] Facts { get; set; }
    public string Url { get; set; }
    public string AltText { get; set; }
    public TeamsImage[] Images { get; set; }
    public TeamsColumnSet ColumnSet { get; set; }
}

public class TeamsColumnSet
{
    public string Type { get; set; } = "ColumnSet";
    public TeamsColumn[] Columns { get; set; }
}

public class TeamsColumn
{
    public string Type { get; set; } = "Column";
    public string Width { get; set; }
    public TeamsCardBody[] Items { get; set; }
    public bool IsVisible { get; set; } = true;
    public string VerticalContentAlignment { get; set; }
}

public class TeamsFact
{
    public string Title { get; set; }
    public string Value { get; set; }
}

public class TeamsImage
{
    public string Url { get; set; }
    public string AltText { get; set; }
    public string Size { get; set; }
}

public class TeamsCardAction
{
    public string Type { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public string Url { get; set; }
    public TeamsActionInput Input { get; set; }
    public TeamsActionData Data { get; set; }
}

public class TeamsActionInput
{
    public string Id { get; set; }
    public bool IsMultiline { get; set; }
    public string Placeholder { get; set; }
    public string Value { get; set; }
    public TeamsChoice[] Choices { get; set; }
}

public class TeamsChoice
{
    public string Title { get; set; }
    public string Value { get; set; }
}

public class TeamsActionData
{
    public string Type { get; set; }
    public Dictionary<string, object> Data { get; set; }
}

public class TeamsMeeting
{
    public string Id { get; set; }
    public string Subject { get; set; }
    public string JoinUrl { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string OrganizerId { get; set; }
    public string[] AttendeeIds { get; set; }
    public bool IsOnlineMeeting { get; set; }
    public string MeetingType { get; set; }
}

public class TeamsMeetingRequest
{
    public string Subject { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string OrganizerId { get; set; }
    public string[] AttendeeIds { get; set; }
    public string Description { get; set; }
    public bool IsOnlineMeeting { get; set; }
    public string MeetingType { get; set; }
}

public class TeamsCall
{
    public string Id { get; set; }
    public string CallType { get; set; }
    public string InitiatorId { get; set; }
    public string TargetId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; }
    public string CallDirection { get; set; }
}

public class TeamsCallRequest
{
    public string CallType { get; set; }
    public string TargetId { get; set; }
    public string CallDirection { get; set; }
    public TeamsMediaConfig MediaConfig { get; set; }
}

public class TeamsMediaConfig
{
    public bool Audio { get; set; } = true;
    public bool Video { get; set; } = false;
    public bool ScreenSharing { get; set; } = false;
}

public class TeamsUser
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public string JobTitle { get; set; }
    public string Department { get; set; }
    public TeamsPresence Presence { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastActivity { get; set; }
}

public class TeamsPresence
{
    public string Id { get; set; }
    public string Availability { get; set; }
    public string Activity { get; set; }
    public string Status { get; set; }
}

public class TeamsChannel
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string TeamId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MembershipType { get; set; }
}

public class TeamsMention
{
    public string Id { get; set; }
    public string MentionText { get; set; }
    public string Mentioned { get; set; }
    public bool User { get; set; }
}

public class TeamsAttachment
{
    public string Id { get; set; }
    public string ContentType { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public long Size { get; set; }
    public string ThumbnailUrl { get; set; }
}
