namespace HelpDeskSystem.Application.DTOs.Reports;

public class DashboardSummaryDto
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public double AverageResolutionTimeHours { get; set; }
}