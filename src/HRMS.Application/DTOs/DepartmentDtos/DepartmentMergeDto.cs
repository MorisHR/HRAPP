using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// DTO for merging two departments
/// </summary>
public class DepartmentMergeDto
{
    /// <summary>
    /// Source department ID (will be deactivated or deleted after merge)
    /// </summary>
    [Required]
    public Guid SourceDepartmentId { get; set; }

    /// <summary>
    /// Target department ID (will receive all employees and sub-departments)
    /// </summary>
    [Required]
    public Guid TargetDepartmentId { get; set; }

    /// <summary>
    /// Whether to delete the source department after merge (default: deactivate only)
    /// </summary>
    public bool DeleteSource { get; set; } = false;

    /// <summary>
    /// Reason for merge (for audit trail)
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result of department merge operation
/// </summary>
public class DepartmentMergeResultDto
{
    public Guid SourceDepartmentId { get; set; }
    public string SourceDepartmentName { get; set; } = string.Empty;
    public Guid TargetDepartmentId { get; set; }
    public string TargetDepartmentName { get; set; } = string.Empty;
    public int EmployeesMoved { get; set; }
    public int SubDepartmentsMoved { get; set; }
    public bool SourceDeleted { get; set; }
    public DateTime MergedAt { get; set; }
    public string MergedBy { get; set; } = string.Empty;
}
