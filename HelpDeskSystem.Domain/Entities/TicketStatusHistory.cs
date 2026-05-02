using HelpDeskSystem.Shared.Base;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Domain.Entities;

public class TicketStatusHistory : BaseEntity
{
    public int TicketId { get; set; }
    public TicketStatus OldStatus { get; set; }
    public TicketStatus NewStatus { get; set; }
    public int ChangedByUserId { get; set; }
    public string Comment { get; set; } = string.Empty;

    // Navigation
    public Ticket? Ticket { get; set; }
}