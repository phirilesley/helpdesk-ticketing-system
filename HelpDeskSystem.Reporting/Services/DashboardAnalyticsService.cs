using HelpDeskSystem.Application.DTOs.Reports;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Reporting.Services;

public class DashboardAnalyticsService : IDashboardAnalyticsService
{
    private readonly HelpDeskDbContext _context;

    public DashboardAnalyticsService(HelpDeskDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? tenantId = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var tickets = await query.Where(t => !t.IsDeleted).ToListAsync();

        var totalTickets = tickets.Count;
        var openTickets = tickets.Count(t => t.Status != TicketStatus.Closed);
        var closedTickets = tickets.Count(t => t.Status == TicketStatus.Closed);
        var avgResolutionTime = await CalculateAverageResolutionTimeAsync(tenantId);

        return new DashboardSummaryDto
        {
            TotalTickets = totalTickets,
            OpenTickets = openTickets,
            ClosedTickets = closedTickets,
            AverageResolutionTimeHours = avgResolutionTime
        };
    }

    public async Task<IEnumerable<TicketStatusSummaryDto>> GetTicketsByStatusAsync(int? tenantId = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var statusGroups = await query
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.Status)
            .Select(g => new TicketStatusSummaryDto
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();

        return statusGroups;
    }

    public async Task<IEnumerable<AgentPerformanceDto>> GetAgentPerformanceAsync(int? tenantId = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var agentStats = await query
            .Where(t => t.AssignedToUserId.HasValue && !t.IsDeleted)
            .GroupBy(t => t.AssignedToUserId)
            .Select(g => new AgentPerformanceDto
            {
                AgentId = g.Key.Value,
                TotalTickets = g.Count(),
                ResolvedTickets = g.Count(t => t.Status == TicketStatus.Closed),
                AverageResolutionTimeHours = g.Where(t => t.ClosedAtUtc.HasValue)
                    .Average(t => EF.Functions.DateDiffHour(t.CreatedAtUtc, t.ClosedAtUtc.Value))
            })
            .ToListAsync();

        return agentStats;
    }

    public async Task<SlaComplianceDto> GetSlaComplianceAsync(int? tenantId = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var tickets = await query
            .Where(t => !t.IsDeleted)
            .Include(t => t.Priority)
            .Include(t => t.Category)
            .ToListAsync();

        var totalTickets = tickets.Count;
        var breachedTickets = 0;

        foreach (var ticket in tickets)
        {
            // Simple SLA check - can be enhanced
            if (ticket.Status != TicketStatus.Closed && ticket.CreatedAtUtc.AddHours(24) < DateTime.UtcNow)
            {
                breachedTickets++;
            }
        }

        return new SlaComplianceDto
        {
            TotalTickets = totalTickets,
            BreachedTickets = breachedTickets,
            CompliancePercentage = totalTickets > 0 ? (1 - (double)breachedTickets / totalTickets) * 100 : 0
        };
    }

    public async Task<IEnumerable<TicketTrendDto>> GetTicketTrendsAsync(int? tenantId = null, int days = 30)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var startDate = DateTime.UtcNow.AddDays(-days);

        var tickets = await query
            .Where(t => !t.IsDeleted && t.CreatedAtUtc >= startDate)
            .Select(t => new { t.CreatedAtUtc, t.Status })
            .ToListAsync();

