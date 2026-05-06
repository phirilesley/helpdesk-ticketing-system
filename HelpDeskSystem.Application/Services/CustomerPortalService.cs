using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HelpDeskSystem.Application.Services;

/// <summary>
/// Real implementation of the customer self-service portal.
/// Handles unauthenticated ticket submission, status lookup, messaging,
/// and CSAT (customer satisfaction) rating — matching Zendesk/Freshdesk portal features.
/// </summary>
public class CustomerPortalService : ICustomerPortalService
{
    private readonly HelpDeskDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CustomerPortalService> _logger;

    public CustomerPortalService(
        HelpDeskDbContext context,
        INotificationService notificationService,
        ILogger<CustomerPortalService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PortalTicketDto> SubmitTicketAsync(PortalCreateTicketDto dto)
    {
        // Find or create a portal user account
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.RequesterEmail && u.TenantId == dto.TenantId);

        if (user == null)
        {
            user = new User
            {
                Email = dto.RequesterEmail,
                FullName = dto.RequesterName,
                TenantId = dto.TenantId,
                IsActive = true,
                IsPortalUser = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Find default priority
        var defaultPriority = await _context.TicketPriorities
            .AsNoTracking()
            .OrderBy(p => p.Level)
            .FirstOrDefaultAsync();

        var ticket = new Ticket
        {
            TicketNumber = Shared.Helpers.TicketNumberGenerator.Generate(),
            Title = dto.Subject,
            Description = dto.Description,
            CategoryId = dto.CategoryId ?? 0,
            PriorityId = defaultPriority?.Id ?? 1,
            CreatedByUserId = user.Id,
            TenantId = dto.TenantId,
            Status = TicketStatus.New,
            SourceChannel = "portal"
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Apply SLA rule
        var slaRule = await _context.TicketSlaRules
            .AsNoTracking()
            .Where(r => r.PriorityId == ticket.PriorityId && r.IsActive && !r.IsDeleted)
            .FirstOrDefaultAsync();

        if (slaRule != null)
        {
            ticket.DueAtUtc = DateTime.UtcNow.AddMinutes(slaRule.ResolutionTimeMinutes);
            await _context.SaveChangesAsync();
        }

        // Send confirmation notification
        await _notificationService.NotifyAsync(
            user.Id,
            "Ticket Submitted Successfully",
            $"Your support ticket #{ticket.TicketNumber} has been received. We'll respond shortly.",
            NotificationType.Info);

        _logger.LogInformation("Portal ticket {TicketNumber} submitted by {Email}", ticket.TicketNumber, dto.RequesterEmail);

        return await MapToPortalDtoAsync(ticket);
    }

    public async Task<PortalTicketDto?> GetTicketByNumberAsync(string ticketNumber, string email)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.Messages.Where(m => !m.IsInternalNote).OrderBy(m => m.CreatedAtUtc))
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && !t.IsDeleted);

        if (ticket == null) return null;

        // Verify requester owns this ticket (email match)
        var creator = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ticket.CreatedByUserId);

