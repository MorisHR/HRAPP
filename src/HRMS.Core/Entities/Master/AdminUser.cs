namespace HRMS.Core.Entities.Master;

/// <summary>
/// Super Admin user - stored in Master schema
/// Users who can manage all tenants
/// </summary>
public class AdminUser : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }

    // SECURITY: Account Lockout Fields
    public bool LockoutEnabled { get; set; } = true;
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; } = 0;
}
