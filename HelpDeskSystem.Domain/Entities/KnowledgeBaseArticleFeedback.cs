using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class KnowledgeBaseArticleFeedback : BaseEntity
{
    public int ArticleId { get; set; }
    public int? UserId { get; set; }
    public bool IsHelpful { get; set; }
    public string Comment { get; set; } = string.Empty;

    public KnowledgeBaseArticle? Article { get; set; }
}
