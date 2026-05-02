using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class EscalationRule : BaseEntity
{
    public int PriorityId { get; set; }
    public int EscalateAfterMinutes { get; set; }
    public string EscalateToRole { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public TicketPriority? Priority { get; set; }
}