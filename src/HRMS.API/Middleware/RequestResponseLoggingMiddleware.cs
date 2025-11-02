using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace HRMS.API.Middleware;

/// <summary>
/// Production-grade request/response logging middleware with PII masking
/// Logs all HTTP requests and responses for audit and debugging
/// Automatically masks sensitive information (passwords, tokens, credit cards, etc.)
/// Configurable to skip certain endpoints (e.g., health checks)
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    // Paths to skip logging (reduce noise)
    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/healthcheck",
        "/swagger",
        "/hangfire"
    };

    // PII patterns to mask
    private static readonly Dictionary<string, Regex> PiiPatterns = new()
    {
        ["password"] = new Regex(@"""password"":\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        ["token"] = new Regex(@"""token"":\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        ["secret"] = new Regex(@"""secret"":\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        ["apiKey"] = new Regex(@"""apiKey"":\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        ["authorization"] = new Regex(@"""authorization"":\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        ["creditCard"] = new Regex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", RegexOptions.Compiled),
        ["ssn"] = new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled),
        ["email"] = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled)
    };

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for certain paths
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var correlationId = context.GetCorrelationId() ?? context.TraceIdentifier;
        var stopwatch = Stopwatch.StartNew();

        // Log request
        await LogRequest(context, correlationId);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            stopwatch.Stop();

            // Log response
            await LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequest(HttpContext context, string correlationId)
    {
        context.Request.EnableBuffering();

        var request = context.Request;
        var body = await ReadRequestBody(request);

        var logMessage = new StringBuilder();
        logMessage.AppendLine($"HTTP Request [{correlationId}]");
        logMessage.AppendLine($"Method: {request.Method}");
        logMessage.AppendLine($"Path: {request.Path}{request.QueryString}");
        logMessage.AppendLine($"Protocol: {request.Protocol}");
        logMessage.AppendLine($"User: {context.User?.Identity?.Name ?? "Anonymous"}");

        // Log headers (excluding sensitive ones)
        logMessage.AppendLine("Headers:");
        foreach (var (key, value) in request.Headers.Where(h => !IsSensitiveHeader(h.Key)))
        {
            logMessage.AppendLine($"  {key}: {value}");
        }

        // Log body (with PII masking)
        if (!string.IsNullOrWhiteSpace(body))
        {
            var maskedBody = MaskPii(body);
            logMessage.AppendLine($"Body: {maskedBody}");
        }

        _logger.LogInformation(logMessage.ToString());
    }

    private async Task LogResponse(HttpContext context, string correlationId, long elapsedMs)
    {
        var response = context.Response;
        response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        var logMessage = new StringBuilder();
        logMessage.AppendLine($"HTTP Response [{correlationId}]");
        logMessage.AppendLine($"Status: {response.StatusCode}");
        logMessage.AppendLine($"Duration: {elapsedMs}ms");

        // Log response body (with PII masking) only if it's not too large
        if (body.Length <= 10000 && !string.IsNullOrWhiteSpace(body))
        {
            var maskedBody = MaskPii(body);
            logMessage.AppendLine($"Body: {maskedBody}");
        }
        else if (body.Length > 10000)
        {
            logMessage.AppendLine($"Body: [Response too large: {body.Length} bytes]");
        }

        // Log as warning if status is error
        if (response.StatusCode >= 400)
        {
            _logger.LogWarning(logMessage.ToString());
        }
        else
        {
            _logger.LogInformation(logMessage.ToString());
        }
    }

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private string MaskPii(string content)
    {
        if (_environment.IsProduction())
        {
            // Apply all PII masking patterns
            foreach (var pattern in PiiPatterns)
            {
                content = pattern.Value.Replace(content, match =>
                {
                    return pattern.Key switch
                    {
                        "password" => $"\"{pattern.Key}\":\"***MASKED***\"",
                        "token" => $"\"{pattern.Key}\":\"***MASKED***\"",
                        "secret" => $"\"{pattern.Key}\":\"***MASKED***\"",
                        "apiKey" => $"\"{pattern.Key}\":\"***MASKED***\"",
                        "authorization" => $"\"{pattern.Key}\":\"***MASKED***\"",
                        "creditCard" => "****-****-****-****",
                        "ssn" => "***-**-****",
                        "email" => "***@***.***",
                        _ => "***MASKED***"
                    };
                });
            }
        }

        return content;
    }

    private bool ShouldSkipLogging(PathString path)
    {
        return SkipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
    }

    private bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "Cookie",
            "X-API-Key",
            "X-Auth-Token"
        };

        return sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Extension method to add request/response logging middleware
/// </summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
