using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HelpDeskSystem.Files.Services;

public interface IFileService
{
    Task<TicketAttachment> UploadAttachmentAsync(int ticketId, int userId, IFormFile file, string contentType);
    Task<Stream> DownloadAttachmentAsync(int attachmentId, int userId);
    Task<bool> DeleteAttachmentAsync(int attachmentId, int userId);
    Task<IEnumerable<TicketAttachment>> GetTicketAttachmentsAsync(int ticketId, int userId);
    Task<TicketAttachment> GetAttachmentDetailsAsync(int attachmentId, int userId);
    Task<bool> ValidateFileAsync(IFormFile file);
}

public class FileService : IFileService
{
    private readonly HelpDeskDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FileService> _logger;
    private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".zip", ".rar" };
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

    public FileService(
        HelpDeskDbContext context,
        IHostEnvironment environment,
        ILogger<FileService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<TicketAttachment> UploadAttachmentAsync(int ticketId, int userId, IFormFile file, string contentType)
    {
        if (!await ValidateFileAsync(file))
            throw new InvalidOperationException("Invalid file");

        // Verify user has access to ticket
        var ticket = await _context.Tickets
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || (user.TenantId != ticket.TenantId && !user.IsSuperAdmin))
            throw new UnauthorizedAccessException("Access denied");

        // Create unique file name
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", ticket.TenantId.ToString());
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create attachment record
        var attachment = new TicketAttachment
        {
            TicketId = ticketId,
            UploadedByUserId = userId,
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.TicketAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("File {FileName} uploaded for ticket {TicketId} by user {UserId}", file.FileName, ticketId, userId);

        return attachment;
    }

    public async Task<Stream> DownloadAttachmentAsync(int attachmentId, int userId)
    {
        var attachment = await _context.TicketAttachments
            .Include(a => a.Ticket)
            .ThenInclude(t => t.Tenant)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);

        if (attachment == null)
            throw new NotFoundException("Attachment not found");

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || (user.TenantId != attachment.Ticket.TenantId && !user.IsSuperAdmin))
            throw new UnauthorizedAccessException("Access denied");

        if (!File.Exists(attachment.FilePath))
            throw new NotFoundException("File not found on disk");

        return new FileStream(attachment.FilePath, FileMode.Open, FileAccess.Read);
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, int userId)
    {
        var attachment = await _context.TicketAttachments
            .Include(a => a.Ticket)
            .ThenInclude(t => t.Tenant)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);

        if (attachment == null)
            return false;

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || (user.TenantId != attachment.Ticket.TenantId && !user.IsSuperAdmin))
            throw new UnauthorizedAccessException("Access denied");

        // Delete file from disk
        if (File.Exists(attachment.FilePath))
        {
            File.Delete(attachment.FilePath);
        }

        // Delete record
        _context.TicketAttachments.Remove(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Attachment {AttachmentId} deleted by user {UserId}", attachmentId, userId);

        return true;
    }

    public async Task<IEnumerable<TicketAttachment>> GetTicketAttachmentsAsync(int ticketId, int userId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            throw new NotFoundException("Ticket not found");

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || (user.TenantId != ticket.TenantId && !user.IsSuperAdmin))
            throw new UnauthorizedAccessException("Access denied");

        return await _context.TicketAttachments
            .Where(a => a.TicketId == ticketId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<TicketAttachment> GetAttachmentDetailsAsync(int attachmentId, int userId)
    {
        var attachment = await _context.TicketAttachments
            .Include(a => a.Ticket)
            .ThenInclude(t => t.Tenant)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);

        if (attachment == null)
            throw new NotFoundException("Attachment not found");

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || (user.TenantId != attachment.Ticket.TenantId && !user.IsSuperAdmin))
            throw new UnauthorizedAccessException("Access denied");

        return attachment;
    }

    public async Task<bool> ValidateFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Check file size
        if (file.Length > _maxFileSize)
            return false;

        // Check file extension
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(fileExtension))
            return false;

        // Check content type
        var allowedContentTypes = new[]
        {
            "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain", "image/jpeg", "image/png", "image/gif", "application/zip", "application/x-rar-compressed"
        };

        if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        return true;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
