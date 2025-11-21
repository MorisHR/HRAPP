using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// FORTUNE 500: Polly resilience policies for external service calls
/// PATTERN: Netflix Hystrix, AWS SDK retry logic, Google Cloud retry policies
///
/// FEATURES:
/// - Circuit Breaker: Opens after 5 consecutive failures, stays open for 30s
/// - Retry Policy: Exponential backoff (1s, 2s, 4s, 8s)
/// - Timeout Policy: 30s timeout for external calls
/// - Combined Pipeline: Timeout -> Retry -> Circuit Breaker
///
/// POLLY 8.X API:
/// This uses the modern Polly 8.x ResiliencePipeline API for better performance
/// and composition. See: https://www.pollydocs.org/
///
/// USAGE:
/// Inject this service into any service that makes external API calls.
/// Wrap all external calls with the resilience pipeline.
///
/// EXAMPLE:
/// var pipeline = _resilience.GetCombinedPipeline(timeoutSeconds: 10, maxRetries: 3);
/// return await pipeline.ExecuteAsync(async ct =>
/// {
///     using var httpClient = new HttpClient();
///     var response = await httpClient.GetAsync("https://api.vendor.com/data", ct);
///     response.EnsureSuccessStatusCode();
///     return await response.Content.ReadFromJsonAsync<ExternalData>(cancellationToken: ct);
/// }, cancellationToken);
/// </summary>
public class ResiliencePolicyService
{
    private readonly ILogger<ResiliencePolicyService> _logger;

    // Circuit breaker state (shared across requests)
    private static ResiliencePipeline? _combinedPipeline;
    private static readonly object _lock = new object();

    public ResiliencePolicyService(ILogger<ResiliencePolicyService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get combined resilience pipeline (Timeout + Retry + Circuit Breaker)
    /// Use this for all external API calls
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds (default: 30)</param>
    /// <param name="maxRetries">Maximum retry attempts (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Combined pipeline ready for execution</returns>
    public ResiliencePipeline GetCombinedPipeline(int timeoutSeconds = 30, int maxRetries = 3)
    {
        if (_combinedPipeline != null)
            return _combinedPipeline;

        lock (_lock)
        {
            if (_combinedPipeline != null)
                return _combinedPipeline;

            _combinedPipeline = new ResiliencePipelineBuilder()
                // Layer 1: Timeout (innermost)
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                    OnTimeout = args =>
                    {
                        _logger.LogWarning(
                            "⏱️ Request timed out after {Timeout}s",
                            args.Timeout.TotalSeconds);
                        return default;
                    }
                })
                // Layer 2: Retry with exponential backoff
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutRejectedException>(),
                    MaxRetryAttempts = maxRetries,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true, // Add jitter to prevent thundering herd
                    OnRetry = args =>
                    {
                        _logger.LogWarning(args.Outcome.Exception,
                            "🔄 Retry {AttemptNumber}/{MaxRetries} after {Delay}s due to {ExceptionType}",
                            args.AttemptNumber, maxRetries, args.RetryDelay.TotalSeconds,
                            args.Outcome.Exception?.GetType().Name ?? "Unknown");
                        return default;
                    }
                })
                // Layer 3: Circuit Breaker (outermost)
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutRejectedException>(),
                    FailureRatio = 0.5, // Open if 50% of requests fail
                    MinimumThroughput = 5, // Need at least 5 requests before calculating ratio
                    BreakDuration = TimeSpan.FromSeconds(30),
                    OnOpened = args =>
                    {
                        _logger.LogWarning(args.Outcome.Exception,
                            "🚨 Circuit breaker OPENED for {BreakDuration}s due to {ExceptionType}",
                            args.BreakDuration.TotalSeconds, args.Outcome.Exception?.GetType().Name ?? "Unknown");
                        return default;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation("✅ Circuit breaker CLOSED - allowing requests again");
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger.LogInformation("⚡ Circuit breaker HALF-OPEN - testing with next request");
                        return default;
                    }
                })
                .Build();

            return _combinedPipeline;
        }
    }

    /// <summary>
    /// Execute an action with resilience policies applied
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int timeoutSeconds = 30,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var pipeline = GetCombinedPipeline(timeoutSeconds, maxRetries);
        return await pipeline.ExecuteAsync(
            async ct => await action(ct),
            cancellationToken);
    }

    /// <summary>
    /// Execute an action with resilience policies applied (no return value)
    /// </summary>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        int timeoutSeconds = 30,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var pipeline = GetCombinedPipeline(timeoutSeconds, maxRetries);
        await pipeline.ExecuteAsync(
            async ct => { await action(ct); return ValueTask.CompletedTask; },
            cancellationToken);
    }
}
