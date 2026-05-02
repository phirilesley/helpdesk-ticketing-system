using HelpDeskSystem.Application.DTOs.Integrations;
using HelpDeskSystem.Application.DTOs.Tickets;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Application.Services;

public class EmailIngestionService : IEmailIngestionService
{
    private readonly HelpDeskDbContext _context;
    private readonly ITicketService _ticketService;
    private readonly IAuditService _auditService;
    private readonly ITenantSecurityPolicyService _tenantSecurityPolicyService;

    public EmailIngestionService(
        HelpDeskDbContext context,
        ITicketService ticketService,
        IAuditService auditService,
        ITenantSecurityPolicyService tenantSecurityPolicyService)
    {
        _context = context;
        _ticketService = ticketService;
        _auditService = auditService;
        _tenantSecurityPolicyService = tenantSecurityPolicyService;
    }

    public async Task<InboundEmailResultDto> ProcessInboundAsync(
        InboundEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalMessageId))
            return new InboundEmailResultDto { Success = false, Status = "INVALID", Message = "ExternalMessageId is required." };

        var existing = await _context.InboundEmailLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalMessageId == request.ExternalMessageId, cancellationToken);
        if (existing != null)
        {
            return new InboundEmailResultDto
            {
                Success = existing.ProcessingStatus == "CREATED",
                Status = "DUPLICATE",
                TicketId = existing.CreatedTicketId,
                Message = "Message already processed."
            };
        }

        var log = new InboundEmailLog
        {
            ExternalMessageId = request.ExternalMessageId.Trim(),
            FromEmail = request.FromEmail.Trim(),
            Subject = request.Subject.Trim(),
            Body = request.Body.Trim(),
            ProcessingStatus = "RECEIVED"
        };

        _context.InboundEmailLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Domain == request.TenantDomain && x.IsActive, cancellationToken);
            if (tenant == null)
            {
                log.ProcessingStatus = "REJECTED";
                log.ErrorMessage = "Tenant not found.";
                log.ProcessedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new InboundEmailResultDto { Success = false, Status = "TENANT_NOT_FOUND", Message = log.ErrorMessage };
            }

            var blocked = await _tenantSecurityPolicyService.IsInboundEmailBlockedAsync(tenant.Id, cancellationToken);
            if (blocked)
            {
                log.ProcessingStatus = "REJECTED";
                log.TenantId = tenant.Id;
                log.ErrorMessage = "Inbound email ticket creation blocked by tenant policy.";
                log.ProcessedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new InboundEmailResultDto { Success = false, Status = "BLOCKED_BY_POLICY", Message = log.ErrorMessage };
            }

            var sender = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == request.FromEmail && x.TenantId == tenant.Id && x.IsActive, cancellationToken);
            if (sender == null)
            {
                log.ProcessingStatus = "REJECTED";
                log.TenantId = tenant.Id;
                log.ErrorMessage = "Sender user not found in tenant.";
                log.ProcessedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new InboundEmailResultDto { Success = false, Status = "SENDER_NOT_FOUND", Message = log.ErrorMessage };
            }

            var categoryId = request.CategoryId ?? await _context.TicketCategories.AsNoTracking().Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync(cancellationToken);
            var priorityId = request.PriorityId ?? await _context.TicketPriorities.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.Level).Select(x => x.Id).FirstAsync(cancellationToken);

            var created = await _ticketService.CreateTicketAsync(new CreateTicketDto
            {
                Title = string.IsNullOrWhiteSpace(request.Subject) ? "Email Ticket" : request.Subject.Trim(),
                Description = request.Body.Trim(),
                CategoryId = categoryId,
                PriorityId = priorityId,
                CreatedByUserId = sender.Id
            });

            log.TenantId = tenant.Id;
            log.CreatedTicketId = created.Id;
            log.ProcessingStatus = "CREATED";
            log.ProcessedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                sender.Id,
                "EMAIL_TO_TICKET_CREATED",
                "Ticket",
                created.Id.ToString(),
                newValues: $"{{\"externalMessageId\":\"{log.ExternalMessageId}\",\"tenantId\":{tenant.Id}}}",
                cancellationToken: cancellationToken);

            return new InboundEmailResultDto
            {
                Success = true,
                Status = "CREATED",
                TicketId = created.Id,
                Message = "Ticket created from inbound email."
            };
        }
        catch (Exception ex)
        {
            log.ProcessingStatus = "FAILED";
            log.ErrorMessage = ex.Message;
            log.ProcessedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new InboundEmailResultDto
            {
                Success = false,
                Status = "FAILED",
                Message = ex.Message
            };
        }
    }
}
