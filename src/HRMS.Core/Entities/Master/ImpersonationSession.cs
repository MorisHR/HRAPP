using System;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Impersonation session entity
/// Tracks when SuperAdmins impersonate tenant users for support/troubleshooting
/// </summary>
public class ImpersonationSession
{
    public string Id { get; set; } = null!;
    public string AdminUserId { get; set; } = null!;
    public Guid TargetUserId { get; set; }
    public string TargetUserEmail { get; set; } = null!;
    public string TargetUserName { get; set; } = null!;
    public Guid? TenantId { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
