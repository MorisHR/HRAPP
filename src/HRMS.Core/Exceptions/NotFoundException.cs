namespace HRMS.Core.Exceptions;

/// <summary>
/// Thrown when a requested resource is not found
/// HTTP 404 Not Found
/// </summary>
public class NotFoundException : HRMSException
{
    public NotFoundException(
        string errorCode,
        string userMessage,
        string? technicalDetails = null,
        string? suggestedAction = null)
        : base(errorCode, userMessage, technicalDetails, suggestedAction ?? "Please verify your selection or contact support if you believe this is an error.")
    {
    }
}
