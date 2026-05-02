using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Application.DTOs.Tickets;

public class TicketDto
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public bool IsSlaPaused { get; set; }
    public int TenantId { get; set; }
    public bool IsDeleted { get; set; }
}
