using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class KnowledgeBaseCategory : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public int DisplayOrder { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<KnowledgeBaseArticle> Articles { get; set; } = new List<KnowledgeBaseArticle>();
}
