using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using HRMS.Core.Exceptions;

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

        ErrorResponse response;

        // Handle our custom HRMS exceptions first
        if (exception is HRMSException hrmsException)
        {
            var statusCode = hrmsException switch
            {
                ValidationException => HttpStatusCode.BadRequest,
                NotFoundException => HttpStatusCode.NotFound,
                ConflictException => HttpStatusCode.Conflict,
                UnauthorizedException => HttpStatusCode.Unauthorized,
                ForbiddenException => HttpStatusCode.Forbidden,
                BusinessRuleException => (HttpStatusCode)422, // Unprocessable Entity
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)statusCode;

            response = new ErrorResponse
            {
                StatusCode = (int)statusCode,
                ErrorCode = hrmsException.ErrorCode,
                Message = hrmsException.UserMessage,
                SuggestedAction = hrmsException.SuggestedAction,
                // SECURITY: Only expose correlation ID in development (not production)
                // In production, correlation ID is logged server-side only for support tracking
                CorrelationId = _environment.IsDevelopment() ? context.TraceIdentifier : null,
                Timestamp = DateTime.UtcNow,
                SupportContact = "support@morishr.com" // TODO: Make configurable
            };

            // Include technical details only in development
            if (_environment.IsDevelopment())
            {
                response.Details = hrmsException.TechnicalDetails ?? exception.ToString();
                response.InnerException = exception.InnerException?.Message;
            }
        }
        else
        {
            // Handle standard .NET exceptions with user-friendly messages
            var (statusCode, message, errorCode, suggestedAction) = exception switch
            {
                ArgumentNullException => (
                    HttpStatusCode.BadRequest,
                    "Required information is missing. Please review your input and try again.",
                    ErrorCodes.VAL_REQUIRED_FIELD,
                    "Check that all required fields are filled in."
                ),
                ArgumentException => (
                    HttpStatusCode.BadRequest,
                    "Invalid information provided. Please check your input and try again.",
                    ErrorCodes.VAL_INVALID_FORMAT,
                    "Review the highlighted fields and correct any errors."
                ),
                InvalidOperationException => (
                    HttpStatusCode.BadRequest,
                    "This operation cannot be completed at this time.",
                    ErrorCodes.SYS_UNEXPECTED_ERROR,
                    "Please try again or contact support if the issue persists."
                ),
                System.UnauthorizedAccessException => (
                    HttpStatusCode.Forbidden,
                    "You don't have permission to access this resource.",
                    ErrorCodes.AUTH_INSUFFICIENT_PERMISSIONS,
                    "Contact your administrator if you need access."
                ),
                KeyNotFoundException => (
                    HttpStatusCode.NotFound,
                    "The requested information could not be found.",
                    ErrorCodes.SYS_UNEXPECTED_ERROR,
                    "Verify your selection or contact support if you believe this is an error."
                ),
                DbUpdateConcurrencyException => (
                    HttpStatusCode.Conflict,
                    "This record was recently modified by another user. Please refresh and try again.",
                    ErrorCodes.SYS_DATABASE_ERROR,
                    "Reload the page to see the latest information, then retry your changes."
                ),
                DbUpdateException => (
                    HttpStatusCode.BadRequest,
                    "Unable to save your changes due to a data conflict.",
                    ErrorCodes.SYS_DATABASE_ERROR,
                    "Check your input and try again. Contact support if the issue continues."
                ),
                TimeoutException => (
                    HttpStatusCode.RequestTimeout,
                    "Your request is taking longer than expected. Please try again.",
                    ErrorCodes.SYS_EXTERNAL_SERVICE_ERROR,
                    "Wait a moment and retry. If this persists, try again later."
                ),
                _ => (
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Our team has been notified and is working on it.",
                    ErrorCodes.SYS_UNEXPECTED_ERROR,
                    "Please try again in a few moments. Contact support with error ID if this continues."
                )
            };

            context.Response.StatusCode = (int)statusCode;

            response = new ErrorResponse
            {
                StatusCode = (int)statusCode,
                ErrorCode = errorCode,
                Message = message,
                SuggestedAction = suggestedAction,
                // SECURITY: Only expose correlation ID in development (not production)
                // In production, correlation ID is logged server-side only for support tracking
                CorrelationId = _environment.IsDevelopment() ? context.TraceIdentifier : null,
                Timestamp = DateTime.UtcNow,
                SupportContact = "support@morishr.com"
            };

            // Include stack trace and detailed error only in development
            if (_environment.IsDevelopment())
            {
                response.Details = exception.ToString();
                response.InnerException = exception.InnerException?.Message;
            }
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
/// Extension method to add global exception handling middleware
/// </summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
