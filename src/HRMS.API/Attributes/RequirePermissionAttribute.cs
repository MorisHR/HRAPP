namespace HRMS.API.Attributes;

/// <summary>
/// Attribute to enforce permission-based authorization on controller actions
/// Used in conjunction with PermissionAuthorizationFilter
///
/// Usage:
/// [RequirePermission(Permissions.TENANT_CREATE)]
/// public async Task<IActionResult> CreateTenant() { ... }
///
/// Multiple permissions (OR logic - user needs ANY of the permissions):
/// [RequirePermission(Permissions.TENANT_VIEW)]
/// [RequirePermission(Permissions.TENANT_UPDATE)]
/// public async Task<IActionResult> GetTenant() { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    /// <summary>
    /// Required permission constant
    /// Should use values from HRMS.Core.Constants.Permissions
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Create a permission requirement attribute
    /// </summary>
    /// <param name="permission">Permission constant (e.g., "TENANT_CREATE")</param>
    public RequirePermissionAttribute(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}
