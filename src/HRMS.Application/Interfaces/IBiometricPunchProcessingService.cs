using HRMS.Application.DTOs.BiometricPunchDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Core intelligence of the biometric attendance capture system
/// Processes raw punches from biometric devices and creates attendance records
///
/// CRITICAL RESPONSIBILITIES:
/// - Validates device authorization and employee access
/// - Detects duplicate punches (anti-fraud)
/// - Resolves device user IDs to employee IDs
/// - Creates tamper-proof hash chain audit trail
/// - Processes punches into attendance records
/// - Handles error states and reprocessing
///
/// This is the MOST CRITICAL service in the attendance system
/// </summary>
public interface IBiometricPunchProcessingService
{
    /// <summary>
    /// Process a biometric punch from a device and create/update attendance record
    /// This is the CORE method that powers the entire attendance capture system
    ///
    /// PROCESSING FLOW:
    /// 1. Validate device exists and is active
    /// 2. Resolve DeviceUserId to EmployeeId
    /// 3. Check employee device access authorization
    /// 4. Detect duplicate punches (15-minute window)
    /// 5. Create BiometricPunchRecord with tamper-proof hash
    /// 6. Process into Attendance record (CheckIn/CheckOut)
    /// 7. Calculate working hours if both times present
    /// 8. Handle errors and mark status appropriately
    ///
    /// ANTI-FRAUD FEATURES:
    /// - Duplicate detection (same employee + device + punch type + 15-min window)
    /// - Device authorization validation (EmployeeDeviceAccess table)
    /// - Verification quality threshold (minimum 70%)
    /// - Daily punch limit (max 10 punches/day per employee)
    /// - Tamper-proof hash chain (SHA-256)
    /// </summary>
    /// <param name="punchDto">Punch data from biometric device</param>
    /// <param name="deviceId">ID of the device (resolved from API key or serial number)</param>
    /// <param name="tenantId">Tenant ID for multi-tenancy</param>
    /// <returns>Result indicating success/failure with details</returns>
    Task<PunchProcessingResultDto> ProcessPunchAsync(
        DevicePunchCaptureDto punchDto,
        Guid deviceId,
        Guid tenantId);

    /// <summary>
    /// Get a single punch record by ID
    /// Used for viewing punch details and troubleshooting
    /// </summary>
    /// <param name="id">Punch record ID</param>
    /// <returns>Punch record DTO or null if not found</returns>
    Task<BiometricPunchRecordDto?> GetPunchRecordAsync(Guid id);

    /// <summary>
    /// Get all pending (unprocessed) punch records for a tenant
    /// Used for monitoring and manual reprocessing
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>List of pending punch records</returns>
    Task<List<BiometricPunchRecordDto>> GetPendingPunchesAsync(Guid tenantId);

    /// <summary>
    /// Get all punch records for an employee within a date range
    /// Used for employee attendance history and audit trails
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="fromDate">Start date (inclusive)</param>
    /// <param name="toDate">End date (inclusive)</param>
    /// <returns>List of punch records ordered by punch time</returns>
    Task<List<BiometricPunchRecordDto>> GetEmployeePunchesAsync(
        Guid employeeId,
        DateTime fromDate,
        DateTime toDate);

    /// <summary>
    /// Reprocess all failed punch records for a tenant
    /// Useful when there was a temporary error (e.g., employee not yet enrolled)
    ///
    /// This method finds all punches with ProcessingStatus = "Failed" and attempts
    /// to process them again. Common scenarios:
    /// - Employee was added to system after punch was recorded
    /// - Device access was granted after punch was recorded
    /// - Temporary database connectivity issue
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Task</returns>
    Task ReprocessFailedPunchesAsync(Guid tenantId);
}
