using HelpDeskSystem.Shared.Base;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Domain.Entities;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAtUtc { get; set; }
}