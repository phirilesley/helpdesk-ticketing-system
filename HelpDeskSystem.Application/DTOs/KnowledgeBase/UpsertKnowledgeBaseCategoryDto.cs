namespace HelpDeskSystem.Application.DTOs.KnowledgeBase;

public class UpsertKnowledgeBaseCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public int DisplayOrder { get; set; }
}
