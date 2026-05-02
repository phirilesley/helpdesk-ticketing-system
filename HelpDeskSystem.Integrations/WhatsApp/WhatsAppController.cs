using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace HelpDeskSystem.Integrations.WhatsApp;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(IWhatsAppService whatsAppService, ITicketService ticketService, ILogger<WhatsAppController> logger)
    {
        _whatsAppService = whatsAppService;
        _ticketService = ticketService;
        _logger = logger;
    }

    [HttpGet("webhook")]
    public async Task<IActionResult> VerifyWebhook([FromQuery] string hubMode, [FromQuery] string hubVerifyToken, [FromQuery] string hubChallenge)
    {
        try
        {
            var isValid = await _whatsAppService.VerifyWebhookAsync(hubMode, hubVerifyToken, hubChallenge);
            if (isValid)
            {
                return Content(hubChallenge, "text/plain");
            }

            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify WhatsApp webhook");
            return StatusCode(500);
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessWebhook([FromBody] WhatsAppWebhookPayload payload)
    {
        try
        {
            // Verify webhook signature if configured
            if (Request.Headers.ContainsKey("X-Hub-Signature-256"))
            {
                var signature = Request.Headers["X-Hub-Signature-256"].ToString();
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                
                if (!VerifyWebhookSignature(signature, body))
                {
                    _logger.LogWarning("Invalid WhatsApp webhook signature");
                    return Unauthorized();
                }
            }

            var webhookEvent = await _whatsAppService.ProcessIncomingWebhookAsync(payload);
            
            if (webhookEvent != null)
            {
                await ProcessWhatsAppEventAsync(webhookEvent);
                
                // Mark message as read if it's an incoming message
                if (webhookEvent.EventType == "message" && !string.IsNullOrEmpty(webhookEvent.MessageId))
                {
                    await _whatsAppService.MarkMessageAsReadAsync(webhookEvent.MessageId);
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process WhatsApp webhook");
            return StatusCode(500);
        }
    }

    [HttpPost("messages/send")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendMessage([FromBody] SendWhatsAppMessageRequest request)
    {
        try
        {
            var messageId = await _whatsAppService.SendMessageAsync(
                request.To, 
                request.Message, 
                request.TemplateName, 
                request.TemplateVariables);

            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message");
            return StatusCode(500, new { Error = "Failed to send WhatsApp message" });
        }
    }

    [HttpPost("messages/send-media")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendMediaMessage([FromBody] SendWhatsAppMediaRequest request)
    {
        try
        {
            var messageId = await _whatsAppService.SendMediaMessageAsync(request.To, request.MediaUrl, request.MediaType, request.Caption);
            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp media message");
            return StatusCode(500, new { Error = "Failed to send WhatsApp media message" });
        }
    }

    [HttpPost("messages/send-interactive")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendInteractiveMessage([FromBody] SendWhatsAppInteractiveRequest request)
    {
        try
        {
            var messageId = await _whatsAppService.SendInteractiveMessageAsync(request.To, request.InteractiveMessage);
            return Ok(new { MessageId = messageId, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp interactive message");
            return StatusCode(500, new { Error = "Failed to send WhatsApp interactive message" });
        }
    }

    [HttpGet("phone-numbers")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WhatsAppPhoneNumber[]>> GetConnectedPhoneNumbers()
    {
        try
        {
            var phoneNumbers = await _whatsAppService.GetConnectedPhoneNumbersAsync();
            return Ok(phoneNumbers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connected phone numbers");
            return StatusCode(500, new { Error = "Failed to get connected phone numbers" });
        }
    }

    [HttpGet("templates")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WhatsAppMessageTemplate[]>> GetMessageTemplates()
    {
        try
        {
            var templates = await _whatsAppService.GetMessageTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message templates");
            return StatusCode(500, new { Error = "Failed to get message templates" });
        }
    }

    [HttpGet("analytics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WhatsAppAnalytics>> GetAnalytics([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        try
        {
            var analytics = await _whatsAppService.GetAnalyticsAsync(from, to);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WhatsApp analytics");
            return StatusCode(500, new { Error = "Failed to get WhatsApp analytics" });
        }
    }

    private bool VerifyWebhookSignature(string signature, string payload)
    {
        try
        {
            var options = HttpContext.RequestServices.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
            
            if (string.IsNullOrEmpty(options.WebhookSecret))
            {
                return true; // Skip verification if no secret is configured
            }

            var expectedSignature = $"sha256={ComputeHmacSha256(options.WebhookSecret, payload)}";
            return signature == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify WhatsApp webhook signature");
            return false;
        }
    }

    private string ComputeHmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    private async Task ProcessWhatsAppEventAsync(WhatsAppWebhookEvent webhookEvent)
    {
        try
        {
            switch (webhookEvent.EventType)
            {
                case "message":
                    await ProcessIncomingMessageAsync(webhookEvent);
                    break;
                case "status":
                    await ProcessMessageStatusAsync(webhookEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process WhatsApp event: {EventType}", webhookEvent.EventType);
        }
    }

    private async Task ProcessIncomingMessageAsync(WhatsAppWebhookEvent webhookEvent)
    {
        try
        {
            // Check if there's an existing ticket for this phone number
            var existingTickets = await _ticketService.GetTicketsByCustomerPhoneAsync(webhookEvent.From);
            
            if (existingTickets.Any())
            {
                // Add message to existing ticket
                var latestTicket = existingTickets.OrderByDescending(t => t.CreatedAt).First();
                var messageContent = GetMessageContent(webhookEvent);
                await _ticketService.AddMessageAsync(latestTicket.Id, messageContent, "Customer", webhookEvent.From);
            }
            else
            {
                // Create new ticket
                var messageContent = GetMessageContent(webhookEvent);
                var ticketRequest = new CreateTicketRequest
                {
                    Title = $"WhatsApp message from {webhookEvent.From}",
                    Description = messageContent,
                    CustomerEmail = $"{webhookEvent.From}@whatsapp.customer",
                    CustomerPhone = webhookEvent.From,
                    Priority = "Medium",
                    Category = "WhatsApp"
                };

                await _ticketService.CreateTicketAsync(ticketRequest);
            }

            _logger.LogInformation("Processed incoming WhatsApp message from {From}", webhookEvent.From);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming WhatsApp message");
        }
    }

    private async Task ProcessMessageStatusAsync(WhatsAppWebhookEvent webhookEvent)
    {
        try
        {
            // Update message status in database
            _logger.LogInformation("WhatsApp message status updated: {MessageId} - {Status}", 
                webhookEvent.MessageId, webhookEvent.Status?.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process WhatsApp message status");
        }
    }

    private string GetMessageContent(WhatsAppWebhookEvent webhookEvent)
    {
        var content = new StringBuilder();

        switch (webhookEvent.MessageType)
        {
            case "text":
                content.Append(webhookEvent.MessageText);
                break;
            case "image":
                content.Append($"📷 Image: {webhookEvent.Media?.Id}");
                break;
            case "audio":
                content.Append($"🎵 Audio: {webhookEvent.Media?.Id}");
                break;
            case "video":
                content.Append($"🎥 Video: {webhookEvent.Media?.Id}");
                break;
            case "document":
                content.Append($"📄 Document: {webhookEvent.Media?.Id}");
                break;
            case "location":
                content.Append($"📍 Location: {webhookEvent.Location?.Name} ({webhookEvent.Location?.Latitude}, {webhookEvent.Location?.Longitude})");
                break;
            case "contact":
                content.Append($"👤 Contact: {webhookEvent.Contact?.Name?.FormattedName}");
                break;
            case "interactive":
                content.Append($"🔘 Interactive: {webhookEvent.Interactive?.Type}");
                break;
            default:
                content.Append($"📎 {webhookEvent.MessageType}");
                break;
        }

        return content.ToString();
    }
}

// Request DTOs
public class SendWhatsAppMessageRequest
{
    public string To { get; set; }
    public string Message { get; set; }
    public string TemplateName { get; set; }
    public Dictionary<string, string> TemplateVariables { get; set; }
}

public class SendWhatsAppMediaRequest
{
    public string To { get; set; }
    public string MediaUrl { get; set; }
    public string MediaType { get; set; }
    public string Caption { get; set; }
}

public class SendWhatsAppInteractiveRequest
{
    public string To { get; set; }
    public WhatsAppInteractiveMessage InteractiveMessage { get; set; }
}
