using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 500: Comprehensive Security Dashboard Analytics
/// One-stop API for all security metrics and KPIs
/// PATTERN: Splunk Security Dashboard, Azure Sentinel Overview, AWS Security Hub
/// COMPLIANCE: SOC 2, ISO 27001, PCI-DSS, NIST 800-53, GDPR Article 32
/// </summary>
public class SecurityDashboardAnalyticsDto
{
    /// <summary>
    /// Overall security score (0-100)
    /// Composite score based on all security metrics
    /// </summary>
    public decimal OverallSecurityScore { get; set; }

    /// <summary>
    /// Security score trend: "improving", "declining", "stable"
    /// </summary>
    public string SecurityTrend { get; set; } = "stable";

    /// <summary>
    /// Critical issues requiring immediate attention
    /// </summary>
    public int CriticalIssuesCount { get; set; }

    /// <summary>
    /// High priority issues
    /// </summary>
    public int HighPriorityIssuesCount { get; set; }

    /// <summary>
    /// Medium priority issues
    /// </summary>
    public int MediumPriorityIssuesCount { get; set; }

    /// <summary>
    /// Failed login summary
    /// </summary>
    public FailedLoginSummary FailedLogins { get; set; } = new();

    /// <summary>
    /// Brute force attack summary
    /// </summary>
    public BruteForceSummary BruteForce { get; set; } = new();

    /// <summary>
    /// IP blacklist summary
    /// </summary>
    public IpBlacklistSummary IpBlacklist { get; set; } = new();

    /// <summary>
    /// Session management summary
    /// </summary>
    public SessionManagementSummary Sessions { get; set; } = new();

    /// <summary>
    /// MFA compliance summary
    /// </summary>
    public MfaComplianceSummary MfaCompliance { get; set; } = new();

    /// <summary>
    /// Password compliance summary
    /// </summary>
    public PasswordComplianceSummary PasswordCompliance { get; set; } = new();

    /// <summary>
    /// Anomaly detection summary
    /// </summary>
    public AnomalyDetectionSummary AnomalyDetection { get; set; } = new();

    /// <summary>
    /// Security alerts summary
    /// </summary>
    public SecurityAlertsSummary SecurityAlerts { get; set; } = new();

    /// <summary>
    /// Real-time activity feed (last 10 critical events)
    /// </summary>
    public List<SecurityActivityEntry> RecentCriticalActivity { get; set; } = new();

    /// <summary>
    /// Security metrics by tenant (top 10 at-risk)
    /// </summary>
    public List<TenantSecurityMetrics> AtRiskTenants { get; set; } = new();

    /// <summary>
    /// Compliance status across all standards
    /// </summary>
    public ComplianceStatus ComplianceStatus { get; set; } = new();

    /// <summary>
    /// Last dashboard refresh timestamp
    /// </summary>
    public DateTime LastRefreshedAt { get; set; }

    /// <summary>
    /// Data freshness (seconds since last refresh)
    /// </summary>
    public int DataFreshnessSeconds { get; set; }
}

/// <summary>
/// Failed login summary metrics
/// </summary>
public class FailedLoginSummary
{
    public int TotalLast24Hours { get; set; }
    public int TotalLast7Days { get; set; }
    public decimal TrendPercentage { get; set; }
    public string TrendDirection { get; set; } = "stable";
    public int UniqueIps { get; set; }
    public int BlacklistedIps { get; set; }
}

/// <summary>
/// Brute force attack summary metrics
/// </summary>
public class BruteForceSummary
{
    public int ActiveAttacks { get; set; }
    public int AttacksBlockedLast24Hours { get; set; }
    public int AttacksBlockedLast7Days { get; set; }
    public decimal BlockSuccessRate { get; set; }
    public int CurrentAttackRate { get; set; }
}

/// <summary>
/// IP blacklist summary metrics
/// </summary>
public class IpBlacklistSummary
{
    public int TotalBlacklisted { get; set; }
    public int AutoBlacklisted { get; set; }
    public int ManuallyBlacklisted { get; set; }
    public int ExpiringIn24Hours { get; set; }
    public long BlockedRequestsLast24Hours { get; set; }
}

/// <summary>
/// Session management summary metrics
/// </summary>
public class SessionManagementSummary
{
    public int ActiveSessions { get; set; }
    public int UniqueActiveUsers { get; set; }
    public int SuspiciousSessions { get; set; }
    public int ConcurrentSessions { get; set; }
    public decimal AverageSessionDuration { get; set; }
}

