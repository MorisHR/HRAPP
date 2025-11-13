namespace HRMS.Core.Exceptions;

/// <summary>
/// Thrown when user input fails validation
/// HTTP 400 Bad Request
/// </summary>
public class ValidationException : HRMSException
{
    public ValidationException(
        string errorCode,
        string userMessage,
        string? technicalDetails = null,
        string? suggestedAction = null)
        : base(errorCode, userMessage, technicalDetails, suggestedAction ?? "Please review your information and try again.")
    {
    }
}
