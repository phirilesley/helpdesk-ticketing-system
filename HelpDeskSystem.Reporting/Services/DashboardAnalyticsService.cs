using HelpDeskSystem.Application.DTOs.Reports;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
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