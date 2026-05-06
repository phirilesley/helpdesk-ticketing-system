using HelpDeskSystem.Application.DTOs.Tickets;

namespace HelpDeskSystem.Application.Interfaces;

public interface IAiTriageService
{
    Task<AiTriageResultDto> TriageTicketAsync(int ticketId, int tenantId);
    Task<AiSuggestedReplyDto> SuggestReplyAsync(int ticketId, int tenantId);
    Task<AiArticleSuggestionsDto> SuggestKnowledgeArticlesAsync(string title, string description, int tenantId);
    Task<AiSentimentDto> AnalyzeSentimentAsync(string text);
    Task<AiDuplicateCheckDto> FindDuplicatesAsync(int ticketId, int tenantId);
}

public record AiTriageResultDto(
    int TicketId,
    int? SuggestedCategoryId,
    string? SuggestedCategoryName,
    int? SuggestedPriorityId,
    string? SuggestedPriorityName,
    int? SuggestedAssigneeUserId,
    string? SuggestedAssigneeName,
    double ConfidenceScore,
    string Reasoning,
    List<string> DetectedKeywords,
    string SentimentLabel,
    double SentimentScore
);

public record AiSuggestedReplyDto(
    int TicketId,
    string SuggestedReply,
    double ConfidenceScore,
    List<int> ReferencedArticleIds
);

public record AiArticleSuggestionsDto(
    List<AiArticleMatch> Articles
);

public record AiArticleMatch(int ArticleId, string Title, double RelevanceScore);

public record AiSentimentDto(string Label, double Score, string Detail);

public record AiDuplicateCheckDto(
    int TicketId,
    List<AiDuplicateMatch> PossibleDuplicates
);

public record AiDuplicateMatch(int TicketId, string TicketNumber, string Title, double SimilarityScore);
