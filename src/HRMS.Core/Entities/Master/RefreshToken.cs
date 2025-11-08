using System;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Refresh token entity for secure token rotation
/// Follows OWASP best practices for JWT refresh tokens
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the admin user who owns this token
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// The actual refresh token string (cryptographically secure random)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When this refresh token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When this refresh token was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// IP address that created this token (for security tracking)
    /// </summary>
    public string CreatedByIp { get; set; } = string.Empty;

    /// <summary>
    /// When this token was revoked (null if still active)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// IP address that revoked this token
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// The token that replaced this one (for token rotation)
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Reason why this token was revoked (logout, security, etc.)
    /// </summary>
    public string? ReasonRevoked { get; set; }

    // Navigation properties
    public virtual AdminUser AdminUser { get; set; } = null!;

    // Computed properties for convenience
    /// <summary>
    /// Check if token has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Check if token has been revoked
    /// </summary>
    public bool IsRevoked => RevokedAt != null;

    /// <summary>
    /// Check if token is currently active (not expired and not revoked)
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}
