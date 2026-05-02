namespace HelpDeskSystem.Integrations.Slack;

public interface ISlackService
{
    Task<string> SendMessageAsync(string channelId, string message, SlackMessageFormat format = SlackMessageFormat.Plain);
    Task<string> SendBlockMessageAsync(string channelId, SlackBlock[] blocks);
    Task<string> SendEphemeralMessageAsync(string channelId, string userId, string message);
    Task<string> UploadFileAsync(string channelId, string filePath, string title, string initialComment = null);
    Task<SlackWebhookEvent> ProcessIncomingWebhookAsync(SlackWebhookPayload payload);
    Task<bool> VerifyWebhookSignatureAsync(string signature, string timestamp, string requestBody);
    Task<SlackUser[]> GetTeamMembersAsync();
    Task<SlackChannel[]> GetChannelsAsync();
    Task<SlackUser> GetUserProfileAsync(string userId);
    Task<SlackPresence> GetUserPresenceAsync(string userId);
    Task<bool> SetUserPresenceAsync(string userId, SlackPresenceStatus presence);
    Task<SlackConversationHistory> GetConversationHistoryAsync(string channelId, string cursor = null, int limit = 100);
    Task<string> OpenDirectMessageAsync(string userId);
    Task<SlackMessage[]> SearchMessagesAsync(string query, int count = 25);
}

public class SlackWebhookEvent
{
    public string EventType { get; set; }
    public string EventId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ChannelId { get; set; }
    public string UserId { get; set; }
    public string MessageText { get; set; }
    public string MessageType { get; set; }
    public SlackBlock[] Blocks { get; set; }
    public SlackAttachment[] Attachments { get; set; }
    public SlackFile[] Files { get; set; }
    public SlackReaction[] Reactions { get; set; }
    public SlackThreadInfo ThreadInfo { get; set; }
    public bool IsThreaded { get; set; }
    public string ThreadTs { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}

public class SlackWebhookPayload
{
    public string Type { get; set; }
    public string Challenge { get; set; }
    public string Token { get; set; }
    public SlackEvent Event { get; set; }
    public SlackTeam Team { get; set; }
    public SlackUser User { get; set; }
}

public class SlackEvent
{
    public string Type { get; set; }
    public string Channel { get; set; }
    public string User { get; set; }
    public string Text { get; set; }
    public string Ts { get; set; }
    public string ThreadTs { get; set; }
    public SlackBlock[] Blocks { get; set; }
    public SlackAttachment[] Attachments { get; set; }
    public SlackFile[] Files { get; set; }
    public SlackReaction[] Reactions { get; set; }
    public string Subtype { get; set; }
    public string MessageTs { get; set; }
    public string Reaction { get; set; }
    public string ItemUser { get; set; }
    public string Item { get; set; }
}

public class SlackTeam
{
    public string Id { get; set; }
    public string Domain { get; set; }
    public string Name { get; set; }
}

public class SlackMessageFormat
{
    public const string Plain = "plain";
    public const string Markdown = "mrkdwn";
}

public class SlackBlock
{
    public string Type { get; set; }
    public string Text { get; set; }
    public SlackBlockText BlockText { get; set; }
    public SlackBlockElement[] Elements { get; set; }
    public SlackBlockAccessory Accessory { get; set; }
    public string BlockId { get; set; }
    public SlackBlockField[] Fields { get; set; }
    public string Title { get; set; }
    public string ImageUrl { get; set; }
    public string AltText { get; set; }
}

public class SlackBlockText
{
    public string Type { get; set; }
    public string Text { get; set; }
    public bool Emoji { get; set; }
    public bool Verbatim { get; set; }
}

public class SlackBlockElement
{
    public string Type { get; set; }
    public string Text { get; set; }
    public string ActionId { get; set; }
    public string Url { get; set; }
    public string Value { get; set; }
    public SlackBlockText BlockText { get; set; }
    public SlackOption[] Options { get; set; }
    public SlackOption InitialOption { get; set; }
    public SlackBlockElement[] Elements { get; set; }
    public string Placeholder { get; set; }
    public bool Multiple { get; set; }
    public string Style { get; set; }
    public string Confirm { get; set; }
}

public class SlackOption
{
    public string Text { get; set; }
    public string Value { get; set; }
    public string Url { get; set; }
}

public class SlackBlockAccessory
{
    public string Type { get; set; }
    public string ImageUrl { get; set; }
    public string AltText { get; set; }
    public SlackBlockElement Button { get; set; }
}

public class SlackBlockField
{
    public string Type { get; set; }
    public string Text { get; set; }
}

public class SlackAttachment
{
    public string Id { get; set; }
    public string Color { get; set; }
    public string Fallback { get; set; }
    public string CallbackId { get; set; }
    public string Title { get; set; }
    public string TitleLink { get; set; }
    public string Text { get; set; }
    public string Pretext { get; set; }
    public string ServiceName { get; set; }
    public string ServiceIcon { get; set; }
    public string AuthorName { get; set; }
    public string AuthorLink { get; set; }
    public string AuthorIcon { get; set; }
    public string ImageUrl { get; set; }
    public string ThumbUrl { get; set; }
    public string Footer { get; set; }
    public string FooterIcon { get; set; }
    public DateTime Ts { get; set; }
    public SlackAttachmentField[] Fields { get; set; }
    public SlackAction[] Actions { get; set; }
    public SlackAttachmentMarkdown[] MarkdownIn { get; set; }
}

public class SlackAttachmentField
{
    public string Title { get; set; }
    public string Value { get; set; }
    public bool Short { get; set; }
}

public class SlackAttachmentMarkdown
{
    public string Type { get; set; }
}

public class SlackAction
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public string Url { get; set; }
    public string Style { get; set; }
}