/// <summary>
/// MFA compliance summary metrics
/// </summary>
public class MfaComplianceSummary
{
    public decimal AdoptionRate { get; set; }
    public decimal ComplianceRate { get; set; }
    public int NonCompliantUsers { get; set; }
    public int RecentEnrollments { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty;
}

/// <summary>
/// Password compliance summary metrics
/// </summary>
public class PasswordComplianceSummary
{
    public decimal ComplianceRate { get; set; }
    public int WeakPasswords { get; set; }
    public int ExpiringSoon { get; set; }
    public int CompromisedPasswords { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty;
}

/// <summary>
/// Anomaly detection summary metrics
/// </summary>
public class AnomalyDetectionSummary
{
    public int TotalAnomaliesLast24Hours { get; set; }
    public int TotalAnomaliesLast7Days { get; set; }
    public int UnresolvedAnomalies { get; set; }
    public int HighRiskAnomalies { get; set; }
    public Dictionary<string, int> AnomalyTypeDistribution { get; set; } = new();
}

/// <summary>
/// Security alerts summary metrics
/// </summary>
public class SecurityAlertsSummary
{
    public int ActiveCriticalAlerts { get; set; }
    public int ActiveHighAlerts { get; set; }
    public int TotalAlertsLast24Hours { get; set; }
    public int TotalAlertsLast7Days { get; set; }
    public int UnacknowledgedAlerts { get; set; }
}

/// <summary>
/// Security activity entry for real-time feed
/// </summary>
public class SecurityActivityEntry
{
    /// <summary>
    /// Activity timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Activity type
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Severity: "Critical", "High", "Medium", "Low"
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Activity description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Affected user/resource
    /// </summary>
    public string? AffectedEntity { get; set; }

    /// <summary>
    /// Source IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Whether the threat was blocked
    /// </summary>
    public bool WasBlocked { get; set; }
}

/// <summary>
/// Tenant security metrics
/// </summary>
public class TenantSecurityMetrics
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
    /// Overall security score (0-100)
    /// </summary>
    public decimal SecurityScore { get; set; }

    /// <summary>
    /// Risk level: "Critical", "High", "Medium", "Low"
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Failed logins (last 24 hours)
    /// </summary>
    public int FailedLogins24h { get; set; }

    /// <summary>
    /// Active security alerts
    /// </summary>
    public int ActiveAlerts { get; set; }

    /// <summary>
    /// MFA adoption rate
    /// </summary>
    public decimal MfaAdoptionRate { get; set; }

    /// <summary>
    /// Password compliance rate
    /// </summary>
    public decimal PasswordComplianceRate { get; set; }

    /// <summary>
    /// Number of users with security issues
    /// </summary>
    public int UsersWithIssues { get; set; }

    /// <summary>
    /// Primary security concern
    /// </summary>
    public string? PrimaryConcern { get; set; }
}

/// <summary>
/// Compliance status across standards
/// </summary>
public class ComplianceStatus
{
    /// <summary>
    /// PCI-DSS compliance percentage
    /// </summary>
    public decimal PciDssCompliance { get; set; }

    /// <summary>
    /// SOC 2 compliance percentage
    /// </summary>
    public decimal Soc2Compliance { get; set; }

    /// <summary>
    /// ISO 27001 compliance percentage
    /// </summary>
    public decimal Iso27001Compliance { get; set; }

    /// <summary>
    /// GDPR compliance percentage
    /// </summary>
    public decimal GdprCompliance { get; set; }

    /// <summary>
    /// NIST 800-53 compliance percentage
    /// </summary>
    public decimal Nist80053Compliance { get; set; }

    /// <summary>
    /// Overall compliance status: "Compliant", "PartiallyCompliant", "NonCompliant"
    /// </summary>
    public string OverallStatus { get; set; } = string.Empty;

    /// <summary>
    /// Non-compliant controls count
    /// </summary>
    public int NonCompliantControls { get; set; }

    /// <summary>
    /// Last compliance audit date
    /// </summary>
    public DateTime? LastAuditDate { get; set; }
}

/// <summary>
/// Security metrics historical data point
/// </summary>
public class SecurityMetricsHistorical
{
    /// <summary>
    /// Date of the metrics
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Overall security score
    /// </summary>
    public decimal SecurityScore { get; set; }

    /// <summary>
    /// Failed login count
    /// </summary>
    public int FailedLogins { get; set; }

    /// <summary>
    /// Brute force attacks
    /// </summary>
    public int BruteForceAttacks { get; set; }

    /// <summary>
    /// Security alerts
    /// </summary>
    public int SecurityAlerts { get; set; }

    /// <summary>
    /// MFA adoption rate
    /// </summary>
    public decimal MfaAdoptionRate { get; set; }
}
