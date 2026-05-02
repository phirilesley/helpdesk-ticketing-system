using System.Security.Claims;

namespace HelpDeskSystem.Realtime.Extensions;

public static class PrincipalExtensions
{
    public static int? GetTenantId(this ClaimsPrincipal principal)
    {
        var tenantClaim = principal.FindFirst("tenant_id");
        if (tenantClaim != null && int.TryParse(tenantClaim.Value, out var tenantId))
            return tenantId;
        
        return null;
    }

    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;
        
        return null;
    }
}
