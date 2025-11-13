namespace HRMS.Core.Exceptions;

/// <summary>
/// Thrown when a business rule is violated
/// HTTP 422 Unprocessable Entity
/// </summary>
public class BusinessRuleException : HRMSException
{
    public BusinessRuleException(
        string errorCode,
        string userMessage,
        string? technicalDetails = null,
        string? suggestedAction = null)
        : base(errorCode, userMessage, technicalDetails, suggestedAction ?? "Please review the requirements and try again.")
    {
    }
}
