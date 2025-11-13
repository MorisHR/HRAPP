using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRMS.Core.Interfaces;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using HRMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Production-grade MFA service using TOTP (Google Authenticator compatible)
/// Implements time-based one-time passwords with backup code support
/// </summary>
public class MfaService : IMfaService
{
    private readonly ILogger<MfaService> _logger;
    private readonly MasterDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;

    public MfaService(
        ILogger<MfaService> logger,
        MasterDbContext context,
        IAuditLogService auditLogService,
        IEmailService emailService)
    {
        _logger = logger;
        _context = context;
        _auditLogService = auditLogService;
        _emailService = emailService;
    }

    /// <summary>
    /// Generates a cryptographically secure TOTP secret
    /// </summary>
    public string GenerateTotpSecret()
    {
        // Generate 20 bytes (160 bits) of random data for Base32 encoding
        var key = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(key);

        _logger.LogDebug("Generated new TOTP secret (length: {Length})", secret.Length);
        return secret;
    }

    /// <summary>
    /// Generates QR code for Google Authenticator
    /// Format: otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}
    /// </summary>
    public string GenerateQrCode(string email, string secret, string issuer = "MorisHR")
    {
        // OTPAuth URL format for Google Authenticator
        var otpAuthUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

        _logger.LogDebug("Generating QR code for {Email}", email);

        // Generate QR code using QRCoder
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        var qrCodeBytes = qrCode.GetGraphic(20); // 20 pixels per module
        var base64QrCode = Convert.ToBase64String(qrCodeBytes);

        return base64QrCode;
    }

    /// <summary>
    /// Validates TOTP code with 90-second window (±30 seconds clock drift tolerance)
    /// Uses UTC to work across all timezones
    /// </summary>
    public bool ValidateTotpCode(string secret, string totpCode)
    {
        try
        {
            _logger.LogInformation("=== TOTP VALIDATION DEBUG ===");
            _logger.LogInformation("Secret received: {Secret}", secret);
            _logger.LogInformation("TOTP Code received: {TotpCode}", totpCode);
            _logger.LogInformation("Server UTC time: {UtcNow}", DateTime.UtcNow);

            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(totpCode))
            {
                _logger.LogWarning("TOTP validation failed: Empty secret or code");
                return false;
            }

            // Remove any spaces or dashes from user input
            totpCode = totpCode.Replace(" ", "").Replace("-", "");

            if (totpCode.Length != 6 || !totpCode.All(char.IsDigit))
            {
                _logger.LogWarning("TOTP validation failed: Invalid code format (expected 6 digits)");
                return false;
            }

            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);

            // Log what the current expected code is
            var currentCode = totp.ComputeTotp(DateTime.UtcNow);
            _logger.LogInformation("Expected TOTP code at current time: {CurrentCode}", currentCode);

