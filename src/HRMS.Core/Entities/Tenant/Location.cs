using HRMS.Core.Entities;
using System.Text.Json;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Physical work locations (factories, offices, warehouses, branches)
/// where employees work and biometric devices are installed
/// </summary>
public class Location : BaseEntity
{
    // Basic Information
    public string LocationCode { get; set; } = string.Empty;  // "FAC", "OFF", "WH"
    public string LocationName { get; set; } = string.Empty;  // "Factory - Pont Fer"
    public string? LocationType { get; set; }  // "Factory", "Office", "Warehouse", "Branch"

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }  // Mauritius districts: Port Louis, Pamplemousses, etc.
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "Mauritius";

    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // Working Hours (JSON format for flexibility)
    // Example: {"monday": {"start": "08:00", "end": "17:00"}, "tuesday": {...}}
    public string? WorkingHoursJson { get; set; }
    public string Timezone { get; set; } = "Indian/Mauritius";

    // Management
    public Guid? LocationManagerId { get; set; }
    public Employee? LocationManager { get; set; }
    public int? CapacityHeadcount { get; set; }

    // Geographic coordinates (for distance calculations)
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<AttendanceMachine> BiometricDevices { get; set; } = new List<AttendanceMachine>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
