using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class KnowledgeBaseArticleVersion : BaseEntity
{
    public int ArticleId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public string ChangeNote { get; set; } = string.Empty;

    public KnowledgeBaseArticle? Article { get; set; }
}
