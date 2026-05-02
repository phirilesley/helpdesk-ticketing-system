using HelpDeskSystem.Application.DTOs.Messages;

namespace HelpDeskSystem.Application.Interfaces;

public interface ITicketMessageService
{
    Task<TicketMessageDto> SendMessageAsync(CreateTicketMessageDto dto);
    Task<IEnumerable<TicketMessageDto>> GetMessagesAsync(int ticketId);
}