using HelpDeskSystem.API.Security;
using HelpDeskSystem.Files.DTOs;
using HelpDeskSystem.Files.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly IFileService _fileService;

    public AttachmentsController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("tickets/{ticketId}/upload")]
    public async Task<ActionResult<FileUploadDto>> UploadAttachment(int ticketId, IFormFile file)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var attachment = await _fileService.UploadAttachmentAsync(ticketId, userId.Value, file, file.ContentType);
            
            var result = new FileUploadDto
            {
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                DownloadUrl = $"/api/attachments/{attachment.Id}/download",
                UploadedAt = attachment.CreatedAtUtc,
                UploadedBy = User.Identity?.Name ?? "Unknown"
            };

            return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<FileUploadDto>> GetAttachment(int id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var attachment = await _fileService.GetAttachmentDetailsAsync(id, userId.Value);
            
            var result = new FileUploadDto
            {
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                DownloadUrl = $"/api/attachments/{attachment.Id}/download",
                UploadedAt = attachment.CreatedAtUtc,
                UploadedBy = "User" // Would need to join with Users table for actual name
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ex.Message.Contains("not found") ? NotFound() : BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var stream = await _fileService.DownloadAttachmentAsync(id, userId.Value);
            var attachment = await _fileService.GetAttachmentDetailsAsync(id, userId.Value);
            
            return File(stream, attachment.ContentType, attachment.FileName);
        }
        catch (Exception ex)
        {
            return ex.Message.Contains("not found") ? NotFound() : BadRequest(ex.Message);
        }
    }

    [HttpGet("tickets/{ticketId}")]
    public async Task<ActionResult<IEnumerable<FileUploadDto>>> GetTicketAttachments(int ticketId)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var attachments = await _fileService.GetTicketAttachmentsAsync(ticketId, userId.Value);
            
            var result = attachments.Select(a => new FileUploadDto
            {
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                DownloadUrl = $"/api/attachments/{a.Id}/download",
                UploadedAt = a.CreatedAtUtc,
                UploadedBy = "User" // Would need to join with Users table for actual name
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return ex.Message.Contains("not found") ? NotFound() : BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var result = await _fileService.DeleteAttachmentAsync(id, userId.Value);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return ex.Message.Contains("not found") ? NotFound() : BadRequest(ex.Message);
        }
    }

    [HttpGet("validation")]
    public ActionResult<FileValidationResult> GetFileValidationRules()
    {
        return Ok(new FileValidationResult
        {
            IsValid = true,
            MaxFileSize = 10 * 1024 * 1024, // 10MB
            AllowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".zip", ".rar" }
        });
    }
}
