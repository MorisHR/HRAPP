namespace HRMS.Core.Enums;

/// <summary>
/// Tenant status indicator
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Tenant created, awaiting email activation (gray)
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Tenant is fully operational (green)
    /// </summary>
    Active = 1,

    /// <summary>
    /// Tenant is temporarily blocked (yellow)
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Tenant marked for deletion, in grace period (red)
    /// </summary>
    SoftDeleted = 3,

    /// <summary>
    /// Subscription expired (orange)
    /// </summary>
    Expired = 4,

    /// <summary>
    /// In trial period (blue)
    /// </summary>
    Trial = 5
}
