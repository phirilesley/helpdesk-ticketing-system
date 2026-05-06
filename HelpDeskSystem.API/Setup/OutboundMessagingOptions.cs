namespace HelpDeskSystem.API.Setup;

public class OutboundMessagingOptions
{
    public const string SectionName = "OutboundMessaging";
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 50;
    public int MaxAttemptsDefault { get; set; } = 5;
    public int PartitionCount { get; set; } = 1;
    public int PartitionId { get; set; }
}
