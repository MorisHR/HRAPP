using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using HRMS.Application.DTOs.DeviceWebhookDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Device Webhook Service - Processes push data from biometric devices
/// Implements Fortune 500-grade IoT device push architecture
/// </summary>
public class DeviceWebhookService : IDeviceWebhookService
{
    private readonly ILogger<DeviceWebhookService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEncryptionService _encryptionService;

    public DeviceWebhookService(
        ILogger<DeviceWebhookService> logger,
        IConfiguration configuration,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _configuration = configuration;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Process attendance data pushed from a biometric device
    /// </summary>
    public async Task<DeviceWebhookResponseDto> ProcessAttendanceDataAsync(DevicePushAttendanceDto dto)
    {
        var response = new DeviceWebhookResponseDto
        {
            ProcessedAt = DateTime.UtcNow
        };

        try
        {
            // 1. Validate device API key and get device info
            var (isValid, tenantId, deviceGuid) = await ValidateDeviceApiKeyAsync(dto.DeviceId, dto.ApiKey);

            if (!isValid || !tenantId.HasValue || !deviceGuid.HasValue)
            {
                throw new UnauthorizedAccessException("Invalid device credentials");
            }

            _logger.LogInformation(
                "🔓 Device authenticated: DeviceId={DeviceId}, TenantId={TenantId}",
                dto.DeviceId,
                tenantId.Value);

            // 2. Get tenant-specific database context
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var tenantSchema = await GetTenantSchemaAsync(tenantId.Value);
            var dbContext = CreateTenantDbContext(connectionString!, tenantSchema);
            await using var _ = dbContext;

            // Get device entity
            var device = await dbContext.AttendanceMachines
                .FirstOrDefaultAsync(d => d.Id == deviceGuid.Value);

            if (device == null)
            {
                throw new InvalidOperationException("Device not found");
            }

            // 3. Process each attendance record as BiometricPunchRecord
            var processedCount = 0;
            var skippedCount = 0;
            var errors = new List<string>();

            foreach (var record in dto.Records)
            {
                try
                {
                    // Check for duplicate by punch time and employee
                    var isDuplicate = await dbContext.BiometricPunchRecords
                        .AnyAsync(p =>
                            p.DeviceId == deviceGuid.Value &&
                            p.PunchTime == record.PunchTime &&
                            p.DeviceUserId == record.EmployeeId);

                    if (isDuplicate)
                    {
                        skippedCount++;
                        _logger.LogDebug(
                            "⏭️ Skipping duplicate record: EmployeeId={EmployeeId}, PunchTime={PunchTime}",
                            record.EmployeeId,
                            record.PunchTime);
                        continue;
                    }

                    // Map employee ID from device to internal employee
                    var employee = await dbContext.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeCode == record.EmployeeId);

                    // Create biometric punch record
                    var punchRecord = new BiometricPunchRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId.Value,
                        DeviceId = deviceGuid.Value,
                        DeviceUserId = record.EmployeeId,
                        DeviceSerialNumber = device.SerialNumber ?? device.DeviceCode,
                        EmployeeId = employee?.Id,
                        PunchTime = record.PunchTime,
                        PunchType = MapPunchType(record.PunchType),
                        VerificationMethod = MapVerifyMode(record.VerifyMode),
                        VerificationQuality = 100, // Default quality
                        ProcessingStatus = "Pending",
                        HashChain = GenerateHashChain(deviceGuid.Value, record.EmployeeId, record.PunchTime),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    dbContext.BiometricPunchRecords.Add(punchRecord);
                    processedCount++;

                    _logger.LogDebug(
                        "✅ Created punch record: Employee={EmployeeId}, Time={PunchTime}, Type={PunchType}",
                        record.EmployeeId,
                        record.PunchTime,
                        punchRecord.PunchType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "⚠️ Error processing punch record for employee {EmployeeId}",
                        record.EmployeeId);
                    errors.Add($"Error processing record for {record.EmployeeId}: {ex.Message}");
                    skippedCount++;
                }
            }

            // 4. Update device sync status
            device.LastSyncTime = DateTime.UtcNow;
            device.LastSyncAt = DateTime.UtcNow;
            device.LastSyncStatus = processedCount > 0 ? "Success" : "No New Records";
            device.LastSyncRecordCount = processedCount;
            device.DeviceStatus = "Active";
            device.UpdatedAt = DateTime.UtcNow;

            // 5. Create sync log
            var syncLog = new DeviceSyncLog
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceGuid.Value,
                SyncStartTime = dto.Timestamp,
                SyncEndTime = DateTime.UtcNow,
                SyncDurationSeconds = (int)(DateTime.UtcNow - dto.Timestamp).TotalSeconds,
                SyncStatus = processedCount > 0 ? "Success" : "NoNewRecords",
                RecordsFetched = dto.Records.Count,
                RecordsProcessed = processedCount,
                RecordsSkipped = skippedCount,
                SyncMethod = "Webhook",
                ErrorMessage = errors.Any() ? string.Join("; ", errors) : null,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.DeviceSyncLogs.Add(syncLog);

            // 6. Update API key usage
            var apiKeyHash = HashApiKey(dto.ApiKey);
            var apiKey = await dbContext.DeviceApiKeys
                .FirstOrDefaultAsync(k => k.DeviceId == deviceGuid.Value && k.ApiKeyHash == apiKeyHash);

            if (apiKey != null)
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
                apiKey.UsageCount++;
                apiKey.UpdatedAt = DateTime.UtcNow;
            }

            // 7. Save all changes
            await dbContext.SaveChangesAsync();

            response.Success = true;
            response.Message = $"Successfully processed {processedCount} punch records";
            response.RecordsProcessed = processedCount;
            response.RecordsSkipped = skippedCount;
            response.Errors = errors;

            _logger.LogInformation(
                "✅ Webhook processing complete: Processed={Processed}, Skipped={Skipped}, Device={DeviceId}",
                processedCount,
                skippedCount,
                dto.DeviceId);

            return response;
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw auth exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing attendance webhook from device {DeviceId}", dto.DeviceId);
            response.Success = false;
            response.Message = "Internal error processing attendance data";
            response.Errors.Add(ex.Message);
            return response;
        }
    }

    /// <summary>
    /// Process heartbeat/status update from a biometric device
    /// </summary>
    public async Task ProcessHeartbeatAsync(DeviceHeartbeatDto dto)
    {
        try
        {
            // 1. Validate device API key
            var (isValid, tenantId, deviceGuid) = await ValidateDeviceApiKeyAsync(dto.DeviceId, dto.ApiKey);

            if (!isValid || !tenantId.HasValue || !deviceGuid.HasValue)
            {
                throw new UnauthorizedAccessException("Invalid device credentials");
            }

            // 2. Get tenant-specific database context
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var tenantSchema = await GetTenantSchemaAsync(tenantId.Value);
            var dbContext = CreateTenantDbContext(connectionString!, tenantSchema);
            await using var _ = dbContext;

            // 3. Update device status
            var device = await dbContext.AttendanceMachines
                .FirstOrDefaultAsync(d => d.Id == deviceGuid.Value);

            if (device == null)
            {
                throw new InvalidOperationException("Device not found");
            }

            device.LastSyncAt = DateTime.UtcNow;
            device.DeviceStatus = dto.Status.IsOnline ? "Active" : "Offline";
            device.FirmwareVersion = dto.Status.FirmwareVersion ?? device.FirmwareVersion;
            device.Model = dto.Status.DeviceModel ?? device.Model;
            device.UpdatedAt = DateTime.UtcNow;

            // 4. Update API key usage
            var apiKeyHash = HashApiKey(dto.ApiKey);
            var apiKey = await dbContext.DeviceApiKeys
                .FirstOrDefaultAsync(k => k.DeviceId == deviceGuid.Value && k.ApiKeyHash == apiKeyHash);

            if (apiKey != null)
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
                apiKey.UsageCount++;
                apiKey.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "💓 Heartbeat processed: Device={DeviceId}, Status={Status}",
                dto.DeviceId,
                dto.Status.IsOnline ? "Online" : "Offline");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing heartbeat from device {DeviceId}", dto.DeviceId);
            throw;
        }
    }

    /// <summary>
    /// Validate device API key and get device information
    /// </summary>
    public async Task<(bool IsValid, Guid? TenantId, Guid? DeviceGuid)> ValidateDeviceApiKeyAsync(
        string deviceId,
        string apiKey)
    {
        try
        {
            var apiKeyHash = HashApiKey(apiKey);
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Get all tenant schemas from master.Tenants table
            var masterDbContext = CreateTenantDbContext(connectionString!, "master");
            await using (masterDbContext)
            {
                var tenantSchemas = await masterDbContext.Database.SqlQueryRaw<string>(
                    "SELECT \"SchemaName\" FROM master.\"Tenants\" WHERE \"IsDeleted\" = false")
                    .ToListAsync();

                _logger.LogDebug("🔍 Searching for device {DeviceId} across {Count} tenant schemas",
                    deviceId, tenantSchemas.Count);

                // Search each tenant schema for the device
                foreach (var schema in tenantSchemas)
                {
                    try
                    {
                        var tenantDbContext = CreateTenantDbContext(connectionString!, schema);
                        await using (tenantDbContext)
                        {
                            var device = await tenantDbContext.AttendanceMachines
                                .FirstOrDefaultAsync(d => d.DeviceCode == deviceId || d.MachineId == deviceId);

                            if (device != null)
                            {
                                _logger.LogDebug("✅ Found device in schema: {Schema}", schema);

                                // Find valid API key for this device
                                var apiKeyEntity = await tenantDbContext.DeviceApiKeys
                                    .FirstOrDefaultAsync(k =>
                                        k.DeviceId == device.Id &&
                                        k.ApiKeyHash == apiKeyHash &&
                                        k.IsActive &&
                                        (!k.ExpiresAt.HasValue || k.ExpiresAt.Value > DateTime.UtcNow));

                                if (apiKeyEntity != null)
                                {
                                    _logger.LogInformation(
                                        "🔓 Device authenticated: DeviceId={DeviceId}, Schema={Schema}, TenantId={TenantId}",
                                        deviceId, schema, apiKeyEntity.TenantId);
                                    return (true, apiKeyEntity.TenantId, device.Id);
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "🔒 Invalid or expired API key for device {DeviceId} in schema {Schema}",
                                        deviceId, schema);
                                    return (false, null, null);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error searching schema {Schema} for device {DeviceId}",
                            schema, deviceId);
                        continue;
                    }
                }

                _logger.LogWarning("🔒 Device not found in any tenant schema: {DeviceId}", deviceId);
                return (false, null, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error validating device API key for {DeviceId}", deviceId);
            return (false, null, null);
        }
    }

    // ==========================================
    // HELPER METHODS
    // ==========================================

    /// <summary>
    /// Get tenant schema name from tenant ID
    /// </summary>
    private async Task<string> GetTenantSchemaAsync(Guid tenantId)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var masterDbContext = CreateTenantDbContext(connectionString!, "master");
        await using (masterDbContext)
        {
            var schema = await masterDbContext.Database.SqlQueryRaw<string>(
                $"SELECT \"SchemaName\" FROM master.\"Tenants\" WHERE \"Id\" = '{tenantId}' AND \"IsDeleted\" = false")
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(schema))
            {
                throw new InvalidOperationException($"Tenant schema not found for TenantId: {tenantId}");
            }

            return schema;
        }
    }

    /// <summary>
    /// Hash API key using SHA-256
    /// </summary>
    private string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Generate hash chain for punch record integrity
    /// </summary>
    private string GenerateHashChain(Guid deviceId, string userId, DateTime punchTime)
    {
        var data = $"{deviceId}|{userId}|{punchTime:O}";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Map device punch type to internal enum
    /// </summary>
    private string MapPunchType(int punchType)
    {
        return punchType switch
        {
            0 => "CheckIn",
            1 => "CheckOut",
            2 => "Break",
            3 => "BreakEnd",
            4 => "CheckIn", // Overtime In -> CheckIn
            5 => "CheckOut", // Overtime Out -> CheckOut
            _ => "CheckIn" // Default
        };
    }

    /// <summary>
    /// Map device verify mode to internal enum
    /// </summary>
    private string MapVerifyMode(int verifyMode)
    {
        return verifyMode switch
        {
            0 => "PIN",
            1 => "Fingerprint",
            2 => "Card",
            3 => "Face",
            15 => "Palm",
            _ => "Fingerprint"
        };
    }

    /// <summary>
    /// Create a TenantDbContext for the specified tenant schema
    /// </summary>
    private TenantDbContext CreateTenantDbContext(string connectionString, string tenantSchema)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(connectionString, o =>
        {
            o.MigrationsHistoryTable("__EFMigrationsHistory", tenantSchema);
            o.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        });

        return new TenantDbContext(optionsBuilder.Options, tenantSchema, _encryptionService);
    }
}
