using HelpDeskSystem.Shared.Base;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Domain.Entities;

public class TicketMessage : BaseEntity
{
    public int TicketId { get; set; }
    public int SenderUserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public TicketMessageType MessageType { get; set; } = TicketMessageType.Message;
    public bool IsInternalNote { get; set; } = false;

    // Navigation
    public Ticket? Ticket { get; set; }
}