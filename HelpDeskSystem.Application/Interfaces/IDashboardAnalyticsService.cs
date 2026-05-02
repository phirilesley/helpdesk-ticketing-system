using HelpDeskSystem.Application.DTOs.Reports;

namespace HelpDeskSystem.Application.Interfaces;

public interface IDashboardAnalyticsService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? tenantId = null);
    Task<IEnumerable<TicketStatusSummaryDto>> GetTicketsByStatusAsync(int? tenantId = null);
    Task<IEnumerable<AgentPerformanceDto>> GetAgentPerformanceAsync(int? tenantId = null);
    Task<SlaComplianceDto> GetSlaComplianceAsync(int? tenantId = null);
    
    // Advanced Analytics Methods
    Task<IEnumerable<TicketTrendDto>> GetTicketTrendsAsync(int? tenantId = null, int days = 30);
    Task<CustomerSatisfactionDto> GetCustomerSatisfactionAsync(int? tenantId = null);
    Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceAsync(int? tenantId = null);
    Task<RealTimeMetricsDto> GetRealTimeMetricsAsync(int? tenantId = null);
    Task<PerformanceReportDto> GetPerformanceReportAsync(int? tenantId = null, DateTime? startDate = null, DateTime? endDate = null);
}