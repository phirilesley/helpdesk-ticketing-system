namespace HelpDeskSystem.API.Setup;

public class MultiRegionOptions
{
    public const string SectionName = "MultiRegion";
    public bool EnableSyntheticBackgroundChecks { get; set; } = true;
    public int SyntheticCheckIntervalMinutes { get; set; } = 10;
    public int SyntheticCheckTimeoutSeconds { get; set; } = 10;
    public string LocalHealthReadyUrl { get; set; } = "http://localhost:5229/health/ready";
}
