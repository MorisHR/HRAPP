using Serilog.Context;

namespace HRMS.API.Middleware;

/// <summary>
/// Production-grade correlation ID middleware
/// Adds unique correlation IDs to all requests for distributed tracing
/// Supports X-Correlation-ID header for cross-service correlation
/// Enriches logs with correlation ID for easy request tracking
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from header or generate new one
        var correlationId = GetOrCreateCorrelationId(context);

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers.Add(CorrelationIdHeaderName, correlationId);
            }
            return Task.CompletedTask;
        });

        // Add to HttpContext items for easy access
        context.Items[CorrelationIdHeaderName] = correlationId;

        // Enrich Serilog logs with correlation ID
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogDebug("Request started with CorrelationId: {CorrelationId}", correlationId);

            await _next(context);

            _logger.LogDebug("Request completed with CorrelationId: {CorrelationId}", correlationId);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID exists in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension method to add correlation ID middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}

/// <summary>
/// Helper extension to get correlation ID from HttpContext
/// </summary>
public static class HttpContextCorrelationIdExtensions
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public static string? GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdHeaderName, out var correlationId))
        {
            return correlationId?.ToString();
        }

        return null;
    }
}
