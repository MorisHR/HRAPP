using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using HRMS.Core.Interfaces;

namespace HRMS.Infrastructure.Middleware;

/// <summary>
/// Authorization filter to ensure tenant context is available for tenant-scoped endpoints
/// Prevents access to tenant endpoints without valid tenant context
/// </summary>
public class TenantAuthorizationFilter : IAuthorizationFilter
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantAuthorizationFilter> _logger;

    public TenantAuthorizationFilter(ITenantContext tenantContext, ILogger<TenantAuthorizationFilter> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if tenant context is available
        if (_tenantContext.TenantId == null || _tenantContext.TenantId == Guid.Empty)
        {
            _logger.LogWarning(
                "Tenant-scoped endpoint accessed without valid tenant context. Path: {Path}",
                context.HttpContext.Request.Path);

            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "Tenant context not available. Please ensure you are accessing the application through a valid tenant subdomain."
            });
            return;
        }

        _logger.LogDebug(
            "Tenant authorization successful. TenantId: {TenantId}, Path: {Path}",
            _tenantContext.TenantId, context.HttpContext.Request.Path);
    }
}
