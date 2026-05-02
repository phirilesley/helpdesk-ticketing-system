namespace HelpDeskSystem.Application.DTOs.Reports;

public class SlaComplianceDto
{
    public int TotalTickets { get; set; }
    public int BreachedTickets { get; set; }
    public double CompliancePercentage { get; set; }
}
