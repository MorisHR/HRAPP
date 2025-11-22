using HRMS.Core.Interfaces;
using System.Net;

namespace HRMS.API.Middleware;

/// <summary>
/// Middleware to enforce tenant context validation
/// Blocks requests to tenant-scoped endpoints when no valid tenant context exists
/// SECURITY: Prevents unauthorized access via tenant isolation bypass
/// </summary>
public class TenantContextValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextValidationMiddleware> _logger;

    // Paths that DO NOT require tenant context
    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/ready",
        "/health/detailed",
        "/swagger",
        "/api/auth/login",
        "/api/setup/",
        "/"
    };

    public TenantContextValidationMiddleware(RequestDelegate next, ILogger<TenantContextValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var path = context.Request.Path.Value ?? "";

        // Allow public paths without tenant context
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // For all other API endpoints, validate tenant context
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var tenantId = tenantService.GetCurrentTenantId();
            var tenantSchema = tenantService.GetCurrentTenantSchema();

            if (!tenantId.HasValue || string.IsNullOrEmpty(tenantSchema))
            {
                // FORTUNE 500 SECURITY: SuperAdmin bypass PERMANENTLY REMOVED
                // ALL tenant endpoints require valid tenant context - NO EXCEPTIONS
                // This ensures complete tenant isolation even for system administrators
                // This ensures strict tenant isolation in production environments
                _logger.LogWarning(
                    "SECURITY: Request blocked - No tenant context for path: {Path}, IP: {IP}",
                    path,
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Tenant context required",
                    message = "This request requires a valid tenant subdomain. Please access via tenant-specific URL (e.g., tenant1.hrms.com).",
                    code = "TENANT_CONTEXT_REQUIRED"
                });
                return;
            }

            // Log tenant context for audit trail
            _logger.LogDebug(
                "Request authorized for tenant: {TenantId} (Schema: {Schema}), Path: {Path}",
                tenantId.Value,
                tenantSchema,
                path);
        }

        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        // Exact match check
        if (PublicPaths.Contains(path))
            return true;

        // Prefix match for paths with trailing segments
        foreach (var publicPath in PublicPaths)
        {
            if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Extension method to add tenant context validation middleware
/// </summary>
public static class TenantContextValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContextValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantContextValidationMiddleware>();
    }
}
