using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 500: Brute Force Attack Statistics
/// Real-time monitoring of credential stuffing and brute force attacks
/// PATTERN: Cloudflare Bot Management, AWS WAF, Akamai Kona
/// COMPLIANCE: PCI-DSS 6.5.10, OWASP Top 10 A07:2021
/// </summary>
public class BruteForceStatisticsDto
{
    /// <summary>
    /// Total brute force attacks detected in period
    /// </summary>
    public int TotalAttacksDetected { get; set; }

    /// <summary>
    /// Number of attacks currently in progress
    /// </summary>
    public int ActiveAttacks { get; set; }

    /// <summary>
    /// Number of attacks successfully blocked
    /// </summary>
    public int AttacksBlocked { get; set; }

    /// <summary>
    /// Block success rate (percentage)
    /// </summary>
    public decimal BlockSuccessRate { get; set; }

    /// <summary>
    /// Number of IPs currently auto-blacklisted
    /// </summary>
    public int BlacklistedIpsCount { get; set; }

    /// <summary>
    /// Number of accounts currently locked due to brute force
    /// </summary>
    public int LockedAccountsCount { get; set; }

    /// <summary>
    /// Average attack duration (minutes)
    /// </summary>
    public decimal AverageAttackDuration { get; set; }

    /// <summary>
    /// Peak attack rate (attempts per minute)
    /// </summary>
    public int PeakAttackRate { get; set; }

    /// <summary>
    /// Current attack rate (attempts per minute)
    /// </summary>
    public int CurrentAttackRate { get; set; }

    /// <summary>
    /// Trend percentage compared to previous period
    /// </summary>
    public decimal TrendPercentage { get; set; }

    /// <summary>
    /// Active attacks details
    /// </summary>
    public List<ActiveAttackDto> ActiveAttacksList { get; set; } = new();

    /// <summary>
    /// Recently blocked IPs (last 24 hours)
    /// </summary>
    public List<BlockedIpDto> RecentlyBlockedIps { get; set; } = new();

    /// <summary>
    /// Attack pattern distribution
    /// </summary>
    public Dictionary<string, int> AttackPatterns { get; set; } = new();

    /// <summary>
    /// Hourly attack distribution (0-23)
    /// </summary>
    public Dictionary<int, int> HourlyDistribution { get; set; } = new();

    /// <summary>
    /// Top 10 targeted endpoints
    /// </summary>
    public List<TargetedEndpoint> TopTargetedEndpoints { get; set; } = new();
}

/// <summary>
/// Active brute force attack
/// </summary>
public class ActiveAttackDto
{
    /// <summary>
    /// Attack ID
    /// </summary>
    public string AttackId { get; set; } = string.Empty;

    /// <summary>
    /// Source IP address
    /// </summary>
    public string SourceIp { get; set; } = string.Empty;

    /// <summary>
    /// Targeted user/account
    /// </summary>
    public string? TargetedUser { get; set; }

    /// <summary>
    /// Tenant subdomain (if applicable)
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Attack start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Number of attempts so far
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Current attack rate (attempts per minute)
    /// </summary>
    public int AttackRate { get; set; }

    /// <summary>
    /// Attack pattern: "distributed", "single_ip", "credential_stuffing"
    /// </summary>
    public string AttackPattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether the attack is currently blocked
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }
}

/// <summary>
/// Blocked IP details
/// </summary>
public class BlockedIpDto
{
    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Block reason
    /// </summary>
    public string BlockReason { get; set; } = string.Empty;

    /// <summary>
    /// When the IP was blocked
    /// </summary>
    public DateTime BlockedAt { get; set; }

    /// <summary>
    /// Block expiration time (if temporary)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Total violation count that triggered the block
    /// </summary>
    public int ViolationCount { get; set; }

    /// <summary>
    /// Whether this is a permanent block
    /// </summary>
    public bool IsPermanent { get; set; }

    /// <summary>
    /// Geographic location (country code)
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Number of users this IP attempted to access
    /// </summary>
    public int TargetedUserCount { get; set; }
}

/// <summary>
/// Targeted endpoint statistics
/// </summary>
public class TargetedEndpoint
{
    /// <summary>
    /// Endpoint path
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Number of attacks targeting this endpoint
    /// </summary>
    public int AttackCount { get; set; }

    /// <summary>
    /// Percentage of total attacks
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Average attack severity
    /// </summary>
    public string AverageSeverity { get; set; } = string.Empty;
}
