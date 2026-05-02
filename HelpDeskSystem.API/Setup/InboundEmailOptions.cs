namespace HelpDeskSystem.API.Setup;

public class InboundEmailOptions
{
    public const string SectionName = "InboundEmail";

    public bool Enabled { get; set; } = true;
    public string SharedSecret { get; set; } = "replace-with-strong-shared-secret";
}
