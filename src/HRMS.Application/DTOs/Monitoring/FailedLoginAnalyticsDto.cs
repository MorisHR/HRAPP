using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 500: Failed Login Analytics
/// Provides comprehensive analysis of authentication failures across all tenants
/// PATTERN: AWS GuardDuty, Azure Sentinel, Splunk ES
/// COMPLIANCE: PCI-DSS 8.1.6, NIST 800-53 AC-7, ISO 27001 A.9.4.3
/// </summary>
public class FailedLoginAnalyticsDto
{
    /// <summary>
    /// Total failed login attempts in the period
    /// </summary>
    public int TotalFailedLogins { get; set; }

    /// <summary>
    /// Number of unique users with failed logins
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Number of unique source IPs
    /// </summary>
    public int UniqueIpAddresses { get; set; }

    /// <summary>
    /// Number of IPs that have been auto-blacklisted
    /// </summary>
    public int BlacklistedIps { get; set; }

    /// <summary>
    /// Failed logins in the last 24 hours
    /// </summary>
    public int Last24Hours { get; set; }

    /// <summary>
    /// Failed logins in the last 7 days
    /// </summary>
    public int Last7Days { get; set; }

    /// <summary>
    /// Failed logins in the last 30 days
    /// </summary>
    public int Last30Days { get; set; }

    /// <summary>
    /// Percentage change from previous period
    /// </summary>
    public decimal TrendPercentage { get; set; }

    /// <summary>
    /// Trend direction: "up", "down", "stable"
    /// </summary>
    public string TrendDirection { get; set; } = "stable";

    /// <summary>
    /// Time series data for charts (hourly/daily aggregates)
    /// </summary>
    public List<TimeSeriesDataPoint> TimeSeriesData { get; set; } = new();

    /// <summary>
    /// Top 10 IPs by failed login attempts
    /// </summary>
    public List<IpFailureCount> TopFailureIps { get; set; } = new();

    /// <summary>
    /// Top 10 targeted users
    /// </summary>
    public List<UserFailureCount> TopTargetedUsers { get; set; } = new();

    /// <summary>
    /// Failures by tenant
    /// </summary>
    public List<TenantFailureCount> FailuresByTenant { get; set; } = new();

    /// <summary>
    /// Peak failure hour (0-23)
    /// </summary>
    public int PeakHour { get; set; }

    /// <summary>
    /// Failures during peak hour
    /// </summary>
    public int PeakHourCount { get; set; }

    /// <summary>
    /// Geographic distribution (country codes)
    /// </summary>
    public Dictionary<string, int> GeographicDistribution { get; set; } = new();
}

/// <summary>
/// Time series data point for charts
/// </summary>
public class TimeSeriesDataPoint
{
    /// <summary>
    /// Timestamp of the data point
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Count of events at this timestamp
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Label for display (e.g., "Jan 15", "14:00")
    /// </summary>
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// IP address failure count
/// </summary>
public class IpFailureCount
{
    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Whether this IP is currently blacklisted
    /// </summary>
    public bool IsBlacklisted { get; set; }

    /// <summary>
    /// First seen timestamp
    /// </summary>
    public DateTime FirstSeen { get; set; }

    /// <summary>
    /// Last seen timestamp
    /// </summary>
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// Number of unique users targeted
    /// </summary>
    public int UniqueUsersTargeted { get; set; }

    /// <summary>
    /// Geographic location (country code)
    /// </summary>
    public string? CountryCode { get; set; }
}

/// <summary>
/// User failure count
/// </summary>
public class UserFailureCount
{
    /// <summary>
    /// User email or username
    /// </summary>
    public string UserIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of unique source IPs
    /// </summary>
    public int UniqueIps { get; set; }

    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Whether the account is currently locked
    /// </summary>
    public bool IsAccountLocked { get; set; }

    /// <summary>
    /// First failure timestamp
    /// </summary>
    public DateTime FirstFailure { get; set; }

    /// <summary>
    /// Last failure timestamp
    /// </summary>
    public DateTime LastFailure { get; set; }
}

/// <summary>
/// Tenant failure count
/// </summary>
public class TenantFailureCount
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
    /// Number of failed login attempts
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of unique users with failures
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Number of unique source IPs
    /// </summary>
    public int UniqueIps { get; set; }
}
