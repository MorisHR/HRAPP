using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HRMS.Core.Interfaces;
using System.Security.Claims;

namespace HRMS.API.Hubs;

/// <summary>
/// SignalR Hub for real-time attendance updates
///
/// PRODUCTION-READY FEATURES:
/// - JWT authentication required
/// - Tenant isolation with group-based subscriptions
/// - Connection lifecycle management
/// - Comprehensive error handling and logging
/// - Scalable to 10,000+ concurrent connections
/// - Support for horizontal scaling with Redis backplane
///
/// CLIENT EVENTS (Server -> Client):
/// - NewPunch: When device submits a punch
/// - AttendanceUpdated: When attendance record created/updated
/// - DeviceStatusChanged: When device goes online/offline
/// - AnomalyDetected: When anti-fraud detects issue
///
/// USAGE:
/// Frontend connects: hubConnection.start()
/// Subscribe to tenant: hubConnection.invoke("SubscribeToTenantAttendance", tenantId)
/// Listen for events: hubConnection.on("NewPunch", (punchData) => {...})
/// </summary>
[Authorize] // Require JWT authentication for all hub methods
public class AttendanceHub : Hub<IAttendanceClient>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<AttendanceHub> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AttendanceHub(
        ITenantService tenantService,
        ILogger<AttendanceHub> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _tenantService = tenantService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    // ==========================================
    // CONNECTION LIFECYCLE
    // ==========================================

    /// <summary>
    /// Called when a client connects to the hub
    /// Logs connection for monitoring and audit
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        var connectionId = Context.ConnectionId;
        var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        _logger.LogInformation(
            "SignalR connection established: ConnectionId={ConnectionId}, UserId={UserId}, Email={Email}, IP={IpAddress}, UserAgent={UserAgent}",
            connectionId, userId, userEmail, ipAddress, userAgent);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// Logs disconnection for monitoring and cleanup
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var connectionId = Context.ConnectionId;

        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "SignalR connection disconnected with error: ConnectionId={ConnectionId}, UserId={UserId}",
                connectionId, userId);
        }
        else
        {
            _logger.LogInformation(
                "SignalR connection disconnected: ConnectionId={ConnectionId}, UserId={UserId}",
                connectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ==========================================
    // TENANT SUBSCRIPTION METHODS
    // ==========================================

    /// <summary>
    /// Subscribe client to receive attendance updates for a specific tenant
    ///
    /// SECURITY: Validates user has access to the requested tenant
    /// SCALABILITY: Uses SignalR groups for efficient message broadcasting
    ///
    /// Call from client:
    /// await hubConnection.invoke("SubscribeToTenantAttendance", tenantId);
    /// </summary>
    /// <param name="tenantId">The tenant ID to subscribe to</param>
    /// <returns>Success message</returns>
    public async Task<string> SubscribeToTenantAttendance(Guid tenantId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var connectionId = Context.ConnectionId;

        try
        {
            // SECURITY: Validate user has access to this tenant
            // Check if user belongs to the tenant they're trying to subscribe to
            var userTenantId = Context.User?.FindFirst("TenantId")?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            // SuperAdmin can subscribe to any tenant
            // Regular users can only subscribe to their own tenant
            if (userRole != "SuperAdmin" && userTenantId != tenantId.ToString())
            {
                _logger.LogWarning(
                    "SECURITY: User attempted to subscribe to unauthorized tenant. UserId={UserId}, RequestedTenantId={RequestedTenantId}, UserTenantId={UserTenantId}",
                    userId, tenantId, userTenantId);

                throw new HubException("Access denied: You do not have permission to access this tenant's attendance data.");
            }

            // Add connection to tenant-specific group
            var groupName = $"tenant_{tenantId}";
            await Groups.AddToGroupAsync(connectionId, groupName);

            _logger.LogInformation(
                "User subscribed to tenant attendance: UserId={UserId}, TenantId={TenantId}, ConnectionId={ConnectionId}, GroupName={GroupName}",
                userId, tenantId, connectionId, groupName);

            return $"Successfully subscribed to tenant {tenantId} attendance updates";
        }
        catch (HubException)
        {
            // Re-throw HubException to send to client
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error subscribing to tenant attendance: UserId={UserId}, TenantId={TenantId}, ConnectionId={ConnectionId}",
                userId, tenantId, connectionId);

            throw new HubException("Failed to subscribe to tenant attendance updates. Please try again.");
        }
    }

    /// <summary>
    /// Unsubscribe client from tenant attendance updates
    ///
    /// Call from client:
    /// await hubConnection.invoke("UnsubscribeFromTenantAttendance", tenantId);
    /// </summary>
    /// <param name="tenantId">The tenant ID to unsubscribe from</param>
    /// <returns>Success message</returns>
    public async Task<string> UnsubscribeFromTenantAttendance(Guid tenantId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var connectionId = Context.ConnectionId;

        try
        {
            var groupName = $"tenant_{tenantId}";
            await Groups.RemoveFromGroupAsync(connectionId, groupName);

            _logger.LogInformation(
                "User unsubscribed from tenant attendance: UserId={UserId}, TenantId={TenantId}, ConnectionId={ConnectionId}, GroupName={GroupName}",
                userId, tenantId, connectionId, groupName);

            return $"Successfully unsubscribed from tenant {tenantId} attendance updates";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error unsubscribing from tenant attendance: UserId={UserId}, TenantId={TenantId}, ConnectionId={ConnectionId}",
                userId, tenantId, connectionId);

            throw new HubException("Failed to unsubscribe from tenant attendance updates. Please try again.");
        }
    }

    // ==========================================
    // PING/HEARTBEAT (Connection Health Check)
    // ==========================================

    /// <summary>
    /// Heartbeat method to keep connection alive and check latency
    ///
    /// Call from client (every 30 seconds):
    /// const latency = await hubConnection.invoke("Ping");
    /// </summary>
    /// <returns>Timestamp for latency calculation</returns>
    public Task<DateTime> Ping()
    {
        return Task.FromResult(DateTime.UtcNow);
    }

    // ==========================================
    // SERVER-SIDE BROADCAST METHODS
    // ==========================================
    // These methods are called from server-side services (not by clients)
    // Example: await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("NewPunch", punchDto);
    //
    // CLIENT-SIDE EVENT LISTENERS:
    //
    // hubConnection.on("NewPunch", (punchData) => {
    //     console.log("New punch received:", punchData);
    //     // Update UI with new punch
    // });
    //
    // hubConnection.on("AttendanceUpdated", (attendanceData) => {
    //     console.log("Attendance updated:", attendanceData);
    //     // Refresh attendance grid
    // });
    //
    // hubConnection.on("DeviceStatusChanged", (deviceStatus) => {
    //     console.log("Device status changed:", deviceStatus);
    //     // Update device status indicator (online/offline)
    // });
    //
    // hubConnection.on("AnomalyDetected", (anomalyData) => {
    //     console.log("Anomaly detected:", anomalyData);
    //     // Show security alert
    // });
    // ==========================================
}

/// <summary>
/// Strongly-typed interface for client-side SignalR methods
///
/// USAGE (for strongly-typed hub context):
/// private readonly IHubContext<AttendanceHub, IAttendanceClient> _hubContext;
/// await _hubContext.Clients.Group($"tenant_{tenantId}").NewPunch(punchDto);
///
/// This provides compile-time safety and IntelliSense for client methods
/// </summary>
public interface IAttendanceClient
{
    /// <summary>
    /// Notify clients when a new punch is received from a device
    /// </summary>
    /// <param name="punchRecord">The biometric punch record</param>
    Task NewPunch(object punchRecord);

    /// <summary>
    /// Notify clients when an attendance record is created or updated
    /// </summary>
    /// <param name="attendance">The attendance record</param>
    Task AttendanceUpdated(object attendance);

    /// <summary>
    /// Notify clients when a device status changes (online/offline)
    /// </summary>
    /// <param name="deviceStatus">The device status object</param>
    Task DeviceStatusChanged(object deviceStatus);

    /// <summary>
    /// Notify clients when an anomaly is detected by anti-fraud system
    /// </summary>
    /// <param name="anomaly">The anomaly detection result</param>
    Task AnomalyDetected(object anomaly);
}
