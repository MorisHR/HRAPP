using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;

namespace HRMS.API.Middleware;

/// <summary>
/// Fortune 500-grade Device API Key Authentication Middleware
/// Provides secure authentication for biometric devices and external systems
///
/// AUTHENTICATION FLOW:
/// 1. Extract "X-Device-API-Key" header
/// 2. Validate API key against database (hash comparison)
/// 3. Check if key is active and not expired
/// 4. Validate IP address (if configured)
/// 5. Enforce rate limiting
/// 6. Update usage statistics
/// 7. Set HttpContext.Items["DeviceId"] for downstream use
/// 8. Log all authentication attempts
///
/// SECURITY FEATURES:
/// - Constant-time hash comparison (prevents timing attacks)
/// - IP address whitelisting with CIDR support
/// - Per-key rate limiting (default 60 req/min)
/// - Comprehensive audit logging
/// - Fail-secure design (deny on error)
///
/// COMPLIANCE:
/// - SOC 2 Type II: Secure API authentication
/// - ISO 27001: Access control mechanisms
/// - NIST 800-53: Cryptographic authentication
/// - PCI DSS: API key management
///
/// USAGE:
/// Apply to specific endpoints or controllers using [DeviceApiKeyAuth] attribute
/// Or apply globally to all /api/devices/* endpoints
/// </summary>
public class DeviceApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceApiKeyAuthenticationMiddleware> _logger;

    // Standard header name for device API keys
    private const string API_KEY_HEADER_NAME = "X-Device-API-Key";

    // Paths that require device API key authentication
    // Customize this list based on your API structure
    private static readonly HashSet<string> _protectedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/devices/attendance",
        "/api/devices/sync",
        "/api/devices/heartbeat",
        "/api/attendance/device-upload",
        "/api/biometric/device-upload"
    };

    public DeviceApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<DeviceApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IDeviceApiKeyService deviceApiKeyService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip authentication for non-protected paths
        if (!RequiresDeviceApiKeyAuth(path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 1: Extract API key from header
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var apiKeyHeader) ||
                string.IsNullOrWhiteSpace(apiKeyHeader))
            {
                _logger.LogWarning(
                    "Device API key authentication failed: Missing {HeaderName} header. Path: {Path}",
                    API_KEY_HEADER_NAME, path);

                await RespondUnauthorized(
                    context,
                    $"Missing {API_KEY_HEADER_NAME} header",
                    "API key authentication required");

                return;
            }

            var apiKey = apiKeyHeader.ToString();

            // Step 2: Extract client IP address
            var ipAddress = GetClientIpAddress(context);

            _logger.LogDebug(
                "Device API key authentication attempt. Path: {Path}, IP: {IpAddress}",
                path, ipAddress);

            // Step 3: Validate API key
            var validationResult = await deviceApiKeyService.ValidateApiKeyAsync(apiKey, ipAddress);

            stopwatch.Stop();

            // Step 4: Handle validation result
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Device API key authentication failed. Path: {Path}, IP: {IpAddress}, Reason: {Reason}, Duration: {DurationMs}ms",
                    path, ipAddress, validationResult.FailureReason, stopwatch.ElapsedMilliseconds);

                if (validationResult.RateLimitExceeded)
                {
                    await RespondRateLimitExceeded(
                        context,
                        validationResult.RetryAfterSeconds);
                }
                else
                {
                    await RespondUnauthorized(
                        context,
                        "Invalid API key",
                        validationResult.FailureReason ?? "Authentication failed");
                }

                return;
            }

            // Step 5: Authentication successful - enrich context
            context.Items["DeviceId"] = validationResult.DeviceId;
            context.Items["DeviceApiKeyId"] = validationResult.ApiKey?.Id;
            context.Items["TenantId"] = validationResult.TenantId;
            context.Items["AuthenticationType"] = "DeviceApiKey";

            _logger.LogInformation(
                "Device API key authentication successful. DeviceId: {DeviceId}, TenantId: {TenantId}, Path: {Path}, IP: {IpAddress}, Duration: {DurationMs}ms",
                validationResult.DeviceId,
                validationResult.TenantId,
                path,
                ipAddress,
                stopwatch.ElapsedMilliseconds);

            // Step 6: Proceed to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error in device API key authentication middleware. Path: {Path}, Duration: {DurationMs}ms",
                path,
                stopwatch.ElapsedMilliseconds);

            // Fail-secure: Deny request on error
            await RespondServiceUnavailable(
                context,
                "Authentication service temporarily unavailable");
        }
    }

    /// <summary>
    /// Check if the request path requires device API key authentication
    /// Override this method or customize _protectedPaths for your API structure
    /// </summary>
    private bool RequiresDeviceApiKeyAuth(string path)
    {
        // Option 1: Check against specific protected paths
        foreach (var protectedPath in _protectedPaths)
        {
            if (path.StartsWith(protectedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Option 2: Protect all /api/devices/* endpoints
        if (path.StartsWith("/api/devices/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extract client IP address from request
    /// Handles X-Forwarded-For, X-Real-IP headers for proxied requests
    /// </summary>
    private string GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header (used by load balancers, reverse proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first (original client)
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Check X-Real-IP header (used by some proxies)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to direct connection IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Respond with 401 Unauthorized
    /// </summary>
    private async Task RespondUnauthorized(
        HttpContext context,
        string error,
        string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        // Add standard authentication challenge header
        context.Response.Headers["WWW-Authenticate"] = $"DeviceApiKey realm=\"Device API\", error=\"{error}\"";

        var errorResponse = new
        {
            error = error,
            message = message,
            statusCode = 401,
            timestamp = DateTime.UtcNow,
            documentation = "https://docs.example.com/device-api-authentication"
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }

    /// <summary>
    /// Respond with 429 Too Many Requests (rate limit exceeded)
    /// </summary>
    private async Task RespondRateLimitExceeded(
        HttpContext context,
        int retryAfterSeconds)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/json";

        // Add standard rate limit headers
        context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
        context.Response.Headers["X-RateLimit-Limit"] = "60"; // Default limit
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds).ToUnixTimeSeconds().ToString();

        var errorResponse = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Please retry after {retryAfterSeconds} seconds.",
            statusCode = 429,
            retryAfter = retryAfterSeconds,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }

    /// <summary>
    /// Respond with 503 Service Unavailable (internal error)
    /// Fail-secure: Deny request when authentication service fails
    /// </summary>
    private async Task RespondServiceUnavailable(
        HttpContext context,
        string message)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Service unavailable",
            message = message,
            statusCode = 503,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}

/// <summary>
/// Extension methods for easy middleware registration
/// </summary>
public static class DeviceApiKeyAuthenticationMiddlewareExtensions
{
    /// <summary>
    /// Add Device API Key Authentication middleware to the pipeline
    ///
    /// USAGE in Program.cs:
    /// app.UseDeviceApiKeyAuthentication();
    ///
    /// IMPORTANT: Place AFTER UseRouting() and BEFORE UseAuthorization()
    /// </summary>
    public static IApplicationBuilder UseDeviceApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DeviceApiKeyAuthenticationMiddleware>();
    }
}
