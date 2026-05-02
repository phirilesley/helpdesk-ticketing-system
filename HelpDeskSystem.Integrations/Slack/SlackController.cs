using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace HelpDeskSystem.Integrations.Slack;

[ApiController]
[Route("api/slack")]
public class SlackController : ControllerBase
{
    private readonly ISlackService _slackService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<SlackController> _logger;

    public SlackController(ISlackService slackService, ITicketService ticketService, ILogger<SlackController> logger)
    {
        _slackService = slackService;
        _ticketService = ticketService;
        _logger = logger;
    }

    [HttpGet("webhook")]
    public async Task<IActionResult> VerifyWebhook([FromQuery] string challenge, [FromQuery] string token)
    {
        try
        {
            // Verify webhook token
            var options = HttpContext.RequestServices.GetRequiredService<IOptions<SlackOptions>>().Value;
            if (token != options.SigningSecret)
            {
                _logger.LogWarning("Invalid Slack webhook token");
                return Unauthorized();
            }

            return Content(challenge, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Slack webhook");
            return StatusCode(500);
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessWebhook([FromBody] SlackWebhookPayload payload)
    {
        try
        {
            // Verify webhook signature
            var signature = Request.Headers["X-Slack-Signature"].ToString();
            var timestamp = Request.Headers["X-Slack-Request-Timestamp"].ToString();
            
            Request.Body.Position = 0;
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            
            if (!await _slackService.VerifyWebhookSignatureAsync(signature, timestamp, requestBody))
            {
                _logger.LogWarning("Invalid Slack webhook signature");
                return Unauthorized();
            }

            var webhookEvent = await _slackService.ProcessIncomingWebhookAsync(payload);
            
            if (webhookEvent != null)
            {
                await ProcessSlackEventAsync(webhookEvent);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Slack webhook");
            return StatusCode(500);
        }
    }

    [HttpPost("messages/send")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendMessage([FromBody] SendSlackMessageRequest request)
    {
        try
        {
            var messageId = await _slackService.SendMessageAsync(request.ChannelId, request.Message, request.Format);
            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack message");
            return StatusCode(500, new { Error = "Failed to send Slack message" });
        }
    }

    [HttpPost("messages/send-blocks")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendBlockMessage([FromBody] SendSlackBlockMessageRequest request)
    {
        try
        {
            var messageId = await _slackService.SendBlockMessageAsync(request.ChannelId, request.Blocks);
            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack block message");
            return StatusCode(500, new { Error = "Failed to send Slack block message" });
        }
    }

    [HttpPost("messages/send-ephemeral")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendEphemeralMessage([FromBody] SendSlackEphemeralMessageRequest request)
    {
        try
        {
            var messageId = await _slackService.SendEphemeralMessageAsync(request.ChannelId, request.UserId, request.Message);
            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack ephemeral message");
            return StatusCode(500, new { Error = "Failed to send Slack ephemeral message" });
        }
    }

    [HttpPost("files/upload")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> UploadFile([FromForm] UploadSlackFileRequest request)
    {
        try
        {
            var fileId = await _slackService.UploadFileAsync(request.ChannelId, request.FilePath, request.Title, request.InitialComment);
            return Ok(new { FileId = fileId, Status = "uploaded" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Slack");
            return StatusCode(500, new { Error = "Failed to upload file to Slack" });
        }
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<SlackUser[]>> GetTeamMembers()
    {
        try
        {
            var members = await _slackService.GetTeamMembersAsync();
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get team members");
            return StatusCode(500, new { Error = "Failed to get team members" });
        }
    }

    [HttpGet("channels")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<SlackChannel[]>> GetChannels()
    {
        try
        {
            var channels = await _slackService.GetChannelsAsync();
            return Ok(channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channels");
            return StatusCode(500, new { Error = "Failed to get channels" });
        }
    }

    [HttpGet("users/{userId}")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<SlackUser>> GetUserProfile(string userId)
    {
        try
        {
            var user = await _slackService.GetUserProfileAsync(userId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user profile for {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to get user profile" });
        }
    }

    [HttpGet("users/{userId}/presence")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<SlackPresence>> GetUserPresence(string userId)
    {
        try
        {
            var presence = await _slackService.GetUserPresenceAsync(userId);
            return Ok(presence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user presence for {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to get user presence" });
        }
    }

    [HttpPost("users/{userId}/presence")]
    [Authorize]
    public async Task<ActionResult> SetUserPresence(string userId, [FromBody] SetSlackPresenceRequest request)
    {
        try
        {
            var success = await _slackService.SetUserPresenceAsync(userId, request.Presence);
            if (success)
            {
                return Ok(new { Message = "Presence updated successfully" });
            }
            return BadRequest(new { Error = "Failed to update presence" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user presence for {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to set user presence" });
        }
    }

    [HttpGet("channels/{channelId}/history")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<SlackConversationHistory>> GetConversationHistory(string channelId, [FromQuery] string cursor = null, [FromQuery] int limit = 100)
    {
        try
        {
            var history = await _slackService.GetConversationHistoryAsync(channelId, cursor, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conversation history for channel {ChannelId}", channelId);
            return StatusCode(500, new { Error = "Failed to get conversation history" });
        }
    }

    [HttpPost("conversations/open")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> OpenDirectMessage([FromBody] OpenDirectMessageRequest request)
    {
        try
        {
            var channelId = await _slackService.OpenDirectMessageAsync(request.UserId);
            return Ok(new { ChannelId = channelId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open direct message for user {UserId}", request.UserId);
            return StatusCode(500, new { Error = "Failed to open direct message" });
        }
    }

    [HttpPost("search/messages")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<SlackMessage[]>> SearchMessages([FromBody] SearchMessagesRequest request)
    {
        try
        {
            var messages = await _slackService.SearchMessagesAsync(request.Query, request.Count);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search messages with query: {Query}", request.Query);
            return StatusCode(500, new { Error = "Failed to search messages" });
        }
    }

    private async Task ProcessSlackEventAsync(SlackWebhookEvent webhookEvent)
    {
        try
        {
            switch (webhookEvent.EventType)
            {
                case "url_verification":
                    // Handle URL verification for Slack Events API
                    break;
                case "message":
                    await ProcessIncomingMessageAsync(webhookEvent);
                    break;
                case "reaction_added":
                case "reaction_removed":
                    await ProcessReactionEventAsync(webhookEvent);
                    break;
                case "member_joined_channel":
                case "member_left_channel":
                    await ProcessMemberEventAsync(webhookEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Slack event: {EventType}", webhookEvent.EventType);
        }
    }

    private async Task ProcessIncomingMessageAsync(SlackWebhookEvent webhookEvent)
    {
        try
        {
            // Check if there's an existing ticket for this user
            var existingTickets = await _ticketService.GetTicketsByCustomerEmailAsync($"{webhookEvent.UserId}@slack.customer");
            
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
                    Title = $"Slack message from {webhookEvent.UserId}",
                    Description = messageContent,
                    CustomerEmail = $"{webhookEvent.UserId}@slack.customer",
                    Priority = "Medium",
                    Category = "Slack"
                };

                await _ticketService.CreateTicketAsync(ticketRequest);
            }

            _logger.LogInformation("Processed incoming Slack message from user {UserId}", webhookEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming Slack message");
        }
    }

    private async Task ProcessReactionEventAsync(SlackWebhookEvent webhookEvent)
    {
        try
        {
            // Log reaction events for analytics
            _logger.LogInformation("Slack reaction event: {EventType} from user {UserId}", webhookEvent.EventType, webhookEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Slack reaction event");
        }
    }

    private async Task ProcessMemberEventAsync(SlackWebhookEvent webhookEvent)
    {
        try
        {
            // Log member events for analytics
            _logger.LogInformation("Slack member event: {EventType} for user {UserId}", webhookEvent.EventType, webhookEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Slack member event");
        }
    }

    private string GetMessageContent(SlackWebhookEvent webhookEvent)
    {
        var content = new StringBuilder();

        if (!string.IsNullOrEmpty(webhookEvent.MessageText))
        {
            content.Append(webhookEvent.MessageText);
        }

        if (webhookEvent.Blocks?.Any() == true)
        {
            content.Append(" [Rich Content]");
        }

        if (webhookEvent.Attachments?.Any() == true)
        {
            var attachments = string.Join(", ", webhookEvent.Attachments.Select(a => a.Title ?? "Attachment"));
            content.Append($" [Attachments: {attachments}]");
        }

        if (webhookEvent.Files?.Any() == true)
        {
            var files = string.Join(", ", webhookEvent.Files.Select(f => f.Name));
            content.Append($" [Files: {files}]");
        }

        if (webhookEvent.Reactions?.Any() == true)
        {
            var reactions = string.Join(" ", webhookEvent.Reactions.Select(r => $"{r.Name}({r.Count})"));
            content.Append($" [Reactions: {reactions}]");
        }

        if (webhookEvent.IsThreaded)
        {
            content.Append(" [Thread]");
        }

        return content.ToString();
    }
}

// Request DTOs
public class SendSlackMessageRequest
{
    public string ChannelId { get; set; }
    public string Message { get; set; }
    public string Format { get; set; }
}

public class SendSlackBlockMessageRequest
{
    public string ChannelId { get; set; }
    public SlackBlock[] Blocks { get; set; }
}

public class SendSlackEphemeralMessageRequest
{
    public string ChannelId { get; set; }
    public string UserId { get; set; }
    public string Message { get; set; }
}

public class UploadSlackFileRequest
{
    public string ChannelId { get; set; }
    public string FilePath { get; set; }
    public string Title { get; set; }
    public string InitialComment { get; set; }
}

public class SetSlackPresenceRequest
{
    public string Presence { get; set; }
}

public class OpenDirectMessageRequest
{
    public string UserId { get; set; }
}

public class SearchMessagesRequest
{
    public string Query { get; set; }
    public int Count { get; set; } = 25;
}
