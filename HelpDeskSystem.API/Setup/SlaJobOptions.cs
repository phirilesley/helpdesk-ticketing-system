namespace HelpDeskSystem.API.Setup;

public class SlaJobOptions
{
    public const string SectionName = "SlaJobs";

    public bool Enabled { get; set; } = true;
    public string Cron { get; set; } = "*/5 * * * *";
}
