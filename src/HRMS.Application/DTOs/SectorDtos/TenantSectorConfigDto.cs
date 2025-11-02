namespace HRMS.Application.DTOs.SectorDtos;

/// <summary>
/// Tenant's sector configuration
/// </summary>
public class TenantSectorConfigDto
{
    public Guid Id { get; set; }
    public int SectorId { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public string SectorCode { get; set; } = string.Empty;
    public DateTime SelectedAt { get; set; }
    public Guid? SelectedByUserId { get; set; }
    public string? SelectedByUserName { get; set; }
    public string? Notes { get; set; }

    // Compliance rules summary
    public int TotalComplianceRules { get; set; }
    public int CustomizedRules { get; set; }
    public int DefaultRules { get; set; }
}