            // Also log codes for ±1 step
            var previousCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(-30));
            var futureCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(30));
            _logger.LogInformation("Previous step code (-30s): {PreviousCode}", previousCode);
            _logger.LogInformation("Future step code (+30s): {FutureCode}", futureCode);

            // PRODUCTION SECURITY: Verify with window of ±1 step (90 seconds total)
            // This provides 30 seconds before and after the current time window for clock drift tolerance
            var verificationWindow = new VerificationWindow(
                previous: 1,  // Allow codes from 30 seconds ago
                future: 1     // Allow codes from 30 seconds in the future
            );

            var isValid = totp.VerifyTotp(
                totpCode,
                out long timeStepMatched,
                verificationWindow
            );

            if (isValid)
            {
                _logger.LogInformation("TOTP validation successful (timeStep: {TimeStep})", timeStepMatched);
            }
            else
            {
                _logger.LogWarning("TOTP validation failed: Code {Code} does not match any of [previous: {Previous}, current: {Current}, future: {Future}]",
                    totpCode, previousCode, currentCode, futureCode);
            }

            _logger.LogInformation("=== END TOTP VALIDATION DEBUG ===");
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TOTP code");
            return false;
        }
    }

    /// <summary>
    /// Generates backup codes (8 characters, alphanumeric, uppercase)
    /// Format: ABCD-1234 (4 letters + 4 numbers with dash for readability)
    /// </summary>
    public List<string> GenerateBackupCodes(int count = 10)
    {
        var codes = new List<string>();
        const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Removed I, O to avoid confusion with 1, 0
        const string numbers = "0123456789";

        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < count; i++)
        {
            var letterPart = GenerateRandomString(rng, letters, 4);
            var numberPart = GenerateRandomString(rng, numbers, 4);
            var code = $"{letterPart}{numberPart}"; // Store without dash, add dash for display only
            codes.Add(code);
        }

        _logger.LogInformation("Generated {Count} backup codes", count);
        return codes;
    }

    /// <summary>
    /// Hashes backup code with SHA256
    /// </summary>
    public string HashBackupCode(string code)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Validates backup code against JSON array of hashes
    /// </summary>
    public bool ValidateBackupCode(string code, string hashedCodes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(hashedCodes))
            {
                return false;
            }

            // Remove any spaces or dashes from user input
            code = code.Replace(" ", "").Replace("-", "").ToUpperInvariant();

            var hashes = JsonSerializer.Deserialize<List<string>>(hashedCodes);
            if (hashes == null || !hashes.Any())
            {
                return false;
            }

            var codeHash = HashBackupCode(code);
            var isValid = hashes.Contains(codeHash);

            if (isValid)
            {
                _logger.LogInformation("Backup code validated successfully");
            }
            else
            {
                _logger.LogWarning("Backup code validation failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup code");
            return false;
        }
    }

    /// <summary>
    /// Revokes (removes) a used backup code from the list
    /// </summary>
    public string RevokeBackupCode(string code, string hashedCodes)
    {
        try
        {
            // Remove any spaces or dashes
            code = code.Replace(" ", "").Replace("-", "").ToUpperInvariant();

            var hashes = JsonSerializer.Deserialize<List<string>>(hashedCodes);
            if (hashes == null)
            {
                return hashedCodes;
            }

            var codeHash = HashBackupCode(code);
            hashes.Remove(codeHash);

            _logger.LogInformation("Backup code revoked. Remaining codes: {Count}", hashes.Count);

            return JsonSerializer.Serialize(hashes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking backup code");
            return hashedCodes;
        }
    }

    /// <summary>
    /// Helper method to generate random string from character set
    /// </summary>
    private static string GenerateRandomString(RandomNumberGenerator rng, string characterSet, int length)
    {
        var result = new char[length];
        var randomBytes = new byte[length];
        rng.GetBytes(randomBytes);

        for (int i = 0; i < length; i++)
        {
            result[i] = characterSet[randomBytes[i] % characterSet.Length];
        }

        return new string(result);
    }

    /// <summary>
    /// Admin override to disable MFA for a user (recovery/support scenario)
    /// </summary>
    public async Task<(bool Success, string Message)> AdminDisableMfaAsync(
        Guid adminUserId,
        Guid targetUserId,
        string reason)
    {
        try
        {
            // Verify admin has permission
            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.Id == adminUserId && u.IsActive);

            if (admin == null)
            {
                _logger.LogWarning("MFA disable attempted by non-existent or inactive admin user: {AdminUserId}", adminUserId);
                return (false, "Only active administrators can disable MFA for users");
            }

            // Find target user
            var targetUser = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (targetUser == null)
            {
                _logger.LogWarning("MFA disable attempted for non-existent user: {TargetUserId}", targetUserId);
                return (false, "User not found");
            }

            if (!targetUser.IsTwoFactorEnabled)
            {
                _logger.LogInformation("MFA disable attempted but MFA already disabled for user: {TargetUserId}", targetUserId);
                return (true, "MFA is already disabled for this user");
            }

            // Disable MFA
            targetUser.IsTwoFactorEnabled = false;
            targetUser.TwoFactorSecret = null;

            // Note: Revoke backup codes if your system has a separate MfaBackupCodes table
            // For now, we'll clear the BackupCodes JSON field if it exists
            if (!string.IsNullOrWhiteSpace(targetUser.BackupCodes))
            {
                targetUser.BackupCodes = null;
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogSecurityEventAsync(
                AuditActionType.MFA_DISABLED,
                AuditSeverity.CRITICAL,
                userId: targetUserId,
                description: $"MFA disabled by admin {adminUserId}. Reason: {reason}");

            // Send email notification to user
            await _emailService.SendEmailAsync(
                targetUser.Email,
                "Multi-Factor Authentication Disabled",
                $"Your multi-factor authentication has been disabled by an administrator.\n\n" +
                $"Reason: {reason}\n\n" +
                $"If you did not request this change, please contact your administrator immediately.\n\n" +
                $"This is a security notification from MorisHR.");

            _logger.LogInformation("MFA disabled for user {TargetUserId} by admin {AdminUserId}. Reason: {Reason}",
                targetUserId, adminUserId, reason);

            return (true, "MFA disabled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling MFA for user {TargetUserId}", targetUserId);
            return (false, "An error occurred");
        }
    }
}
