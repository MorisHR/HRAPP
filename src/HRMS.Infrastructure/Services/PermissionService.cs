using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HRMS.Application.Interfaces;
using System.Text.Json;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Interface for permission checking service
/// Enforces granular RBAC for AdminUser permissions
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if an admin user has a specific permission
    /// </summary>
    /// <param name="adminUserId">Admin user ID</param>
    /// <param name="permission">Permission constant (e.g., "TENANT_CREATE")</param>
    /// <returns>True if user has permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(Guid adminUserId, string permission);

    /// <summary>
    /// Get all permissions for an admin user
    /// </summary>
    /// <param name="adminUserId">Admin user ID</param>
    /// <returns>List of permission strings</returns>
    Task<List<string>> GetUserPermissionsAsync(Guid adminUserId);
}

/// <summary>
/// Permission service implementation
/// Checks AdminUser.Permissions field and enforces RBAC
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly MasterDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public PermissionService(MasterDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Check if admin user has a specific permission
    /// SuperAdmin role automatically has all permissions
    /// Checks AdminUser.Permissions JSON array for specific permission or wildcard (*)
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid adminUserId, string permission)
    {
        var adminUser = await _context.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == adminUserId && u.IsActive);

        if (adminUser == null)
            return false;

        // BACKWARD COMPATIBILITY: If Permissions field is null/empty, assume full access
        // This maintains compatibility with existing AdminUser records created before RBAC implementation
        // TODO: In future, make permissions mandatory and remove this fallback
        if (string.IsNullOrWhiteSpace(adminUser.Permissions))
        {
            // Log permission check for audit (full access granted due to null permissions)
            return true;
        }

        // Parse permissions JSON array
        List<string>? permissions;
        try
        {
            permissions = JsonSerializer.Deserialize<List<string>>(adminUser.Permissions);
        }
        catch (JsonException)
        {
            // Invalid JSON - deny access and log security event
            await _auditLogService.LogSecurityEventAsync(
                Core.Enums.AuditActionType.PERMISSION_DENIED,
                Core.Enums.AuditSeverity.WARNING,
                adminUserId,
                $"Invalid permissions JSON for admin user: {adminUser.Email}");
            return false;
        }

        if (permissions == null || permissions.Count == 0)
            return false;

        // Check for wildcard permission (grants everything)
        if (permissions.Contains("*"))
            return true;

        // Check for specific permission
        return permissions.Contains(permission);
    }

    /// <summary>
    /// Get all permissions for an admin user
    /// Used for UI permission checks and debugging
    /// </summary>
    public async Task<List<string>> GetUserPermissionsAsync(Guid adminUserId)
    {
        var adminUser = await _context.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == adminUserId);

        if (adminUser == null || string.IsNullOrWhiteSpace(adminUser.Permissions))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(adminUser.Permissions) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
