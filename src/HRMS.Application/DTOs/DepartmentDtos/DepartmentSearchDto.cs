namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// DTO for advanced department search with filtering, sorting, and pagination
/// </summary>
public class DepartmentSearchDto
{
    /// <summary>
    /// Search query - searches in name, code, and description
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by parent department ID
    /// </summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>
    /// Filter departments without parent (root departments only)
    /// </summary>
    public bool? RootOnly { get; set; }

    /// <summary>
    /// Filter by cost center code
    /// </summary>
    public string? CostCenterCode { get; set; }

    /// <summary>
    /// Filter by whether department has a head assigned
    /// </summary>
    public bool? HasHead { get; set; }

    /// <summary>
    /// Minimum employee count
    /// </summary>
    public int? MinEmployeeCount { get; set; }

    /// <summary>
    /// Maximum employee count
    /// </summary>
    public int? MaxEmployeeCount { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size (max 100)
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort by field (Name, Code, EmployeeCount, CreatedAt, UpdatedAt)
    /// </summary>
    public string SortBy { get; set; } = "Name";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// Search result with pagination metadata
/// </summary>
public class DepartmentSearchResultDto
{
    public List<DepartmentDto> Departments { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
