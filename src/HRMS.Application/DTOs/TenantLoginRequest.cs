using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

/// <summary>
/// Login request for tenant employees
/// </summary>
public class TenantLoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subdomain is required")]
    public string Subdomain { get; set; } = string.Empty;
}
