namespace HelpDeskSystem.Application.DTOs.KnowledgeBase;

public class KnowledgeBaseFeedbackRequestDto
{
    public bool IsHelpful { get; set; }
    public string Comment { get; set; } = string.Empty;
}
