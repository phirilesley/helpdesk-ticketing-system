namespace HelpDeskSystem.Application.Interfaces;

public interface ICustomerPortalService
{
    Task<PortalTicketDto> SubmitTicketAsync(PortalCreateTicketDto dto);
    Task<PortalTicketDto?> GetTicketByNumberAsync(string ticketNumber, string email);
    Task<IEnumerable<PortalTicketDto>> GetMyTicketsAsync(string email, int tenantId);
    Task<PortalAddMessageDto> AddMessageAsync(string ticketNumber, string email, string message, List<string>? attachmentUrls = null);
    Task<bool> RateTicketAsync(string ticketNumber, string email, int rating, string? comment = null);
    Task<IEnumerable<PortalArticleDto>> SearchKnowledgeBaseAsync(string query, int tenantId);
    Task<PortalTicketStatusDto> GetTicketStatusAsync(string ticketNumber);
    Task<PortalCsatSummaryDto> GetCsatSummaryAsync(int tenantId, DateTime? from = null, DateTime? to = null);
}

public record PortalCreateTicketDto(
    string RequesterName,
    string RequesterEmail,
    string Subject,
    string Description,
    int? CategoryId,
    int TenantId,
    string? AttachmentUrl = null
);

public record PortalTicketDto(
    int Id,
    string TicketNumber,
    string Subject,
    string Description,
    string Status,
    string Priority,
    string Category,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DueAt,
    List<PortalMessageDto> Messages,
    int? CsatRating,
    bool CanRate
);

public record PortalMessageDto(
    int Id,
    string SenderName,
    bool IsAgent,
    string Body,
    DateTime SentAt,
    string? AttachmentUrl
);

public record PortalAddMessageDto(bool Success, string Message);

public record PortalArticleDto(int Id, string Title, string Slug, string Excerpt, int ViewCount);

public record PortalTicketStatusDto(string TicketNumber, string Status, DateTime? LastUpdated, string? AssignedAgentName);

public record PortalCsatSummaryDto(
    double AverageRating,
    int TotalResponses,
    int Rating5Count,
    int Rating4Count,
    int Rating3Count,
    int Rating2Count,
    int Rating1Count,
    double SatisfactionPercentage
);
