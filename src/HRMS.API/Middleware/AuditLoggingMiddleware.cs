using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Application.Interfaces;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace HRMS.API.Middleware;

/// <summary>
/// Production-grade audit logging middleware
/// Automatically logs all HTTP requests for comprehensive audit trail
/// Captures user context, performance metrics, and request/response data
/// Logs asynchronously to avoid blocking user requests
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private static readonly HashSet<string> _excludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/health",
        "/api/metrics",
        "/swagger",
        "/hangfire"
    };

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip excluded paths (health checks, metrics, static files)
        if (ShouldSkipAudit(path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var originalResponseBodyStream = context.Response.Body;

        try
        {
            // Proceed with request
            await _next(context);

            stopwatch.Stop();

            // Log successful request asynchronously
            _ = LogAuditAsync(context, auditLogService, stopwatch.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log failed request asynchronously
            _ = LogAuditAsync(context, auditLogService, stopwatch.ElapsedMilliseconds, ex);

            // Re-throw to let global exception handler deal with it
            throw;
        }
    }

    private bool ShouldSkipAudit(string path)
    {
        // Skip health checks, metrics, and static files
        foreach (var excludedPath in _excludedPaths)
        {
            if (path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Skip static file extensions
        if (path.Contains('.') &&
            (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".png") ||
             path.EndsWith(".jpg") || path.EndsWith(".ico") || path.EndsWith(".woff")))
        {
            return true;
        }

        return false;
    }

    private async Task LogAuditAsync(
        HttpContext context,
        IAuditLogService auditLogService,
        long durationMs,
        Exception? exception)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;
            var user = context.User;

            // Extract user information
            var userId = GetUserId(user);
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            var userFullName = user.FindFirst(ClaimTypes.Name)?.Value;
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

            // Extract tenant information
            var tenantId = GetTenantId(context);
            var tenantName = context.Items["TenantSubdomain"]?.ToString();

            // Determine action type based on HTTP method and path
            var actionType = DetermineActionType(request.Method, request.Path);

            // Determine category based on path
            var category = DetermineCategory(request.Path);

            // Determine severity
            var severity = DetermineSeverity(response.StatusCode, exception, actionType);

            // Sanitize query string (remove passwords)
            var queryString = SanitizeQueryString(request.QueryString.ToString());

            // Get correlation ID
            var correlationId = context.Items["X-Correlation-ID"]?.ToString() ?? Guid.NewGuid().ToString();

            // Build audit log entry
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                PerformedAt = DateTime.UtcNow,
                ActionType = actionType,
                Category = category,
                Severity = severity,
                Success = exception == null && response.StatusCode < 400,

                // User information
                UserId = userId,
                UserEmail = userEmail,
                UserFullName = userFullName,
                UserRole = userRole,

                // Tenant information
                TenantId = tenantId,
                TenantName = tenantName,

                // HTTP information
                HttpMethod = request.Method,
                RequestPath = request.Path,
                QueryString = queryString,
                ResponseCode = response.StatusCode,
                DurationMs = (int)durationMs,

                // Network information
                IpAddress = GetClientIpAddress(context),
                UserAgent = request.Headers.UserAgent.ToString(),

                // Metadata
                CorrelationId = correlationId,
                ErrorMessage = exception?.Message,

                // Additional metadata
                AdditionalMetadata = BuildAdditionalMetadata(context, exception)
            };

            // Log asynchronously (fire and forget pattern with error handling)
            await Task.Run(async () =>
            {
                try
                {
                    await auditLogService.LogAsync(auditLog);
                }
                catch (Exception ex)
                {
                    // Log audit logging failure but don't throw
                    _logger.LogError(ex, "Failed to write audit log for {Method} {Path}",
                        request.Method, request.Path);
                }
            });
        }
        catch (Exception ex)
        {
            // Audit logging should NEVER break user requests
            _logger.LogError(ex, "Audit logging middleware failed");
        }
    }

    private Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                          user.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    private Guid? GetTenantId(HttpContext context)
    {
        var tenantIdStr = context.Items["TenantId"]?.ToString();

        if (Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return tenantId;
        }

        return null;
    }

    private AuditActionType DetermineActionType(string httpMethod, string path)
    {
        var pathLower = path.ToLowerInvariant();

        // Authentication endpoints
        if (pathLower.Contains("/auth/login"))
            return AuditActionType.LOGIN_SUCCESS;
        if (pathLower.Contains("/auth/logout"))
            return AuditActionType.LOGOUT;
        if (pathLower.Contains("/auth/refresh"))
            return AuditActionType.TOKEN_REFRESHED;
        if (pathLower.Contains("/auth/mfa"))
            return AuditActionType.MFA_VERIFICATION_SUCCESS;

        // Tenant management
        if (pathLower.Contains("/tenants"))
        {
            return httpMethod switch
            {
                "GET" => AuditActionType.RECORD_VIEWED,
                "POST" => AuditActionType.TENANT_CREATED,
                "PUT" or "PATCH" => AuditActionType.TENANT_UPDATED,
                "DELETE" => AuditActionType.TENANT_DELETED,
                _ => AuditActionType.RECORD_VIEWED
            };
        }

        // Employee management
        if (pathLower.Contains("/employees"))
        {
            return httpMethod switch
            {
                "GET" => AuditActionType.RECORD_VIEWED,
                "POST" => AuditActionType.EMPLOYEE_CREATED,
                "PUT" or "PATCH" => AuditActionType.EMPLOYEE_UPDATED,
                "DELETE" => AuditActionType.EMPLOYEE_DELETED,
                _ => AuditActionType.RECORD_VIEWED
            };
        }

        // Leave management
        if (pathLower.Contains("/leave"))
        {
            return httpMethod switch
            {
                "POST" when pathLower.Contains("/approve") => AuditActionType.LEAVE_REQUEST_APPROVED,
                "POST" when pathLower.Contains("/reject") => AuditActionType.LEAVE_REQUEST_REJECTED,
                "POST" => AuditActionType.LEAVE_REQUEST_CREATED,
                "PUT" or "PATCH" => AuditActionType.LEAVE_REQUEST_UPDATED,
                "DELETE" => AuditActionType.LEAVE_REQUEST_CANCELLED,
                _ => AuditActionType.RECORD_VIEWED
            };
        }

        // Payroll
        if (pathLower.Contains("/payroll"))
        {
            return httpMethod switch
            {
                "POST" when pathLower.Contains("/process") => AuditActionType.PAYROLL_PROCESSED,
                "POST" when pathLower.Contains("/approve") => AuditActionType.PAYROLL_APPROVED,
                "GET" when pathLower.Contains("/payslip") => AuditActionType.RECORD_VIEWED,
                _ => AuditActionType.RECORD_VIEWED
            };
        }

        // Generic CRUD operations
        return httpMethod switch
        {
            "GET" => AuditActionType.RECORD_VIEWED,
            "POST" => AuditActionType.RECORD_CREATED,
            "PUT" or "PATCH" => AuditActionType.RECORD_UPDATED,
            "DELETE" => AuditActionType.RECORD_DELETED,
            _ => AuditActionType.RECORD_VIEWED
        };
    }

    private AuditCategory DetermineCategory(string path)
    {
        var pathLower = path.ToLowerInvariant();

        if (pathLower.Contains("/auth"))
            return AuditCategory.AUTHENTICATION;
        if (pathLower.Contains("/admin") || pathLower.Contains("/tenants"))
            return AuditCategory.SYSTEM_ADMIN;
        if (pathLower.Contains("/employees") || pathLower.Contains("/departments"))
            return AuditCategory.DATA_CHANGE;
        if (pathLower.Contains("/leave") || pathLower.Contains("/attendance"))
            return AuditCategory.DATA_CHANGE;
        if (pathLower.Contains("/payroll"))
            return AuditCategory.DATA_CHANGE;
        if (pathLower.Contains("/reports"))
            return AuditCategory.COMPLIANCE;

        return AuditCategory.SYSTEM_EVENT;
    }

    private AuditSeverity DetermineSeverity(int statusCode, Exception? exception, AuditActionType actionType)
    {
        // Failed requests
        if (exception != null || statusCode >= 500)
            return AuditSeverity.CRITICAL;

        if (statusCode >= 400)
            return AuditSeverity.WARNING;

        // High-value actions
        if (actionType == AuditActionType.TENANT_DELETED ||
            actionType == AuditActionType.PAYROLL_PROCESSED ||
            actionType == AuditActionType.EMPLOYEE_SALARY_UPDATED)
        {
            return AuditSeverity.WARNING;
        }

        if (actionType == AuditActionType.LOGIN_SUCCESS ||
            actionType == AuditActionType.MFA_SETUP_COMPLETED ||
            actionType == AuditActionType.PASSWORD_CHANGED)
        {
            return AuditSeverity.INFO;
        }

        return AuditSeverity.INFO;
    }

    private string? SanitizeQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return null;

        // Remove sensitive parameters
        var sanitized = queryString;
        var sensitiveParams = new[] { "password", "pwd", "secret", "token", "key" };

        foreach (var param in sensitiveParams)
        {
            var pattern = $"{param}=[^&]*";
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, pattern, $"{param}=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Try X-Forwarded-For first (if behind proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        // Fall back to direct connection
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string? BuildAdditionalMetadata(HttpContext context, Exception? exception)
    {
        try
        {
            var metadata = new Dictionary<string, object?>
            {
                { "Protocol", context.Request.Protocol },
                { "Scheme", context.Request.Scheme },
                { "Host", context.Request.Host.ToString() },
                { "ContentType", context.Request.ContentType },
                { "ContentLength", context.Request.ContentLength }
            };

            if (exception != null)
            {
                metadata["ExceptionType"] = exception.GetType().Name;
                metadata["StackTrace"] = exception.StackTrace;
            }

            return JsonSerializer.Serialize(metadata);
        }
        catch
        {
            return null;
        }
    }
}
