using HRMS.DeviceSync.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace HRMS.DeviceSync.Services;

public class HrmsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HrmsApiClient> _logger;
    private readonly SyncServiceConfiguration _config;

    public HrmsApiClient(
        HttpClient httpClient,
        ILogger<HrmsApiClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = configuration.GetSection("SyncService").Get<SyncServiceConfiguration>()
            ?? throw new InvalidOperationException("SyncService configuration not found");

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_config.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> PushAttendanceRecordsAsync(
        string deviceCode,
        List<DeviceAttendanceRecord> records)
    {
        if (!records.Any())
        {
            _logger.LogInformation("No records to push for device {DeviceCode}", deviceCode);
            return true;
        }

        try
        {
            _logger.LogInformation(
                "Pushing {Count} attendance records to API for device {DeviceCode}",
                records.Count, deviceCode);

            // Format data to match webhook endpoint expectations
            var payload = new
            {
                deviceId = deviceCode,
                apiKey = _config.ApiKey,
                timestamp = DateTime.UtcNow,
                records = records.Select(r => new
                {
                    employeeId = r.EmployeeId,
                    enrollNumber = r.EnrollNumber,
                    punchTime = r.PunchTime,
                    punchType = r.InOutMode,
                    verifyMode = r.VerifyMode,
                    workCode = r.WorkCode,
                    deviceRecordId = $"{r.PunchTime:yyyyMMddHHmmss}_{r.EnrollNumber}"
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/device-webhook/attendance",
                payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(
                    "Successfully pushed {Count} records to API. Response: {Response}",
                    records.Count, result);
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Failed to push records to API. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception while pushing attendance records to API for device {DeviceCode}",
                deviceCode);
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing API connection to {BaseUrl}", _httpClient.BaseAddress);

            var response = await _httpClient.GetAsync("/api/device-webhook/ping");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("API connection test successful");
                return true;
            }
            else
            {
                _logger.LogWarning("API connection test failed. Status: {StatusCode}",
                    response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API connection test failed");
            return false;
        }
    }
}
