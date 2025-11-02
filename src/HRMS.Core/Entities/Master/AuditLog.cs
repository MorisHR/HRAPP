namespace HRMS.Core.Entities.Master;

/// <summary>
/// System-level audit log - stored in Master schema
/// Tracks all critical system-wide operations
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalInfo { get; set; }
}
