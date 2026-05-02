namespace HelpDeskSystem.Application.DTOs.KnowledgeBase;

public class UpsertKnowledgeBaseArticleDto
{
    public int CategoryId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string SearchKeywords { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public string ChangeNote { get; set; } = string.Empty;
}
