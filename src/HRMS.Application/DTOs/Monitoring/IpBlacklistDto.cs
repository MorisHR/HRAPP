using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 500: IP Blacklist Management
/// Centralized IP whitelist/blacklist management with auto-blocking
/// PATTERN: Cloudflare WAF, AWS Shield, Fail2Ban
/// COMPLIANCE: PCI-DSS 1.3, NIST 800-53 SC-7
/// </summary>
public class IpBlacklistDto
{
    /// <summary>
    /// Total blacklisted IPs
    /// </summary>
    public int TotalBlacklisted { get; set; }

    /// <summary>
    /// Auto-blacklisted IPs (by system)
    /// </summary>
    public int AutoBlacklisted { get; set; }

    /// <summary>
    /// Manually blacklisted IPs (by admin)
    /// </summary>
    public int ManuallyBlacklisted { get; set; }

    /// <summary>
    /// Whitelisted IPs
    /// </summary>
    public int TotalWhitelisted { get; set; }

    /// <summary>
    /// Temporary blocks (will expire)
    /// </summary>
    public int TemporaryBlocks { get; set; }

    /// <summary>
    /// Permanent blocks
    /// </summary>
    public int PermanentBlocks { get; set; }

    /// <summary>
    /// Blocks expiring in next 24 hours
    /// </summary>
    public int ExpiringBlocks { get; set; }

    /// <summary>
    /// Total blocked requests in period
    /// </summary>
    public long TotalBlockedRequests { get; set; }

    /// <summary>
    /// Blacklisted IPs list
    /// </summary>
    public List<BlacklistedIpEntry> BlacklistedIps { get; set; } = new();

    /// <summary>
    /// Whitelisted IPs list
    /// </summary>
    public List<WhitelistedIpEntry> WhitelistedIps { get; set; } = new();

    /// <summary>
    /// Recent block activity (last 24 hours)
    /// </summary>
    public List<BlockActivityEntry> RecentActivity { get; set; } = new();

    /// <summary>
    /// Geographic distribution of blocked IPs
    /// </summary>
    public Dictionary<string, int> BlockedByCountry { get; set; } = new();
}

/// <summary>
/// Blacklisted IP entry
/// </summary>
public class BlacklistedIpEntry
{
    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Block reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Block type: "Auto", "Manual", "System"
    /// </summary>
    public string BlockType { get; set; } = string.Empty;

    /// <summary>
    /// When blocked
    /// </summary>
    public DateTime BlockedAt { get; set; }

    /// <summary>
    /// Blocked by (username or "System")
    /// </summary>
    public string BlockedBy { get; set; } = string.Empty;

    /// <summary>
    /// Expiration time (null for permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Total violation count
    /// </summary>
    public int ViolationCount { get; set; }

    /// <summary>
    /// Number of blocked requests
    /// </summary>
    public long BlockedRequestCount { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime? LastActivity { get; set; }

    /// <summary>
    /// Geographic location
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Country name
    /// </summary>
    public string? CountryName { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Whitelisted IP entry
/// </summary>
public class WhitelistedIpEntry
{
    /// <summary>
    /// IP address or CIDR block
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Whitelist reason/description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Added by (username)
    /// </summary>
    public string AddedBy { get; set; } = string.Empty;

    /// <summary>
    /// When added
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Expiration time (null for permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this is a corporate network
    /// </summary>
    public bool IsCorporateNetwork { get; set; }

    /// <summary>
    /// Associated tenant (if applicable)
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Last verified/reviewed
    /// </summary>
    public DateTime? LastVerified { get; set; }
}

/// <summary>
/// Block activity entry
/// </summary>
public class BlockActivityEntry
{
    /// <summary>
    /// Activity timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Activity type: "Blocked", "Unblocked", "Whitelisted", "RequestBlocked"
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Activity description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User who performed the action (or "System")
    /// </summary>
    public string PerformedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request model for adding IP to blacklist
/// </summary>
public class AddIpToBlacklistRequest
{
    /// <summary>
    /// IP address to blacklist
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Reason for blacklisting
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a permanent block
    /// </summary>
    public bool IsPermanent { get; set; } = true;

    /// <summary>
    /// Duration in hours (if not permanent)
    /// </summary>
    public int? DurationHours { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for adding IP to whitelist
/// </summary>
public class AddIpToWhitelistRequest
{
    /// <summary>
    /// IP address or CIDR block to whitelist
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Description/reason for whitelisting
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a corporate network
    /// </summary>
    public bool IsCorporateNetwork { get; set; }

    /// <summary>
    /// Associated tenant subdomain (if applicable)
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Expiration time (null for permanent)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
