using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using HelpDeskSystem.Application.Configuration;
using HelpDeskSystem.Application.DTOs.Notifications;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HelpDeskSystem.Application.Services;

public class NotificationService : INotificationService
{
    private readonly HelpDeskDbContext _context;
    private readonly NotificationChannelOptions _channels;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        HelpDeskDbContext context,
        NotificationChannelOptions channels,
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _channels = channels;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task NotifyAsync(
        int userId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        var userEmail = await _context.Users
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await SendEmailIfEnabledAsync(userEmail, title, message, cancellationToken);
        await SendWebhookIfEnabledAsync(notification, userEmail, cancellationToken);
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(
        int userId,
        bool unreadOnly = false,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId && !n.IsDeleted);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc,
                ReadAtUtc = n.ReadAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted,
                cancellationToken);

        if (notification == null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            notification.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private async Task SendEmailIfEnabledAsync(string? toEmail, string title, string message, CancellationToken cancellationToken)
    {
        if (!_channels.Email.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(_channels.Email.SmtpHost) || string.IsNullOrWhiteSpace(_channels.Email.FromAddress))
            return;

        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(_channels.Email.FromAddress, _channels.Email.FromDisplayName),
                Subject = title,
                Body = message,
                IsBodyHtml = false
            };

            mail.To.Add(toEmail);

            using var smtp = new SmtpClient(_channels.Email.SmtpHost, _channels.Email.Port)
            {
                EnableSsl = _channels.Email.UseSsl,
                Credentials = string.IsNullOrWhiteSpace(_channels.Email.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_channels.Email.Username, _channels.Email.Password)
            };

            using var registration = cancellationToken.Register(() => smtp.SendAsyncCancel());
            await smtp.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email notification send failed for {Email}", toEmail);
        }
    }

    private async Task SendWebhookIfEnabledAsync(Notification notification, string? email, CancellationToken cancellationToken)
    {
        if (!_channels.Webhook.Enabled || _channels.Webhook.Urls.Count == 0)
            return;

        var payload = new
        {
            notification.Id,
            notification.UserId,
            UserEmail = email,
            notification.Title,
            notification.Message,
            Type = notification.Type.ToString(),
            notification.CreatedAtUtc
        };

        var json = JsonSerializer.Serialize(payload);

        foreach (var url in _channels.Webhook.Urls.Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                if (!string.IsNullOrWhiteSpace(_channels.Webhook.BearerToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channels.Webhook.BearerToken);
                }

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook notification send failed for {Url}", url);
            }
        }
    }
}
