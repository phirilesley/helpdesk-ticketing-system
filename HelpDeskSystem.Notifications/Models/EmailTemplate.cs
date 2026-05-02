using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Notifications.Models;

public class NotificationSettings
{
    public int TenantId { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public Dictionary<string, bool> NotificationTypes { get; set; } = new();
}
