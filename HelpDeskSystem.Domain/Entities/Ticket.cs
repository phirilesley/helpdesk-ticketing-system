using HelpDeskSystem.Shared.Base;
using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Domain.Entities;

public class Ticket : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int PriorityId { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public bool IsSlaPaused { get; set; }
    public DateTime? SlaPausedAtUtc { get; set; }
    public int SlaPausedTotalMinutes { get; set; }
    public int TenantId { get; set; }

    // Navigation properties
    public TicketCategory? Category { get; set; }
    public TicketPriority? Priority { get; set; }
    public Tenant? Tenant { get; set; }
    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    public ICollection<TicketAssignment> Assignments { get; set; } = new List<TicketAssignment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketStatusHistory> StatusHistory { get; set; } = new List<TicketStatusHistory>();
}
