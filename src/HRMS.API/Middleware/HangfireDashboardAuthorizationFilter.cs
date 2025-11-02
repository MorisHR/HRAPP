using Hangfire.Dashboard;

namespace HRMS.API.Middleware;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// In production, implement proper authentication
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // For development: Allow all
        // TODO: In production, implement proper authentication
        // Example: Check if user has admin role
        // return httpContext.User.Identity?.IsAuthenticated == true &&
        //        httpContext.User.IsInRole("Admin");

        return true; // Allow all in development
    }
}
