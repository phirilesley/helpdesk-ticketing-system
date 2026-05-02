namespace HelpDeskSystem.Application.DTOs.Security;

public class TenantSecurityPolicyDto
{
    public int TenantId { get; set; }
    public bool RequireMfaForPrivilegedUsers { get; set; }
    public string AllowedIpRanges { get; set; } = string.Empty;
    public bool BlockInboundEmailTicketCreation { get; set; }
    public bool HasScimToken { get; set; }
}
