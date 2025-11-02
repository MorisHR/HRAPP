using HRMS.Core.Entities.Master;

namespace HRMS.Core.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Authenticates a super admin user by email and password
    /// Returns (token, expiresAt, adminUser) on success, null on failure
    /// </summary>
    Task<(string Token, DateTime ExpiresAt, AdminUser User)?> LoginAsync(string email, string password);

    /// <summary>
    /// Generates a JWT token for an admin user
    /// </summary>
    string GenerateJwtToken(Guid userId, string email, string userName);

    /// <summary>
    /// Validates a JWT token and returns the user ID
    /// </summary>
    Guid? ValidateToken(string token);

    /// <summary>
    /// Unlocks a locked user account (Admin/SuperAdmin only)
    /// </summary>
    Task<bool> UnlockAccountAsync(Guid userId);
}
