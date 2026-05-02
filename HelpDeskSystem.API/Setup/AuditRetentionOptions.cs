namespace HelpDeskSystem.API.Setup;

public class AuditRetentionOptions
{
    public const string SectionName = "AuditRetention";

    public bool Enabled { get; set; } = true;
    public int RetentionDays { get; set; } = 180;
    public string Cron { get; set; } = "0 3 * * *";
}
