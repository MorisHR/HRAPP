using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Services;

namespace HRMS.API.Middleware;

/// <summary>
/// Middleware to resolve tenant context from subdomain
/// Executes on every HTTP request
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        try
        {
            // Resolve tenant from request
            if (tenantService is TenantService service)
            {
                var (tenantId, schemaName) = await service.ResolveTenantFromRequestAsync();

                if (tenantId.HasValue && !string.IsNullOrEmpty(schemaName))
                {
                    // Set tenant context for this request
                    tenantService.SetTenantContext(tenantId.Value, schemaName);
                    _logger.LogInformation("Resolved tenant: {TenantId}, Schema: {SchemaName}", tenantId.Value, schemaName);
                }
                else
                {
                    // No tenant context (could be admin panel or public endpoint)
                    _logger.LogDebug("No tenant context resolved for request: {Path}", context.Request.Path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant context");
            // Continue without tenant context (may result in authorization failure later)
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to add tenant resolution middleware
/// </summary>
public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantResolutionMiddleware>();
    }
}
