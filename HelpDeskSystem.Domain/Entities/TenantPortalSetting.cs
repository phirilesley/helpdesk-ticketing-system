using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class TenantPortalSetting : BaseEntity
{
    public int TenantId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#1F6FEB";
    public string LogoUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public string WelcomeMessage { get; set; } = "How can we help you today?";

    public Tenant? Tenant { get; set; }
}
