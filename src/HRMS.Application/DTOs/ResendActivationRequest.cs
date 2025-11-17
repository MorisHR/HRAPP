using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

/// <summary>
/// Request DTO for resending tenant activation email
/// FORTUNE 500 PATTERN: Multi-tenant SaaS activation resend
/// SECURITY: Email + Subdomain validation prevents unauthorized resend attempts
/// RATE LIMITING: Enforced at controller level (3 per hour per tenant)
/// </summary>
public class ResendActivationRequest
{
    /// <summary>
    /// Company subdomain (e.g., "acme" for acme.hrms.com)
    /// REQUIRED: Identifies which tenant needs activation resend
    /// VALIDATION: Must match existing tenant subdomain
    /// </summary>
    [Required(ErrorMessage = "Company subdomain is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Subdomain must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Subdomain can only contain lowercase letters, numbers, and hyphens")]
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address for the tenant
    /// SECURITY: Must match the original registration email
    /// VALIDATION: Prevents unauthorized users from triggering resend
    /// </summary>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
    public string Email { get; set; } = string.Empty;
}
