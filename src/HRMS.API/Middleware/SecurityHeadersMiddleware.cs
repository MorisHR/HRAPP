using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace HRMS.API.Middleware;

/// <summary>
/// Fortune 500-Grade Security Headers Middleware
///
/// Implements comprehensive security headers to protect against:
/// - XSS (Cross-Site Scripting) attacks
/// - Clickjacking attacks
/// - MIME-sniffing attacks
/// - Man-in-the-middle attacks
/// - Code injection attacks
///
/// Compliance: GDPR Article 32, SOC 2 Type II (CC6.1, CC6.6, CC6.7), ISO 27001:2022 (A.8.24, A.8.16, A.8.9)
/// OWASP: A03:2021 - Injection, A05:2021 - Security Misconfiguration, A02:2021 - Cryptographic Failures
///
/// Security Grade Target: A+ on SecurityHeaders.com
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // ======================
        // CRITICAL SECURITY HEADERS
        // ======================

        // 1. CRITICAL: Content Security Policy (CSP) - Prevents XSS attacks
        // OWASP: A03:2021 - Injection
        if (_environment.IsProduction())
        {
            // Production: Strict CSP with unsafe-inline/unsafe-eval (Phase 1)
            // TODO Phase 2: Remove unsafe-inline/unsafe-eval and implement nonce-based CSP
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://www.googletagmanager.com; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' data: https://fonts.gstatic.com; " +
                "img-src 'self' data: https: blob:; " +
                "connect-src 'self' https://morishr.com https://*.morishr.com https://www.googleapis.com; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'; " +
                "upgrade-insecure-requests; " +
                "block-all-mixed-content;"
            );
        }
        else if (_environment.IsStaging())
        {
            // Staging: Use CSP Report-Only mode for testing
            context.Response.Headers.Append("Content-Security-Policy-Report-Only",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
                "report-uri /api/security/csp-violation-report"
            );
        }
        else
        {
            // Development: Relaxed CSP for hot-reload and debugging
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "connect-src 'self' ws://localhost:* http://localhost:* https://localhost:* wss://localhost:*"
            );
        }

        // 2. CRITICAL: X-Frame-Options - Prevents clickjacking attacks
        // OWASP: Clickjacking Prevention
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // 3. HIGH: X-Content-Type-Options - Prevents MIME-sniffing attacks
        // OWASP: A05:2021 - Security Misconfiguration
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // 4. CRITICAL: Strict-Transport-Security (HSTS) - Enforces HTTPS
        // OWASP: A02:2021 - Cryptographic Failures
        // Note: Skip HSTS for localhost/development to avoid certificate issues
        if (!IsLocalRequest(context))
        {
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }

        // 5. MEDIUM: X-XSS-Protection - Legacy XSS filter for older browsers
        // Note: Modern browsers rely on CSP, this is for backward compatibility
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // 6. MEDIUM: Referrer-Policy - Controls referrer information
        // OWASP: Privacy & Data Protection (GDPR Article 32)
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // 7. MEDIUM: Permissions-Policy - Controls browser features and APIs
        // OWASP: Attack Surface Reduction
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), " +
            "microphone=(), " +
            "camera=(), " +
            "payment=(), " +
            "usb=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "accelerometer=(), " +
            "ambient-light-sensor=(), " +
            "autoplay=(), " +
            "encrypted-media=(), " +
            "fullscreen=(self), " +
            "picture-in-picture=()"
        );

        // 8. MEDIUM: Cross-Origin-Opener-Policy (COOP) - Prevents window.opener attacks
        context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");

        // 9. MEDIUM: Cross-Origin-Resource-Policy (CORP) - Protects against Spectre attacks
        context.Response.Headers.Append("Cross-Origin-Resource-Policy", "same-origin");

        // 10. LOW: X-Permitted-Cross-Domain-Policies - Restrict Adobe Flash/PDF access
        context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        // 11. LOW: X-DNS-Prefetch-Control - Privacy consideration
        context.Response.Headers.Append("X-DNS-Prefetch-Control", "off");

        // ======================
        // SECURITY: REMOVE SERVER INFORMATION DISCLOSURE HEADERS
        // ======================
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");

        // Log security headers applied (only in development for debugging)
        if (_environment.IsDevelopment())
        {
            _logger.LogDebug("Security headers applied to request: {Path}", context.Request.Path);
        }

        // Continue to next middleware
        await _next(context);
    }

    /// <summary>
    /// Determines if the current request is from localhost
    /// </summary>
    private bool IsLocalRequest(HttpContext context)
    {
        var connection = context.Connection;
        if (connection.RemoteIpAddress != null)
        {
            // Check if localhost
            if (connection.LocalIpAddress != null)
            {
                return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
            }
            else
            {
                return System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress);
            }
        }

        // If no remote IP, assume local (e.g., in-process request)
        return true;
    }
}

/// <summary>
/// Extension method for easy registration of SecurityHeadersMiddleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds Fortune 500-grade security headers to all HTTP responses
    ///
    /// Usage: app.UseSecurityHeaders();
    ///
    /// Position in pipeline: MUST be early (after UseHttpsRedirection, before UseRouting)
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
