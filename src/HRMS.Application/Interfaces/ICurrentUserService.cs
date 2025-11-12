namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for accessing the current authenticated user's information from HTTP context
/// Provides user identity information for audit trails and authorization
///
/// COMPLIANCE: SOX, GDPR - Accurate user tracking for audit trails
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID from claims (sub claim)
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user's username from claims (preferred_username or name claim)
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Gets the current user's email from claims
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user's tenant ID from claims (if available)
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Indicates whether the user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the username for audit trails, with fallback to "System" for background jobs
    /// Use this for CreatedBy/UpdatedBy fields
    /// </summary>
    string GetAuditUsername();
}
