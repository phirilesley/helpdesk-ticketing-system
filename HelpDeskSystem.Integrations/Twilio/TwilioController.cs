using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace HelpDeskSystem.Integrations.Twilio;

[ApiController]
[Route("api/twilio")]
public class TwilioController : ControllerBase
{
    private readonly ITwilioService _twilioService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<TwilioController> _logger;

    public TwilioController(ITwilioService twilioService, ITicketService ticketService, ILogger<TwilioController> logger)
    {
        _twilioService = twilioService;
        _ticketService = ticketService;
        _logger = logger;
    }

    [HttpPost("sms/send")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> SendSms([FromBody] SendSmsRequest request)
    {
        try
        {
            var messageSid = await _twilioService.SendSmsAsync(request.To, request.Message, request.FromNumber);
            return Ok(new { MessageSid = messageSid, Status = "sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS");
            return StatusCode(500, new { Error = "Failed to send SMS" });
        }
    }

    [HttpPost("call/initiate")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<ActionResult<string>> InitiateCall([FromBody] InitiateCallRequest request)
    {
        try
        {
            var callSid = await _twilioService.InitiateCallAsync(request.To, request.FromNumber, request.WebhookUrl);
            return Ok(new { CallSid = callSid, Status = "initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate call");
            return StatusCode(500, new { Error = "Failed to initiate call" });
        }
    }

    [HttpPost("sms/incoming")]
    [AllowAnonymous]
    public async Task<ActionResult> IncomingSms()
    {
        try
        {
            var formData = new Dictionary<string, string>();
            foreach (var key in Request.Form.Keys)
            {
                formData[key] = Request.Form[key];
            }

            // Validate webhook signature
            var signature = Request.Headers["X-Twilio-Signature"];
            var webhookUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            
            if (!await _twilioService.ValidateWebhookSignatureAsync(signature, webhookUrl, formData))
            {
                _logger.LogWarning("Invalid Twilio webhook signature");
                return Unauthorized();
            }

            var webhookEvent = await _twilioService.ProcessIncomingWebhookAsync(formData);
            
            // Create or update ticket from incoming SMS
            await ProcessIncomingMessageAsync(webhookEvent);

            // Return TwiML response
            var twiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Message>Thank you for your message. We'll get back to you shortly.</Message>
</Response>";

            return Content(twiml, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming SMS");
            return StatusCode(500);
        }
    }

    [HttpPost("call/incoming")]
    [AllowAnonymous]
    public async Task<ActionResult> IncomingCall()
    {
        try
        {
            var formData = new Dictionary<string, string>();
            foreach (var key in Request.Form.Keys)
            {
                formData[key] = Request.Form[key];
            }

            // Validate webhook signature
            var signature = Request.Headers["X-Twilio-Signature"];
            var webhookUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            
            if (!await _twilioService.ValidateWebhookSignatureAsync(signature, webhookUrl, formData))
            {
                _logger.LogWarning("Invalid Twilio webhook signature");
                return Unauthorized();
            }

            var webhookEvent = await _twilioService.ProcessIncomingWebhookAsync(formData);
            
            // Create ticket from incoming call
            await ProcessIncomingCallAsync(webhookEvent);

            // Return TwiML response for call handling
            var twiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say>Thank you for calling. Please leave a message after the tone.</Say>
    <Record maxLength=""30"" action=""/api/twilio/call/recording"" method=""POST"" />
    <Hangup />
</Response>";

            return Content(twiml, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming call");
            return StatusCode(500);
        }
    }

    [HttpPost("call/recording")]
    [AllowAnonymous]
    public async Task<ActionResult> CallRecording()
    {
        try
        {
            var formData = new Dictionary<string, string>();
            foreach (var key in Request.Form.Keys)
            {
                formData[key] = Request.Form[key];
            }

            var recordingUrl = formData.GetValueOrDefault("RecordingUrl");
            var callSid = formData.GetValueOrDefault("CallSid");
            
            _logger.LogInformation("Received call recording: {RecordingUrl} for call: {CallSid}", recordingUrl, callSid);

            // Process recording and attach to ticket
            await ProcessCallRecordingAsync(callSid, recordingUrl);

            var twiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say>Thank you for your message. We'll get back to you shortly.</Say>
    <Hangup />
</Response>";

            return Content(twiml, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process call recording");
            return StatusCode(500);
        }
    }

    [HttpGet("phone-numbers/available")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TwilioPhoneNumber[]>> GetAvailablePhoneNumbers()
    {
        try
        {
            var phoneNumbers = await _twilioService.GetAvailablePhoneNumbersAsync();
            return Ok(phoneNumbers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available phone numbers");
            return StatusCode(500, new { Error = "Failed to get available phone numbers" });
        }
    }

    [HttpPost("phone-numbers/purchase")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> PurchasePhoneNumber([FromBody] PurchasePhoneNumberRequest request)
    {
        try
        {
            var success = await _twilioService.PurchasePhoneNumberAsync(request.PhoneNumber);
            if (success)
            {
                return Ok(new { Message = "Phone number purchased successfully" });
            }
            return BadRequest(new { Error = "Failed to purchase phone number" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purchase phone number");
            return StatusCode(500, new { Error = "Failed to purchase phone number" });
        }
    }

    [HttpGet("logs/calls")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TwilioCallLog[]>> GetCallLogs([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var callLogs = await _twilioService.GetCallLogsAsync(from, to);
            return Ok(callLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get call logs");
            return StatusCode(500, new { Error = "Failed to get call logs" });
        }
    }

    [HttpGet("logs/messages")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TwilioMessageLog[]>> GetMessageLogs([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var messageLogs = await _twilioService.GetMessageLogsAsync(from, to);
            return Ok(messageLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message logs");
            return StatusCode(500, new { Error = "Failed to get message logs" });
        }
    }

    private async Task ProcessIncomingMessageAsync(TwilioWebhookEvent webhookEvent)
    {
        try
        {
            // Check if there's an existing ticket for this phone number
            var existingTickets = await _ticketService.GetTicketsByCustomerPhoneAsync(webhookEvent.From);
            
            if (existingTickets.Any())
            {
                // Add message to existing ticket
                var latestTicket = existingTickets.OrderByDescending(t => t.CreatedAt).First();
                await _ticketService.AddMessageAsync(latestTicket.Id, webhookEvent.Body, "Customer", webhookEvent.From);
            }
            else
            {
                // Create new ticket
                var ticketRequest = new CreateTicketRequest
                {
                    Title = $"SMS from {webhookEvent.From}",
                    Description = webhookEvent.Body,
                    CustomerEmail = $"{webhookEvent.From}@sms.customer",
                    CustomerPhone = webhookEvent.From,
                    Priority = "Medium",
                    Category = "Inbound SMS"
                };

                await _ticketService.CreateTicketAsync(ticketRequest);
            }

            _logger.LogInformation("Processed incoming SMS from {From}", webhookEvent.From);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming SMS");
        }
    }

    private async Task ProcessIncomingCallAsync(TwilioWebhookEvent webhookEvent)
    {
        try
        {
            // Create ticket for missed or incoming call
            var ticketRequest = new CreateTicketRequest
            {
                Title = $"Incoming call from {webhookEvent.From}",
                Description = $"Incoming call received. Call SID: {webhookEvent.CallSid}",
                CustomerEmail = $"{webhookEvent.From}@call.customer",
                CustomerPhone = webhookEvent.From,
                Priority = "High",
                Category = "Inbound Call"
            };

            await _ticketService.CreateTicketAsync(ticketRequest);
            _logger.LogInformation("Processed incoming call from {From}", webhookEvent.From);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming call");
        }
    }

    private async Task ProcessCallRecordingAsync(string callSid, string recordingUrl)
    {
        try
        {
            // Find ticket associated with this call and attach recording
            var tickets = await _ticketService.SearchTicketsAsync(callSid);
            var ticket = tickets.FirstOrDefault();
            
            if (ticket != null)
            {
                // Add recording as attachment or note
                await _ticketService.AddMessageAsync(ticket.Id, 
                    $"Call recording available: {recordingUrl}", 
                    "System", 
                    "Twilio Recording");
            }

            _logger.LogInformation("Processed call recording for call: {CallSid}", callSid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process call recording");
        }
    }
}

public class SendSmsRequest
{
    public string To { get; set; }
    public string Message { get; set; }
    public string FromNumber { get; set; }
}

public class InitiateCallRequest
{
    public string To { get; set; }
    public string FromNumber { get; set; }
    public string WebhookUrl { get; set; }
}

public class PurchasePhoneNumberRequest
{
    public string PhoneNumber { get; set; }
}
