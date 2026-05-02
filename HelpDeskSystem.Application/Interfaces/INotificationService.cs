using HelpDeskSystem.Application.DTOs.Notifications;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(int userId, string title, string message, NotificationType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 50, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
}
