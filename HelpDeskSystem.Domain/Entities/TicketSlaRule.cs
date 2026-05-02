using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class TicketSlaRule : BaseEntity
{
    public int PriorityId { get; set; }
    public int CategoryId { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
    public int EscalateAfterMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
}