namespace HRMS.Core.Exceptions;

/// <summary>
/// Thrown when user is not authenticated
/// HTTP 401 Unauthorized
/// </summary>
public class UnauthorizedException : HRMSException
{
    public UnauthorizedException(
        string errorCode,
        string userMessage,
        string? technicalDetails = null,
        string? suggestedAction = null)
        : base(errorCode, userMessage, technicalDetails, suggestedAction ?? "Please sign in again to continue.")
    {
    }
}
