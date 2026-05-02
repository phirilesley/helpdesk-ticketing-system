using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Application.DTOs.Reports;
using HelpDeskSystem.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AdvancedAnalyticsController : ControllerBase
{
    private readonly IDashboardAnalyticsService _analyticsService;
    private readonly ILogger<AdvancedAnalyticsController> _logger;

    public AdvancedAnalyticsController(
        IDashboardAnalyticsService analyticsService,
        ILogger<AdvancedAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("trends")]
    public async Task<ActionResult<IEnumerable<TicketTrendDto>>> GetTicketTrends(
        [FromQuery] int days = 30)
    {
        try
        {
            var tenantId = User.GetTenantId();
            var trends = await _analyticsService.GetTicketTrendsAsync(tenantId, days);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket trends");
            return StatusCode(500, "Error retrieving ticket trends");
        }
    }

    [HttpGet("customer-satisfaction")]
    public async Task<ActionResult<CustomerSatisfactionDto>> GetCustomerSatisfaction()
    {
        try
        {
            var tenantId = User.GetTenantId();
            var satisfaction = await _analyticsService.GetCustomerSatisfactionAsync(tenantId);
            return Ok(satisfaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer satisfaction data");
            return StatusCode(500, "Error retrieving customer satisfaction data");
        }
    }

    [HttpGet("category-performance")]
    public async Task<ActionResult<IEnumerable<CategoryPerformanceDto>>> GetCategoryPerformance()
    {
        try
        {
            var tenantId = User.GetTenantId();
            var performance = await _analyticsService.GetCategoryPerformanceAsync(tenantId);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category performance");
            return StatusCode(500, "Error retrieving category performance");
        }
    }

    [HttpGet("real-time-metrics")]
    public async Task<ActionResult<RealTimeMetricsDto>> GetRealTimeMetrics()
    {
        try
        {
            var tenantId = User.GetTenantId();
            var metrics = await _analyticsService.GetRealTimeMetricsAsync(tenantId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real-time metrics");
            return StatusCode(500, "Error retrieving real-time metrics");
        }
    }

    [HttpGet("performance-report")]
    public async Task<ActionResult<PerformanceReportDto>> GetPerformanceReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = User.GetTenantId();
            var report = await _analyticsService.GetPerformanceReportAsync(tenantId, startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance report");
            return StatusCode(500, "Error generating performance report");
        }
    }

    [HttpGet("export-report")]
    public async Task<IActionResult> ExportPerformanceReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string format = "csv")
    {
        try
        {
            var tenantId = User.GetTenantId();
            var report = await _analyticsService.GetPerformanceReportAsync(tenantId, startDate, endDate);

            if (format.ToLower() == "csv")
            {
                var csv = GenerateCsvReport(report);
                var content = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(content, "text/csv", $"performance-report-{DateTime.Now:yyyy-MM-dd}.csv");
            }
            else
            {
                return BadRequest("Unsupported format. Use 'csv'.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting performance report");
            return StatusCode(500, "Error exporting report");
        }
    }

    private string GenerateCsvReport(PerformanceReportDto report)
    {
        var csv = new System.Text.StringBuilder();
        
        // Header
        csv.AppendLine("Performance Report");
        csv.AppendLine($"Period,{report.Period.StartDate:yyyy-MM-dd} to {report.Period.EndDate:yyyy-MM-dd}");
        csv.AppendLine($"Total Tickets,{report.TotalTickets}");
        csv.AppendLine();

        // Status breakdown
        csv.AppendLine("Status Breakdown");
        csv.AppendLine("Status,Count,Percentage");
        foreach (var status in report.TicketsByStatus)
        {
            csv.AppendLine($"{status.Status},{status.Count},{status.Percentage:F2}%");
        }
        csv.AppendLine();

        // Priority breakdown
        csv.AppendLine("Priority Breakdown");
        csv.AppendLine("Priority,Count,Percentage");
        foreach (var priority in report.TicketsByPriority)
        {
            csv.AppendLine($"{priority.Priority},{priority.Count},{priority.Percentage:F2}%");
        }
        csv.AppendLine();

        // Category performance
        csv.AppendLine("Category Performance");
        csv.AppendLine("Category,Tickets,Avg Resolution Time (Hours)");
        foreach (var category in report.TicketsByCategory)
        {
            csv.AppendLine($"{category.Category},{category.Count},{category.AverageResolutionTime:F2}");
        }
        csv.AppendLine();

        // Top agents
        csv.AppendLine("Top Performing Agents");
        csv.AppendLine("Agent,Total Tickets,Resolved Tickets,Avg Resolution Time (Hours),Satisfaction Score");
        foreach (var agent in report.TopPerformingAgents)
        {
            csv.AppendLine($"{agent.AgentName},{agent.TotalTickets},{agent.ResolvedTickets},{agent.AverageResolutionTime:F2},{agent.SatisfactionScore:F2}");
        }

        return csv.ToString();
    }
}
