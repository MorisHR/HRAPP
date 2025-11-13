using HRMS.API.Attributes;
using HRMS.Application.Interfaces;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRMS.API.Filters;

/// <summary>
/// Authorization filter to enforce permission-based access control
/// Checks RequirePermissionAttribute on controller actions
/// Validates that the authenticated admin user has the required permissions
///
/// SECURITY DESIGN:
/// - Runs after authentication (requires valid JWT token)
/// - Extracts user ID from JWT claims
/// - Checks AdminUser.Permissions field via IPermissionService
/// - Logs permission denials for security audit
/// - Returns 403 Forbidden if permission check fails
/// </summary>
public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PermissionAuthorizationFilter> _logger;

    public PermissionAuthorizationFilter(
        IPermissionService permissionService,
        IAuditLogService auditLogService,
        ILogger<PermissionAuthorizationFilter> logger)
    {
        _permissionService = permissionService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get required permissions from attributes on the action/controller
        var requiredPermissions = context.ActionDescriptor
            .EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .Select(a => a.Permission)
            .ToList();

        // No permission requirements - allow access
        if (!requiredPermissions.Any())
            return;

        // Get current user ID from JWT claims
        // Try multiple claim types for compatibility
        var userIdClaim = context.HttpContext.User.FindFirst("sub")?.Value
                       ?? context.HttpContext.User.FindFirst("userId")?.Value
                       ?? context.HttpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            // No valid user ID in claims - authentication issue
            _logger.LogWarning("Permission check failed: No valid user ID in claims for endpoint {Endpoint}",
                context.ActionDescriptor.DisplayName);

            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "Authentication required"
            });
            return;
        }

        // Get user email for audit logging
        var userEmail = context.HttpContext.User.FindFirst("email")?.Value
                     ?? context.HttpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                     ?? "unknown";

        // Check if user has ANY of the required permissions (OR logic)
        // If multiple [RequirePermission] attributes are present, user needs at least one
        foreach (var permission in requiredPermissions)
        {
            if (await _permissionService.HasPermissionAsync(userId, permission))
            {
                // User has permission - allow access
                _logger.LogDebug("Permission check passed: User {UserId} has permission {Permission} for {Endpoint}",
                    userId, permission, context.ActionDescriptor.DisplayName);
                return;
            }
        }

        // User does not have any of the required permissions - deny access
        _logger.LogWarning("Permission denied: User {UserId} ({Email}) lacks required permissions [{Permissions}] for {Endpoint}",
            userId, userEmail, string.Join(", ", requiredPermissions), context.ActionDescriptor.DisplayName);

        // Log permission denial for security audit
        await _auditLogService.LogSecurityEventAsync(
            AuditActionType.PERMISSION_DENIED,
            AuditSeverity.WARNING,
            userId: userId,
            description: $"Permission denied for endpoint: {context.ActionDescriptor.DisplayName}. Required permissions: {string.Join(", ", requiredPermissions)}",
            additionalInfo: System.Text.Json.JsonSerializer.Serialize(new
            {
                endpoint = context.ActionDescriptor.DisplayName,
                requiredPermissions,
                userEmail,
                httpMethod = context.HttpContext.Request.Method,
                path = context.HttpContext.Request.Path.Value
            })
        );

        // Return 403 Forbidden
        context.Result = new ObjectResult(new
        {
            success = false,
            message = "You do not have permission to perform this action",
            requiredPermissions,
            error = "PERMISSION_DENIED"
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
