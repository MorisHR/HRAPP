using HRMS.DeviceSync.Models;
using System.Runtime.InteropServices;

namespace HRMS.DeviceSync.Services;

/// <summary>
/// Service for connecting to and fetching data from ZKTeco biometric devices
/// </summary>
public class ZKTecoDeviceService : IDisposable
{
    private readonly ILogger<ZKTecoDeviceService> _logger;
    private dynamic? _zkDevice;
    private bool _isConnected;
    private int _machineNumber = 1;

    public ZKTecoDeviceService(ILogger<ZKTecoDeviceService> logger)
    {
        _logger = logger;
        InitializeSDK();
    }

    private void InitializeSDK()
    {
        try
        {
            // Initialize ZKTeco SDK
            // Note: zkemkeeper.dll must be in SDK folder or registered as COM
            Type? type = Type.GetTypeFromProgID("zkemkeeper.ZKEM");
            if (type != null)
            {
                _zkDevice = Activator.CreateInstance(type);
                _logger.LogInformation("ZKTeco SDK initialized successfully");
            }
            else
            {
                _logger.LogWarning("ZKTeco SDK (zkemkeeper.dll) not found. SDK integration will not work.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ZKTeco SDK");
        }
    }

    /// <summary>
    /// Connect to a biometric device
    /// </summary>
    public async Task<bool> ConnectAsync(DeviceConfiguration device)
    {
        if (_zkDevice == null)
        {
            _logger.LogError("SDK not initialized. Cannot connect to device {DeviceCode}", device.DeviceCode);
            return false;
        }

        try
        {
            _logger.LogInformation("Connecting to device {DeviceCode} at {IpAddress}:{Port}",
                device.DeviceCode, device.IpAddress, device.Port);

            // Connect to device
            bool connected = await Task.Run(() =>
                _zkDevice.Connect_Net(device.IpAddress, device.Port));

            if (connected)
            {
                // Set communication password if configured
                if (device.CommPassword > 0)
                {
                    _zkDevice.SetDeviceCommPwd(device.CommPassword);
                }

                _isConnected = true;
                _logger.LogInformation("Successfully connected to device {DeviceCode}", device.DeviceCode);
                return true;
            }
            else
            {
                int errorCode = 0;
                _zkDevice.GetLastError(ref errorCode);
                _logger.LogWarning("Failed to connect to device {DeviceCode}. Error code: {ErrorCode}",
                    device.DeviceCode, errorCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while connecting to device {DeviceCode}", device.DeviceCode);
            return false;
        }
    }

    /// <summary>
    /// Fetch all attendance records from device
    /// </summary>
    public async Task<List<DeviceAttendanceRecord>> FetchAttendanceRecordsAsync(string deviceCode)
    {
        var records = new List<DeviceAttendanceRecord>();

        if (!_isConnected || _zkDevice == null)
        {
            _logger.LogWarning("Not connected to device. Cannot fetch attendance records.");
            return records;
        }

        try
        {
            _logger.LogInformation("Fetching attendance records from device {DeviceCode}", deviceCode);

            // Disable device to ensure data integrity
            await Task.Run(() => _zkDevice.DisableDevice(_machineNumber, true));

            // Read all attendance logs
            string enrollNumber = string.Empty;
            int verifyMode = 0;
            int inOutMode = 0;
            int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
            int workCode = 0;

            bool readResult = await Task.Run(() =>
                _zkDevice.ReadGeneralLogData(_machineNumber));

            if (readResult)
            {
                // Iterate through all records
                while (_zkDevice.SSR_GetGeneralLogData(
                    _machineNumber,
                    out enrollNumber,
                    out verifyMode,
                    out inOutMode,
                    out year,
                    out month,
                    out day,
                    out hour,
                    out minute,
                    out second,
                    ref workCode))
                {
                    try
                    {
                        var punchTime = new DateTime(year, month, day, hour, minute, second);

                        var record = new DeviceAttendanceRecord
                        {
                            EnrollNumber = enrollNumber,
                            EmployeeId = enrollNumber, // Map enrollment number to employee ID
                            PunchTime = punchTime,
                            VerifyMode = verifyMode, // 0=Password, 1=Fingerprint, 15=Face
                            InOutMode = inOutMode, // 0=CheckIn, 1=CheckOut, 2=BreakOut, 3=BreakIn, 4=OTIn, 5=OTOut
                            WorkCode = workCode
                        };

                        records.Add(record);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse attendance record for employee {EnrollNumber}",
                            enrollNumber);
                    }
                }

                _logger.LogInformation("Fetched {Count} attendance records from device {DeviceCode}",
                    records.Count, deviceCode);
            }
            else
            {
                int errorCode = 0;
                _zkDevice.GetLastError(ref errorCode);
                _logger.LogWarning("Failed to read attendance records. Error code: {ErrorCode}", errorCode);
            }

            // Re-enable device
            await Task.Run(() => _zkDevice.EnableDevice(_machineNumber, true));

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching attendance records from device {DeviceCode}",
                deviceCode);

            // Ensure device is re-enabled
            try
            {
                if (_zkDevice != null)
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                }
            }
            catch { }

            return records;
        }
    }

