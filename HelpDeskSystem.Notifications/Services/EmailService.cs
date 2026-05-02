using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Notifications.Models;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace HelpDeskSystem.Notifications.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(int userId, string subject, string htmlBody, string textBody, string? referenceType = null, int? referenceId = null);
    Task<bool> SendEmailToAddressAsync(string toEmail, string subject, string htmlBody, string textBody, int tenantId);
    Task<bool> SendTemplateEmailAsync(int userId, string templateType, Dictionary<string, object> variables, string? referenceType = null, int? referenceId = null);
    Task<bool> ProcessPendingEmailsAsync();
    Task<EmailTemplate?> GetTemplateAsync(string templateType, int tenantId);
    Task<bool> CreateTemplateAsync(EmailTemplate template);
    Task<bool> UpdateTemplateAsync(EmailTemplate template);
    Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables);
}

public class EmailService : IEmailService
{
    private readonly HelpDeskDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IOptions<EmailSettings> _emailSettings;

    public EmailService(
        HelpDeskDbContext context,
        ILogger<EmailService> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _logger = logger;
        _emailSettings = emailSettings;
    }

    public async Task<bool> SendEmailAsync(int userId, string subject, string htmlBody, string textBody, string? referenceType = null, int? referenceId = null)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || string.IsNullOrEmpty(user.Email))
            return false;

        return await SendEmailToAddressAsync(user.Email, subject, htmlBody, textBody, user.TenantId ?? 0);
    }

    public async Task<bool> SendEmailToAddressAsync(string toEmail, string subject, string htmlBody, string textBody, int tenantId)
    {
        try
        {
            var settings = await GetNotificationSettingsAsync(tenantId);
            if (!settings.EmailEnabled)
            {
                _logger.LogWarning("Email is disabled for tenant {TenantId}", tenantId);
                return false;
            }

            var emailNotification = new EmailNotification
            {
                TenantId = tenantId,
                ToEmail = toEmail,
                Subject = subject,
                HtmlBody = htmlBody,
                TextBody = textBody,
                Status = NotificationStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _context.EmailNotifications.Add(emailNotification);
            await _context.SaveChangesAsync();

            // Process email immediately
            return await ProcessSingleEmailAsync(emailNotification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendTemplateEmailAsync(int userId, string templateType, Dictionary<string, object> variables, string? referenceType = null, int? referenceId = null)
    {
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        var template = await GetTemplateAsync(templateType, user.TenantId ?? 0);
        if (template == null)
        {
            _logger.LogWarning("Email template {TemplateType} not found for tenant {TenantId}", templateType, user.TenantId);
            return false;
        }

        // Add common variables
        variables["UserName"] = user.FirstName + " " + user.LastName;
        variables["UserEmail"] = user.Email;
        variables["TenantName"] = user.Tenant?.Name ?? "Support";
        variables["CurrentDate"] = DateTime.Now.ToString("MMMM dd, yyyy");

        var subject = await ProcessTemplateAsync(template.Subject, variables);
        var htmlBody = await ProcessTemplateAsync(template.HtmlBody, variables);
        var textBody = await ProcessTemplateAsync(template.TextBody, variables);

        return await SendEmailAsync(userId, subject, htmlBody, textBody, referenceType, referenceId);
    }

    public async Task<bool> ProcessPendingEmailsAsync()
    {
        var pendingEmails = await _context.EmailNotifications
            .Where(e => e.Status == NotificationStatus.Pending && e.RetryCount < 3)
            .OrderBy(e => e.CreatedAtUtc)
            .Take(50) // Process in batches
            .ToListAsync();

        var successCount = 0;
        foreach (var email in pendingEmails)
        {
            if (await ProcessSingleEmailAsync(email.Id))
                successCount++;
        }

        _logger.LogInformation("Processed {SuccessCount} of {TotalCount} pending emails", successCount, pendingEmails.Count);
        return successCount > 0;
    }

    private async Task<bool> ProcessSingleEmailAsync(int emailId)
    {
        var email = await _context.EmailNotifications.FindAsync(emailId);
        if (email == null)
            return false;

        try
        {
            email.Status = NotificationStatus.Processing;
            email.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var settings = await GetNotificationSettingsAsync(email.TenantId);
            
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl = settings.SmtpUseSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = email.Subject,
                Body = email.HtmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email.ToEmail);

            // Add text version for clients that don't support HTML
            if (!string.IsNullOrEmpty(email.TextBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(email.TextBody, null, "text/plain");
                mailMessage.AlternateViews.Add(textView);
            }

            await client.SendMailAsync(mailMessage);

            email.Status = NotificationStatus.Sent;
            email.SentAtUtc = DateTime.UtcNow;
            email.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email sent successfully to {Email}", email.ToEmail);
            return true;
        }
        catch (Exception ex)
        {
            email.Status = NotificationStatus.Failed;
            email.ErrorMessage = ex.Message;
            email.RetryCount++;
            email.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Failed to send email to {Email}. Attempt {RetryCount}", email.ToEmail, email.RetryCount);
            return false;
        }
    }

    public async Task<EmailTemplate?> GetTemplateAsync(string templateType, int tenantId)
    {
        return await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateType == templateType && t.TenantId == tenantId && t.IsActive && !t.IsDeleted);
    }

    public async Task<bool> CreateTemplateAsync(EmailTemplate template)
    {
        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateTemplateAsync(EmailTemplate template)
    {
        _context.EmailTemplates.Update(template);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var result = template;

        // Process {{variable}} placeholders
        var regex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.IgnoreCase);
        var matches = regex.Matches(result);

        foreach (Match match in matches)
        {
            var variableName = match.Groups[1].Value;
            if (variables.TryGetValue(variableName, out var value))
            {
                result = result.Replace(match.Value, value?.ToString() ?? string.Empty);
            }
        }

        return result;
    }

    private async Task<NotificationSettings> GetNotificationSettingsAsync(int tenantId)
    {
        // This would typically come from a settings table
        // For now, we'll use default settings
        return new NotificationSettings
        {
            TenantId = tenantId,
            EmailEnabled = true,
            InAppEnabled = true,
            SmtpHost = _emailSettings.Value.SmtpHost,
            SmtpPort = _emailSettings.Value.SmtpPort,
            SmtpUseSsl = _emailSettings.Value.SmtpUseSsl,
            SmtpUsername = _emailSettings.Value.SmtpUsername,
            SmtpPassword = _emailSettings.Value.SmtpPassword,
            FromEmail = _emailSettings.Value.FromEmail,
            FromName = _emailSettings.Value.FromName
        };
    }
}

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
