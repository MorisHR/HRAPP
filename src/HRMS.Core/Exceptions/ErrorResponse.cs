namespace HRMS.Core.Exceptions;

/// <summary>
/// Standard error response returned to clients
/// Fortune 500 grade: structured, actionable, trackable
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Error code for tracking (e.g., "AUTH_001", "EMP_404")
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly message safe to display
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Suggested action for the user
    /// </summary>
    public string? SuggestedAction { get; set; }

    /// <summary>
    /// Correlation ID for tracking across services
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the error
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Support contact information
    /// </summary>
    public string? SupportContact { get; set; }

    /// <summary>
    /// Technical details (only in development)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Inner exception message (only in development)
    /// </summary>
    public string? InnerException { get; set; }

    /// <summary>
    /// Validation errors (for 400 Bad Request)
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
