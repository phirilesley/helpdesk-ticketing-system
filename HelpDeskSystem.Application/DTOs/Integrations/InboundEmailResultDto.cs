namespace HelpDeskSystem.Application.DTOs.Integrations;

public class InboundEmailResultDto
{
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? TicketId { get; set; }
    public string Message { get; set; } = string.Empty;
}
