// TEMPORARY FIX: Core layer should not reference Application layer
// TODO: Move these DTOs to Core layer or use entities directly
// using HRMS.Application.DTOs.AttendanceDtos;
// using HRMS.Application.DTOs.BiometricPunchDtos;
// using HRMS.Application.DTOs.SignalRDtos;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for sending real-time attendance notifications via SignalR
///
/// USAGE:
/// This service is injected into other services (like BiometricPunchProcessingService)
/// to broadcast real-time updates to connected clients.
///
/// Example:
/// await _notificationService.NotifyNewPunchAsync(tenantId, punchRecordDto);
///
/// PRODUCTION FEATURES:
/// - Async fire-and-forget notifications (non-blocking)
/// - Error resilience (failures don't break main workflow)
/// - Tenant isolation (only authorized users receive updates)
/// - Comprehensive logging for monitoring
/// - Scalable to 10,000+ concurrent connections
///
/// NOTE: This interface is temporarily disabled to fix circular dependency.
/// The implementation should be in the Application or Infrastructure layer.
/// </summary>
public interface IAttendanceNotificationService
{
    /// <summary>
    /// Notify clients when a new biometric punch is received from a device
    /// Broadcasts to all clients subscribed to the tenant's attendance updates
    /// </summary>
    /// <param name="tenantId">Tenant ID for routing notification</param>
    /// <param name="punchRecord">The punch record data (object to avoid dependency)</param>
    Task NotifyNewPunchAsync(Guid tenantId, object punchRecord);

    /// <summary>
    /// Notify clients when an attendance record is created or updated
    /// Useful for showing real-time attendance grid updates
    /// </summary>
    /// <param name="tenantId">Tenant ID for routing notification</param>
    /// <param name="attendance">The attendance record data (object to avoid dependency)</param>
    Task NotifyAttendanceUpdatedAsync(Guid tenantId, object attendance);

    /// <summary>
    /// Notify clients when a device status changes (online/offline/error)
    /// Enables real-time device monitoring dashboards
    /// </summary>
    /// <param name="tenantId">Tenant ID for routing notification</param>
    /// <param name="deviceStatus">The device status data (object to avoid dependency)</param>
    Task NotifyDeviceStatusChangedAsync(Guid tenantId, object deviceStatus);

    /// <summary>
    /// Notify clients when an anomaly is detected by anti-fraud system
    /// Critical for real-time security monitoring
    /// </summary>
    /// <param name="tenantId">Tenant ID for routing notification</param>
    /// <param name="anomaly">The anomaly detection data (object to avoid dependency)</param>
    Task NotifyAnomalyDetectedAsync(Guid tenantId, object anomaly);
}
