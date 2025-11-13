namespace HRMS.Core.Exceptions;

/// <summary>
/// Base exception for all HRMS exceptions
/// Provides user-friendly messages and error codes for Fortune 500 grade error handling
/// </summary>
public abstract class HRMSException : Exception
{
    /// <summary>
    /// Error code for tracking and support (e.g., "AUTH_001", "EMP_404")
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// User-friendly message safe to display to end users
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Technical details for logging (not shown to users)
    /// </summary>
    public string? TechnicalDetails { get; }

    /// <summary>
    /// Suggested action for the user
    /// </summary>
    public string? SuggestedAction { get; }

    protected HRMSException(
        string errorCode,
        string userMessage,
        string? technicalDetails = null,
        string? suggestedAction = null,
        Exception? innerException = null)
        : base(userMessage, innerException)
    {
        ErrorCode = errorCode;
        UserMessage = userMessage;
        TechnicalDetails = technicalDetails;
        SuggestedAction = suggestedAction;
    }
}
