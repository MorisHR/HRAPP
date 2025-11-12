using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService that extracts user information from HttpContext
/// Supports both JWT authentication and session-based authentication
///
/// COMPLIANCE: SOX, GDPR - Accurate user tracking for audit trails
/// FALLBACK: Returns "System" for background jobs (no HTTP context)
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current user's ID from claims (sub claim)
    /// </summary>
    public string? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return null;

            // Try standard JWT "sub" claim first
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue("sub")
                      ?? user.FindFirstValue("user_id");

            return userId;
        }
    }

    /// <summary>
    /// Gets the current user's username from claims
    /// Tries multiple claim types for compatibility
    /// </summary>
    public string? Username
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return null;

            // Try multiple claim types for username
            var username = user.FindFirstValue("preferred_username")
                        ?? user.FindFirstValue(ClaimTypes.Name)
                        ?? user.FindFirstValue("name")
                        ?? user.FindFirstValue("username")
                        ?? user.FindFirstValue(ClaimTypes.Email)
                        ?? user.Identity?.Name;

            return username;
        }
    }

    /// <summary>
    /// Gets the current user's email from claims
    /// </summary>
    public string? Email
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return null;

            var email = user.FindFirstValue(ClaimTypes.Email)
                     ?? user.FindFirstValue("email");

            return email;
        }
    }

    /// <summary>
    /// Gets the current user's tenant ID from claims (if available)
    /// </summary>
    public Guid? TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return null;

            var tenantIdStr = user.FindFirstValue("tenant_id")
                           ?? user.FindFirstValue("TenantId");

            if (Guid.TryParse(tenantIdStr, out var tenantId))
                return tenantId;

            return null;
        }
    }

    /// <summary>
    /// Indicates whether the user is authenticated
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true;
        }
    }

    /// <summary>
    /// Gets the username for audit trails, with fallback to "System" for background jobs
    /// This method should be used for CreatedBy/UpdatedBy fields
    /// </summary>
    public string GetAuditUsername()
    {
        // If we have an authenticated user, use their username or email
        if (IsAuthenticated)
        {
            return Username ?? Email ?? "System";
        }

        // For background jobs, API key auth, or unauthenticated requests
        return "System";
    }
}
