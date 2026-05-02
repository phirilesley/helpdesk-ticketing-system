namespace HelpDeskSystem.API.Setup;

public class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; set; } = true;
    public string DefaultTenantName { get; set; } = "Default Tenant";
    public string DefaultTenantDomain { get; set; } = "default.local";
    public string SuperAdminEmail { get; set; } = "superadmin@helpdesk.local";
    public string SuperAdminPassword { get; set; } = "ChangeThisStrongPassword!";
    public string SuperAdminUsername { get; set; } = "superadmin";
    public string SuperAdminFirstName { get; set; } = "Super";
    public string SuperAdminLastName { get; set; } = "Admin";
}