public class SlackFile
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
    public string Mimetype { get; set; }
    public string Filetype { get; set; }
    public string UrlPrivate { get; set; }
    public string UrlPrivateDownload { get; set; }
    public long Size { get; set; }
    public DateTime Created { get; set; }
    public DateTime Timestamp { get; set; }
    public string Permalink { get; set; }
    public string PermalinkPublic { get; set; }
    public bool Editable { get; set; }
    public bool HasRichPreview { get; set; }
}

public class SlackReaction
{
    public string Name { get; set; }
    public int Count { get; set; }
    public string[] Users { get; set; }
}

public class SlackThreadInfo
{
    public int ReplyCount { get; set; }
    public int ReplyUsersCount { get; set; }
    public string[] ReplyUsers { get; set; }
    public string LatestReply { get; set; }
    public DateTime LatestReplyTimestamp { get; set; }
}

public class SlackUser
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string Name { get; set; }
    public string RealName { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public string Title { get; set; }
    public string Department { get; set; }
    public string Phone { get; set; }
    public string TimeZone { get; set; }
    public SlackPresence Presence { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsOwner { get; set; }
    public bool IsPrimaryOwner { get; set; }
    public bool IsRestricted { get; set; }
    public bool IsUltraRestricted { get; set; }
    public bool IsBot { get; set; }
    public DateTime Updated { get; set; }
    public SlackProfile Profile { get; set; }
}

public class SlackProfile
{
    public string RealName { get; set; }
    public string DisplayName { get; set; }
    public string RealNameNormalized { get; set; }
    public string DisplayNameNormalized { get; set; }
    public string Email { get; set; }
    public string Title { get; set; }
    public string Phone { get; set; }
    public string Skype { get; set; }
    public string Image24 { get; set; }
    public string Image32 { get; set; }
    public string Image48 { get; set; }
    public string Image72 { get; set; }
    public string Image192 { get; set; }
    public string Image512 { get; set; }
    public string StatusText { get; set; }
    public string StatusEmoji { get; set; }
    public string Team { get; set; }
}

public class SlackPresence
{
    public string Presence { get; set; }
    public DateTime LastActivity { get; set; }
    public bool Online { get; set; }
}

public class SlackPresenceStatus
{
    public const string Active = "active";
    public const string Away = "away";
}

public class SlackChannel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NameNormalized { get; set; }
    public bool IsChannel { get; set; }
    public bool IsGroup { get; set; }
    public bool IsIm { get; set; }
    public bool IsMpim { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsArchived { get; set; }
    public bool IsGeneral { get; set; }
    public string Topic { get; set; }
    public string Purpose { get; set; }
    public DateTime Created { get; set; }
    public string Creator { get; set; }
    public int NumMembers { get; set; }
    public string[] Members { get; set; }
    public string[] PreviousNames { get; set; }
}

public class SlackConversationHistory
{
    public SlackMessage[] Messages { get; set; }
    public bool HasMore { get; set; }
    public string NextCursor { get; set; }
}

public class SlackMessage
{
    public string Type { get; set; }
    public string Channel { get; set; }
    public string User { get; set; }
    public string Text { get; set; }
    public string Ts { get; set; }
    public string ThreadTs { get; set; }
    public SlackBlock[] Blocks { get; set; }
    public SlackAttachment[] Attachments { get; set; }
    public SlackFile[] Files { get; set; }
    public SlackReaction[] Reactions { get; set; }
    public bool IsThreaded { get; set; }
    public int ReplyCount { get; set; }
    public string[] ReplyUsers { get; set; }
    public string LatestReply { get; set; }
}
