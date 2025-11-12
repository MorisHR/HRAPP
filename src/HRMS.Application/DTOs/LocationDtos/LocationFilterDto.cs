using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.LocationDtos;

/// <summary>
/// Filter DTO for querying locations with various criteria
/// </summary>
public class LocationFilterDto
{
    /// <summary>
    /// Filter by district (Mauritius has 9 districts)
    /// </summary>
    [StringLength(100)]
    public string? District { get; set; }

    /// <summary>
    /// Filter by location type (City, Town, Village)
    /// </summary>
    [StringLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// Filter by region
    /// </summary>
    [StringLength(100)]
    public string? Region { get; set; }

    /// <summary>
    /// Filter by postal code
    /// </summary>
    [StringLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Filter by active status (null = all, true = active only, false = inactive only)
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Search term for name/code (case-insensitive partial match)
    /// </summary>
    [StringLength(200)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by location manager
    /// </summary>
    public Guid? LocationManagerId { get; set; }

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination (max 100)
    /// </summary>
    [Range(1, 100)]
    public int? PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field (LocationName, LocationCode, District, City, CreatedAt)
    /// </summary>
    [StringLength(50)]
    public string? SortBy { get; set; } = "LocationName";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDirection must be 'asc' or 'desc'")]
    public string? SortDirection { get; set; } = "asc";
}
