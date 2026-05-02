using HelpDeskSystem.Application.DTOs.KnowledgeBase;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Application.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly HelpDeskDbContext _context;

    public KnowledgeBaseService(HelpDeskDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<KnowledgeBaseCategoryDto>> GetCategoriesAsync(
        int tenantId,
        bool includePrivate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.KnowledgeBaseCategories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!includePrivate)
        {
            query = query.Where(x => x.IsPublic);
        }

        return await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new KnowledgeBaseCategoryDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Name = x.Name,
                Description = x.Description,
                IsPublic = x.IsPublic,
                DisplayOrder = x.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<KnowledgeBaseArticleDto>> SearchPublishedArticlesAsync(
        int tenantId,
        string? query,
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        var articlesQuery = _context.KnowledgeBaseArticles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsPublished);

        if (categoryId.HasValue)
        {
            articlesQuery = articlesQuery.Where(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            articlesQuery = articlesQuery.Where(x =>
                x.Title.Contains(term) ||
                x.Summary.Contains(term) ||
                x.Body.Contains(term) ||
                x.SearchKeywords.Contains(term));
        }

        return await BuildArticleProjection(articlesQuery)
            .OrderByDescending(x => x.PublishedAtUtc)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<KnowledgeBaseArticleDto?> GetPublishedArticleBySlugAsync(
        int tenantId,
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return await BuildArticleProjection(
                _context.KnowledgeBaseArticles
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsPublished && x.Slug == slug.Trim()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<KnowledgeBaseCategoryDto> UpsertCategoryAsync(
        int tenantId,
        int? categoryId,
        UpsertKnowledgeBaseCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        KnowledgeBaseCategory entity;
        if (categoryId.HasValue)
        {
            entity = await _context.KnowledgeBaseCategories
                .FirstOrDefaultAsync(x => x.Id == categoryId.Value && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Knowledge base category not found.");
        }
        else
        {
            entity = new KnowledgeBaseCategory { TenantId = tenantId };
            _context.KnowledgeBaseCategories.Add(entity);
        }

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description.Trim();
        entity.IsPublic = dto.IsPublic;
        entity.DisplayOrder = dto.DisplayOrder;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new KnowledgeBaseCategoryDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Description = entity.Description,
            IsPublic = entity.IsPublic,
            DisplayOrder = entity.DisplayOrder
        };
    }

    public async Task<KnowledgeBaseArticleDto> UpsertArticleAsync(
        int tenantId,
        int actorUserId,
        int? articleId,
        UpsertKnowledgeBaseArticleDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Slug))
            throw new InvalidOperationException("Article slug is required.");

        var categoryExists = await _context.KnowledgeBaseCategories
            .AnyAsync(x => x.Id == dto.CategoryId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (!categoryExists)
            throw new InvalidOperationException("Category not found for tenant.");

        KnowledgeBaseArticle entity;
        if (articleId.HasValue)
        {
            entity = await _context.KnowledgeBaseArticles
                .FirstOrDefaultAsync(x => x.Id == articleId.Value && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Knowledge base article not found.");
        }
        else
        {
            entity = new KnowledgeBaseArticle
            {
                TenantId = tenantId,
                CreatedByUserId = actorUserId
            };
            _context.KnowledgeBaseArticles.Add(entity);
        }

        entity.CategoryId = dto.CategoryId;
        entity.Slug = dto.Slug.Trim().ToLowerInvariant();
        entity.Title = dto.Title.Trim();
        entity.Summary = dto.Summary.Trim();
        entity.Body = dto.Body.Trim();
        entity.SearchKeywords = dto.SearchKeywords.Trim();
        entity.IsPublished = dto.IsPublished;
        entity.PublishedAtUtc = dto.IsPublished ? DateTime.UtcNow : null;
        entity.UpdatedByUserId = actorUserId;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var currentVersion = await _context.KnowledgeBaseArticleVersions
            .Where(x => x.ArticleId == entity.Id)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        _context.KnowledgeBaseArticleVersions.Add(new KnowledgeBaseArticleVersion
        {
            ArticleId = entity.Id,
            VersionNumber = currentVersion + 1,
            Title = entity.Title,
            Summary = entity.Summary,
            Body = entity.Body,
            ChangedByUserId = actorUserId,
            ChangeNote = string.IsNullOrWhiteSpace(dto.ChangeNote) ? "Article update" : dto.ChangeNote.Trim()
        });

        await _context.SaveChangesAsync(cancellationToken);

        var result = await BuildArticleProjection(_context.KnowledgeBaseArticles.AsNoTracking().Where(x => x.Id == entity.Id))
            .FirstAsync(cancellationToken);
        return result;
    }

    public async Task<bool> DeleteArticleAsync(int tenantId, int articleId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.KnowledgeBaseArticles
            .FirstOrDefaultAsync(x => x.Id == articleId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task AddFeedbackAsync(
        int tenantId,
        int articleId,
        int? userId,
        KnowledgeBaseFeedbackRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.KnowledgeBaseArticles
            .AnyAsync(x => x.Id == articleId && x.TenantId == tenantId && x.IsPublished && !x.IsDeleted, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Article not found.");

        _context.KnowledgeBaseArticleFeedback.Add(new KnowledgeBaseArticleFeedback
        {
            ArticleId = articleId,
            UserId = userId,
            IsHelpful = dto.IsHelpful,
            Comment = dto.Comment.Trim()
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<KnowledgeBaseArticleDto> BuildArticleProjection(IQueryable<KnowledgeBaseArticle> source)
    {
        return source.Select(x => new KnowledgeBaseArticleDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            CategoryId = x.CategoryId,
            CategoryName = x.Category != null ? x.Category.Name : string.Empty,
            Slug = x.Slug,
            Title = x.Title,
            Summary = x.Summary,
            Body = x.Body,
            SearchKeywords = x.SearchKeywords,
            IsPublished = x.IsPublished,
            PublishedAtUtc = x.PublishedAtUtc,
            VersionCount = x.Versions.Count(v => !v.IsDeleted),
            HelpfulCount = x.Feedback.Count(f => !f.IsDeleted && f.IsHelpful),
            UnhelpfulCount = x.Feedback.Count(f => !f.IsDeleted && !f.IsHelpful)
        });
    }
}
