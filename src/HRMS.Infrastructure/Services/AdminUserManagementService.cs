using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Data;
using System.Text.Json;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing SuperAdmin user accounts
/// Handles CRUD operations, permission management, and security features
/// </summary>
public class AdminUserManagementService
{
    private readonly MasterDbContext _masterDbContext;
    private readonly ILogger<AdminUserManagementService> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;

    public AdminUserManagementService(
        MasterDbContext masterDbContext,
        ILogger<AdminUserManagementService> logger,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService)
    {
        _masterDbContext = masterDbContext;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Get all SuperAdmin users with pagination and filtering
    /// </summary>
    public async Task<(List<AdminUser> Users, int TotalCount)> GetAllAsync(
        int pageIndex = 0,
        int pageSize = 20,
        string? searchTerm = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _masterDbContext.AdminUsers
            .Where(u => !u.IsDeleted);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u =>
                u.Email.Contains(searchTerm) ||
                u.UserName.Contains(searchTerm) ||
                u.FirstName.Contains(searchTerm) ||
                u.LastName.Contains(searchTerm));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    /// <summary>
    /// Get SuperAdmin user by ID
    /// </summary>
    public async Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _masterDbContext.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Get SuperAdmin user by email
    /// </summary>
    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _masterDbContext.AdminUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Create a new SuperAdmin user
    /// </summary>
    public async Task<(bool Success, string Message, AdminUser? User)> CreateAsync(
        string userName,
        string email,
        string password,
        string firstName,
        string lastName,
        string? phoneNumber,
        List<SuperAdminPermission>? permissions,
        int sessionTimeoutMinutes,
        Guid createdBySuperAdminId,
        string createdBySuperAdminEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if email already exists
            if (await _masterDbContext.AdminUsers.AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken))
            {
                return (false, "An admin user with this email already exists.", null);
            }

            // Check if username already exists
            if (await _masterDbContext.AdminUsers.AnyAsync(u => u.UserName.ToLower() == userName.ToLower(), cancellationToken))
            {
                return (false, "An admin user with this username already exists.", null);
            }

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(password);

            // Create user
            var user = new AdminUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email.ToLower(),
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                IsActive = true,
                IsTwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                SessionTimeoutMinutes = sessionTimeoutMinutes,
                LastPasswordChangeDate = DateTime.UtcNow,
                PasswordExpiresAt = DateTime.UtcNow.AddDays(90), // 90-day password rotation
                MustChangePassword = true, // Force password change on first login
                CreatedBySuperAdminId = createdBySuperAdminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Set permissions
            if (permissions != null && permissions.Any())
            {
                user.Permissions = JsonSerializer.Serialize(permissions.Select(p => p.ToString()).ToList());
            }

            _masterDbContext.AdminUsers.Add(user);
            await _masterDbContext.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.SUPERADMIN_CREATED,
                createdBySuperAdminId,
                createdBySuperAdminEmail,
                targetTenantId: null,
                targetTenantName: null,
                description: $"Created SuperAdmin user: {email}",
                oldValues: null,
                newValues: JsonSerializer.Serialize(new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.FirstName,
                    user.LastName,
                    user.IsActive,
                    user.SessionTimeoutMinutes,
                    Permissions = permissions?.Select(p => p.ToString()).ToList() ?? new List<string>()
                }),
                success: true);

            _logger.LogInformation("SuperAdmin user created: {Email} by {CreatedBy}", email, createdBySuperAdminEmail);

            return (true, "SuperAdmin user created successfully.", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SuperAdmin user: {Email}", email);
            return (false, $"Failed to create user: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update SuperAdmin user
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAsync(
        Guid userId,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        bool? isActive,
        List<SuperAdminPermission>? permissions,
        int? sessionTimeoutMinutes,
        Guid modifiedBySuperAdminId,
        string modifiedBySuperAdminEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return (false, "User not found.");
            }

            // Store old values for audit
            var oldValues = new
            {
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.IsActive,
                user.SessionTimeoutMinutes,
                Permissions = string.IsNullOrEmpty(user.Permissions)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(user.Permissions)
            };

            // Update fields
            if (!string.IsNullOrWhiteSpace(firstName))
                user.FirstName = firstName;

            if (!string.IsNullOrWhiteSpace(lastName))
                user.LastName = lastName;

            if (phoneNumber != null)
                user.PhoneNumber = phoneNumber;

            if (isActive.HasValue)
                user.IsActive = isActive.Value;

            if (sessionTimeoutMinutes.HasValue)
                user.SessionTimeoutMinutes = sessionTimeoutMinutes.Value;

            if (permissions != null)
            {
                user.Permissions = JsonSerializer.Serialize(permissions.Select(p => p.ToString()).ToList());
            }

            user.LastModifiedBySuperAdminId = modifiedBySuperAdminId;
            user.UpdatedAt = DateTime.UtcNow;

            await _masterDbContext.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.RECORD_UPDATED,
                modifiedBySuperAdminId,
                modifiedBySuperAdminEmail,
                targetTenantId: null,
                targetTenantName: null,
                description: $"Updated SuperAdmin user: {user.Email}",
                oldValues: JsonSerializer.Serialize(oldValues),
                newValues: JsonSerializer.Serialize(new
                {
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    user.IsActive,
                    user.SessionTimeoutMinutes,
                    Permissions = string.IsNullOrEmpty(user.Permissions)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(user.Permissions)
                }),
                success: true);

            _logger.LogInformation("SuperAdmin user updated: {Email} by {ModifiedBy}", user.Email, modifiedBySuperAdminEmail);

            return (true, "User updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SuperAdmin user: {UserId}", userId);
            return (false, $"Failed to update user: {ex.Message}");
        }
    }

    /// <summary>
    /// Change SuperAdmin password
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(
        Guid userId,
        string newPassword,
        Guid modifiedBySuperAdminId,
        string modifiedBySuperAdminEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return (false, "User not found.");
            }

            // Hash new password
            var passwordHash = _passwordHasher.HashPassword(newPassword);

            // Update password history (keep last 5)
            var passwordHistory = string.IsNullOrEmpty(user.PasswordHistory)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(user.PasswordHistory) ?? new List<string>();

            passwordHistory.Insert(0, user.PasswordHash);
            if (passwordHistory.Count > 5)
            {
                passwordHistory = passwordHistory.Take(5).ToList();
            }

            user.PasswordHash = passwordHash;
            user.PasswordHistory = JsonSerializer.Serialize(passwordHistory);
            user.LastPasswordChangeDate = DateTime.UtcNow;
            user.PasswordExpiresAt = DateTime.UtcNow.AddDays(90);
            user.MustChangePassword = false;
            user.LastModifiedBySuperAdminId = modifiedBySuperAdminId;
            user.UpdatedAt = DateTime.UtcNow;

            await _masterDbContext.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.PASSWORD_CHANGED,
                modifiedBySuperAdminId,
                modifiedBySuperAdminEmail,
                targetTenantId: null,
                targetTenantName: null,
                description: $"Password changed for SuperAdmin user: {user.Email}",
                oldValues: null,
                newValues: null,
                success: true);

            _logger.LogInformation("SuperAdmin password changed: {Email} by {ModifiedBy}", user.Email, modifiedBySuperAdminEmail);

            return (true, "Password changed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password for SuperAdmin user: {UserId}", userId);
            return (false, $"Failed to change password: {ex.Message}");
        }
    }

    /// <summary>
    /// Soft delete SuperAdmin user
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAsync(
        Guid userId,
        Guid deletedBySuperAdminId,
        string deletedBySuperAdminEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return (false, "User not found.");
            }

            // Prevent deleting initial setup account
            if (user.IsInitialSetupAccount)
            {
                return (false, "Cannot delete the initial setup account.");
            }

            // Prevent self-deletion
            if (user.Id == deletedBySuperAdminId)
            {
                return (false, "You cannot delete your own account.");
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _masterDbContext.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.SUPERADMIN_DELETED,
                deletedBySuperAdminId,
                deletedBySuperAdminEmail,
                targetTenantId: null,
                targetTenantName: null,
                description: $"Deleted SuperAdmin user: {user.Email}",
                oldValues: JsonSerializer.Serialize(new { user.Email, user.IsActive, user.IsDeleted }),
                newValues: JsonSerializer.Serialize(new { user.Email, IsActive = false, IsDeleted = true }),
                success: true);

            _logger.LogInformation("SuperAdmin user deleted: {Email} by {DeletedBy}", user.Email, deletedBySuperAdminEmail);

            return (true, "User deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete SuperAdmin user: {UserId}", userId);
            return (false, $"Failed to delete user: {ex.Message}");
        }
    }

    /// <summary>
    /// Update SuperAdmin permissions
    /// </summary>
    public async Task<(bool Success, string Message)> UpdatePermissionsAsync(
        Guid userId,
        List<SuperAdminPermission> permissions,
        Guid modifiedBySuperAdminId,
        string modifiedBySuperAdminEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return (false, "User not found.");
            }

            var oldPermissions = string.IsNullOrEmpty(user.Permissions)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(user.Permissions);

            user.Permissions = JsonSerializer.Serialize(permissions.Select(p => p.ToString()).ToList());
            user.LastModifiedBySuperAdminId = modifiedBySuperAdminId;
            user.UpdatedAt = DateTime.UtcNow;

            await _masterDbContext.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.SUPERADMIN_PERMISSION_CHANGED,
                modifiedBySuperAdminId,
                modifiedBySuperAdminEmail,
                targetTenantId: null,
                targetTenantName: null,
                description: $"Updated permissions for SuperAdmin user: {user.Email}",
                oldValues: JsonSerializer.Serialize(new { Permissions = oldPermissions }),
                newValues: JsonSerializer.Serialize(new { Permissions = permissions.Select(p => p.ToString()).ToList() }),
                success: true);

            _logger.LogInformation("SuperAdmin permissions updated: {Email} by {ModifiedBy}", user.Email, modifiedBySuperAdminEmail);

            return (true, "Permissions updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update permissions for SuperAdmin user: {UserId}", userId);
            return (false, $"Failed to update permissions: {ex.Message}");
        }
    }

    /// <summary>
    /// Unlock SuperAdmin account
    /// </summary>
    public async Task<(bool Success, string Message)> UnlockAccountAsync(
        Guid userId,
        Guid unlockedBySuperAdminId,
        string unlockedBySuperAdminEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return (false, "User not found.");
            }

            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.UpdatedAt = DateTime.UtcNow;

            await _masterDbContext.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.SUPERADMIN_UNLOCKED_ACCOUNT,
                unlockedBySuperAdminId,
                unlockedBySuperAdminEmail,
                targetTenantId: null,
                targetTenantName: null,
                description: $"Unlocked SuperAdmin account: {user.Email}",
                oldValues: null,
                newValues: null,
                success: true);

            _logger.LogInformation("SuperAdmin account unlocked: {Email} by {UnlockedBy}", user.Email, unlockedBySuperAdminEmail);

            return (true, "Account unlocked successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock SuperAdmin account: {UserId}", userId);
            return (false, $"Failed to unlock account: {ex.Message}");
        }
    }

    /// <summary>
    /// Get SuperAdmin activity logs
    /// </summary>
    public async Task<List<AuditLog>> GetActivityLogsAsync(
        Guid userId,
        int pageIndex = 0,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await _masterDbContext.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.PerformedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
