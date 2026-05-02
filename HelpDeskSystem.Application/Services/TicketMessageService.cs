using HelpDeskSystem.Application.DTOs.Messages;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Application.Services;

public class TicketMessageService : ITicketMessageService
{
    private readonly HelpDeskDbContext _context;

    public TicketMessageService(HelpDeskDbContext context)
    {
        _context = context;
    }

    public async Task<TicketMessageDto> SendMessageAsync(CreateTicketMessageDto dto)
    {
        var message = new TicketMessage
        {
            TicketId = dto.TicketId,
            SenderUserId = dto.SenderUserId,
            Message = dto.Message,
            MessageType = dto.MessageType,
            IsInternalNote = dto.IsInternalNote
        };

        _context.TicketMessages.Add(message);
        await _context.SaveChangesAsync();

        return MapToDto(message);
    }

    public async Task<IEnumerable<TicketMessageDto>> GetMessagesAsync(int ticketId)
    {
        var messages = await _context.TicketMessages
            .Where(m => m.TicketId == ticketId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync();

        return messages.Select(MapToDto);
    }

    private TicketMessageDto MapToDto(TicketMessage message)
    {
        return new TicketMessageDto
        {
            Id = message.Id,
            TicketId = message.TicketId,
            SenderUserId = message.SenderUserId,
            Message = message.Message,
            MessageType = message.MessageType,
            IsInternalNote = message.IsInternalNote,
            CreatedAtUtc = message.CreatedAtUtc
        };
    }
}