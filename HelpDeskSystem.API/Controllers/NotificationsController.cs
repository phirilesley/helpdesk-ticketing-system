using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.DTOs.Notifications;
using HelpDeskSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int limit = 50)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notifications = await _notificationService.GetUserNotificationsAsync(
            userId.Value,
            unreadOnly,
            limit,
            HttpContext.RequestAborted);

        return Ok(notifications);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var updated = await _notificationService.MarkAsReadAsync(id, userId.Value, HttpContext.RequestAborted);
        if (!updated)
            return NotFound();

        return NoContent();
    }
}
