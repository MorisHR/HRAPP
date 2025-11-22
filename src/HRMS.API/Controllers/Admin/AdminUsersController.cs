using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Services;
using System.Security.Claims;

namespace HRMS.API.Controllers.Admin;

[ApiController]
[Route("api/admin-users")]
[Authorize] // Requires authentication
public class AdminUsersController : ControllerBase
{
    private readonly AdminUserManagementService _adminUserService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        AdminUserManagementService adminUserService,
        ILogger<AdminUsersController> logger)
    {
        _adminUserService = adminUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all SuperAdmin users with pagination and filtering
    /// </summary>
    /// <remarks>
    /// Required permission: SUPERADMIN_READ
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (users, totalCount) = await _adminUserService.GetAllAsync(
                pageIndex,
                pageSize,
                searchTerm,
                isActive,
                cancellationToken);

            // Remove sensitive fields before returning
            var sanitizedUsers = users.Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FirstName,
                u.LastName,
                u.PhoneNumber,
                u.IsActive,
                u.IsTwoFactorEnabled,
                u.LastLoginDate,
                u.LastLoginIPAddress,
                u.SessionTimeoutMinutes,
                Permissions = string.IsNullOrEmpty(u.Permissions)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(u.Permissions),
                u.CreatedAt,
                u.UpdatedAt,
                IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTime.UtcNow,
                u.MustChangePassword,
                u.LastPasswordChangeDate,
                u.PasswordExpiresAt
            }).ToList();

            return Ok(new
            {
                success = true,
                data = sanitizedUsers,
                pagination = new
                {
                    pageIndex,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SuperAdmin users");
            return StatusCode(500, new { success = false, error = "Failed to retrieve users", details = ex.Message });
        }
    }

    /// <summary>
    /// Get SuperAdmin user by ID
    /// </summary>
    /// <remarks>
    /// Required permission: SUPERADMIN_READ
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _adminUserService.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            // Remove sensitive fields
            var sanitizedUser = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.IsActive,
                user.IsTwoFactorEnabled,
                user.LastLoginDate,
                user.LastLoginIPAddress,
                user.SessionTimeoutMinutes,
                Permissions = string.IsNullOrEmpty(user.Permissions)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.Permissions),
                user.AllowedIPAddresses,
                user.AllowedLoginHours,
                user.CreatedAt,
                user.UpdatedAt,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow,
                user.LockoutEnd,
                user.AccessFailedCount,
                user.MustChangePassword,
                user.LastPasswordChangeDate,
                user.PasswordExpiresAt,
                user.LastFailedLoginAttempt,
                user.IsInitialSetupAccount,
                user.StatusNotes
            };

            return Ok(new { success = true, data = sanitizedUser });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SuperAdmin user: {UserId}", id);
            return StatusCode(500, new { success = false, error = "Failed to retrieve user", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new SuperAdmin user
    /// </summary>
    /// <remarks>
    /// Required permission: SUPERADMIN_CREATE
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user context
            var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            // Validate request
            if (string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new { success = false, message = "Required fields are missing." });
            }

            // Validate password strength
            if (request.Password.Length < 12)
            {
                return BadRequest(new { success = false, message = "Password must be at least 12 characters long." });
            }

            var (success, message, user) = await _adminUserService.CreateAsync(
                request.UserName,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Permissions,
                request.SessionTimeoutMinutes ?? 15,
                currentUserId,
                currentUserEmail,
                cancellationToken);

            if (!success || user == null)
            {
                return BadRequest(new { success = false, message });
            }

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
            {
                success = true,
                message,
                data = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.IsActive
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SuperAdmin user");
            return StatusCode(500, new { success = false, error = "Failed to create user", details = ex.Message });
        }
    }

    /// <summary>
    /// Update SuperAdmin user
    /// </summary>
    /// <remarks>
    /// Required permission: SUPERADMIN_UPDATE
    /// </remarks>
    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user context
            var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            var (success, message) = await _adminUserService.UpdateAsync(
                id,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.IsActive,
                request.Permissions,
                request.SessionTimeoutMinutes,
                currentUserId,
                currentUserEmail,
                cancellationToken);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SuperAdmin user: {UserId}", id);
            return StatusCode(500, new { success = false, error = "Failed to update user", details = ex.Message });
        }
    }

    /// <summary>
    /// Change SuperAdmin password
    /// </summary>
    /// <remarks>
    /// Required permission: SUPERADMIN_UPDATE
    /// </remarks>
    [HttpPost("{id}/change-password")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user context
            var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            // Validate password strength
            if (request.NewPassword.Length < 12)
            {
                return BadRequest(new { success = false, message = "Password must be at least 12 characters long." });
            }

            var (success, message) = await _adminUserService.ChangePasswordAsync(
                id,
                request.NewPassword,
                currentUserId,
                currentUserEmail,
                cancellationToken);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password for SuperAdmin user: {UserId}", id);
            return StatusCode(500, new { success = false, error = "Failed to change password", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete SuperAdmin user (soft delete)
    /// </summary>
    /// <remarks>
    /// Required permission: SUPERADMIN_DELETE
    /// </remarks>
    [HttpDelete("{id}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user context
            var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            var (success, message) = await _adminUserService.DeleteAsync(
                id,
                currentUserId,
                currentUserEmail,
                cancellationToken);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete SuperAdmin user: {UserId}", id);
            return StatusCode(500, new { success = false, error = "Failed to delete user", details = ex.Message });
        }
    }

    /// <summary>
    /// Update SuperAdmin permissions
    /// </summary>
    /// <remarks>
    /// Required permission: SUPERADMIN_PERMISSION_MANAGE
    /// </remarks>
    [HttpPut("{id}/permissions")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdatePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user context
            var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            var (success, message) = await _adminUserService.UpdatePermissionsAsync(
                id,
                request.Permissions,
                currentUserId,
                currentUserEmail,
                cancellationToken);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update permissions for SuperAdmin user: {UserId}", id);
            return StatusCode(500, new { success = false, error = "Failed to update permissions", details = ex.Message });
        }
    }

    /// <summary>
    /// Unlock SuperAdmin account
    /// </summary>
    /// <remarks>
    /// Required permission: SUPERADMIN_UNLOCK
    /// </remarks>
    [HttpPost("{id}/unlock")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UnlockAccount(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user context
            var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            var (success, message) = await _adminUserService.UnlockAccountAsync(
                id,
                currentUserId,
                currentUserEmail,
                cancellationToken);

            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock SuperAdmin account: {UserId}", id);
            return StatusCode(500, new { success = false, error = "Failed to unlock account", details = ex.Message });
        }
    }

    /// <summary>
    /// Get SuperAdmin activity logs
    /// </summary>
    /// <remarks>
    /// Required permission: AUDIT_LOG_ACCESS
    /// </remarks>
    [HttpGet("{id}/activity")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetActivityLogs(
        Guid id,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _adminUserService.GetActivityLogsAsync(id, pageIndex, pageSize, cancellationToken);

            return Ok(new
            {
                success = true,
                data = logs,
                pagination = new
                {
                    pageIndex,
                    pageSize,
                    totalCount = logs.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity logs for SuperAdmin user: {UserId}", id);
            return StatusCode(500, new { success = false, error = "Failed to retrieve activity logs", details = ex.Message });
        }
    }
}

// ============================================
// REQUEST MODELS
// ============================================

public class CreateAdminUserRequest
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public List<SuperAdminPermission>? Permissions { get; set; }
    public int? SessionTimeoutMinutes { get; set; }
}

public class UpdateAdminUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsActive { get; set; }
    public List<SuperAdminPermission>? Permissions { get; set; }
    public int? SessionTimeoutMinutes { get; set; }
}

public class ChangePasswordRequest
{
    public required string NewPassword { get; set; }
}

public class UpdatePermissionsRequest
{
    public required List<SuperAdminPermission> Permissions { get; set; }
}
