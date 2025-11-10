namespace HRMS.Core.Entities.Master;

/// <summary>
/// Super Admin user - stored in Master schema
/// Users who can manage all tenants
/// FORTUNE 500 ENHANCED: Added password rotation, IP whitelisting, granular permissions
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

    /// <summary>
    /// Backup codes for 2FA (hashed with SHA256). Max 10 codes.
    /// Format: JSON array of hashed codes ["hash1", "hash2", ...]
    /// </summary>
    public string? BackupCodes { get; set; }

    // SECURITY: Account Lockout Fields
    public bool LockoutEnabled { get; set; } = true;
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; } = 0;

    // ============================================
    // FORTUNE 500 SECURITY ENHANCEMENTS
    // ============================================

    /// <summary>
    /// Password Rotation: Date when password was last changed
    /// Used to enforce password expiry policy (e.g., 90-day rotation)
    /// </summary>
    public DateTime? LastPasswordChangeDate { get; set; }

    /// <summary>
    /// Password Rotation: Date when current password expires
    /// Calculated as LastPasswordChangeDate + PasswordExpiryDays
    /// </summary>
    public DateTime? PasswordExpiresAt { get; set; }

    /// <summary>
    /// Force Password Change: Flag to force password change on next login
    /// Used for: Initial login, security breach, admin-forced reset
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// IP Whitelisting: JSON array of allowed IP addresses/ranges
    /// Format: ["192.168.1.0/24", "10.0.0.1", "203.0.113.0/24"]
    /// Empty/null = no restrictions (allow from any IP)
    /// </summary>
    public string? AllowedIPAddresses { get; set; }

    /// <summary>
    /// Session Security: Custom session timeout in minutes for this admin
    /// Default SuperAdmin: 15 minutes (stricter than normal users)
    /// Can be customized per admin for operational needs
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Granular Permissions: JSON array of specific permissions granted
    /// Format: ["TENANT_CREATE", "TENANT_DELETE", "SECURITY_VIEW_LOGS"]
    /// Empty/null = full SuperAdmin permissions (backward compatible)
    /// </summary>
    public string? Permissions { get; set; }

    /// <summary>
    /// Last IP Address: IP address of last successful login
    /// Used for geographic anomaly detection and audit trail
    /// </summary>
    public string? LastLoginIPAddress { get; set; }

    /// <summary>
    /// Login Attempt Tracking: Timestamp of last failed login attempt
    /// Used for rate limiting and brute force detection
    /// </summary>
    public DateTime? LastFailedLoginAttempt { get; set; }

    /// <summary>
    /// Account Creation: Flag indicating if this is the initial system setup account
    /// Used to track the bootstrap SuperAdmin created during first setup
    /// </summary>
    public bool IsInitialSetupAccount { get; set; } = false;

    /// <summary>
    /// Password History: JSON array of previous password hashes
    /// Format: ["hash1", "hash2", "hash3"] (last 5 passwords)
    /// Prevents password reuse for security compliance
    /// </summary>
    public string? PasswordHistory { get; set; }

    /// <summary>
    /// Audit Tracking: SuperAdmin who created this admin account
    /// Used for accountability and audit trail
    /// </summary>
    public Guid? CreatedBySuperAdminId { get; set; }

    /// <summary>
    /// Audit Tracking: SuperAdmin who last modified this admin account
    /// Used for accountability and audit trail
    /// </summary>
    public Guid? LastModifiedBySuperAdminId { get; set; }

    /// <summary>
    /// Login Restrictions: Allowed login hours in JSON format
    /// Format: {"start": 8, "end": 18, "timezone": "UTC"}
    /// Empty/null = 24/7 access allowed
    /// </summary>
    public string? AllowedLoginHours { get; set; }

    /// <summary>
    /// Account Status: Notes about account status (suspension reason, etc.)
    /// Used for admin-to-admin communication about account issues
    /// </summary>
    public string? StatusNotes { get; set; }
}