        if (creator?.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) != true)
            return null;

        return await MapToPortalDtoAsync(ticket);
    }

    public async Task<IEnumerable<PortalTicketDto>> GetMyTicketsAsync(string email, int tenantId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.TenantId == tenantId);

        if (user == null) return [];

        var tickets = await _context.Tickets
            .Where(t => t.CreatedByUserId == user.Id && t.TenantId == tenantId && !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(50)
            .ToListAsync();

        var dtos = new List<PortalTicketDto>();
        foreach (var t in tickets)
            dtos.Add(await MapToPortalDtoAsync(t));

        return dtos;
    }

    public async Task<PortalAddMessageDto> AddMessageAsync(string ticketNumber, string email, string message, List<string>? attachmentUrls = null)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && !t.IsDeleted);

        if (ticket == null)
            return new PortalAddMessageDto(false, "Ticket not found.");

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ticket.CreatedByUserId);

        if (user?.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) != true)
            return new PortalAddMessageDto(false, "Unauthorized.");

        var ticketMessage = new TicketMessage
        {
            TicketId = ticket.Id,
            SenderUserId = user.Id,
            Message = message,
            MessageType = TicketMessageType.Message,
            IsInternalNote = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.TicketMessages.Add(ticketMessage);

        // Reopen if closed
        if (ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.Resolved)
        {
            ticket.Status = TicketStatus.New;
            ticket.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Notify assigned agent
        if (ticket.AssignedToUserId.HasValue)
        {
            await _notificationService.NotifyAsync(
                ticket.AssignedToUserId.Value,
                "Customer Replied",
                $"Customer replied on ticket #{ticket.TicketNumber}",
                NotificationType.Info);
        }

        _logger.LogInformation("Portal customer reply on ticket {TicketNumber}", ticketNumber);
        return new PortalAddMessageDto(true, "Message sent successfully.");
    }

    public async Task<bool> RateTicketAsync(string ticketNumber, string email, int rating, string? comment = null)
    {
        if (rating < 1 || rating > 5)
            return false;

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && !t.IsDeleted
                                   && (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed));

        if (ticket == null) return false;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ticket.CreatedByUserId);

        if (user?.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) != true)
            return false;

        ticket.CsatRating = rating;
        ticket.CsatComment = comment;
        ticket.CsatSubmittedAtUtc = DateTime.UtcNow;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("CSAT rating {Rating}/5 submitted for ticket {TicketNumber}", rating, ticketNumber);
        return true;
    }

    public async Task<IEnumerable<PortalArticleDto>> SearchKnowledgeBaseAsync(string query, int tenantId)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var lowerQuery = query.ToLowerInvariant();
        var words = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .Where(w => w.Length > 2)
                              .ToArray();

        var articles = await _context.KnowledgeBaseArticles
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.IsPublished && !a.IsDeleted)
            .Select(a => new { a.Id, a.Title, a.Slug, a.Body })
            .ToListAsync();

        return articles
            .Select(a =>
            {
                var combined = $"{a.Title} {a.Body}".ToLowerInvariant();
                var matchScore = words.Count(w => combined.Contains(w));
                return new { a.Id, a.Title, a.Slug, Score = matchScore };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(10)
            .Select(x => new PortalArticleDto(
                x.Id,
                x.Title,
                x.Slug,
                string.Empty,
                0))
            .ToList();
    }

    public async Task<PortalTicketStatusDto> GetTicketStatusAsync(string ticketNumber)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && !t.IsDeleted);

        if (ticket == null)
            return new PortalTicketStatusDto(ticketNumber, "Not Found", null, null);

        string? agentName = null;
        if (ticket.AssignedToUserId.HasValue)
        {
            var agent = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ticket.AssignedToUserId.Value);
            agentName = agent?.FullName;
        }

        return new PortalTicketStatusDto(ticketNumber, ticket.Status.ToString(), ticket.UpdatedAtUtc ?? ticket.CreatedAtUtc, agentName);
    }

    public async Task<PortalCsatSummaryDto> GetCsatSummaryAsync(int tenantId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.CsatRating.HasValue);

        if (from.HasValue) query = query.Where(t => t.CsatSubmittedAtUtc >= from.Value);
        if (to.HasValue)   query = query.Where(t => t.CsatSubmittedAtUtc <= to.Value);

        var ratings = await query.Select(t => t.CsatRating!.Value).ToListAsync();

        if (!ratings.Any())
            return new PortalCsatSummaryDto(0, 0, 0, 0, 0, 0, 0, 0);

        var totalResolved = await _context.Tickets
            .AsNoTracking()
            .CountAsync(t => t.TenantId == tenantId && !t.IsDeleted
                          && (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed));

        double avg = ratings.Average();
        double satisfactionPct = ratings.Count(r => r >= 4) / (double)ratings.Count * 100;

        return new PortalCsatSummaryDto(
            AverageRating: Math.Round(avg, 2),
            TotalResponses: ratings.Count,
            Rating5Count: ratings.Count(r => r == 5),
            Rating4Count: ratings.Count(r => r == 4),
            Rating3Count: ratings.Count(r => r == 3),
            Rating2Count: ratings.Count(r => r == 2),
            Rating1Count: ratings.Count(r => r == 1),
            SatisfactionPercentage: Math.Round(satisfactionPct, 1)
        );
    }

    private async Task<PortalTicketDto> MapToPortalDtoAsync(Ticket ticket)
    {
        var messages = ticket.Messages?
            .Where(m => !m.IsInternalNote)
            .Select(m => new PortalMessageDto(
                Id: m.Id,
                SenderName: "Support",
                IsAgent: false,
                Body: m.Message,
                SentAt: m.CreatedAtUtc,
                AttachmentUrl: null))
            .ToList() ?? [];

        // Enrich messages with sender info
        var userIds = ticket.Messages?.Select(m => m.SenderUserId).Distinct().ToList() ?? [];
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        messages = ticket.Messages?
            .Where(m => !m.IsInternalNote)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m =>
            {
                users.TryGetValue(m.SenderUserId, out var sender);
                return new PortalMessageDto(
                    Id: m.Id,
                    SenderName: sender?.FullName ?? "Support",
                    IsAgent: sender?.IsPortalUser != true,
                    Body: m.Message,
                    SentAt: m.CreatedAtUtc,
                    AttachmentUrl: null);
            })
            .ToList() ?? [];

        bool canRate = (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
                    && !ticket.CsatRating.HasValue;

        return new PortalTicketDto(
            Id: ticket.Id,
            TicketNumber: ticket.TicketNumber,
            Subject: ticket.Title,
            Description: ticket.Description,
            Status: ticket.Status.ToString(),
            Priority: ticket.Priority?.Name ?? "Normal",
            Category: ticket.Category?.Name ?? "General",
            CreatedAt: ticket.CreatedAtUtc,
            UpdatedAt: ticket.UpdatedAtUtc,
            DueAt: ticket.DueAtUtc,
            Messages: messages,
            CsatRating: ticket.CsatRating,
            CanRate: canRate
        );
    }
}
