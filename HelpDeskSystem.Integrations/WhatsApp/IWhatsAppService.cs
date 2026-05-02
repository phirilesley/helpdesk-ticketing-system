namespace HelpDeskSystem.Integrations.WhatsApp;

public interface IWhatsAppService
{
    Task<string> SendMessageAsync(string to, string message, string templateName = null, Dictionary<string, string> templateVariables = null);
    Task<string> SendMediaMessageAsync(string to, string mediaUrl, string mediaType, string caption = null);
    Task<string> SendInteractiveMessageAsync(string to, WhatsAppInteractiveMessage interactiveMessage);
    Task<WhatsAppWebhookEvent> ProcessIncomingWebhookAsync(WhatsAppWebhookPayload payload);
    Task<bool> VerifyWebhookAsync(string hubMode, string hubVerifyToken, string hubChallenge);
    Task<WhatsAppPhoneNumber[]> GetConnectedPhoneNumbersAsync();
    Task<WhatsAppMessageTemplate[]> GetMessageTemplatesAsync();
    Task<WhatsAppAnalytics> GetAnalyticsAsync(DateTime from, DateTime to);
    Task<bool> MarkMessageAsReadAsync(string messageId);
}

public class WhatsAppWebhookEvent
{
    public string EventType { get; set; }
    public string MessageId { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string MessageText { get; set; }
    public string MessageType { get; set; }
    public DateTime Timestamp { get; set; }
    public WhatsAppMessageStatus? Status { get; set; }
    public WhatsAppMedia? Media { get; set; }
    public WhatsAppInteractive? Interactive { get; set; }
    public WhatsAppLocation? Location { get; set; }
    public WhatsAppContact? Contact { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public class WhatsAppWebhookPayload
{
    public object Entry { get; set; }
    public string Object { get; set; }
}

public class WhatsAppInteractiveMessage
{
    public string Type { get; set; }
    public WhatsAppInteractiveBody Body { get; set; }
    public WhatsAppInteractiveAction Action { get; set; }
    public WhatsAppInteractiveHeader Header { get; set; }
}

public class WhatsAppInteractiveBody
{
    public string Text { get; set; }
}

public class WhatsAppInteractiveAction
{
    public string Button { get; set; }
    public WhatsAppInteractiveButton[] Buttons { get; set; }
    public WhatsAppInteractiveSection[] Sections { get; set; }
}

public class WhatsAppInteractiveButton
{
    public string Type { get; set; }
    public string Reply { get; set; }
    public string Title { get; set; }
    public string Id { get; set; }
}

public class WhatsAppInteractiveSection
{
    public string Title { get; set; }
    public WhatsAppInteractiveRow[] Rows { get; set; }
}

public class WhatsAppInteractiveRow
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}

public class WhatsAppInteractiveHeader
{
    public string Type { get; set; }
    public string Text { get; set; }
}

public class WhatsAppMessageStatus
{
    public string Id { get; set; }
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string RecipientId { get; set; }
}

public class WhatsAppMedia
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Url { get; set; }
    public string MimeType { get; set; }
    public string Sha256 { get; set; }
    public long FileSize { get; set; }
}

public class WhatsAppInteractive
{
    public string Type { get; set; }
    public WhatsAppInteractiveReply Reply { get; set; }
    public WhatsAppInteractiveListReply ListReply { get; set; }
    public WhatsAppInteractiveButtonReply ButtonReply { get; set; }
}

public class WhatsAppInteractiveReply
{
    public string Title { get; set; }
    public string Id { get; set; }
    public string Description { get; set; }
}

public class WhatsAppInteractiveListReply
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}

public class WhatsAppInteractiveButtonReply
{
    public string Id { get; set; }
    public string Title { get; set; }
}

public class WhatsAppLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
}

public class WhatsAppContact
{
    public WhatsAppContactName Name { get; set; }
    public WhatsAppContactPhone[] Phones { get; set; }
    public WhatsAppContactEmail[] Emails { get; set; }
    public WhatsAppContactAddress[] Addresses { get; set; }
    public WhatsAppContactOrganization Organization { get; set; }
}

public class WhatsAppContactName
{
    public string FormattedName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }
    public string Suffix { get; set; }
    public string Prefix { get; set; }
}

public class WhatsAppContactPhone
{
    public string Phone { get; set; }
    public string Type { get; set; }
    public string WaId { get; set; }
}

public class WhatsAppContactEmail
{
    public string Email { get; set; }
    public string Type { get; set; }
}

public class WhatsAppContactAddress
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public string Country { get; set; }
    public string Type { get; set; }
}

public class WhatsAppContactOrganization
{
    public string Company { get; set; }
    public string Department { get; set; }
    public string Title { get; set; }
}

public class WhatsAppPhoneNumber
{
    public string Id { get; set; }
    public string PhoneNumber { get; set; }
    public string DisplayName { get; set; }
    public string QualityRating { get; set; }
    public bool IsVerified { get; set; }
    public bool IsBusiness { get; set; }
    public WhatsAppNameStatus NameStatus { get; set; }
}

public class WhatsAppNameStatus
{
    public string Status { get; set; }
    public string Reason { get; set; }
}

public class WhatsAppMessageTemplate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public string Category { get; set; }
    public string Language { get; set; }
    public WhatsAppTemplateComponent[] Components { get; set; }
}

public class WhatsAppTemplateComponent
{
    public string Type { get; set; }
    public string Text { get; set; }
    public WhatsAppTemplateParameter[] Parameters { get; set; }
}

public class WhatsAppTemplateParameter
{
    public string Type { get; set; }
    public string Text { get; set; }
    public WhatsAppMediaObject Media { get; set; }
}

public class WhatsAppMediaObject
{
    public string Link { get; set; }
    public string Filename { get; set; }
}

public class WhatsAppAnalytics
{
    public int TotalMessages { get; set; }
    public int SentMessages { get; set; }
    public int DeliveredMessages { get; set; }
    public int ReadMessages { get; set; }
    public int FailedMessages { get; set; }
    public Dictionary<string, int> MessageTypes { get; set; }
    public Dictionary<string, int> DailyStats { get; set; }
}
