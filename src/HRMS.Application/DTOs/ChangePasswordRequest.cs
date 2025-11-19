using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

/// <summary>
/// Request model for changing password (authenticated users)
/// FORTUNE 500: Comprehensive password change with security validation
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Current password for verification
    /// SECURITY: Prevents unauthorized password changes if session hijacked
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// SECURITY: Must meet complexity requirements (validated by backend)
    /// - Minimum 12 characters
    /// - At least one uppercase, lowercase, number, and special character
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// SECURITY: Prevents typos in password entry
    /// </summary>
    [Required(ErrorMessage = "Please confirm your new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response model for password change operations
/// </summary>
public class ChangePasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
