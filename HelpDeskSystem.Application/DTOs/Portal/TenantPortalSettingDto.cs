namespace HelpDeskSystem.Application.DTOs.Portal;

public class TenantPortalSettingDto
{
    public int TenantId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#1F6FEB";
    public string LogoUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public string WelcomeMessage { get; set; } = string.Empty;
}
