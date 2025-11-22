namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for managing blacklisted JWT tokens (immediate revocation capability)
/// FORTUNE 500 SECURITY: Enables instant token revocation for terminated employees, security incidents, or password resets
/// Uses Redis for distributed caching across multiple application instances
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Blacklist a specific access token by its JTI (JWT ID) claim
    /// Token will be invalid until its natural expiry time
    /// </summary>
    /// <param name="jti">JWT ID from the 'jti' claim</param>
    /// <param name="expiresAt">When the token naturally expires (for automatic cleanup)</param>
    /// <param name="reason">Reason for blacklisting (for audit purposes)</param>
    Task BlacklistTokenAsync(string jti, DateTime expiresAt, string reason);

    /// <summary>
    /// Check if a token is blacklisted
    /// Called on every authenticated request for real-time validation
    /// </summary>
    /// <param name="jti">JWT ID to check</param>
    /// <returns>True if token is blacklisted (should be rejected)</returns>
    Task<bool> IsTokenBlacklistedAsync(string jti);

    /// <summary>
    /// Blacklist all active tokens for a specific user
    /// Use case: Employee termination, password reset, security incident
    /// </summary>
    /// <param name="userId">User ID whose tokens should be blacklisted</param>
    /// <param name="reason">Reason for mass blacklisting</param>
    Task BlacklistUserTokensAsync(Guid userId, string reason);

    /// <summary>
    /// Remove a token from the blacklist (rare - used for testing or false positives)
    /// </summary>
    /// <param name="jti">JWT ID to un-blacklist</param>
    Task RemoveFromBlacklistAsync(string jti);

    /// <summary>
    /// Get statistics about blacklisted tokens
    /// </summary>
    /// <returns>Count of currently blacklisted tokens</returns>
    Task<int> GetBlacklistCountAsync();

    /// <summary>
    /// Track a user's active token for future mass revocation
    /// Called when a new token is generated
    /// </summary>
    /// <param name="userId">User ID who owns the token</param>
    /// <param name="jti">JWT ID from the token</param>
    /// <param name="expiresAt">When the token expires</param>
    Task TrackUserTokenAsync(Guid userId, string jti, DateTime expiresAt);
}
