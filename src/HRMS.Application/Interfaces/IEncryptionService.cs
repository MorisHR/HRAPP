namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data at rest
/// Implements AES-256-GCM for Fortune 500-grade security
///
/// SECURITY REQUIREMENTS:
/// - AES-256-GCM encryption for confidentiality and integrity
/// - Unique IV (Initialization Vector) per encryption operation
/// - HMAC-based authentication tags to prevent tampering
/// - Secure key management via Google Secret Manager
/// - Key rotation support
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext data using AES-256-GCM
    /// </summary>
    /// <param name="plaintext">The data to encrypt (nullable)</param>
    /// <returns>Base64-encoded encrypted data with IV and authentication tag, or null if input is null/empty</returns>
    /// <remarks>
    /// Output format: [IV (12 bytes)][Ciphertext (variable)][Tag (16 bytes)]
    /// All encoded as Base64 for safe storage in text columns
    /// </remarks>
    string? Encrypt(string? plaintext);

    /// <summary>
    /// Decrypts data encrypted with AES-256-GCM
    /// </summary>
    /// <param name="ciphertext">Base64-encoded encrypted data, or null</param>
    /// <returns>Original plaintext data, or null if input is null/empty</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if data has been tampered with or key is incorrect
    /// </exception>
    string? Decrypt(string? ciphertext);

    /// <summary>
    /// Determines if encryption is enabled (based on key availability)
    /// </summary>
    /// <returns>True if encryption is active, false if running in passthrough mode</returns>
    bool IsEncryptionEnabled();

    /// <summary>
    /// Gets the current key version being used for encryption
    /// Used for key rotation tracking
    /// </summary>
    /// <returns>Key version identifier (e.g., "v1", "v2")</returns>
    string GetKeyVersion();
}
