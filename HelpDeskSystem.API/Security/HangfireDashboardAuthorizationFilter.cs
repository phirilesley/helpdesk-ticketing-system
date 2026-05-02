using Hangfire.Dashboard;

namespace HelpDeskSystem.API.Security;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
    }
}