    /// <summary>
    /// Clear attendance records from device after successful sync
    /// </summary>
    public async Task<bool> ClearAttendanceRecordsAsync(string deviceCode)
    {
        if (!_isConnected || _zkDevice == null)
        {
            return false;
        }

        try
        {
            _logger.LogInformation("Clearing attendance records from device {DeviceCode}", deviceCode);

            bool result = await Task.Run(() =>
                _zkDevice.ClearGLog(_machineNumber));

            if (result)
            {
                _logger.LogInformation("Successfully cleared attendance records from device {DeviceCode}",
                    deviceCode);
                return true;
            }
            else
            {
                int errorCode = 0;
                _zkDevice.GetLastError(ref errorCode);
                _logger.LogWarning("Failed to clear attendance records. Error code: {ErrorCode}", errorCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while clearing attendance records from device {DeviceCode}",
                deviceCode);
            return false;
        }
    }

    /// <summary>
    /// Disconnect from device
    /// </summary>
    public void Disconnect()
    {
        if (_isConnected && _zkDevice != null)
        {
            try
            {
                _zkDevice.Disconnect();
                _isConnected = false;
                _logger.LogInformation("Disconnected from device");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while disconnecting from device");
            }
        }
    }

    /// <summary>
    /// Get device information
    /// </summary>
    public async Task<Dictionary<string, string>> GetDeviceInfoAsync()
    {
        var info = new Dictionary<string, string>();

        if (!_isConnected || _zkDevice == null)
        {
            return info;
        }

        try
        {
            string firmwareVersion = string.Empty;
            string platform = string.Empty;
            string serialNumber = string.Empty;
            string deviceName = string.Empty;

            await Task.Run(() =>
            {
                _zkDevice.GetFirmwareVersion(_machineNumber, ref firmwareVersion);
                _zkDevice.GetPlatform(_machineNumber, ref platform);
                _zkDevice.GetSerialNumber(_machineNumber, out serialNumber);
                _zkDevice.GetDeviceName(_machineNumber, out deviceName);
            });

            info["FirmwareVersion"] = firmwareVersion;
            info["Platform"] = platform;
            info["SerialNumber"] = serialNumber;
            info["DeviceName"] = deviceName;

            _logger.LogInformation("Device Info: {@DeviceInfo}", info);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get device information");
        }

        return info;
    }

    public void Dispose()
    {
        Disconnect();

        if (_zkDevice != null)
        {
            try
            {
                if (Marshal.IsComObject(_zkDevice))
                {
                    Marshal.ReleaseComObject(_zkDevice);
                }
            }
            catch { }

            _zkDevice = null;
        }

        GC.SuppressFinalize(this);
    }
}
