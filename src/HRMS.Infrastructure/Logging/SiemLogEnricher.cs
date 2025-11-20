using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace HRMS.Infrastructure.Logging;

/// <summary>
/// FORTUNE 500 SIEM LOG ENRICHER
///
/// Enriches all log events with security-relevant metadata for SIEM analysis.
/// Follows patterns from Splunk Enterprise Security, Elastic SIEM, Azure Sentinel.
///
/// ENRICHMENT FIELDS:
/// - User context (UserId, Email, Roles)
/// - Tenant context (TenantId, Subdomain)
/// - Request context (IP, UserAgent, Method, Path)
/// - Security context (AuthMethod, SessionId)
/// - Geo context (Country, City) - if available
///
/// COMPLIANCE: SOC 2, ISO 27001, PCI-DSS 10.2, NIST 800-53 AU-3
/// </summary>
public class SiemLogEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SiemLogEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        // ======================
        // USER CONTEXT
        // ======================
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            // User ID
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
            }

            // Email
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserEmail", email));
            }

            // Username
            var username = user.FindFirst(ClaimTypes.Name)?.Value ?? user.Identity.Name;
            if (!string.IsNullOrEmpty(username))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Username", username));
            }

            // Roles
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            if (roles.Any())
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserRoles", roles, destructureObjects: true));
            }

            // Authentication Method
            var authMethod = user.FindFirst("auth_method")?.Value ?? "jwt";
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AuthMethod", authMethod));
        }
        else
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", "anonymous"));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Username", "anonymous"));
        }

        // ======================
        // TENANT CONTEXT
        // ======================
        var tenantId = user?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TenantId", tenantId));
        }

        var tenantSubdomain = user?.FindFirst("tenant_subdomain")?.Value;
        if (!string.IsNullOrEmpty(tenantSubdomain))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TenantSubdomain", tenantSubdomain));
        }

        // ======================
        // REQUEST CONTEXT
        // ======================
        var request = httpContext.Request;

        // Source IP (handles X-Forwarded-For, X-Real-IP)
        var ipAddress = GetClientIpAddress(httpContext);
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SourceIp", ipAddress));

        // User Agent
        var userAgent = request.Headers["User-Agent"].ToString();
        if (!string.IsNullOrEmpty(userAgent))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserAgent", userAgent));
        }

        // HTTP Method
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("HttpMethod", request.Method));

        // Request Path
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", request.Path.Value ?? "/"));

        // Query String (sanitized - no sensitive data)
        if (request.QueryString.HasValue)
        {
            var sanitizedQuery = SanitizeQueryString(request.QueryString.Value);
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("QueryString", sanitizedQuery));
        }

        // ======================
        // SECURITY CONTEXT
        // ======================

        // Session ID (if available)
        var sessionId = httpContext.Session?.Id;
        if (!string.IsNullOrEmpty(sessionId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SessionId", sessionId));
        }

        // Correlation ID (if available from middleware)
        if (httpContext.Items.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId?.ToString() ?? ""));
        }

        // MFA Status
        var mfaVerified = user?.FindFirst("mfa_verified")?.Value == "true";
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MfaVerified", mfaVerified));

        // ======================
        // ENVIRONMENT CONTEXT
        // ======================
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment",
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"));

        // ======================
        // TIMESTAMP (ISO 8601)
        // ======================
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("EventTimestamp",
            DateTimeOffset.UtcNow.ToString("o")));
    }

    /// <summary>
    /// Extract client IP address with proxy support
    /// Checks X-Forwarded-For, X-Real-IP, then falls back to RemoteIpAddress
    /// </summary>
    private string GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For (standard proxy header)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, first one is the client
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Check X-Real-IP (alternative proxy header)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp.Trim();
        }

        // Fallback to RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Sanitize query string to remove sensitive parameters
    /// Prevents logging of passwords, tokens, API keys
    /// </summary>
    private string SanitizeQueryString(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return "";
        }

        var sensitiveParams = new[] { "password", "token", "apikey", "secret", "key", "auth" };
        var parts = queryString.TrimStart('?').Split('&');
        var sanitized = new List<string>();

        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].ToLowerInvariant();
                if (sensitiveParams.Any(s => key.Contains(s)))
                {
                    sanitized.Add($"{keyValue[0]}=***REDACTED***");
                }
                else
                {
                    sanitized.Add(part);
                }
            }
            else
            {
                sanitized.Add(part);
            }
        }

        return "?" + string.Join("&", sanitized);
    }
}

/// <summary>
/// Extension method to add SIEM enricher to Serilog configuration
/// </summary>
public static class SiemLogEnricherExtensions
{
    public static LoggerConfiguration WithSiemEnrichment(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        IServiceProvider serviceProvider)
    {
        if (enrichmentConfiguration == null)
            throw new ArgumentNullException(nameof(enrichmentConfiguration));

        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        if (httpContextAccessor != null)
        {
            return enrichmentConfiguration.With(new SiemLogEnricher(httpContextAccessor));
        }

        return enrichmentConfiguration.FromLogContext();
    }
}
