namespace HRMS.Application.DTOs.LocationDtos;

/// <summary>
/// Location summary for list views with statistics
/// </summary>
public class LocationSummaryDto
{
    public Guid Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? LocationType { get; set; }

    // Address summary
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Region { get; set; }
    public string Country { get; set; } = "Mauritius";

    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // Management
    public Guid? LocationManagerId { get; set; }
    public string? LocationManagerName { get; set; }
    public int? CapacityHeadcount { get; set; }

    // Statistics
    public int DeviceCount { get; set; }
    public int EmployeeCount { get; set; }

    // Status
    public bool IsActive { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
