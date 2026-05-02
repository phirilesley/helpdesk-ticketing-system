using HelpDeskSystem.Application.DTOs.Integrations;

namespace HelpDeskSystem.Application.Interfaces;

public interface IEmailIngestionService
{
    Task<InboundEmailResultDto> ProcessInboundAsync(InboundEmailRequestDto request, CancellationToken cancellationToken = default);
}
