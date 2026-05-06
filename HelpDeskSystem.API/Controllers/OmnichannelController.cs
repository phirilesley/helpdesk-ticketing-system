using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HelpDeskSystem.API.Security;
using HelpDeskSystem.API.Services;
using HelpDeskSystem.Application.DTOs.Tickets;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Route("api/omnichannel")]
public class OmnichannelController : ControllerBase
{
    private readonly HelpDeskDbContext _context;
    private readonly ITicketService _ticketService;
    private readonly IOmnichannelInboundNormalizationService _normalizationService;

    public OmnichannelController(
        HelpDeskDbContext context,
        ITicketService ticketService,
        IOmnichannelInboundNormalizationService normalizationService)
    {
        _context = context;
        _ticketService = ticketService;
        _normalizationService = normalizationService;
    }

    [HttpGet("connectors")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<OmnichannelConnector>>> GetConnectors([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var connectors = await _context.OmnichannelConnectors
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderBy(x => x.ChannelType)
            .ThenBy(x => x.Name)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(connectors);
    }

    [HttpPost("connectors")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<OmnichannelConnector>> UpsertConnector([FromBody] UpsertConnectorRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        OmnichannelConnector entity;
        if (request.Id.HasValue)
        {
            entity = await _context.OmnichannelConnectors
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new OmnichannelConnector { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new OmnichannelConnector { TenantId = resolvedTenantId.Value };
            _context.OmnichannelConnectors.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.ChannelType = request.ChannelType;
        entity.ProviderKey = request.ProviderKey.Trim().ToLowerInvariant();
        entity.ConfigJson = request.ConfigJson.Trim();
        entity.SignatureHeaderName = string.IsNullOrWhiteSpace(request.SignatureHeaderName) ? "X-Channel-Signature" : request.SignatureHeaderName.Trim();
        entity.SignatureAlgorithm = string.IsNullOrWhiteSpace(request.SignatureAlgorithm) ? "hmac-sha256" : request.SignatureAlgorithm.Trim().ToLowerInvariant();
        entity.DedupWindowMinutes = request.DedupWindowMinutes <= 0 ? 120 : request.DedupWindowMinutes;
        entity.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.InboundSigningSecret))
            entity.InboundSigningSecretHash = HashSecret(request.InboundSigningSecret);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.OmnichannelConnectors.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpPost("inbound/{connectorId:int}")]
    [EnableRateLimiting("omnichannel-webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> ReceiveInbound(int connectorId, [FromBody] InboundChannelRequest request)
    {
        var connector = await _context.OmnichannelConnectors
            .FirstOrDefaultAsync(x => x.Id == connectorId && !x.IsDeleted, HttpContext.RequestAborted);
        if (connector == null || connector.Status != ConnectorStatus.Enabled)
            return NotFound();

        var signatureHeader = string.IsNullOrWhiteSpace(connector.SignatureHeaderName)
            ? "X-Channel-Signature"
            : connector.SignatureHeaderName.Trim();
        var signature = Request.Headers[signatureHeader].ToString();
        if (!string.IsNullOrWhiteSpace(connector.InboundSigningSecretHash))
        {
            if (string.IsNullOrWhiteSpace(signature))
                return Unauthorized(new { error = $"Missing signature header '{signatureHeader}'." });

            if (!string.Equals(HashSecret(signature), connector.InboundSigningSecretHash, StringComparison.Ordinal))
                return Unauthorized();
        }

        var dedupWindowMinutes = connector.DedupWindowMinutes <= 0 ? 120 : connector.DedupWindowMinutes;
        var normalizedExternalMessageId = request.ExternalMessageId.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedExternalMessageId))
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-dedupWindowMinutes);
            var existing = await _context.InboundChannelEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ConnectorId == connector.Id
                    && x.ExternalMessageId == normalizedExternalMessageId
                    && x.CreatedAtUtc >= cutoff
                    && !x.IsDeleted,
                    HttpContext.RequestAborted);

            if (existing != null)
                return Accepted(new { existing.Id, existing.Status, existing.CreatedTicketId, duplicate = true });
        }

        var inbound = new InboundChannelEvent
        {
            TenantId = connector.TenantId,
            ConnectorId = connector.Id,
            ChannelType = connector.ChannelType,
            ExternalConversationId = request.ExternalConversationId.Trim(),
            ExternalMessageId = normalizedExternalMessageId,
            SenderAddress = request.SenderAddress.Trim().ToLowerInvariant(),
            Subject = request.Subject.Trim(),
            Content = request.Content.Trim(),
            RawPayloadJson = JsonSerializer.Serialize(request),
            NormalizedPayloadJson = JsonSerializer.Serialize(new
            {
                connector.ChannelType,
                connector.ProviderKey,
                ConversationId = request.ExternalConversationId.Trim(),
                MessageId = normalizedExternalMessageId,
                Sender = request.SenderAddress.Trim().ToLowerInvariant(),
                Subject = request.Subject.Trim(),
                Content = request.Content.Trim(),
                request.ExternalTimestampUtc
            }),
            ExternalTimestampUtc = request.ExternalTimestampUtc,
            Status = InboundEventStatus.Received
        };

        _context.InboundChannelEvents.Add(inbound);
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        try
        {
            var fallbackCategoryId = await _context.TicketCategories
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            var fallbackPriorityId = await _context.TicketPriorities
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Level)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            var normalizedSenderEmail = request.SenderAddress.Trim().ToLowerInvariant();
            var customer = await _context.Users
                .Where(x => x.TenantId == connector.TenantId && x.Email == normalizedSenderEmail && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            if (customer == null && !string.IsNullOrWhiteSpace(normalizedSenderEmail))
            {
                customer = new User
                {
                    TenantId = connector.TenantId,
                    Email = normalizedSenderEmail,
                    Username = normalizedSenderEmail,
                    PasswordHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")))),
                    FirstName = "External",
                    LastName = "Contact",
                    IsActive = true
                };
                _context.Users.Add(customer);
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
            }

            if (customer != null && fallbackCategoryId > 0 && fallbackPriorityId > 0)
            {
                var ticket = await _ticketService.CreateTicketAsync(new CreateTicketDto
                {
                    CreatedByUserId = customer.Id,
                    Title = string.IsNullOrWhiteSpace(request.Subject) ? $"{connector.ChannelType} request" : request.Subject.Trim(),
                    Description = request.Content.Trim(),
                    CategoryId = fallbackCategoryId,
                    PriorityId = fallbackPriorityId
                });

                inbound.CreatedTicketId = ticket.Id;
                inbound.Status = InboundEventStatus.TicketCreated;
                inbound.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
            }
            else
            {
                inbound.Status = InboundEventStatus.Normalized;
                inbound.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
            }
        }
        catch (Exception ex)
        {
            inbound.Status = InboundEventStatus.Failed;
            inbound.ProcessingError = ex.Message;
            inbound.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
        }

        return Accepted(new { inbound.Id, inbound.Status, inbound.CreatedTicketId });
    }

