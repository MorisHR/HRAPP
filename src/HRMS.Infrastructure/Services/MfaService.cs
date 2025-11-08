using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRMS.Core.Interfaces;
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

    public MfaService(ILogger<MfaService> logger)
    {
        _logger = logger;
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

            // Verify with window of ±1 step (30 seconds each = 90 second total window)
            // This handles clock drift between server and client
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
                _logger.LogWarning("TOTP validation failed: Code does not match");
            }

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
}
