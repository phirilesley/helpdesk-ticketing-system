using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<KnowledgeBaseCategory> KnowledgeBaseCategories { get; set; } = new List<KnowledgeBaseCategory>();
    public ICollection<KnowledgeBaseArticle> KnowledgeBaseArticles { get; set; } = new List<KnowledgeBaseArticle>();
    public ICollection<BusinessHoursProfile> BusinessHoursProfiles { get; set; } = new List<BusinessHoursProfile>();
    public ICollection<TenantSecurityPolicy> SecurityPolicies { get; set; } = new List<TenantSecurityPolicy>();
}
