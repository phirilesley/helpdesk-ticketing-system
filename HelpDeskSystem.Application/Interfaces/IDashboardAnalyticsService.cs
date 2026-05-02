using HelpDeskSystem.Application.DTOs.Reports;

namespace HelpDeskSystem.Application.Interfaces;

public interface IDashboardAnalyticsService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? tenantId = null);
    Task<IEnumerable<TicketStatusSummaryDto>> GetTicketsByStatusAsync(int? tenantId = null);
    Task<IEnumerable<AgentPerformanceDto>> GetAgentPerformanceAsync(int? tenantId = null);
    Task<SlaComplianceDto> GetSlaComplianceAsync(int? tenantId = null);
}