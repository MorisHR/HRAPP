using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Production-grade Google Secret Manager service
/// Retrieves secrets from Google Cloud Secret Manager
/// Implements caching to reduce API calls
/// Provides fallback to environment variables for local development
/// </summary>
public class GoogleSecretManagerService
{
    private readonly ILogger<GoogleSecretManagerService> _logger;
    private readonly string _projectId;
    private readonly bool _enabled;
    private readonly SecretManagerServiceClient? _client;
    private readonly Dictionary<string, (string Value, DateTime CachedAt)> _cache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    public GoogleSecretManagerService(
        ILogger<GoogleSecretManagerService> logger,
        string projectId,
        bool enabled)
    {
        _logger = logger;
        _projectId = projectId;
        _enabled = enabled;

        if (_enabled && !string.IsNullOrEmpty(_projectId))
        {
            try
            {
                _client = SecretManagerServiceClient.Create();
                _logger.LogInformation("Google Secret Manager initialized for project: {ProjectId}", _projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Secret Manager. Falling back to environment variables.");
                _enabled = false;
            }
        }
        else
        {
            _logger.LogInformation("Google Secret Manager disabled. Using environment variables.");
        }
    }

    /// <summary>
    /// Gets a secret value from Google Secret Manager or environment variables
    /// </summary>
    /// <param name="secretName">Secret name (e.g., "JWT_SECRET", "DB_PASSWORD")</param>
    /// <param name="version">Secret version (default: "latest")</param>
    /// <returns>Secret value or null if not found</returns>
    public async Task<string?> GetSecretAsync(string secretName, string version = "latest")
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        // Check cache first
        if (_cache.TryGetValue(secretName, out var cachedSecret))
        {
            if (DateTime.UtcNow - cachedSecret.CachedAt < _cacheExpiration)
            {
                _logger.LogDebug("Retrieved secret '{SecretName}' from cache", secretName);
                return cachedSecret.Value;
            }

            // Remove expired cache entry
            _cache.Remove(secretName);
        }

        // Try Google Secret Manager if enabled
        if (_enabled && _client != null)
        {
            try
            {
                var secretValue = await GetSecretFromGoogleAsync(secretName, version);
                if (!string.IsNullOrEmpty(secretValue))
                {
                    _cache[secretName] = (secretValue, DateTime.UtcNow);
                    return secretValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret '{SecretName}' from Google Secret Manager", secretName);
            }
        }

        // Fallback to environment variable
        var envValue = Environment.GetEnvironmentVariable(secretName);
        if (!string.IsNullOrEmpty(envValue))
        {
            _logger.LogDebug("Retrieved secret '{SecretName}' from environment variable", secretName);
            return envValue;
        }

        _logger.LogWarning("Secret '{SecretName}' not found in Google Secret Manager or environment variables", secretName);
        return null;
    }

    /// <summary>
    /// Gets a secret value synchronously (use sparingly, prefer async)
    /// </summary>
    public string? GetSecret(string secretName, string version = "latest")
    {
        return GetSecretAsync(secretName, version).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates or updates a secret in Google Secret Manager
    /// NOTE: This method requires additional Google Cloud permissions
    /// For production use, manage secrets via Google Cloud Console or Terraform
    /// </summary>
    public Task<bool> SetSecretAsync(string secretName, string secretValue)
    {
        _logger.LogWarning("SetSecretAsync not implemented - please manage secrets via Google Cloud Console");
        return Task.FromResult(false);
    }

    /// <summary>
    /// Clears the secret cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogInformation("Secret cache cleared");
    }

    private async Task<string?> GetSecretFromGoogleAsync(string secretName, string version)
    {
        if (_client == null)
            return null;

        var secretVersionName = new SecretVersionName(_projectId, secretName, version);

        try
        {
            var response = await _client.AccessSecretVersionAsync(secretVersionName);
            var payload = response.Payload.Data.ToStringUtf8();

            _logger.LogDebug("Retrieved secret '{SecretName}' from Google Secret Manager", secretName);
            return payload;
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            _logger.LogWarning("Secret '{SecretName}' not found in Google Secret Manager", secretName);
            return null;
        }
    }
}

/// <summary>
/// Extension methods for configuration builder to integrate with Google Secret Manager
/// </summary>
public static class GoogleSecretManagerConfigurationExtensions
{
    /// <summary>
    /// Adds Google Secret Manager as a configuration source
    /// </summary>
    public static IConfigurationBuilder AddGoogleSecretManager(
        this IConfigurationBuilder builder,
        string projectId,
        bool enabled = true)
    {
        if (enabled && !string.IsNullOrEmpty(projectId))
        {
            // In a production scenario, you would implement IConfigurationSource and IConfigurationProvider
            // For now, we'll manually load secrets in Program.cs
        }

        return builder;
    }
}
