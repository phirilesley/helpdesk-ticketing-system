using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace HelpDeskSystem.Integrations.Teams;

[ApiController]
[Route("api/teams")]
public class TeamsController : ControllerBase
{
    private readonly ITeamsService _teamsService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(ITeamsService teamsService, ITicketService ticketService, ILogger<TeamsController> logger)
    {
        _teamsService = teamsService;
        _ticketService = ticketService;
        _logger = logger;
    }

    [HttpPost("messages/send")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendMessage([FromBody] SendTeamsMessageRequest request)
    {
        try
        {
            var messageId = await _teamsService.SendMessageAsync(request.ChannelId, request.Message, request.Format);
            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams message");
            return StatusCode(500, new { Error = "Failed to send Teams message" });
        }
    }

    [HttpPost("messages/send-adaptive-card")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendAdaptiveCard([FromBody] SendTeamsAdaptiveCardRequest request)
    {
        try
        {
            var messageId = await _teamsService.SendAdaptiveCardAsync(request.ChannelId, request.AdaptiveCard);
            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams adaptive card");
            return StatusCode(500, new { Error = "Failed to send Teams adaptive card" });
        }
    }

    [HttpPost("messages/send-proactive")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendProactiveMessage([FromBody] SendTeamsProactiveMessageRequest request)
    {
        try
        {
            var messageId = await _teamsService.SendProactiveMessageAsync(request.UserId, request.Message);
            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams proactive message");
            return StatusCode(500, new { Error = "Failed to send Teams proactive message" });
        }
    }

    [HttpPost("meetings/create")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<TeamsMeeting>> CreateMeeting([FromBody] TeamsMeetingRequest meetingRequest)
    {
        try
        {
            var meeting = await _teamsService.CreateMeetingAsync(meetingRequest);
            return Ok(meeting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Teams meeting");
            return StatusCode(500, new { Error = "Failed to create Teams meeting" });
        }
    }

    [HttpPost("meetings/{meetingId}/join")]
    [Authorize]
    public async Task<ActionResult> JoinMeeting(string meetingId, [FromBody] JoinMeetingRequest request)
    {
        try
        {
            var success = await _teamsService.JoinMeetingAsync(meetingId, request.UserId);
            if (success)
            {
                return Ok(new { Message = "Successfully joined meeting" });
            }
            return BadRequest(new { Error = "Failed to join meeting" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join Teams meeting");
            return StatusCode(500, new { Error = "Failed to join meeting" });
        }
    }

    [HttpPost("calls/initiate")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<TeamsCall>> InitiateCall([FromBody] TeamsCallRequest callRequest)
    {
        try
        {
            var call = await _teamsService.InitiateCallAsync(User.GetUserId(), callRequest);
            return Ok(call);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate Teams call");
            return StatusCode(500, new { Error = "Failed to initiate Teams call" });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessWebhook([FromBody] TeamsWebhookPayload payload)
    {
        try
        {
            // Verify webhook signature if configured
            if (Request.Headers.ContainsKey("X-Timestamp-Signature"))
            {
                var signature = Request.Headers["X-Timestamp-Signature"].ToString();
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                
                if (!await _teamsService.ValidateWebhookSignatureAsync(signature, body))
                {
                    _logger.LogWarning("Invalid Teams webhook signature");
                    return Unauthorized();
                }
            }

            var webhookEvent = await _teamsService.ProcessIncomingWebhookAsync(payload);
            
            if (webhookEvent != null)
            {
                await ProcessTeamsEventAsync(webhookEvent);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Teams webhook");
            return StatusCode(500);
        }
    }

    [HttpGet("teams/{teamId}/members")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<TeamsUser[]>> GetTeamMembers(string teamId)
    {
        try
        {
            var members = await _teamsService.GetTeamMembersAsync(teamId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team members for team {TeamId}", teamId);
            return StatusCode(500, new { Error = "Failed to get team members" });
        }
    }

    [HttpGet("teams/{teamId}/channels")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<TeamsChannel[]>> GetChannels(string teamId)
    {
        try
        {
            var channels = await _teamsService.GetChannelsAsync(teamId);
            return Ok(channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channels for team {TeamId}", teamId);
            return StatusCode(500, new { Error = "Failed to get channels" });
        }
    }

    [HttpPost("presence/get")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<TeamsPresence[]>> GetPresence([FromBody] GetPresenceRequest request)
    {
        try
        {
            var presence = await _teamsService.GetPresenceAsync(request.UserIds);
            return Ok(presence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get presence for users");
            return StatusCode(500, new { Error = "Failed to get presence" });
        }
    }

    [HttpPost("presence/update")]
    [Authorize]
    public async Task<ActionResult> UpdatePresence([FromBody] UpdatePresenceRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            var success = await _teamsService.UpdatePresenceAsync(userId, request.Presence);
            
            if (success)
            {
                return Ok(new { Message = "Presence updated successfully" });
            }
            
            return BadRequest(new { Error = "Failed to update presence" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update presence for user");
            return StatusCode(500, new { Error = "Failed to update presence" });
        }
    }

    private async Task ProcessTeamsEventAsync(TeamsWebhookEvent webhookEvent)
    {
        try
        {
            switch (webhookEvent.EventType)
            {
                case "message":
                    await ProcessIncomingMessageAsync(webhookEvent);
                    break;
                case "call":
                    await ProcessCallEventAsync(webhookEvent);
                    break;
                case "meeting":
                    await ProcessMeetingEventAsync(webhookEvent);
                    break;
                case "presence":
                    await ProcessPresenceEventAsync(webhookEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Teams event: {EventType}", webhookEvent.EventType);
        }
    }

    private async Task ProcessIncomingMessageAsync(TeamsWebhookEvent webhookEvent)
    {
        try
        {
            // Check if there's an existing ticket for this user
            var existingTickets = await _ticketService.GetTicketsByCustomerEmailAsync($"{webhookEvent.UserId}@teams.customer");
            
            if (existingTickets.Any())
            {
                // Add message to existing ticket
                var latestTicket = existingTickets.OrderByDescending(t => t.CreatedAt).First();
                var messageContent = GetMessageContent(webhookEvent);
                await _ticketService.AddMessageAsync(latestTicket.Id, messageContent, "Customer", webhookEvent.UserId);
            }
            else
            {
                // Create new ticket
                var messageContent = GetMessageContent(webhookEvent);
                var ticketRequest = new CreateTicketRequest
                {
                    Title = $"Teams message from {webhookEvent.UserId}",
                    Description = messageContent,
                    CustomerEmail = $"{webhookEvent.UserId}@teams.customer",
                    Priority = "Medium",
                    Category = "Microsoft Teams"
                };

                await _ticketService.CreateTicketAsync(ticketRequest);
            }

            _logger.LogInformation("Processed incoming Teams message from user {UserId}", webhookEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming Teams message");
        }
    }

    private async Task ProcessCallEventAsync(TeamsWebhookEvent webhookEvent)
    {
        try
        {
            // Create ticket for missed or incoming call
            var ticketRequest = new CreateTicketRequest
            {
                Title = $"Teams call from {webhookEvent.UserId}",
                Description = $"Teams call received. Event: {webhookEvent.EventType}",
                CustomerEmail = $"{webhookEvent.UserId}@teams.customer",
                Priority = "High",
                Category = "Microsoft Teams"
            };

            await _ticketService.CreateTicketAsync(ticketRequest);
            _logger.LogInformation("Processed Teams call event from user {UserId}", webhookEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Teams call event");
        }
    }

    private async Task ProcessMeetingEventAsync(TeamsWebhookEvent webhookEvent)
    {
        try
        {
            // Log meeting events
            _logger.LogInformation("Teams meeting event: {EventType} from user {UserId}", webhookEvent.EventType, webhookEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Teams meeting event");
        }
    }

    private async Task ProcessPresenceEventAsync(TeamsWebhookEvent webhookEvent)
    {
        try
        {
            // Update user presence in system
            _logger.LogInformation("Teams presence event: {EventType} from user {UserId}", webhookEvent.EventType, webhookEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Teams presence event");
        }
    }

    private string GetMessageContent(TeamsWebhookEvent webhookEvent)
    {
        var content = new StringBuilder();

        if (!string.IsNullOrEmpty(webhookEvent.MessageText))
        {
            content.Append(webhookEvent.MessageText);
        }

        if (webhookEvent.AdaptiveCard != null)
        {
            content.Append(" [Adaptive Card]");
        }

        if (webhookEvent.Mentions?.Any() == true)
        {
            var mentions = string.Join(", ", webhookEvent.Mentions.Select(m => m.MentionText));
            content.Append($" [Mentions: {mentions}]");
        }

        if (webhookEvent.Attachments?.Any() == true)
        {
            var attachments = string.Join(", ", webhookEvent.Attachments.Select(a => a.Name));
            content.Append($" [Attachments: {attachments}]");
        }

        return content.ToString();
    }
}

// Request DTOs
public class SendTeamsMessageRequest
{
    public string ChannelId { get; set; }
    public string Message { get; set; }
    public string Format { get; set; }
}

public class SendTeamsAdaptiveCardRequest
{
    public string ChannelId { get; set; }
    public TeamsAdaptiveCard AdaptiveCard { get; set; }
}

public class SendTeamsProactiveMessageRequest
{
    public string UserId { get; set; }
    public string Message { get; set; }
}

public class JoinMeetingRequest
{
    public string UserId { get; set; }
}

public class GetPresenceRequest
{
    public string[] UserIds { get; set; }
}

public class UpdatePresenceRequest
{
    public TeamsPresence Presence { get; set; }
}

// Extension method for getting user ID
public static class UserExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
    }
}