        return tickets
            .GroupBy(t => t.CreatedAtUtc.Date)
            .Select(g => new TicketTrendDto
            {
                Date = g.Key,
                CreatedTickets = g.Count(),
                ResolvedTickets = g.Count(t => t.Status == TicketStatus.Closed),
                OpenTickets = g.Count(t => t.Status != TicketStatus.Closed)
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<CustomerSatisfactionDto> GetCustomerSatisfactionAsync(int? tenantId = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var totalClosedTickets = await query
            .Where(t => !t.IsDeleted && t.Status == TicketStatus.Closed)
            .CountAsync();

        return new CustomerSatisfactionDto
        {
            TotalClosedTickets = totalClosedTickets,
            TicketsWithFeedback = 0,
            FeedbackResponseRate = 0,
            AverageSatisfactionScore = 0
        };
    }

    public async Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceAsync(int? tenantId = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var categoryStats = await query
            .Where(t => !t.IsDeleted)
            .Include(t => t.Category)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryPerformanceDto
            {
                CategoryId = g.Key,
                CategoryName = g.FirstOrDefault().Category.Name,
                TotalTickets = g.Count(),
                ResolvedTickets = g.Count(t => t.Status == TicketStatus.Closed),
                AverageResolutionTimeHours = g.Where(t => t.ClosedAtUtc.HasValue)
                    .Average(t => EF.Functions.DateDiffHour(t.CreatedAtUtc, t.ClosedAtUtc.Value)),
                SlaCompliancePercentage = g.Count(t => t.Status == TicketStatus.Closed) > 0
                    ? (double)g.Count(t => t.Status == TicketStatus.Closed && 
                        EF.Functions.DateDiffHour(t.CreatedAtUtc, t.ClosedAtUtc.Value) <= 24) / 
                       g.Count(t => t.Status == TicketStatus.Closed) * 100
                    : 0
            })
            .ToListAsync();

        return categoryStats;
    }

    public async Task<RealTimeMetricsDto> GetRealTimeMetricsAsync(int? tenantId = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);

        var metrics = await query
            .Where(t => !t.IsDeleted)
            .GroupBy(t => 1) // Single group for aggregated metrics
            .Select(g => new RealTimeMetricsDto
            {
                TotalActiveTickets = g.Count(t => t.Status != TicketStatus.Closed),
                NewTicketsLast24Hours = g.Count(t => t.CreatedAtUtc >= last24Hours),
                ResolvedTicketsLast24Hours = g.Count(t => t.Status == TicketStatus.Closed && t.ClosedAtUtc >= last24Hours),
                CriticalTickets = g.Count(t => t.PriorityId == 1 && t.Status != TicketStatus.Closed),
                OverdueTickets = g.Count(t => t.Status != TicketStatus.Closed && t.CreatedAtUtc.AddHours(24) < now),
                AverageFirstResponseTime = 0,
                ActiveAgents = _context.Users.Count(u => u.IsActive && u.UserRoles.Any(r => r.Role.Name == "Agent")),
                SystemLoadPercentage = Math.Min(100, (double)g.Count(t => t.Status != TicketStatus.Closed) / 
                    Math.Max(1, _context.Users.Count(u => u.IsActive && u.UserRoles.Any(r => r.Role.Name == "Agent")) * 10) * 100)
            })
            .FirstOrDefaultAsync();

        return metrics ?? new RealTimeMetricsDto();
    }

    public async Task<PerformanceReportDto> GetPerformanceReportAsync(int? tenantId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAtUtc >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAtUtc <= endDate.Value);

        var tickets = await query
            .Where(t => !t.IsDeleted)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .ToListAsync();

        var assignedUserIds = tickets
            .Where(t => t.AssignedToUserId.HasValue)
            .Select(t => t.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        var assignedUsers = await _context.Users
            .Where(u => assignedUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

        var report = new PerformanceReportDto
        {
            Period = new DateRangeDto
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow
            },
            TotalTickets = tickets.Count,
            TicketsByStatus = tickets.GroupBy(t => t.Status)
                .Select(g => new StatusMetricDto
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    Percentage = tickets.Count > 0 ? (double)g.Count() / tickets.Count * 100 : 0
                }).ToList(),
            TicketsByPriority = tickets.GroupBy(t => t.Priority?.Name ?? "Unknown")
                .Select(g => new PriorityMetricDto
                {
                    Priority = g.Key,
                    Count = g.Count(),
                    Percentage = tickets.Count > 0 ? (double)g.Count() / tickets.Count * 100 : 0
                }).ToList(),
            TicketsByCategory = tickets.GroupBy(t => t.Category?.Name ?? "Unknown")
                .Select(g => new CategoryMetricDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    AverageResolutionTime = g.Where(t => t.ClosedAtUtc.HasValue)
                        .Average(t => EF.Functions.DateDiffHour(t.CreatedAtUtc, t.ClosedAtUtc.Value))
                }).ToList(),
            TopPerformingAgents = tickets.Where(t => t.AssignedToUserId.HasValue)
                .GroupBy(t => t.AssignedToUserId)
                .Select(g => new AgentMetricDto
                {
                    AgentName = g.Key.HasValue && assignedUsers.TryGetValue(g.Key.Value, out var agentName)
                        ? agentName
                        : "Unassigned",
                    TotalTickets = g.Count(),
                    ResolvedTickets = g.Count(t => t.Status == TicketStatus.Closed),
                    AverageResolutionTime = g.Where(t => t.ClosedAtUtc.HasValue)
                        .Average(t => EF.Functions.DateDiffHour(t.CreatedAtUtc, t.ClosedAtUtc.Value)),
                    SatisfactionScore = 0
                })
                .OrderByDescending(a => a.ResolvedTickets)
                .Take(10)
                .ToList()
        };

        return report;
    }

    private async Task<double> CalculateAverageResolutionTimeAsync(int? tenantId = null)
    {
        var query = _context.Tickets.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var resolvedTickets = await query
            .Where(t => t.Status == TicketStatus.Closed && t.ClosedAtUtc.HasValue)
            .Select(t => EF.Functions.DateDiffHour(t.CreatedAtUtc, t.ClosedAtUtc.Value))
            .ToListAsync();

        return resolvedTickets.Any() ? resolvedTickets.Average() : 0;
    }
}
