namespace HelpDeskSystem.Application.DTOs.Integrations;

public class InboundEmailRequestDto
{
    public string ExternalMessageId { get; set; } = string.Empty;
    public string TenantDomain { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int? PriorityId { get; set; }
}
