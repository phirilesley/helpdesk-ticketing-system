using HelpDeskSystem.Application.DTOs.Tickets;

namespace HelpDeskSystem.Application.Interfaces;

public interface ITicketService
{
    Task<TicketDto> CreateTicketAsync(CreateTicketDto dto);
    Task<TicketDto?> GetTicketByIdAsync(int id);
    Task<IEnumerable<TicketDto>> GetAllTicketsAsync();
    Task UpdateTicketAsync(int id, UpdateTicketDto dto);
    Task DeleteTicketAsync(int id);
    Task AssignTicketAsync(int ticketId, int userId, string reason);
    Task ChangeTicketStatusAsync(int ticketId, Domain.Enums.TicketStatus status, int userId, string comment);
    Task PauseSlaAsync(int ticketId, int userId, string reason);
    Task ResumeSlaAsync(int ticketId, int userId, string reason);
    Task<IEnumerable<TicketDto>> GetTicketsForCreatorAsync(int tenantId, int creatorUserId);
}
