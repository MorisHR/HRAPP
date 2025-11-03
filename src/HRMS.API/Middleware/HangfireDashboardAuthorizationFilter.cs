using Hangfire.Dashboard;

namespace HRMS.API.Middleware;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// SECURITY: Requires authenticated SuperAdmin user
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // PRODUCTION SECURITY: Only authenticated SuperAdmin users can access Hangfire dashboard
        var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
        var isSuperAdmin = httpContext.User.IsInRole("SuperAdmin");

        return isAuthenticated && isSuperAdmin;
    }
}
