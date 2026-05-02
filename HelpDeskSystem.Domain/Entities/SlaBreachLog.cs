using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class SlaBreachLog : BaseEntity
{
    public int TicketId { get; set; }
    public string BreachType { get; set; } = string.Empty; // Response or Resolution
    public DateTime BreachedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Ticket? Ticket { get; set; }
}