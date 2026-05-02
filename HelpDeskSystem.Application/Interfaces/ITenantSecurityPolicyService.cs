using HelpDeskSystem.Application.DTOs.Security;

namespace HelpDeskSystem.Application.Interfaces;

public interface ITenantSecurityPolicyService
{
    Task<TenantSecurityPolicyDto> GetPolicyAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<TenantSecurityPolicyDto> UpsertPolicyAsync(int tenantId, UpsertTenantSecurityPolicyDto dto, CancellationToken cancellationToken = default);
    Task<bool> IsIpAllowedAsync(int tenantId, string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> RequiresMfaForUserAsync(int tenantId, IEnumerable<string> roles, CancellationToken cancellationToken = default);
    Task<bool> ValidateScimTokenAsync(int tenantId, string token, CancellationToken cancellationToken = default);
    Task<bool> IsInboundEmailBlockedAsync(int tenantId, CancellationToken cancellationToken = default);
}
