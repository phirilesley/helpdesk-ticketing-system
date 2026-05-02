using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class TicketCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<TicketSlaRule> SlaRules { get; set; } = new List<TicketSlaRule>();
}