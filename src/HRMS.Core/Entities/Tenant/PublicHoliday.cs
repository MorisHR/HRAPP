using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Public holidays (Mauritius and tenant-specific)
/// </summary>
public class PublicHoliday : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Year { get; set; }
    public HolidayType Type { get; set; } = HolidayType.NationalHoliday;
    public string? Description { get; set; }
    public bool IsRecurring { get; set; } = true;
    public string? Country { get; set; } = "Mauritius";
    public bool IsActive { get; set; } = true;
}
