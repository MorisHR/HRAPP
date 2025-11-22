using System.Text.Json;
using HRMS.Core.Interfaces;
using HRMS.Core.Enums;
using HRMS.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Redis-backed token blacklist service for immediate JWT revocation
/// FORTUNE 500 PATTERN: AWS Cognito, Auth0, Okta all use token blacklists
/// Performance: Sub-millisecond lookups via Redis, automatic expiry cleanup
/// </summary>
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TokenBlacklistService> _logger;
    private const string BLACKLIST_PREFIX = "token:blacklist:";
    private const string USER_TOKENS_PREFIX = "user:tokens:";
    private const string BLACKLIST_COUNT_KEY = "token:blacklist:count";

    public TokenBlacklistService(
        IDistributedCache cache,
        IAuditLogService auditLogService,
        ILogger<TokenBlacklistService> logger)
    {
        _cache = cache;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task BlacklistTokenAsync(string jti, DateTime expiresAt, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jti))
            {
                _logger.LogWarning("Attempted to blacklist token with null/empty JTI");
                return;
            }

            // Calculate TTL (time until token naturally expires)
            var ttl = expiresAt - DateTime.UtcNow;
            if (ttl.TotalSeconds <= 0)
            {
                _logger.LogInformation("Token {Jti} already expired, skipping blacklist", jti);
                return;
            }

            // Store blacklist entry with automatic expiry
            var blacklistEntry = new
            {
                jti,
                blacklistedAt = DateTime.UtcNow,
                expiresAt,
                reason
            };

            var key = $"{BLACKLIST_PREFIX}{jti}";
            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(blacklistEntry),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = expiresAt
                });

            // Increment blacklist counter
            await IncrementBlacklistCountAsync();

            _logger.LogWarning(
                "Token blacklisted: JTI={Jti}, Reason={Reason}, ExpiresAt={ExpiresAt}",
                jti, reason, expiresAt);

            // Audit log
            await _auditLogService.LogSecurityEventAsync(
                AuditActionType.TOKEN_REVOKED,
                AuditSeverity.WARNING,
                userId: null,
                description: $"Access token blacklisted: {reason}",
                additionalInfo: JsonSerializer.Serialize(new
                {
                    jti,
                    reason,
                    blacklistedAt = DateTime.UtcNow,
                    expiresAt
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting token {Jti}", jti);
            throw;
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jti))
            {
                return false;
            }

            var key = $"{BLACKLIST_PREFIX}{jti}";
            var result = await _cache.GetStringAsync(key);

            var isBlacklisted = result != null;

            if (isBlacklisted)
            {
                _logger.LogWarning("Blacklisted token detected: JTI={Jti}", jti);
            }

            return isBlacklisted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token blacklist for {Jti}", jti);
            // Fail open (allow access) on cache errors to prevent DOS
            // Alternative: Fail closed (deny access) for maximum security
            return false;
        }
    }

    public async Task BlacklistUserTokensAsync(Guid userId, string reason)
    {
        try
        {
            // Get list of active tokens for this user
            var userTokensKey = $"{USER_TOKENS_PREFIX}{userId}";
            var tokensJson = await _cache.GetStringAsync(userTokensKey);

            if (string.IsNullOrEmpty(tokensJson))
            {
                _logger.LogInformation("No active tokens found for user {UserId}", userId);
                return;
            }

            var tokens = JsonSerializer.Deserialize<List<TokenInfo>>(tokensJson);
            if (tokens == null || !tokens.Any())
            {
                _logger.LogInformation("No active tokens to blacklist for user {UserId}", userId);
                return;
            }

            // Blacklist each token
            var blacklistTasks = tokens.Select(token =>
                BlacklistTokenAsync(token.Jti, token.ExpiresAt, reason));

            await Task.WhenAll(blacklistTasks);

            // Clear user's token list
            await _cache.RemoveAsync(userTokensKey);

            _logger.LogWarning(
                "Blacklisted {Count} tokens for user {UserId}, Reason: {Reason}",
                tokens.Count, userId, reason);

            // Audit log
            await _auditLogService.LogSecurityEventAsync(
                AuditActionType.MASS_TOKEN_REVOCATION,
                AuditSeverity.CRITICAL,
                userId,
                description: $"All user tokens blacklisted: {reason}",
                additionalInfo: JsonSerializer.Serialize(new
                {
                    userId,
                    tokensBlacklisted = tokens.Count,
                    reason,
                    timestamp = DateTime.UtcNow
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting tokens for user {UserId}", userId);
            throw;
        }
    }

    public async Task RemoveFromBlacklistAsync(string jti)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jti))
            {
                return;
            }

            var key = $"{BLACKLIST_PREFIX}{jti}";
            await _cache.RemoveAsync(key);

            await DecrementBlacklistCountAsync();

            _logger.LogInformation("Token removed from blacklist: JTI={Jti}", jti);

            // Audit log
            await _auditLogService.LogSecurityEventAsync(
                AuditActionType.TOKEN_UNREVOKED,
                AuditSeverity.INFO,
                userId: null,
                description: "Token removed from blacklist",
                additionalInfo: JsonSerializer.Serialize(new { jti, timestamp = DateTime.UtcNow }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing token {Jti} from blacklist", jti);
            throw;
        }
    }

    public async Task<int> GetBlacklistCountAsync()
    {
        try
        {
            var countStr = await _cache.GetStringAsync(BLACKLIST_COUNT_KEY);
            return int.TryParse(countStr, out var count) ? count : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blacklist count");
            return 0;
        }
    }

    /// <summary>
    /// Track a user's active token for future mass revocation
    /// Called when a new token is generated
    /// </summary>
    public async Task TrackUserTokenAsync(Guid userId, string jti, DateTime expiresAt)
    {
        try
        {
            var userTokensKey = $"{USER_TOKENS_PREFIX}{userId}";

            // Get existing tokens
            var tokensJson = await _cache.GetStringAsync(userTokensKey);
            var tokens = string.IsNullOrEmpty(tokensJson)
                ? new List<TokenInfo>()
                : JsonSerializer.Deserialize<List<TokenInfo>>(tokensJson) ?? new List<TokenInfo>();

            // Remove expired tokens
            tokens.RemoveAll(t => t.ExpiresAt <= DateTime.UtcNow);

            // Add new token
            tokens.Add(new TokenInfo { Jti = jti, ExpiresAt = expiresAt });

            // Store with longest expiry time
            var longestExpiry = tokens.Max(t => t.ExpiresAt);
            await _cache.SetStringAsync(
                userTokensKey,
                JsonSerializer.Serialize(tokens),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = longestExpiry
                });

            _logger.LogDebug("Tracked token {Jti} for user {UserId}", jti, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking token for user {UserId}", userId);
            // Non-critical - don't throw
        }
    }

    private async Task IncrementBlacklistCountAsync()
    {
        try
        {
            var countStr = await _cache.GetStringAsync(BLACKLIST_COUNT_KEY);
            var count = int.TryParse(countStr, out var c) ? c : 0;
            count++;

            await _cache.SetStringAsync(
                BLACKLIST_COUNT_KEY,
                count.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing blacklist count");
        }
    }

    private async Task DecrementBlacklistCountAsync()
    {
        try
        {
            var countStr = await _cache.GetStringAsync(BLACKLIST_COUNT_KEY);
            var count = int.TryParse(countStr, out var c) ? c : 0;
            count = Math.Max(0, count - 1);

            await _cache.SetStringAsync(
                BLACKLIST_COUNT_KEY,
                count.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing blacklist count");
        }
    }

    private class TokenInfo
    {
        public string Jti { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
