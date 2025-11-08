using System.Text.Json;

namespace HRMS.Application.DTOs.LocationDtos;

/// <summary>
/// Complete location information for detail view
/// </summary>
public class LocationDto
{
    public Guid Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? LocationType { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }  // Mauritius districts
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "Mauritius";

    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // Working Hours
    public string? WorkingHoursJson { get; set; }
    public string Timezone { get; set; } = "Indian/Mauritius";

    // Management
    public Guid? LocationManagerId { get; set; }
    public string? LocationManagerName { get; set; }
    public int? CapacityHeadcount { get; set; }

    // Geographic coordinates
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Statistics
    public int DeviceCount { get; set; }
    public int EmployeeCount { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
