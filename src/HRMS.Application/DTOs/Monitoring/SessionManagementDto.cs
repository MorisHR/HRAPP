using System;
using System.Collections.Generic;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 500: Session Management and Tracking
/// Real-time monitoring of active user sessions across all tenants
/// PATTERN: Okta Session Management, Auth0 Sessions, AWS Cognito
/// COMPLIANCE: PCI-DSS 8.1.8, NIST 800-53 AC-12, ISO 27001 A.9.1.2
/// </summary>
public class SessionManagementDto
{
    /// <summary>
    /// Total active sessions across all tenants
    /// </summary>
    public int TotalActiveSessions { get; set; }

    /// <summary>
    /// Unique active users
    /// </summary>
    public int UniqueActiveUsers { get; set; }

    /// <summary>
    /// Sessions created in last hour
    /// </summary>
    public int SessionsLastHour { get; set; }

    /// <summary>
    /// Sessions created today
    /// </summary>
    public int SessionsToday { get; set; }

    /// <summary>
    /// Average session duration (minutes)
    /// </summary>
    public decimal AverageSessionDuration { get; set; }

    /// <summary>
    /// Concurrent sessions detected (same user, multiple sessions)
    /// </summary>
    public int ConcurrentSessionsCount { get; set; }

    /// <summary>
    /// Suspicious sessions (flagged by anomaly detection)
    /// </summary>
    public int SuspiciousSessionsCount { get; set; }

    /// <summary>
    /// Sessions expiring in next hour
    /// </summary>
    public int ExpiringSessionsCount { get; set; }

    /// <summary>
    /// Active sessions by tenant
    /// </summary>
    public List<TenantSessionCount> SessionsByTenant { get; set; } = new();

    /// <summary>
    /// Top 10 users by session count
    /// </summary>
    public List<UserSessionCount> TopUsersBySessions { get; set; } = new();

    /// <summary>
    /// Recent session activity
    /// </summary>
    public List<SessionActivityEntry> RecentActivity { get; set; } = new();

    /// <summary>
    /// Sessions by device type
    /// </summary>
    public Dictionary<string, int> SessionsByDeviceType { get; set; } = new();

    /// <summary>
    /// Sessions by geographic location
    /// </summary>
    public Dictionary<string, int> SessionsByLocation { get; set; } = new();

    /// <summary>
    /// Hourly session creation pattern (0-23)
    /// </summary>
    public Dictionary<int, int> HourlySessionPattern { get; set; } = new();
}

/// <summary>
/// Tenant session count
/// </summary>
public class TenantSessionCount
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
    /// Active sessions count
    /// </summary>
    public int ActiveSessions { get; set; }

    /// <summary>
    /// Unique active users
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Concurrent sessions (same user, multiple devices)
    /// </summary>
    public int ConcurrentSessions { get; set; }

    /// <summary>
    /// Average session duration (minutes)
    /// </summary>
    public decimal AverageSessionDuration { get; set; }

    /// <summary>
    /// Peak concurrent sessions today
    /// </summary>
    public int PeakConcurrentSessions { get; set; }

    /// <summary>
    /// Tenant status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// User session count
/// </summary>
public class UserSessionCount
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
    /// Active sessions count
    /// </summary>
    public int SessionCount { get; set; }

    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string TenantSubdomain { get; set; } = string.Empty;

    /// <summary>
    /// Unique IP addresses
    /// </summary>
    public int UniqueIps { get; set; }

    /// <summary>
    /// Unique device types
    /// </summary>
    public int UniqueDevices { get; set; }

    /// <summary>
    /// Whether sessions are flagged as suspicious
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// Session activity entry
/// </summary>
public class SessionActivityEntry
{
    /// <summary>
    /// Activity timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Activity type: "Login", "Logout", "SessionExpired", "ForceLogout"
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
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Device type
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// Session duration (for logout/expiry)
    /// </summary>
    public int? SessionDurationMinutes { get; set; }
}

/// <summary>
/// Detailed active session information
/// </summary>
public class ActiveSessionDto
{
    /// <summary>
    /// Session ID (JWT token ID)
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User email
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device type
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// Session started at
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last activity
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Session expires at
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Session duration so far (minutes)
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Whether this is flagged as suspicious
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    /// Geographic location
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Whether MFA was used
    /// </summary>
    public bool MfaUsed { get; set; }
}
