using HRMS.Core.Entities.Master;

namespace HRMS.Core.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Authenticates a super admin user by email and password
    /// Returns (token, refreshToken, expiresAt, adminUser) on success, null on failure
    /// </summary>
    Task<(string Token, string RefreshToken, DateTime ExpiresAt, AdminUser User)?> LoginAsync(string email, string password, string ipAddress);

    /// <summary>
    /// Generates a JWT token for an admin user
    /// </summary>
    string GenerateJwtToken(Guid userId, string email, string userName);

    /// <summary>
    /// Validates a JWT token and returns the user ID
    /// </summary>
    Guid? ValidateToken(string token);

    /// <summary>
    /// Unlocks a locked user account (Admin/SuperAdmin only)
    /// </summary>
    Task<bool> UnlockAccountAsync(Guid userId);

    // ============================================
    // PRODUCTION-GRADE TOKEN REFRESH METHODS
    // ============================================

    /// <summary>
    /// Refreshes an access token using a valid refresh token
    /// Implements token rotation: old refresh token is revoked, new one issued
    /// </summary>
    /// <param name="refreshToken">The refresh token from HttpOnly cookie</param>
    /// <param name="ipAddress">Client IP address for security tracking</param>
    /// <returns>New access token and refresh token</returns>
    Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken, string ipAddress);

    /// <summary>
    /// Revokes a single refresh token
    /// Used for logout on a specific device
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <param name="ipAddress">IP address that requested the revocation</param>
    /// <param name="reason">Optional reason for revocation</param>
    Task RevokeTokenAsync(string refreshToken, string ipAddress, string? reason = null);

    /// <summary>
    /// Revokes all refresh tokens for a user
    /// Used for "logout all devices" functionality
    /// </summary>
    /// <param name="adminUserId">User ID whose tokens should be revoked</param>
    /// <param name="ipAddress">IP address that requested the revocation</param>
    /// <param name="reason">Reason for mass revocation</param>
    Task RevokeAllTokensAsync(Guid adminUserId, string ipAddress, string reason);

    // ============================================
    // MULTI-FACTOR AUTHENTICATION (MFA) METHODS
    // ============================================

    /// <summary>
    /// Initiates MFA setup for a user (first login)
    /// Generates TOTP secret, QR code, and backup codes
    /// </summary>
    /// <param name="adminUserId">User ID to set up MFA for</param>
    /// <param name="email">User email for QR code generation</param>
    /// <returns>TOTP secret, QR code (base64), and backup codes</returns>
    Task<(string Secret, string QrCodeBase64, List<string> BackupCodes)?> SetupMfaAsync(Guid adminUserId, string email);

    /// <summary>
    /// Completes MFA setup after user scans QR code and verifies TOTP
    /// Saves encrypted secret and hashed backup codes to database
    /// </summary>
    /// <param name="adminUserId">User ID completing setup</param>
    /// <param name="totpCode">6-digit TOTP code from Google Authenticator</param>
    /// <param name="secret">TOTP secret being verified</param>
    /// <param name="backupCodes">Backup codes to hash and store</param>
    /// <returns>True if TOTP verification successful and MFA enabled</returns>
    Task<bool> CompleteMfaSetupAsync(Guid adminUserId, string totpCode, string secret, List<string> backupCodes);

    /// <summary>
    /// Validates TOTP code for login
    /// </summary>
    /// <param name="adminUserId">User ID attempting login</param>
    /// <param name="totpCode">6-digit TOTP code from Google Authenticator</param>
    /// <returns>True if code is valid</returns>
    Task<bool> ValidateMfaAsync(Guid adminUserId, string totpCode);

    /// <summary>
    /// Validates backup code for login (phone lost scenario)
    /// Revokes the backup code after successful validation
    /// </summary>
    /// <param name="adminUserId">User ID attempting login</param>
    /// <param name="backupCode">Backup code entered by user</param>
    /// <returns>True if code is valid and not previously used</returns>
    Task<bool> ValidateBackupCodeAsync(Guid adminUserId, string backupCode);

    /// <summary>
    /// Gets count of remaining backup codes for a user
    /// </summary>
    /// <param name="adminUserId">User ID to check</param>
    /// <returns>Number of unused backup codes remaining</returns>
    Task<int> GetRemainingBackupCodesAsync(Guid adminUserId);

    /// <summary>
    /// Gets an admin user by ID for authentication purposes
    /// </summary>
    /// <param name="adminUserId">Admin user ID</param>
    /// <returns>AdminUser entity or null if not found</returns>
    Task<AdminUser?> GetAdminUserAsync(Guid adminUserId);

    /// <summary>
    /// Generates JWT and refresh tokens for an authenticated user
    /// </summary>
    /// <param name="admin">Admin user to generate tokens for</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <returns>Access token, refresh token, and expiration date</returns>
    (string Token, string RefreshToken, DateTime ExpiresAt) GenerateTokens(AdminUser admin, string ipAddress);
}
