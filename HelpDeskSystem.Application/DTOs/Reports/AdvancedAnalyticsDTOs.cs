using System;

namespace HelpDeskSystem.Application.DTOs.Reports;

public class TicketTrendDto
{
    public DateTime Date { get; set; }
    public int CreatedTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int OpenTickets { get; set; }
}

public class CustomerSatisfactionDto
{
    public int TotalClosedTickets { get; set; }
    public int TicketsWithFeedback { get; set; }
    public double FeedbackResponseRate { get; set; }
    public double AverageSatisfactionScore { get; set; }
}

public class CategoryPerformanceDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public double AverageResolutionTimeHours { get; set; }
    public double SlaCompliancePercentage { get; set; }
}

public class RealTimeMetricsDto
{
    public int TotalActiveTickets { get; set; }
    public int NewTicketsLast24Hours { get; set; }
    public int ResolvedTicketsLast24Hours { get; set; }
    public int CriticalTickets { get; set; }
    public int OverdueTickets { get; set; }
    public double AverageFirstResponseTime { get; set; }
    public int ActiveAgents { get; set; }
    public double SystemLoadPercentage { get; set; }
}

public class PerformanceReportDto
{
    public DateRangeDto Period { get; set; } = new();
    public int TotalTickets { get; set; }
    public List<StatusMetricDto> TicketsByStatus { get; set; } = new();
    public List<PriorityMetricDto> TicketsByPriority { get; set; } = new();
    public List<CategoryMetricDto> TicketsByCategory { get; set; } = new();
    public List<AgentMetricDto> TopPerformingAgents { get; set; } = new();
}

public class DateRangeDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class StatusMetricDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class PriorityMetricDto
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class CategoryMetricDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageResolutionTime { get; set; }
}

public class AgentMetricDto
{
    public string AgentName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public double AverageResolutionTime { get; set; }
    public double SatisfactionScore { get; set; }
}
