namespace HRMS.Core.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with existing data
/// HTTP 409 Conflict
/// </summary>
public class ConflictException : HRMSException
{
    public ConflictException(
        string errorCode,
        string userMessage,
        string? technicalDetails = null,
        string? suggestedAction = null)
        : base(errorCode, userMessage, technicalDetails, suggestedAction ?? "Please review the existing data and try again.")
    {
    }
}
