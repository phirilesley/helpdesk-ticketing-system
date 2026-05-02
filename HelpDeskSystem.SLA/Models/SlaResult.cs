namespace HelpDeskSystem.SLA.Models;

public class SlaResult
{
    public int TicketId { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
    public DateTime ResponseDeadline { get; set; }
    public DateTime ResolutionDeadline { get; set; }
    public bool IsResponseBreached { get; set; }
    public bool IsResolutionBreached { get; set; }
    public bool IsBreached { get; set; }
}