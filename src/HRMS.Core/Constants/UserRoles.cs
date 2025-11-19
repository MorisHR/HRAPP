namespace HRMS.Core.Constants;

/// <summary>
/// Centralized user role constants for consistent role checking across the application
/// Use these instead of magic strings to prevent typos and enable refactoring
/// </summary>
public static class UserRoles
{
    /// <summary>
    /// SuperAdmin: Platform-level administrator with system-wide access
    /// Actions should NEVER have TenantId (system-wide scope)
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// TenantAdmin: Tenant-level administrator with full access to their tenant
    /// Actions should ALWAYS have TenantId (tenant-scoped)
    /// </summary>
    public const string TenantAdmin = "TenantAdmin";

    /// <summary>
    /// HR: Human Resources role with employee management capabilities
    /// Actions should ALWAYS have TenantId (tenant-scoped)
    /// </summary>
    public const string HR = "HR";

    /// <summary>
    /// Manager: Department/team manager with limited HR capabilities
    /// Actions should ALWAYS have TenantId (tenant-scoped)
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// Employee: Standard employee with self-service access
    /// Actions should ALWAYS have TenantId (tenant-scoped)
    /// </summary>
    public const string Employee = "Employee";

    /// <summary>
    /// Helper method to check if a role is system-level (not tenant-scoped)
    /// </summary>
    /// <param name="role">Role name to check</param>
    /// <returns>True if role is system-level (SuperAdmin), false otherwise</returns>
    public static bool IsSystemLevelRole(string? role)
    {
        return role?.Equals(SuperAdmin, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Helper method to check if a role is tenant-scoped
    /// </summary>
    /// <param name="role">Role name to check</param>
    /// <returns>True if role is tenant-scoped, false otherwise</returns>
    public static bool IsTenantScopedRole(string? role)
    {
        return !IsSystemLevelRole(role) && !string.IsNullOrWhiteSpace(role);
    }
}
