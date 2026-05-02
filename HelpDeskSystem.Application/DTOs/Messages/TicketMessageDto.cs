using HelpDeskSystem.Domain.Enums;

namespace HelpDeskSystem.Application.DTOs.Messages;

public class TicketMessageDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int SenderUserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public TicketMessageType MessageType { get; set; }
    public bool IsInternalNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}