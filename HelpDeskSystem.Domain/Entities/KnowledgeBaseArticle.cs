using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class KnowledgeBaseArticle : BaseEntity
{
    public int TenantId { get; set; }
    public int CategoryId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string SearchKeywords { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }

    public Tenant? Tenant { get; set; }
    public KnowledgeBaseCategory? Category { get; set; }
    public ICollection<KnowledgeBaseArticleVersion> Versions { get; set; } = new List<KnowledgeBaseArticleVersion>();
    public ICollection<KnowledgeBaseArticleFeedback> Feedback { get; set; } = new List<KnowledgeBaseArticleFeedback>();
}
