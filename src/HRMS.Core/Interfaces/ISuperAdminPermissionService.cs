using HRMS.Core.Enums;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for validating SuperAdmin permissions with audit logging
/// FORTUNE 500 PATTERN: AWS IAM Policy Evaluation, Azure RBAC
/// </summary>
public interface ISuperAdminPermissionService
{
    /// <summary>
    /// Check if a SuperAdmin has a specific permission
    /// Automatically logs denied access attempts for security monitoring
    /// </summary>
    /// <param name="superAdminId">SuperAdmin user ID</param>
    /// <param name="permission">Required permission</param>
    /// <returns>True if permission granted, false otherwise</returns>
    Task<bool> HasPermissionAsync(Guid superAdminId, SuperAdminPermission permission);

    /// <summary>
    /// Check if a SuperAdmin has ANY of the specified permissions (OR logic)
    /// </summary>
    /// <param name="superAdminId">SuperAdmin user ID</param>
    /// <param name="permissions">List of acceptable permissions</param>
    /// <returns>True if at least one permission is granted</returns>
    Task<bool> HasAnyPermissionAsync(Guid superAdminId, params SuperAdminPermission[] permissions);

    /// <summary>
    /// Check if a SuperAdmin has ALL of the specified permissions (AND logic)
    /// </summary>
    /// <param name="superAdminId">SuperAdmin user ID</param>
    /// <param name="permissions">List of required permissions</param>
    /// <returns>True if all permissions are granted</returns>
    Task<bool> HasAllPermissionsAsync(Guid superAdminId, params SuperAdminPermission[] permissions);

    /// <summary>
    /// Get all permissions for a SuperAdmin
    /// </summary>
    /// <param name="superAdminId">SuperAdmin user ID</param>
    /// <returns>List of granted permissions</returns>
    Task<List<SuperAdminPermission>> GetPermissionsAsync(Guid superAdminId);

    /// <summary>
    /// Assign permissions to a SuperAdmin
    /// Requires SUPERADMIN_PERMISSIONS_MANAGE permission
    /// Logs all permission changes for audit trail
    /// </summary>
    /// <param name="superAdminId">SuperAdmin to modify</param>
    /// <param name="permissions">Permissions to assign</param>
    /// <param name="performedBy">SuperAdmin performing the assignment</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string Message)> AssignPermissionsAsync(
        Guid superAdminId,
        List<SuperAdminPermission> permissions,
        Guid performedBy);

    /// <summary>
    /// Revoke specific permissions from a SuperAdmin
    /// </summary>
    /// <param name="superAdminId">SuperAdmin to modify</param>
    /// <param name="permissions">Permissions to revoke</param>
    /// <param name="performedBy">SuperAdmin performing the revocation</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string Message)> RevokePermissionsAsync(
        Guid superAdminId,
        List<SuperAdminPermission> permissions,
        Guid performedBy);

    /// <summary>
    /// Assign predefined role (e.g., TenantOperations, SupportAdmin)
    /// </summary>
    /// <param name="superAdminId">SuperAdmin to assign role</param>
    /// <param name="roleName">Role name (MasterAdmin, TenantOperations, etc.)</param>
    /// <param name="performedBy">SuperAdmin performing the assignment</param>
    /// <returns>Success status</returns>
    Task<(bool Success, string Message)> AssignRoleAsync(
        Guid superAdminId,
        string roleName,
        Guid performedBy);
}
