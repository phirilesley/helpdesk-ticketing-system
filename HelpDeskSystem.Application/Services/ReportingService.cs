using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace HelpDeskSystem.Application.Services;

/// <summary>
/// Enterprise-grade reporting service. Provides real SQL-backed time-series analytics,
/// agent leaderboards, SLA breach reports, CSAT scoring, and channel breakdowns.
/// Matches the reporting depth of Zendesk Explore and Freshdesk Analytics.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly HelpDeskDbContext _context;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(HelpDeskDbContext context, ILogger<ReportingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FullReportDto> GenerateFullReportAsync(ReportRequestDto request)
    {
        _logger.LogInformation("Generating full report for tenant {TenantId} from {From} to {To}",
            request.TenantId, request.From, request.To);

        var baseQuery = _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.CreatedAtUtc >= request.From && t.CreatedAtUtc <= request.To);

        if (request.TenantId.HasValue)
            baseQuery = baseQuery.Where(t => t.TenantId == request.TenantId.Value);

        if (request.CategoryId.HasValue)
            baseQuery = baseQuery.Where(t => t.CategoryId == request.CategoryId.Value);

        if (request.PriorityId.HasValue)
            baseQuery = baseQuery.Where(t => t.PriorityId == request.PriorityId.Value);

        var ticketsQuery = baseQuery
            .Include(t => t.Priority)
            .Include(t => t.Category);

        var tickets = await ticketsQuery.ToListAsync();

        var total = tickets.Count;
        var resolved = tickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
        var open = tickets.Count(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
        var breachedSla = tickets.Count(t => t.DueAtUtc.HasValue && t.DueAtUtc < DateTime.UtcNow
                                          && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
        var slaCompliance = total > 0 ? Math.Round((double)(total - breachedSla) / total * 100, 1) : 100;

        var resolvedTickets = tickets
            .Where(t => t.ClosedAtUtc.HasValue)
            .Select(t => (t.ClosedAtUtc!.Value - t.CreatedAtUtc).TotalHours)
            .ToList();
        var avgResolution = resolvedTickets.Any() ? Math.Round(resolvedTickets.Average(), 2) : 0;

        // First response time from messages
        var ticketIds = tickets.Select(t => t.Id).ToList();
        var firstResponses = await _context.TicketMessages
            .AsNoTracking()
            .Where(m => ticketIds.Contains(m.TicketId)
                     && !m.IsInternalNote
                     && _context.Users.Any(u => u.Id == m.SenderUserId && !u.IsPortalUser))
            .GroupBy(m => m.TicketId)
            .Select(g => g.OrderBy(m => m.CreatedAtUtc).First())
            .ToListAsync();

        double avgFirstResponse = 0;
        if (firstResponses.Any())
        {
            var ticketCreated = tickets.ToDictionary(t => t.Id, t => t.CreatedAtUtc);
            avgFirstResponse = firstResponses
                .Where(fr => ticketCreated.ContainsKey(fr.TicketId))
                .Select(fr => (fr.CreatedAtUtc - ticketCreated[fr.TicketId]).TotalHours)
                .Where(h => h >= 0)
                .DefaultIfEmpty(0)
                .Average();
            avgFirstResponse = Math.Round(avgFirstResponse, 2);
        }

        // CSAT
        var csatRatings = tickets.Where(t => t.CsatRating.HasValue).Select(t => t.CsatRating!.Value).ToList();
        double csatScore = csatRatings.Any() ? Math.Round(csatRatings.Average(), 2) : 0;

        // Volume trend
        var trend = await GetTicketVolumeTimeSeriesAsync(request.TenantId, request.From, request.To, request.GroupBy);

        // Top agents
        var topAgents = await GetAgentLeaderboardAsync(request.TenantId, request.From, request.To);

        // Category breakdown
        var categoryBreakdown = tickets
            .GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .Select(g =>
            {
                var resHours = g.Where(t => t.ClosedAtUtc.HasValue)
                               .Select(t => (t.ClosedAtUtc!.Value - t.CreatedAtUtc).TotalHours);
                return new CategoryBreakdownDto(
                    CategoryName: g.Key,
                    Count: g.Count(),
                    Percentage: total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0,
                    AvgResolutionHours: resHours.Any() ? Math.Round(resHours.Average(), 2) : 0);
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new FullReportDto(
            GeneratedAt: DateTime.UtcNow,
            TotalTickets: total,
            ResolvedTickets: resolved,
            OpenTickets: open,
            BreachedSlaCount: breachedSla,
            SlaComplianceRate: slaCompliance,
            AverageResolutionHours: avgResolution,
            AverageFirstResponseHours: avgFirstResponse,
            CsatScore: csatScore,
            VolumeTrend: trend,
            TopAgents: topAgents.Take(10),
            CategoryBreakdown: categoryBreakdown
        );
    }

    public async Task<IEnumerable<TimeSeriesPointDto>> GetTicketVolumeTimeSeriesAsync(
        int? tenantId, DateTime from, DateTime to, string groupBy = "day")
    {
        var query = _context.Tickets.AsNoTracking().Where(t => !t.IsDeleted
            && t.CreatedAtUtc >= from && t.CreatedAtUtc <= to);

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var tickets = await query.Select(t => t.CreatedAtUtc).ToListAsync();

        return groupBy.ToLower() switch
        {
            "hour" => tickets
                .GroupBy(d => new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0))
                .Select(g => new TimeSeriesPointDto(g.Key, g.Count()))
                .OrderBy(p => p.Date),
            "week" => tickets
                .GroupBy(d => d.AddDays(-(int)d.DayOfWeek).Date)
                .Select(g => new TimeSeriesPointDto(g.Key, g.Count(), $"Week of {g.Key:MMM dd}"))
                .OrderBy(p => p.Date),
            "month" => tickets
                .GroupBy(d => new DateTime(d.Year, d.Month, 1))
                .Select(g => new TimeSeriesPointDto(g.Key, g.Count(), g.Key.ToString("MMM yyyy")))
                .OrderBy(p => p.Date),
            _ => tickets
                .GroupBy(d => d.Date)
                .Select(g => new TimeSeriesPointDto(g.Key, g.Count()))
                .OrderBy(p => p.Date)
        };
    }

    public async Task<IEnumerable<AgentLeaderboardDto>> GetAgentLeaderboardAsync(
        int? tenantId, DateTime from, DateTime to)
    {
        var ticketQuery = _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.CreatedAtUtc >= from && t.CreatedAtUtc <= to
                     && t.AssignedToUserId.HasValue);

        if (tenantId.HasValue)
            ticketQuery = ticketQuery.Where(t => t.TenantId == tenantId.Value);

        var agentTickets = await ticketQuery
            .GroupBy(t => t.AssignedToUserId!.Value)
            .Select(g => new
            {
                AgentId = g.Key,
                Resolved = g.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed),
                Breached = g.Count(t => t.DueAtUtc.HasValue && t.DueAtUtc < DateTime.UtcNow
                                     && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed),
                AvgResolution = g.Where(t => t.ClosedAtUtc.HasValue)
                                 .Average(t => (double?)(t.ClosedAtUtc!.Value - t.CreatedAtUtc).TotalHours) ?? 0,
                AvgCsat = g.Where(t => t.CsatRating.HasValue).Average(t => (double?)t.CsatRating!.Value) ?? 0
            })
            .ToListAsync();

        if (!agentTickets.Any()) return [];

        var agentIds = agentTickets.Select(a => a.AgentId).ToList();
        var agents = await _context.Users
            .AsNoTracking()
            .Where(u => agentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        // Get first response times
        var ticketIdsPerAgent = await ticketQuery
            .Select(t => new { t.Id, t.AssignedToUserId, t.CreatedAtUtc })
            .ToListAsync();

        var ticketIdsList = ticketIdsPerAgent.Select(t => t.Id).ToList();
        var firstResponses = await _context.TicketMessages
            .AsNoTracking()
            .Where(m => ticketIdsList.Contains(m.TicketId) && !m.IsInternalNote
                     && _context.Users.Any(u => u.Id == m.SenderUserId && !u.IsPortalUser))
            .GroupBy(m => m.TicketId)
            .Select(g => new { TicketId = g.Key, FirstAt = g.Min(m => m.CreatedAtUtc) })
            .ToListAsync();

        var firstResponseLookup = firstResponses.ToDictionary(x => x.TicketId, x => x.FirstAt);

        return agentTickets
            .Select(a =>
            {
                var myTickets = ticketIdsPerAgent.Where(t => t.AssignedToUserId == a.AgentId).ToList();
                var avgFirstResp = myTickets
                    .Where(t => firstResponseLookup.ContainsKey(t.Id))
                    .Select(t => (firstResponseLookup[t.Id] - t.CreatedAtUtc).TotalHours)
                    .Where(h => h >= 0)
                    .DefaultIfEmpty(0)
                    .Average();

                agents.TryGetValue(a.AgentId, out var name);
                return new AgentLeaderboardDto(
                    UserId: a.AgentId,
                    AgentName: name ?? $"Agent#{a.AgentId}",
                    TicketsResolved: a.Resolved,
                    AvgResolutionHours: Math.Round(a.AvgResolution, 2),
                    AvgFirstResponseHours: Math.Round(avgFirstResp, 2),
                    CsatScore: Math.Round(a.AvgCsat, 2),
                    SlaBreaches: a.Breached
                );
            })
            .OrderByDescending(a => a.TicketsResolved);
    }

    public async Task<SlaBreachReportDto> GetSlaBreachReportAsync(int? tenantId, DateTime from, DateTime to)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.CreatedAtUtc >= from && t.CreatedAtUtc <= to
                     && t.DueAtUtc.HasValue
                     && t.DueAtUtc < DateTime.UtcNow
                     && t.Status != TicketStatus.Resolved
                     && t.Status != TicketStatus.Closed);

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var breachedTickets = await query
            .Include(t => t.Priority)
            .OrderByDescending(t => t.DueAtUtc)
            .Take(500)
            .ToListAsync();

        var totalInPeriod = await _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.CreatedAtUtc >= from && t.CreatedAtUtc <= to
                     && (tenantId == null || t.TenantId == tenantId.Value))
            .CountAsync();

        var agentIds = breachedTickets.Where(t => t.AssignedToUserId.HasValue)
                                      .Select(t => t.AssignedToUserId!.Value)
                                      .Distinct().ToList();
        var agents = await _context.Users
            .AsNoTracking()
            .Where(u => agentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var items = breachedTickets.Select(t =>
        {
            string? agentName = t.AssignedToUserId.HasValue && agents.ContainsKey(t.AssignedToUserId.Value)
                ? agents[t.AssignedToUserId.Value] : null;
            return new SlaBreachItemDto(
                TicketId: t.Id,
                TicketNumber: t.TicketNumber,
                Title: t.Title,
                DueAt: t.DueAtUtc!.Value,
                BreachedAt: t.DueAtUtc.Value,
                AgentName: agentName,
                Priority: t.Priority?.Name ?? "Normal");
        }).ToList();

        double breachRate = totalInPeriod > 0
            ? Math.Round((double)breachedTickets.Count / totalInPeriod * 100, 1) : 0;

        return new SlaBreachReportDto(breachedTickets.Count, breachRate, items);
    }

    public async Task<FirstResponseReportDto> GetFirstResponseReportAsync(int? tenantId, DateTime from, DateTime to)
    {
        var ticketQuery = _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.CreatedAtUtc >= from && t.CreatedAtUtc <= to);

        if (tenantId.HasValue)
            ticketQuery = ticketQuery.Where(t => t.TenantId == tenantId.Value);

        var tickets = await ticketQuery.Select(t => new { t.Id, t.CreatedAtUtc, t.DueAtUtc }).ToListAsync();
        var ticketIds = tickets.Select(t => t.Id).ToList();

        var firstResponses = await _context.TicketMessages
            .AsNoTracking()
            .Where(m => ticketIds.Contains(m.TicketId) && !m.IsInternalNote
                     && _context.Users.Any(u => u.Id == m.SenderUserId && !u.IsPortalUser))
            .GroupBy(m => m.TicketId)
            .Select(g => new { TicketId = g.Key, FirstAt = g.Min(m => m.CreatedAtUtc) })
            .ToListAsync();

        var ticketMap = tickets.ToDictionary(t => t.Id, t => t);

        var responseTimes = firstResponses
            .Where(fr => ticketMap.ContainsKey(fr.TicketId))
            .Select(fr =>
            {
                var t = ticketMap[fr.TicketId];
                return (fr.FirstAt - t.CreatedAtUtc).TotalMinutes;
            })
            .Where(m => m >= 0)
            .OrderBy(m => m)
            .ToList();

        if (!responseTimes.Any())
            return new FirstResponseReportDto(0, 0, 0, []);

        double avg = Math.Round(responseTimes.Average(), 1);
        double median = responseTimes[responseTimes.Count / 2];

        // SLA for first response: 4 hours = 240 min by default
        double withinSla = Math.Round(responseTimes.Count(m => m <= 240) / (double)responseTimes.Count * 100, 1);

        var trend = await GetTicketVolumeTimeSeriesAsync(tenantId, from, to, "day");

        return new FirstResponseReportDto(avg, Math.Round(median, 1), withinSla, trend);
    }

    public async Task<ResolutionTimeReportDto> GetResolutionTimeReportAsync(int? tenantId, DateTime from, DateTime to)
    {
        var baseQuery = _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.CreatedAtUtc >= from && t.CreatedAtUtc <= to
                     && t.ClosedAtUtc.HasValue);

        if (tenantId.HasValue)
            baseQuery = baseQuery.Where(t => t.TenantId == tenantId.Value);

        var query = baseQuery.Include(t => t.Category);

        var resolved = await query.ToListAsync();

        if (!resolved.Any())
            return new ResolutionTimeReportDto(0, 0, [], []);

        var hours = resolved.Select(t => (t.ClosedAtUtc!.Value - t.CreatedAtUtc).TotalHours)
                            .OrderBy(h => h)
                            .ToList();

        double avg = Math.Round(hours.Average(), 2);
        double median = hours[hours.Count / 2];

        var trend = resolved
            .GroupBy(t => t.ClosedAtUtc!.Value.Date)
            .Select(g => new TimeSeriesPointDto(g.Key, g.Count()))
            .OrderBy(x => x.Date);

        var byCategory = resolved
            .GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .Select(g =>
            {
                var gHours = g.Select(t => (t.ClosedAtUtc!.Value - t.CreatedAtUtc).TotalHours).ToList();
                return new CategoryBreakdownDto(
                    g.Key, g.Count(),
                    Math.Round((double)g.Count() / resolved.Count * 100, 1),
                    Math.Round(gHours.Average(), 2));
            })
            .OrderByDescending(x => x.Count);

        return new ResolutionTimeReportDto(avg, median, trend, byCategory);
    }

    public async Task<CsatReportDto> GetCsatReportAsync(int? tenantId, DateTime from, DateTime to)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.CsatRating.HasValue
                     && t.CsatSubmittedAtUtc >= from && t.CsatSubmittedAtUtc <= to);

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var csatTickets = await query.ToListAsync();

        var totalResolved = await _context.Tickets
            .AsNoTracking()
            .CountAsync(t => !t.IsDeleted
                          && (t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                          && t.ClosedAtUtc >= from && t.ClosedAtUtc <= to
                          && (tenantId == null || t.TenantId == tenantId.Value));

        if (!csatTickets.Any())
            return new CsatReportDto(0, 0, 0, [], []);

        double avg = Math.Round(csatTickets.Average(t => (double)t.CsatRating!.Value), 2);
        double responseRate = totalResolved > 0
            ? Math.Round((double)csatTickets.Count / totalResolved * 100, 1) : 0;

        var trend = csatTickets
            .GroupBy(t => t.CsatSubmittedAtUtc!.Value.Date)
            .Select(g => new TimeSeriesPointDto(g.Key, (int)Math.Round(g.Average(t => (double)t.CsatRating!.Value))))
            .OrderBy(x => x.Date);

        var agentIds = csatTickets.Where(t => t.AssignedToUserId.HasValue)
                                  .Select(t => t.AssignedToUserId!.Value).Distinct().ToList();
        var agents = await _context.Users
            .AsNoTracking()
            .Where(u => agentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var comments = csatTickets
            .Where(t => !string.IsNullOrWhiteSpace(t.CsatComment))
            .OrderByDescending(t => t.CsatSubmittedAtUtc)
            .Take(20)
            .Select(t =>
            {
                string? agentName = t.AssignedToUserId.HasValue && agents.ContainsKey(t.AssignedToUserId.Value)
                    ? agents[t.AssignedToUserId.Value] : null;
                return new CsatCommentDto(t.Id, t.TicketNumber, t.CsatRating!.Value,
                    t.CsatComment, t.CsatSubmittedAtUtc!.Value, agentName);
            });

        return new CsatReportDto(avg, csatTickets.Count, responseRate, trend, comments);
    }

    public async Task<ChannelReportDto> GetChannelBreakdownAsync(int? tenantId, DateTime from, DateTime to)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.CreatedAtUtc >= from && t.CreatedAtUtc <= to);

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var tickets = await query
            .Select(t => new { t.SourceChannel, t.ClosedAtUtc, t.CreatedAtUtc })
            .ToListAsync();

        var total = tickets.Count;

        var channels = tickets
            .GroupBy(t => string.IsNullOrWhiteSpace(t.SourceChannel) ? "web" : t.SourceChannel.ToLower())
            .Select(g =>
            {
                var resHours = g.Where(t => t.ClosedAtUtc.HasValue)
                                .Select(t => (t.ClosedAtUtc!.Value - t.CreatedAtUtc).TotalHours);
                return new ChannelBreakdownItemDto(
                    Channel: g.Key,
                    Count: g.Count(),
                    Percentage: total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0,
                    AvgResolutionHours: resHours.Any() ? Math.Round(resHours.Average(), 2) : 0);
            })
            .OrderByDescending(x => x.Count);

        return new ChannelReportDto(channels);
    }

    public async Task<byte[]> ExportReportToCsvAsync(ReportRequestDto request)
    {
        var report = await GenerateFullReportAsync(request);

        var sb = new StringBuilder();
        sb.AppendLine("HelpDesk System - Full Report");
        sb.AppendLine($"Generated At,{report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Period,{request.From:yyyy-MM-dd} to {request.To:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("Summary");
        sb.AppendLine($"Total Tickets,{report.TotalTickets}");
        sb.AppendLine($"Resolved Tickets,{report.ResolvedTickets}");
        sb.AppendLine($"Open Tickets,{report.OpenTickets}");
        sb.AppendLine($"SLA Compliance Rate,{report.SlaComplianceRate}%");
        sb.AppendLine($"Avg Resolution (hours),{report.AverageResolutionHours}");
        sb.AppendLine($"Avg First Response (hours),{report.AverageFirstResponseHours}");
        sb.AppendLine($"CSAT Score,{report.CsatScore}/5");
        sb.AppendLine();
        sb.AppendLine("Top Agents");
        sb.AppendLine("Agent Name,Tickets Resolved,Avg Resolution Hrs,Avg First Response Hrs,CSAT Score,SLA Breaches");
        foreach (var agent in report.TopAgents)
            sb.AppendLine($"{agent.AgentName},{agent.TicketsResolved},{agent.AvgResolutionHours},{agent.AvgFirstResponseHours},{agent.CsatScore},{agent.SlaBreaches}");
        sb.AppendLine();
        sb.AppendLine("Category Breakdown");
        sb.AppendLine("Category,Count,Percentage,Avg Resolution Hrs");
        foreach (var cat in report.CategoryBreakdown)
            sb.AppendLine($"{cat.CategoryName},{cat.Count},{cat.Percentage}%,{cat.AvgResolutionHours}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<IEnumerable<BacklogReportDto>> GetBacklogTrendAsync(int? tenantId, int days = 30)
    {
        var results = new List<BacklogReportDto>();
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-days);

        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            var dayEnd = d.AddDays(1);
            var q = _context.Tickets
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.CreatedAtUtc < dayEnd
                         && (t.ClosedAtUtc == null || t.ClosedAtUtc >= d));

            if (tenantId.HasValue)
                q = q.Where(t => t.TenantId == tenantId.Value);

            var openCount = await q.CountAsync(t => t.Status == TicketStatus.New || t.Status == TicketStatus.Waiting);
            var inProgCount = await q.CountAsync(t => t.Status == TicketStatus.InProgress);
            results.Add(new BacklogReportDto(d, openCount, inProgCount));
        }

        return results;
    }
}
