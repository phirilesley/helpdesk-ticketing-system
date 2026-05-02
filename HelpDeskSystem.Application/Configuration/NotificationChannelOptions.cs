namespace HelpDeskSystem.Application.Configuration;

public class NotificationChannelOptions
{
    public const string SectionName = "NotificationChannels";

    public EmailChannelOptions Email { get; set; } = new();
    public WebhookChannelOptions Webhook { get; set; } = new();
}

public class EmailChannelOptions
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = "HelpDeskSystem";
}

public class WebhookChannelOptions
{
    public bool Enabled { get; set; }
    public List<string> Urls { get; set; } = new();
    public string? BearerToken { get; set; }
}
