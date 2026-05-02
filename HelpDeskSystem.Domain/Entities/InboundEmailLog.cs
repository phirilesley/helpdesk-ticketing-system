using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class InboundEmailLog : BaseEntity
{
    public int? TenantId { get; set; }
    public string ExternalMessageId { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ProcessingStatus { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int? CreatedTicketId { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
