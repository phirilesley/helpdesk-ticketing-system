namespace HelpDeskSystem.Integrations.Twilio;

public interface ITwilioService
{
    Task<string> SendSmsAsync(string to, string message, string fromNumber = null);
    Task<string> InitiateCallAsync(string to, string fromNumber = null, string url = null);
    Task<TwilioWebhookEvent> ProcessIncomingWebhookAsync(Dictionary<string, string> formData);
    Task<bool> ValidateWebhookSignatureAsync(string signature, string url, Dictionary<string, string> formData);
    Task<TwilioPhoneNumber[]> GetAvailablePhoneNumbersAsync();
    Task<bool> PurchasePhoneNumberAsync(string phoneNumber);
    Task<TwilioCallLog[]> GetCallLogsAsync(DateTime? from = null, DateTime? to = null);
    Task<TwilioMessageLog[]> GetMessageLogsAsync(DateTime? from = null, DateTime? to = null);
}

public class TwilioWebhookEvent
{
    public string EventType { get; set; }
    public string MessageSid { get; set; }
    public string CallSid { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Body { get; set; }
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; }
}

public class TwilioPhoneNumber
{
    public string PhoneNumber { get; set; }
    public string FriendlyName { get; set; }
    public string CountryCode { get; set; }
    public string Capabilities { get; set; }
    public bool IsAvailable { get; set; }
    public decimal Price { get; set; }
}

public class TwilioCallLog
{
    public string CallSid { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationSeconds { get; set; }
    public decimal Price { get; set; }
    public string Direction { get; set; }
}

public class TwilioMessageLog
{
    public string MessageSid { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Body { get; set; }
    public string Status { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateSent { get; set; }
    public int NumSegments { get; set; }
    public decimal Price { get; set; }
    public string Direction { get; set; }
}
