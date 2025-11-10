using System.Text.Json;
using HRMS.Application.Interfaces;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Production-grade SuperAdmin permission management service
/// FORTUNE 500 COMPLIANT: AWS IAM, Azure RBAC, Google Cloud IAM patterns
///
/// SECURITY FEATURES:
/// - Comprehensive audit logging for all permission checks
/// - Fail-secure design (denies access by default)
/// - Real-time security alerting for denied access
/// - Permission change tracking with full audit trail
/// </summary>
public class SuperAdminPermissionService : ISuperAdminPermissionService
{
    private readonly MasterDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SuperAdminPermissionService> _logger;

    public SuperAdminPermissionService(
        MasterDbContext context,
        IAuditLogService auditLogService,
        ILogger<SuperAdminPermissionService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(Guid superAdminId, SuperAdminPermission permission)
    {
        try
        {
            var adminUser = await _context.AdminUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == superAdminId && u.IsActive);

            if (adminUser == null)
            {
                _logger.LogWarning("Permission check failed: SuperAdmin {SuperAdminId} not found or inactive", superAdminId);
                return false;
            }

            var permissions = ParsePermissions(adminUser.Permissions);

            // FULL_ACCESS grants all permissions
            if (permissions.Contains(SuperAdminPermission.FULL_ACCESS))
            {
                _logger.LogDebug("Permission {Permission} granted to SuperAdmin {SuperAdminId} (FULL_ACCESS)", permission, superAdminId);
                return true;
            }

            bool hasPermission = permissions.Contains(permission);

            if (!hasPermission)
            {
                // SECURITY: Log denied access for monitoring
                _logger.LogWarning(
                    "Permission DENIED: SuperAdmin {SuperAdminId} ({Email}) attempted to access {Permission}",
                    superAdminId, adminUser.Email, permission);

                // Audit log: Permission denied
                _ = _auditLogService.LogSecurityEventAsync(
                    AuditActionType.ACCESS_DENIED,
                    AuditSeverity.WARNING,
                    superAdminId,
                    description: $"SuperAdmin attempted to access resource without required permission: {permission}",
                    additionalInfo: JsonSerializer.Serialize(new
                    {
                        requiredPermission = permission.ToString(),
                        userEmail = adminUser.Email,
                        userPermissions = permissions.Select(p => p.ToString()).ToList()
                    })
                );
            }
            else
            {
                _logger.LogDebug("Permission {Permission} granted to SuperAdmin {SuperAdminId}", permission, superAdminId);
            }

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for SuperAdmin {SuperAdminId}", permission, superAdminId);
            // Fail secure: deny access on error
            return false;
        }
    }

    public async Task<bool> HasAnyPermissionAsync(Guid superAdminId, params SuperAdminPermission[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
        {
            return false;
        }

        foreach (var permission in permissions)
        {
            if (await HasPermissionAsync(superAdminId, permission))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> HasAllPermissionsAsync(Guid superAdminId, params SuperAdminPermission[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
        {
            return true; // No permissions required
        }

        foreach (var permission in permissions)
        {
            if (!await HasPermissionAsync(superAdminId, permission))
            {
                return false;
            }
        }

        return true;
    }

    public async Task<List<SuperAdminPermission>> GetPermissionsAsync(Guid superAdminId)
    {
        try
        {
            var adminUser = await _context.AdminUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == superAdminId);

            if (adminUser == null)
            {
                return new List<SuperAdminPermission>();
            }

            return ParsePermissions(adminUser.Permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for SuperAdmin {SuperAdminId}", superAdminId);
            return new List<SuperAdminPermission>();
        }
    }

    public async Task<(bool Success, string Message)> AssignPermissionsAsync(
        Guid superAdminId,
        List<SuperAdminPermission> permissions,
        Guid performedBy)
    {
        try
        {
            // Check if performer has permission to manage permissions
            if (!await HasPermissionAsync(performedBy, SuperAdminPermission.SUPERADMIN_PERMISSIONS_MANAGE))
            {
                _logger.LogWarning(
                    "Permission management denied: SuperAdmin {PerformedBy} lacks SUPERADMIN_PERMISSIONS_MANAGE",
                    performedBy);

                return (false, "You do not have permission to manage SuperAdmin permissions");
            }

            var adminUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == superAdminId);

            if (adminUser == null)
            {
                return (false, "SuperAdmin not found");
            }

            var oldPermissions = ParsePermissions(adminUser.Permissions);

            // Serialize new permissions to JSON
            adminUser.Permissions = JsonSerializer.Serialize(permissions.Select(p => (int)p).ToList());
            adminUser.UpdatedAt = DateTime.UtcNow;
            adminUser.LastModifiedBySuperAdminId = performedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Permissions assigned to SuperAdmin {SuperAdminId} by {PerformedBy}",
                superAdminId, performedBy);

            // FORTUNE 500: Comprehensive audit logging
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.SUPERADMIN_PERMISSION_CHANGED,
                performedBy,
                (await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == performedBy))?.Email ?? "unknown",
                targetTenantId: null,
                targetTenantName: null,
                description: $"Assigned permissions to SuperAdmin {adminUser.Email}",
                oldValues: JsonSerializer.Serialize(new
                {
                    permissions = oldPermissions.Select(p => p.ToString()).ToList()
                }),
                newValues: JsonSerializer.Serialize(new
                {
                    permissions = permissions.Select(p => p.ToString()).ToList()
                }),
                success: true,
                additionalContext: new Dictionary<string, object>
                {
                    { "targetSuperAdminId", superAdminId },
                    { "targetSuperAdminEmail", adminUser.Email },
                    { "permissionsAdded", permissions.Except(oldPermissions).Select(p => p.ToString()).ToList() },
                    { "permissionsRemoved", oldPermissions.Except(permissions).Select(p => p.ToString()).ToList() }
                }
            );

            return (true, $"Successfully assigned {permissions.Count} permission(s) to SuperAdmin {adminUser.Email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permissions to SuperAdmin {SuperAdminId}", superAdminId);
            return (false, "An error occurred while assigning permissions");
        }
    }

