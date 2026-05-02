namespace HelpDeskSystem.Application.DTOs.KnowledgeBase;

public class KnowledgeBaseCategoryDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int DisplayOrder { get; set; }
}
