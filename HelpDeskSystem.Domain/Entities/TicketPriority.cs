using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class TicketPriority : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }

    // Navigation
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<TicketSlaRule> SlaRules { get; set; } = new List<TicketSlaRule>();
}