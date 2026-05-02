using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class TicketAssignment : BaseEntity
{
    public int TicketId { get; set; }
    public int AssignedToUserId { get; set; }
    public int AssignedByUserId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;

    // Navigation
    public Ticket? Ticket { get; set; }
}