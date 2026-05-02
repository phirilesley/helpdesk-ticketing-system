namespace HelpDeskSystem.Application.DTOs.Security;

public class UpsertTenantSecurityPolicyDto
{
    public bool RequireMfaForPrivilegedUsers { get; set; }
    public string AllowedIpRanges { get; set; } = string.Empty;
    public bool BlockInboundEmailTicketCreation { get; set; }
    public string ScimBearerToken { get; set; } = string.Empty;
}
