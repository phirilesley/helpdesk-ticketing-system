using System.Security.Claims;

namespace HelpDeskSystem.API.Security;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public static int? GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("tenant_id");
        return int.TryParse(value, out var tenantId) ? tenantId : null;
    }
}
