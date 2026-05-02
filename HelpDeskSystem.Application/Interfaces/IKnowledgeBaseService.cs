using HelpDeskSystem.Application.DTOs.KnowledgeBase;

namespace HelpDeskSystem.Application.Interfaces;

public interface IKnowledgeBaseService
{
    Task<IReadOnlyCollection<KnowledgeBaseCategoryDto>> GetCategoriesAsync(int tenantId, bool includePrivate, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<KnowledgeBaseArticleDto>> SearchPublishedArticlesAsync(int tenantId, string? query, int? categoryId, CancellationToken cancellationToken = default);
    Task<KnowledgeBaseArticleDto?> GetPublishedArticleBySlugAsync(int tenantId, string slug, CancellationToken cancellationToken = default);
    Task<KnowledgeBaseCategoryDto> UpsertCategoryAsync(int tenantId, int? categoryId, UpsertKnowledgeBaseCategoryDto dto, CancellationToken cancellationToken = default);
    Task<KnowledgeBaseArticleDto> UpsertArticleAsync(int tenantId, int actorUserId, int? articleId, UpsertKnowledgeBaseArticleDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteArticleAsync(int tenantId, int articleId, CancellationToken cancellationToken = default);
    Task AddFeedbackAsync(int tenantId, int articleId, int? userId, KnowledgeBaseFeedbackRequestDto dto, CancellationToken cancellationToken = default);
}
