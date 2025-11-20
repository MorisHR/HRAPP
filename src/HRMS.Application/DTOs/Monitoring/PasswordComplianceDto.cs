using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 500: Password Strength and Compliance Metrics
/// Monitors password policy compliance and identifies weak passwords
/// PATTERN: 1Password Insights, LastPass Security Dashboard
/// COMPLIANCE: NIST 800-63B, PCI-DSS 8.2, ISO 27001 A.9.4.3, GDPR Article 32
/// </summary>
public class PasswordComplianceDto
{
    /// <summary>
    /// Total active user accounts
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Users with strong passwords (meets all requirements)
    /// </summary>
    public int UsersWithStrongPasswords { get; set; }

    /// <summary>
    /// Users with weak passwords (fails requirements)
    /// </summary>
    public int UsersWithWeakPasswords { get; set; }

    /// <summary>
    /// Password strength compliance rate (percentage)
    /// </summary>
    public decimal ComplianceRate { get; set; }

    /// <summary>
    /// Users with passwords expiring soon (next 7 days)
    /// </summary>
    public int PasswordsExpiringSoon { get; set; }

    /// <summary>
    /// Users with expired passwords
    /// </summary>
    public int ExpiredPasswords { get; set; }

    /// <summary>
    /// Users who never changed password
    /// </summary>
    public int NeverChangedPassword { get; set; }

    /// <summary>
    /// Average password age (days)
    /// </summary>
    public decimal AveragePasswordAge { get; set; }

    /// <summary>
    /// Users with password reuse detected
    /// </summary>
    public int PasswordReuseDetected { get; set; }

    /// <summary>
    /// Users with common/compromised passwords
    /// </summary>
    public int CompromisedPasswords { get; set; }

    /// <summary>
    /// Password changes in last 7 days
    /// </summary>
    public int RecentPasswordChanges { get; set; }

    /// <summary>
    /// Password changes in last 30 days
    /// </summary>
    public int PasswordChangesLast30Days { get; set; }

    /// <summary>
    /// Trend percentage compared to previous period
    /// </summary>
    public decimal TrendPercentage { get; set; }

    /// <summary>
    /// Password compliance by tenant
    /// </summary>
    public List<TenantPasswordCompliance> ComplianceByTenant { get; set; } = new();

    /// <summary>
    /// Password strength distribution
    /// </summary>
    public PasswordStrengthDistribution StrengthDistribution { get; set; } = new();

    /// <summary>
    /// Users with weak passwords (high priority)
    /// </summary>
    public List<WeakPasswordUser> WeakPasswordUsers { get; set; } = new();

    /// <summary>
    /// Password age distribution (days)
    /// </summary>
    public Dictionary<string, int> AgeDistribution { get; set; } = new();

    /// <summary>
    /// Recent password change activity
    /// </summary>
    public List<PasswordChangeActivity> RecentActivity { get; set; } = new();

    /// <summary>
    /// Daily password change trend (last 30 days)
    /// </summary>
    public List<TimeSeriesDataPoint> ChangeTrend { get; set; } = new();
}

/// <summary>
/// Tenant password compliance
/// </summary>
public class TenantPasswordCompliance
{
    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string TenantSubdomain { get; set; } = string.Empty;

    /// <summary>
    /// Tenant name
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Total users
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Users with strong passwords
    /// </summary>
    public int StrongPasswords { get; set; }

    /// <summary>
    /// Users with weak passwords
    /// </summary>
    public int WeakPasswords { get; set; }

    /// <summary>
    /// Compliance rate (percentage)
    /// </summary>
    public decimal ComplianceRate { get; set; }

    /// <summary>
    /// Passwords expiring soon
    /// </summary>
    public int ExpiringSoon { get; set; }

    /// <summary>
    /// Average password age (days)
    /// </summary>
    public decimal AveragePasswordAge { get; set; }

    /// <summary>
    /// Compliance status: "Compliant", "AtRisk", "NonCompliant"
    /// </summary>
    public string ComplianceStatus { get; set; } = string.Empty;
}

/// <summary>
/// Password strength distribution
/// </summary>
public class PasswordStrengthDistribution
{
    /// <summary>
    /// Very strong passwords (exceeds requirements)
    /// </summary>
    public int VeryStrong { get; set; }

    /// <summary>
    /// Strong passwords (meets all requirements)
    /// </summary>
    public int Strong { get; set; }

    /// <summary>
    /// Medium strength (meets minimum requirements)
    /// </summary>
    public int Medium { get; set; }

    /// <summary>
    /// Weak passwords (fails some requirements)
    /// </summary>
    public int Weak { get; set; }

    /// <summary>
    /// Very weak passwords (fails multiple requirements)
    /// </summary>
    public int VeryWeak { get; set; }
}

/// <summary>
/// User with weak password
/// </summary>
public class WeakPasswordUser
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User email
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// User name
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string TenantSubdomain { get; set; } = string.Empty;

    /// <summary>
    /// Password strength: "VeryWeak", "Weak", "Medium"
    /// </summary>
    public string PasswordStrength { get; set; } = string.Empty;

    /// <summary>
    /// Password age (days)
    /// </summary>
    public int PasswordAgeDays { get; set; }

    /// <summary>
    /// Password expires in (days, null if no expiration)
    /// </summary>
    public int? ExpiresInDays { get; set; }

    /// <summary>
    /// Failed requirements (e.g., "Length", "Uppercase", "Special")
    /// </summary>
    public List<string> FailedRequirements { get; set; } = new();

    /// <summary>
    /// Whether password is compromised (found in breach database)
    /// </summary>
    public bool IsCompromised { get; set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }
}

/// <summary>
/// Password change activity
/// </summary>
public class PasswordChangeActivity
{
    /// <summary>
    /// Activity timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Activity type: "Changed", "Reset", "Expired"
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// User email
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// New password strength
    /// </summary>
    public string? NewPasswordStrength { get; set; }

    /// <summary>
    /// Whether the change was forced by admin/policy
    /// </summary>
    public bool WasForced { get; set; }
}
