using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.LocationDtos;

/// <summary>
/// DTO for creating a new location
/// </summary>
public class CreateLocationDto
{
    [Required]
    [StringLength(50)]
    public string LocationCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string LocationName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? LocationType { get; set; }

    // Address
    [StringLength(500)]
    public string? AddressLine1 { get; set; }

    [StringLength(500)]
    public string? AddressLine2 { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    [StringLength(100)]
    public string? District { get; set; }  // Mauritius districts: Port Louis, Pamplemousses, etc.

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = "Mauritius";

    // Contact
    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    // Working Hours (JSON string)
    public string? WorkingHoursJson { get; set; }

    [Required]
    [StringLength(100)]
    public string Timezone { get; set; } = "Indian/Mauritius";

    // Management
    public Guid? LocationManagerId { get; set; }

    [Range(1, 10000)]
    public int? CapacityHeadcount { get; set; }

    // Geographic coordinates
    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    public bool IsActive { get; set; } = true;
}
