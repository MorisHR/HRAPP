using Microsoft.AspNetCore.Antiforgery;

namespace HRMS.API.Middleware;

/// <summary>
/// CSRF Protection Middleware - Fortune 500 Compliance
/// Validates antiforgery tokens on all state-changing HTTP requests
/// Protects against Cross-Site Request Forgery attacks
/// </summary>
public class AntiforgeryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AntiforgeryMiddleware> _logger;

    // Endpoints that should skip CSRF validation
    private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/system-", // Secret SuperAdmin login paths
        "/api/auth/refresh-token",
        "/api/auth/csrf-token", // CSRF token endpoint itself
        "/api/setup/",
        "/health",
        "/swagger"
    };

    public AntiforgeryMiddleware(
        RequestDelegate next,
        IAntiforgery antiforgery,
        ILogger<AntiforgeryMiddleware> logger)
    {
        _next = next;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method.ToUpper();

        // Only validate state-changing operations (POST, PUT, DELETE, PATCH)
        var isStatefulRequest = method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH";

        // Skip validation for exempt paths
        if (isStatefulRequest && !IsExemptPath(path))
        {
            try
            {
                // Validate antiforgery token
                await _antiforgery.ValidateRequestAsync(context);

                _logger.LogDebug(
                    "CSRF token validated successfully for {Method} {Path}",
                    method,
                    path);
            }
            catch (AntiforgeryValidationException ex)
            {
                // SECURITY: Block request with invalid CSRF token
                _logger.LogWarning(
                    "CSRF PROTECTION: Invalid or missing antiforgery token for {Method} {Path} from IP {IP}. Error: {Error}",
                    method,
                    path,
                    context.Connection.RemoteIpAddress,
                    ex.Message);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "CSRF token validation failed",
                    error = "CSRF_TOKEN_INVALID",
                    details = "The request is missing a valid CSRF token. Please refresh the page and try again."
                });

                return; // Block the request
            }
        }

        await _next(context);
    }

    private static bool IsExemptPath(string path)
    {
        // Check exact matches first
        if (ExemptPaths.Contains(path))
            return true;

        // Check prefix matches (for paths with trailing segments)
        foreach (var exemptPath in ExemptPaths)
        {
            if (path.StartsWith(exemptPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Extension method to register CSRF protection middleware
/// </summary>
public static class AntiforgeryMiddlewareExtensions
{
    public static IApplicationBuilder UseAntiforgeryTokenValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AntiforgeryMiddleware>();
    }
}
