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

    public string AdminEmail { get; set; } = "admin@helpdesk.local";
    public string AdminPassword { get; set; } = "Admin@123!";
    public string AdminUsername { get; set; } = "admin";
    public string AdminFirstName { get; set; } = "System";
    public string AdminLastName { get; set; } = "Admin";

    public string AgentEmail { get; set; } = "agent@helpdesk.local";
    public string AgentPassword { get; set; } = "Agent@123!";
    public string AgentUsername { get; set; } = "agent";
    public string AgentFirstName { get; set; } = "Support";
    public string AgentLastName { get; set; } = "Agent";

    public string CustomerEmail { get; set; } = "customer@helpdesk.local";
    public string CustomerPassword { get; set; } = "Customer@123!";
    public string CustomerUsername { get; set; } = "customer";
    public string CustomerFirstName { get; set; } = "Portal";
    public string CustomerLastName { get; set; } = "User";
}
