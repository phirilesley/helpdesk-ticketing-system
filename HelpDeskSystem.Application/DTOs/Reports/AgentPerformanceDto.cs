namespace HelpDeskSystem.Application.DTOs.Reports;

public class AgentPerformanceDto
{
    public int AgentId { get; set; }
    public int TotalTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public double AverageResolutionTimeHours { get; set; }
}