    [HttpPost("inbound/{connectorId:int}/webhook")]
    [EnableRateLimiting("omnichannel-webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> ReceiveWebhookInbound(int connectorId, [FromBody] JsonElement payload)
    {
        var connector = await _context.OmnichannelConnectors
            .FirstOrDefaultAsync(x => x.Id == connectorId && !x.IsDeleted, HttpContext.RequestAborted);
        if (connector == null || connector.Status != ConnectorStatus.Enabled)
            return NotFound();

        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(HttpContext.RequestAborted);
            Request.Body.Position = 0;
        }

        var normalized = await _normalizationService.NormalizeAsync(connector, Request, payload, rawBody, HttpContext.RequestAborted);
        if (!normalized.IsValid)
            return BadRequest(new { error = normalized.Error });

        var typed = new InboundChannelRequest
        {
            ExternalConversationId = normalized.ExternalConversationId,
            ExternalMessageId = normalized.ExternalMessageId,
            SenderAddress = normalized.SenderAddress,
            Subject = normalized.Subject,
            Content = normalized.Content,
            ExternalTimestampUtc = normalized.ExternalTimestampUtc
        };

        var result = await ReceiveInbound(connectorId, typed);
        if (result.Result is ObjectResult objectResult && objectResult.Value is not null)
        {
            return objectResult.StatusCode.HasValue
                ? StatusCode(objectResult.StatusCode.Value, new
                {
                    objectResult.Value,
                    normalized = JsonSerializer.Deserialize<object>(normalized.NormalizedPayloadJson)
                })
                : Ok(new { objectResult.Value, normalized = JsonSerializer.Deserialize<object>(normalized.NormalizedPayloadJson) });
        }

        if (result.Value is not null)
        {
            return Ok(new
            {
                result.Value,
                normalized = JsonSerializer.Deserialize<object>(normalized.NormalizedPayloadJson)
            });
        }

        return Accepted();
    }

    [HttpGet("events")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<InboundChannelEvent>>> GetEvents([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var eventsList = await _context.InboundChannelEvents
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(eventsList);
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }

    private static string HashSecret(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes);
    }
}

public class UpsertConnectorRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChannelType ChannelType { get; set; }
    public string ProviderKey { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";
    public string InboundSigningSecret { get; set; } = string.Empty;
    public string SignatureHeaderName { get; set; } = "X-Channel-Signature";
    public string SignatureAlgorithm { get; set; } = "hmac-sha256";
    public int DedupWindowMinutes { get; set; } = 120;
    public ConnectorStatus Status { get; set; } = ConnectorStatus.Disabled;
}

public class InboundChannelRequest
{
    public string ExternalConversationId { get; set; } = string.Empty;
    public string ExternalMessageId { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? ExternalTimestampUtc { get; set; }
}