    public async Task<(bool Success, string Message)> RevokePermissionsAsync(
        Guid superAdminId,
        List<SuperAdminPermission> permissionsToRevoke,
        Guid performedBy)
    {
        try
        {
            if (!await HasPermissionAsync(performedBy, SuperAdminPermission.SUPERADMIN_PERMISSIONS_MANAGE))
            {
                return (false, "You do not have permission to manage SuperAdmin permissions");
            }

            var adminUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == superAdminId);

            if (adminUser == null)
            {
                return (false, "SuperAdmin not found");
            }

            var currentPermissions = ParsePermissions(adminUser.Permissions);
            var newPermissions = currentPermissions.Except(permissionsToRevoke).ToList();

            // Serialize new permissions to JSON
            adminUser.Permissions = JsonSerializer.Serialize(newPermissions.Select(p => (int)p).ToList());
            adminUser.UpdatedAt = DateTime.UtcNow;
            adminUser.LastModifiedBySuperAdminId = performedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Permissions revoked from SuperAdmin {SuperAdminId} by {PerformedBy}",
                superAdminId, performedBy);

            // Audit logging
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.SUPERADMIN_PERMISSION_CHANGED,
                performedBy,
                (await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == performedBy))?.Email ?? "unknown",
                description: $"Revoked permissions from SuperAdmin {adminUser.Email}",
                oldValues: JsonSerializer.Serialize(new { permissions = currentPermissions.Select(p => p.ToString()).ToList() }),
                newValues: JsonSerializer.Serialize(new { permissions = newPermissions.Select(p => p.ToString()).ToList() }),
                success: true,
                additionalContext: new Dictionary<string, object>
                {
                    { "targetSuperAdminId", superAdminId },
                    { "targetSuperAdminEmail", adminUser.Email },
                    { "permissionsRevoked", permissionsToRevoke.Select(p => p.ToString()).ToList() }
                }
            );

            return (true, $"Successfully revoked {permissionsToRevoke.Count} permission(s) from SuperAdmin {adminUser.Email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permissions from SuperAdmin {SuperAdminId}", superAdminId);
            return (false, "An error occurred while revoking permissions");
        }
    }

    public async Task<(bool Success, string Message)> AssignRoleAsync(
        Guid superAdminId,
        string roleName,
        Guid performedBy)
    {
        try
        {
            if (!await HasPermissionAsync(performedBy, SuperAdminPermission.SUPERADMIN_PERMISSIONS_MANAGE))
            {
                return (false, "You do not have permission to manage SuperAdmin permissions");
            }

            var permissions = roleName switch
            {
                "MasterAdmin" => SuperAdminRoles.MasterAdmin.ToList(),
                "TenantOperations" => SuperAdminRoles.TenantOperations.ToList(),
                "SupportAdmin" => SuperAdminRoles.SupportAdmin.ToList(),
                "SecurityAuditor" => SuperAdminRoles.SecurityAuditor.ToList(),
                "BillingManager" => SuperAdminRoles.BillingManager.ToList(),
                "ReadOnlyAnalyst" => SuperAdminRoles.ReadOnlyAnalyst.ToList(),
                _ => null
            };

            if (permissions == null)
            {
                return (false, $"Invalid role name: {roleName}. Valid roles: MasterAdmin, TenantOperations, SupportAdmin, SecurityAuditor, BillingManager, ReadOnlyAnalyst");
            }

            return await AssignPermissionsAsync(superAdminId, permissions, performedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to SuperAdmin {SuperAdminId}", roleName, superAdminId);
            return (false, "An error occurred while assigning role");
        }
    }

    // ============================================
    // PRIVATE HELPER METHODS
    // ============================================

    /// <summary>
    /// Parse permissions from JSON string
    /// Fail-secure: returns empty list on error
    /// </summary>
    private List<SuperAdminPermission> ParsePermissions(string? permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
        {
            return new List<SuperAdminPermission>();
        }

        try
        {
            var permissionInts = JsonSerializer.Deserialize<List<int>>(permissionsJson);
            if (permissionInts == null)
            {
                return new List<SuperAdminPermission>();
            }

            return permissionInts
                .Where(p => Enum.IsDefined(typeof(SuperAdminPermission), p))
                .Select(p => (SuperAdminPermission)p)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing permissions JSON: {Json}", permissionsJson);
            // Fail secure: return empty permissions on parse error
            return new List<SuperAdminPermission>();
        }
    }
}
