using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HRMS.API.Middleware;

/// <summary>
/// Production-grade global exception handling middleware
/// Catches all unhandled exceptions and returns proper error responses
/// Logs exceptions with full context
/// Masks sensitive information in production
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception occurred. Path: {Path}, Method: {Method}, User: {User}, CorrelationId: {CorrelationId}",
                context.Request.Path,
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous",
                context.TraceIdentifier);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errorCode) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Invalid request: required parameter is missing", "VALIDATION_ERROR"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid request: parameter value is invalid", "VALIDATION_ERROR"),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message, "INVALID_OPERATION"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied", "ACCESS_DENIED"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found", "NOT_FOUND"),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "The record was modified by another user", "CONCURRENCY_ERROR"),
            DbUpdateException => (HttpStatusCode.BadRequest, "Database update failed", "DATABASE_ERROR"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "The request timed out", "TIMEOUT"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred", "INTERNAL_ERROR")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        // Include stack trace and detailed error only in development
        if (_environment.IsDevelopment())
        {
            response.Details = exception.ToString();
            response.InnerException = exception.InnerException?.Message;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Standard error response model
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
    public string? InnerException { get; set; }
}

/// <summary>
/// Extension method to add global exception handling middleware
/// </summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
