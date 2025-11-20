using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 500: MFA (Multi-Factor Authentication) Compliance
/// Real-time tracking of MFA adoption and enforcement across organization
/// PATTERN: Okta MFA, Duo Security, Microsoft Authenticator
/// COMPLIANCE: PCI-DSS 8.3, NIST 800-63B AAL2/AAL3, SOX, GDPR Article 32
/// </summary>
public class MfaComplianceDto
{
    /// <summary>
    /// Total users across all tenants
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Users with MFA enabled
    /// </summary>
    public int UsersWithMfaEnabled { get; set; }

    /// <summary>
    /// Users without MFA enabled
    /// </summary>
    public int UsersWithoutMfa { get; set; }

    /// <summary>
    /// MFA adoption rate (percentage)
    /// </summary>
    public decimal MfaAdoptionRate { get; set; }

    /// <summary>
    /// Users required to have MFA (based on role/policy)
    /// </summary>
    public int UsersRequiringMfa { get; set; }

    /// <summary>
    /// Users non-compliant (required MFA but not enabled)
    /// </summary>
    public int NonCompliantUsers { get; set; }

    /// <summary>
    /// Compliance rate (percentage)
    /// </summary>
    public decimal ComplianceRate { get; set; }

    /// <summary>
    /// MFA enrollments in last 7 days
    /// </summary>
    public int RecentEnrollments { get; set; }

    /// <summary>
    /// MFA enrollments in last 30 days
    /// </summary>
    public int EnrollmentsLast30Days { get; set; }

    /// <summary>
    /// Trend percentage compared to previous period
    /// </summary>
    public decimal TrendPercentage { get; set; }

    /// <summary>
    /// MFA compliance by tenant
    /// </summary>
    public List<TenantMfaCompliance> ComplianceByTenant { get; set; } = new();

    /// <summary>
    /// MFA compliance by role
    /// </summary>
    public List<RoleMfaCompliance> ComplianceByRole { get; set; } = new();

    /// <summary>
    /// Non-compliant users list
    /// </summary>
    public List<NonCompliantUser> NonCompliantUsersList { get; set; } = new();

    /// <summary>
    /// MFA method distribution
    /// </summary>
    public Dictionary<string, int> MfaMethodDistribution { get; set; } = new();

    /// <summary>
    /// Recent MFA enrollment activity
    /// </summary>
    public List<MfaEnrollmentActivity> RecentActivity { get; set; } = new();

    /// <summary>
    /// Daily adoption trend (last 30 days)
    /// </summary>
    public List<TimeSeriesDataPoint> AdoptionTrend { get; set; } = new();
}

/// <summary>
/// Tenant MFA compliance
/// </summary>
public class TenantMfaCompliance
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
    /// Total users in tenant
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Users with MFA enabled
    /// </summary>
    public int UsersWithMfa { get; set; }

    /// <summary>
    /// Users required to have MFA
    /// </summary>
    public int UsersRequiringMfa { get; set; }

    /// <summary>
    /// Non-compliant users
    /// </summary>
    public int NonCompliantUsers { get; set; }

    /// <summary>
    /// Adoption rate (percentage)
    /// </summary>
    public decimal AdoptionRate { get; set; }

    /// <summary>
    /// Compliance rate (percentage)
    /// </summary>
    public decimal ComplianceRate { get; set; }

    /// <summary>
    /// Whether MFA is enforced for this tenant
    /// </summary>
    public bool MfaEnforced { get; set; }

    /// <summary>
    /// Compliance status: "Compliant", "AtRisk", "NonCompliant"
    /// </summary>
    public string ComplianceStatus { get; set; } = string.Empty;
}

/// <summary>
/// Role MFA compliance
/// </summary>
public class RoleMfaCompliance
{
    /// <summary>
    /// Role name
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Total users with this role
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Users with MFA enabled
    /// </summary>
    public int UsersWithMfa { get; set; }

    /// <summary>
    /// Adoption rate (percentage)
    /// </summary>
    public decimal AdoptionRate { get; set; }

    /// <summary>
    /// Whether MFA is required for this role
    /// </summary>
    public bool MfaRequired { get; set; }

    /// <summary>
    /// Risk level: "High", "Medium", "Low"
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;
}

/// <summary>
/// Non-compliant user
/// </summary>
public class NonCompliantUser
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
    /// Days since account creation
    /// </summary>
    public int DaysSinceCreation { get; set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// Number of reminders sent
    /// </summary>
    public int RemindersSent { get; set; }

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }
}

/// <summary>
/// MFA enrollment activity
/// </summary>
public class MfaEnrollmentActivity
{
    /// <summary>
    /// Activity timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Activity type: "Enrolled", "Disabled", "MethodChanged"
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
    /// MFA method: "TOTP", "SMS", "Email", "Hardware"
    /// </summary>
    public string? MfaMethod { get; set; }

    /// <summary>
    /// User role
    /// </summary>
    public string? Role { get; set; }
}
