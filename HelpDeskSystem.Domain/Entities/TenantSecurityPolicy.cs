using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class TenantSecurityPolicy : BaseEntity
{
    public int TenantId { get; set; }
    public bool RequireMfaForPrivilegedUsers { get; set; }
    public string AllowedIpRanges { get; set; } = string.Empty;
    public bool BlockInboundEmailTicketCreation { get; set; }
    public string ScimBearerTokenHash { get; set; } = string.Empty;

    public Tenant? Tenant { get; set; }
}
