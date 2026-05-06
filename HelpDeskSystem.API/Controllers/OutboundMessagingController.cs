using System.Text.Json;
using HelpDeskSystem.API.Security;
using HelpDeskSystem.API.Services;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Route("api/omnichannel/outbound")]
public class OutboundMessagingController : ControllerBase
{
    private readonly IOutboundMessagingService _outboundMessagingService;
    private readonly HelpDeskDbContext _context;

    public OutboundMessagingController(IOutboundMessagingService outboundMessagingService, HelpDeskDbContext context)
    {
        _outboundMessagingService = outboundMessagingService;
        _context = context;
    }

    [HttpPost("queue")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<ActionResult<OutboundChannelMessage>> Queue([FromBody] QueueOutboundMessageApiRequest request)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var connector = await _context.OmnichannelConnectors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ConnectorId && !x.IsDeleted, HttpContext.RequestAborted);
        if (connector == null)
            return NotFound(new { error = "Connector not found." });

        var resolvedTenantId = User.IsInRole("SuperAdmin")
            ? request.TenantId ?? connector.TenantId
            : User.GetTenantId();
        if (!resolvedTenantId.HasValue || connector.TenantId != resolvedTenantId.Value)
            return Forbid();

        var queued = await _outboundMessagingService.QueueMessageAsync(new QueueOutboundMessageRequest
        {
            ConnectorId = request.ConnectorId,
            TicketId = request.TicketId,
            RequestedByUserId = userId.Value,
            IdempotencyKey = request.IdempotencyKey,
            RecipientAddress = request.RecipientAddress,
            Subject = request.Subject,
            Content = request.Content,
            MetadataJson = request.MetadataJson,
            MaxAttempts = request.MaxAttempts
        }, HttpContext.RequestAborted);

        return Ok(queued);
    }

    [HttpGet("messages")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<OutboundChannelMessage>>> GetMessages(
        [FromQuery] int? tenantId = null,
        [FromQuery] int? connectorId = null,
        [FromQuery] OutboundMessageStatus? status = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var query = _context.OutboundChannelMessages
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted);

        if (connectorId.HasValue)
            query = query.Where(x => x.ConnectorId == connectorId.Value);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(500)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(items);
    }

    [HttpGet("receipts/{messageId:int}")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<OutboundDeliveryReceipt>>> GetReceipts(int messageId, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var exists = await _context.OutboundChannelMessages
            .AsNoTracking()
            .AnyAsync(x => x.Id == messageId && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted);
        if (!exists)
            return NotFound();

        var receipts = await _context.OutboundDeliveryReceipts
            .Where(x => x.OutboundChannelMessageId == messageId && x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.ReceivedAtUtc)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(receipts);
    }

    [HttpPost("receipt/{connectorId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult> RecordReceipt(int connectorId, [FromBody] JsonElement payload)
    {
        var providerMessageId = ReadFirst(payload, "messageId", "MessageSid", "wamid", "id");
        var statusRaw = ReadFirst(payload, "status", "MessageStatus", "event");
        if (string.IsNullOrWhiteSpace(providerMessageId))
            return BadRequest(new { error = "Provider message id is required." });

        var status = ParseReceiptStatus(statusRaw);
        await _outboundMessagingService.RecordReceiptAsync(
            connectorId,
            providerMessageId,
            status,
            payload.GetRawText(),
            HttpContext.RequestAborted);

        return Accepted(new { providerMessageId, status = status.ToString() });
    }

    private static string ReadFirst(JsonElement payload, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(key, out var value))
            {
                if (value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;
                return value.GetRawText();
            }
        }

        return string.Empty;
    }

    private static DeliveryReceiptStatus ParseReceiptStatus(string raw)
    {
        var value = raw.Trim().ToLowerInvariant();
        return value switch
        {
            "queued" or "accepted" => DeliveryReceiptStatus.Accepted,
            "sent" => DeliveryReceiptStatus.Sent,
            "delivered" => DeliveryReceiptStatus.Delivered,
            "read" => DeliveryReceiptStatus.Read,
            "failed" or "undelivered" => DeliveryReceiptStatus.Failed,
            _ => DeliveryReceiptStatus.Sent
        };
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }
}

public class QueueOutboundMessageApiRequest
{
    public int? TenantId { get; set; }
    public int ConnectorId { get; set; }
    public int TicketId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public int? MaxAttempts { get; set; }
}
