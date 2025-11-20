using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// DTO for creating a new department with comprehensive validation
/// </summary>
public class CreateDepartmentDto : IValidatableObject
{
    [Required(ErrorMessage = "Department name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Department name must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-&().,]+$", ErrorMessage = "Department name contains invalid characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department code is required")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Department code must be between 2 and 20 characters")]
    [RegularExpression(@"^[A-Z0-9_-]+$", ErrorMessage = "Department code must be uppercase alphanumeric with hyphens or underscores only")]
    public string Code { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-&().,;:!?'""]+$", ErrorMessage = "Description contains invalid characters")]
    public string? Description { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    public Guid? DepartmentHeadId { get; set; }

    [StringLength(50, ErrorMessage = "Cost center code cannot exceed 50 characters")]
    [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Cost center code must be uppercase alphanumeric with hyphens only")]
    public string? CostCenterCode { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Custom validation logic
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Reserved keywords check
        var reservedKeywords = new[] { "ADMIN", "SYSTEM", "ROOT", "SUPERADMIN", "SA", "DBA" };
        if (reservedKeywords.Contains(Code?.ToUpper()))
        {
            yield return new ValidationResult(
                $"Department code '{Code}' is a reserved keyword and cannot be used",
                new[] { nameof(Code) });
        }

        // Sanitize description for XSS prevention
        if (!string.IsNullOrWhiteSpace(Description))
        {
            var dangerousPatterns = new[] { "<script", "javascript:", "onerror=", "onclick=", "<iframe" };
            foreach (var pattern in dangerousPatterns)
            {
                if (Description.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new ValidationResult(
                        "Description contains potentially dangerous content",
                        new[] { nameof(Description) });
                    break;
                }
            }
        }
    }
}
