namespace HelpDeskSystem.Application.DTOs.KnowledgeBase;

public class KnowledgeBaseArticleDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string SearchKeywords { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int VersionCount { get; set; }
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }
}
