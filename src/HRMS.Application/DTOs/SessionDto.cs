namespace HRMS.Application.DTOs;

/// <summary>
/// DTO for active user session information
/// FORTUNE 500 FEATURE: Session management for security monitoring
/// </summary>
public class SessionDto
{
    /// <summary>
    /// Unique session identifier (refresh token ID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Device information (e.g., "Chrome 120 on Windows 10")
    /// </summary>
    public string DeviceInfo { get; set; } = string.Empty;

    /// <summary>
    /// IP address where session was created
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// When this session was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// When this session will expire
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Is this the current session (the one making the request)
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Geographic location (if available from IP)
    /// </summary>
    public string? Location { get; set; }
}

/// <summary>
/// Response for getting all active sessions
/// </summary>
public class ActiveSessionsResponse
{
    /// <summary>
    /// List of active sessions
    /// </summary>
    public List<SessionDto> Sessions { get; set; } = new();

    /// <summary>
    /// Total number of active sessions
    /// </summary>
    public int TotalActiveSessions { get; set; }

    /// <summary>
    /// Maximum allowed concurrent sessions
    /// </summary>
    public int MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Number of sessions that can still be created before limit
    /// </summary>
    public int RemainingSlots { get; set; }
}
