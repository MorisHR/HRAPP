using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMS.API.Filters;

/// <summary>
/// Action filter that checks if user must change password before accessing protected endpoints
/// </summary>
public class MustChangePasswordFilter : IAsyncActionFilter
{
    private readonly MasterDbContext _context;
    private readonly ILogger<MustChangePasswordFilter> _logger;

    public MustChangePasswordFilter(MasterDbContext context, ILogger<MustChangePasswordFilter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Skip for specific endpoints that have the SkipMustChangePassword attribute
        var endpoint = context.HttpContext.GetEndpoint();
        var skipCheck = endpoint?.Metadata.GetMetadata<SkipMustChangePasswordAttribute>();

        if (skipCheck != null)
        {
            await next();
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.HttpContext.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            // If no valid user ID, let the request continue (will be handled by authentication)
            await next();
            return;
        }

        // Check if user must change password
        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (adminUser != null && adminUser.MustChangePassword)
        {
            _logger.LogWarning("User {UserId} attempted to access protected endpoint but must change password", userId);

            context.Result = new ObjectResult(new
            {
                error = "PASSWORD_CHANGE_REQUIRED",
                message = "You must change your password before continuing"
            })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}

/// <summary>
/// Attribute to skip the MustChangePassword check on specific endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SkipMustChangePasswordAttribute : Attribute
{
}
