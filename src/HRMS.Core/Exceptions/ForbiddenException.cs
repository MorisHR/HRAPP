namespace HRMS.Core.Exceptions;

/// <summary>
/// Thrown when user is authenticated but lacks permission
/// HTTP 403 Forbidden
/// </summary>
public class ForbiddenException : HRMSException
{
    public ForbiddenException(
        string errorCode,
        string userMessage,
        string? technicalDetails = null,
        string? suggestedAction = null)
        : base(errorCode, userMessage, technicalDetails, suggestedAction ?? "Contact your administrator if you need access to this resource.")
    {
    }
}
