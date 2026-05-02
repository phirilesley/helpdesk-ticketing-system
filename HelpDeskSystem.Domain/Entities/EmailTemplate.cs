using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class EmailTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> DefaultVariables { get; set; } = new();
}

public class EmailNotification : BaseEntity
{
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime? SentAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    
    // Navigation
    public User? User { get; set; }
    public Tenant? Tenant { get; set; }
}

public enum NotificationStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    Cancelled = 5
}
