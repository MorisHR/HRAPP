using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

/// <summary>
/// Request DTO for employee password setup from welcome email
/// FORTUNE 500: Used for initial password setup after tenant activation
/// </summary>
public class SetEmployeePasswordRequest
{
    [Required(ErrorMessage = "Password reset token is required")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subdomain is required")]
    public string Subdomain { get; set; } = string.Empty;
}
