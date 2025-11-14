using HRMS.DeviceSync.Models;
using HRMS.DeviceSync.Services;

namespace HRMS.DeviceSync;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly SyncServiceConfiguration _config;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _config = configuration.GetSection("SyncService").Get<SyncServiceConfiguration>()
            ?? throw new InvalidOperationException("SyncService configuration not found");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("═══════════════════════════════════════════");
        _logger.LogInformation("  HRMS Device Sync Worker Starting");
        _logger.LogInformation("═══════════════════════════════════════════");
        _logger.LogInformation("API URL: {ApiUrl}", _config.ApiBaseUrl);
        _logger.LogInformation("Sync Interval: {Interval} minutes", _config.SyncIntervalMinutes);
        _logger.LogInformation("Configured Devices: {Count}", _config.Devices.Count);

        foreach (var device in _config.Devices.Where(d => d.IsEnabled))
        {
            _logger.LogInformation("  - {Code} ({IP}:{Port})",
                device.DeviceCode, device.IpAddress, device.Port);
        }

        // Test API connection on startup
        using (var scope = _serviceProvider.CreateScope())
        {
            var apiClient = scope.ServiceProvider.GetRequiredService<HrmsApiClient>();
            var apiConnected = await apiClient.TestConnectionAsync();

            if (!apiConnected)
            {
                _logger.LogWarning("⚠️  API connection test failed. Will retry during sync cycles.");
            }
        }

        _logger.LogInformation("═══════════════════════════════════════════");
        _logger.LogInformation("  Worker Ready - Starting Sync Cycles");
        _logger.LogInformation("═══════════════════════════════════════════\n");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 Starting sync cycle at {Time}", DateTimeOffset.Now);

                await SyncAllDevicesAsync();

                _logger.LogInformation("✅ Sync cycle completed. Next sync in {Minutes} minutes\n",
                    _config.SyncIntervalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during sync cycle");
            }

            // Wait for configured interval
            await Task.Delay(
                TimeSpan.FromMinutes(_config.SyncIntervalMinutes),
                stoppingToken);
        }

        _logger.LogInformation("HRMS Device Sync Worker stopping...");
    }

    private async Task SyncAllDevicesAsync()
    {
        var enabledDevices = _config.Devices.Where(d => d.IsEnabled).ToList();

        _logger.LogInformation("Syncing {Count} enabled devices", enabledDevices.Count);

        // Process devices in parallel (max 5 concurrent)
        var semaphore = new SemaphoreSlim(5);
        var tasks = enabledDevices.Select(async device =>
        {
            await semaphore.WaitAsync();
            try
            {
                await SyncDeviceAsync(device);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task SyncDeviceAsync(DeviceConfiguration device)
    {
        using var scope = _serviceProvider.CreateScope();
        var zkService = scope.ServiceProvider.GetRequiredService<ZKTecoDeviceService>();
        var apiClient = scope.ServiceProvider.GetRequiredService<HrmsApiClient>();

        try
        {
            _logger.LogInformation("📡 Syncing device {DeviceCode} ({IpAddress}:{Port})",
                device.DeviceCode, device.IpAddress, device.Port);

            // Connect to device
            bool connected = await zkService.ConnectAsync(device);
            if (!connected)
            {
                _logger.LogWarning("⚠️  Failed to connect to device {DeviceCode}. Skipping.",
                    device.DeviceCode);
                return;
            }

            // Fetch attendance records
            var records = await zkService.FetchAttendanceRecordsAsync(device.DeviceCode);

            if (!records.Any())
            {
                _logger.LogInformation("   No new attendance records for device {DeviceCode}",
                    device.DeviceCode);
                zkService.Disconnect();
                return;
            }

            _logger.LogInformation("   Fetched {Count} records from device {DeviceCode}",
                records.Count, device.DeviceCode);

            // Push to API
            bool pushed = await apiClient.PushAttendanceRecordsAsync(
                device.DeviceCode,
                records);

            if (pushed)
            {
                _logger.LogInformation(
                    "   ✅ Successfully synced {Count} records for device {DeviceCode}",
                    records.Count, device.DeviceCode);

                // Optional: Clear records from device after successful sync
                // Uncomment if you want to clear device memory after sync
                // _logger.LogInformation("   Clearing records from device...");
                // await zkService.ClearAttendanceRecordsAsync(device.DeviceCode);
            }
            else
            {
                _logger.LogWarning(
                    "   ⚠️  Failed to push records to API for device {DeviceCode}. " +
                    "Records will be retried in next sync.",
                    device.DeviceCode);
            }

            // Disconnect from device
            zkService.Disconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error syncing device {DeviceCode}", device.DeviceCode);
        }
    }
}
