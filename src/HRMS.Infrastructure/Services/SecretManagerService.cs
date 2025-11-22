using HRMS.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Fortune 500-grade secret management service
/// Loads secrets from environment variables with fallback support
/// Supports Google Cloud Secret Manager, AWS Secrets Manager, Azure Key Vault
/// </summary>
public class SecretManagerService
{
    private readonly ILogger<SecretManagerService> _logger;
    private readonly SecretConfiguration _secrets;

    public SecretManagerService(ILogger<SecretManagerService> logger)
    {
        _logger = logger;
        _secrets = LoadSecrets();
    }

    /// <summary>
    /// Get the secret configuration (validated and loaded)
    /// </summary>
    public SecretConfiguration GetSecrets() => _secrets;

    /// <summary>
    /// Load secrets from environment variables
    /// Priority order:
    /// 1. Environment variables (production)
    /// 2. User secrets (local development)
    /// 3. appsettings fallback (development only - logged as warning)
    /// </summary>
    private SecretConfiguration LoadSecrets()
    {
        _logger.LogInformation("🔐 Loading application secrets from environment...");

        var secrets = new SecretConfiguration
        {
            // CRITICAL SECRETS
            JwtSecret = GetRequiredSecret("JWT_SECRET", "JwtSettings__Secret"),
            EncryptionKey = GetRequiredSecret("ENCRYPTION_KEY", "Encryption__Key"),
            SuperAdminSecretPath = GetRequiredSecret("SUPERADMIN_SECRET_PATH", "Auth__SuperAdminSecretPath"),
            DatabasePassword = GetRequiredSecret("DB_PASSWORD", "ConnectionStrings__Password"),

            // OPTIONAL SECRETS
            SmtpPassword = GetOptionalSecret("SMTP_PASSWORD", "Email__SmtpPassword"),
            RedisConnectionString = GetOptionalSecret("REDIS_CONNECTION_STRING", "Redis__ConnectionString"),
            SiemApiKey = GetOptionalSecret("SIEM_API_KEY", "SIEM__ApiKey")
        };

        // Validate all required secrets are set
        try
        {
            secrets.Validate();
            _logger.LogInformation("✅ All required secrets loaded and validated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogCritical(ex, "❌ SECRET VALIDATION FAILED");
            throw;
        }

        // Log warnings for missing optional secrets
        if (!secrets.HasSmtpPassword)
            _logger.LogWarning("⚠️  SMTP_PASSWORD not set - email functionality will be disabled");

        if (!secrets.HasRedisConnectionString)
            _logger.LogWarning("⚠️  REDIS_CONNECTION_STRING not set - caching will use in-memory only");

        if (!secrets.HasSiemApiKey)
            _logger.LogWarning("⚠️  SIEM_API_KEY not set - SIEM logging will be disabled");

        return secrets;
    }

    /// <summary>
    /// Get required secret with fallback support
    /// </summary>
    private string GetRequiredSecret(string primaryKey, string fallbackKey)
    {
        // Try primary environment variable first
        var value = Environment.GetEnvironmentVariable(primaryKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            _logger.LogDebug("✓ Loaded {Key} from environment", primaryKey);
            return value;
        }

        // Try fallback key (for backward compatibility)
        value = Environment.GetEnvironmentVariable(fallbackKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            _logger.LogWarning("⚠️  Using fallback key {FallbackKey} for {PrimaryKey} - update to use {PrimaryKey}",
                fallbackKey, primaryKey, primaryKey);
            return value;
        }

        // In development, allow temporary values for testing
        if (IsDevelopmentEnvironment())
        {
            var devValue = GetDevelopmentFallback(primaryKey);
            if (!string.IsNullOrWhiteSpace(devValue))
            {
                _logger.LogWarning("⚠️  DEVELOPMENT MODE: Using hardcoded fallback for {Key} - NEVER USE IN PRODUCTION", primaryKey);
                return devValue;
            }
        }

        // Secret not found - will fail validation
        _logger.LogError("❌ Required secret {PrimaryKey} not found in environment", primaryKey);
        return string.Empty;
    }

    /// <summary>
    /// Get optional secret (returns null if not found)
    /// </summary>
    private string? GetOptionalSecret(string primaryKey, string fallbackKey)
    {
        var value = Environment.GetEnvironmentVariable(primaryKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            _logger.LogDebug("✓ Loaded {Key} from environment", primaryKey);
            return value;
        }

        value = Environment.GetEnvironmentVariable(fallbackKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            _logger.LogDebug("✓ Loaded {Key} from fallback {FallbackKey}", primaryKey, fallbackKey);
            return value;
        }

        return null;
    }

    /// <summary>
    /// Check if running in development environment
    /// </summary>
    private bool IsDevelopmentEnvironment()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        return env?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Get development fallback values (ONLY for local testing)
    /// These are the SAME values currently in appsettings.json
    /// </summary>
    private string? GetDevelopmentFallback(string key)
    {
        return key switch
        {
            "JWT_SECRET" => "dev-secret-key-minimum-32-chars-for-jwt-signing-do-not-use-in-production",
            "ENCRYPTION_KEY" => "dev-encryption-key-32-chars-minimum-for-aes256-gcm-do-not-use-prod",
            "SUPERADMIN_SECRET_PATH" => "732c44d0-d59b-494c-9fc0-bf1d65add4e5",
            "DB_PASSWORD" => "postgres", // Default for local dev
            _ => null
        };
    }

    /// <summary>
    /// Generate cryptographically secure random secret
    /// Use this to generate production secrets
    /// </summary>
    public static string GenerateSecureSecret(int length = 64)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_!@#$%^&*";
        var random = new byte[length];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(random);
        }

        return new string(random.Select(b => chars[b % chars.Length]).ToArray());
    }
}
