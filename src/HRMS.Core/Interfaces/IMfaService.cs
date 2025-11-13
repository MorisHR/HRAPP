namespace HRMS.Core.Interfaces;

/// <summary>
/// Multi-Factor Authentication (MFA) service interface
/// Handles TOTP (Time-based One-Time Password) operations using Google Authenticator
/// </summary>
public interface IMfaService
{
    /// <summary>
    /// Generates a new TOTP secret for a user
    /// </summary>
    /// <returns>Base32-encoded secret (16 characters)</returns>
    string GenerateTotpSecret();

    /// <summary>
    /// Generates a QR code image for Google Authenticator setup
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="secret">TOTP secret (Base32)</param>
    /// <param name="issuer">App name (default: "MorisHR")</param>
    /// <returns>Base64-encoded PNG image</returns>
    string GenerateQrCode(string email, string secret, string issuer = "MorisHR");

    /// <summary>
    /// Validates a TOTP code against a secret
    /// Uses 90-second window (±30 seconds tolerance for clock drift)
    /// </summary>
    /// <param name="secret">TOTP secret (Base32)</param>
    /// <param name="totpCode">6-digit code from Google Authenticator</param>
    /// <returns>True if code is valid</returns>
    bool ValidateTotpCode(string secret, string totpCode);

    /// <summary>
    /// Generates backup codes for account recovery
    /// </summary>
    /// <param name="count">Number of codes to generate (default: 10)</param>
    /// <returns>List of unhashed backup codes (user must save these)</returns>
    List<string> GenerateBackupCodes(int count = 10);

    /// <summary>
    /// Hashes a backup code for storage
    /// </summary>
    /// <param name="code">Plain-text backup code</param>
    /// <returns>SHA256 hash of the code</returns>
    string HashBackupCode(string code);

    /// <summary>
    /// Validates a backup code against stored hashes
    /// </summary>
    /// <param name="code">Plain-text backup code entered by user</param>
    /// <param name="hashedCodes">JSON array of hashed codes from database</param>
    /// <returns>True if code matches one of the hashed codes</returns>
    bool ValidateBackupCode(string code, string hashedCodes);

    /// <summary>
    /// Revokes a used backup code from the list
    /// </summary>
    /// <param name="code">Plain-text backup code that was used</param>
    /// <param name="hashedCodes">JSON array of hashed codes</param>
    /// <returns>Updated JSON array with the used code removed</returns>
    string RevokeBackupCode(string code, string hashedCodes);

    /// <summary>
    /// Admin override to disable MFA for a user
    /// </summary>
    /// <param name="adminUserId">ID of admin performing the action</param>
    /// <param name="targetUserId">ID of user to disable MFA for</param>
    /// <param name="reason">Reason for disabling MFA</param>
    /// <returns>Success status and message</returns>
    Task<(bool Success, string Message)> AdminDisableMfaAsync(Guid adminUserId, Guid targetUserId, string reason);
}
