namespace HRMS.Core.Settings;

/// <summary>
/// Configuration for anomaly detection thresholds and rules
/// </summary>
public class AnomalyDetectionSettings
{
    public bool Enabled { get; set; } = true;

    // Failed Login Thresholds
    public int FailedLoginThreshold { get; set; } = 5;
    public int FailedLoginWindowMinutes { get; set; } = 15;

    // Mass Export Thresholds
    public int MassExportRecordThreshold { get; set; } = 100;

    // Geographic/Travel Detection
    public bool EnableImpossibleTravelDetection { get; set; } = true;
    public int ImpossibleTravelKmPerHour { get; set; } = 800; // Max human travel speed

    // Concurrent Sessions
    public int ConcurrentSessionThreshold { get; set; } = 3;

    // After Hours Detection
    public int AfterHoursStartHour { get; set; } = 22; // 10 PM
    public int AfterHoursEndHour { get; set; } = 6;    // 6 AM

    // Financial Anomalies
    public decimal SalaryChangePercentageThreshold { get; set; } = 50m; // 50% change

    // Rapid Actions
    public int RapidActionThreshold { get; set; } = 10; // Actions per minute
    public int RapidActionWindowSeconds { get; set; } = 60;

    // Notification Settings
    public bool AutoNotifyOnCritical { get; set; } = true;
    public bool AutoNotifyOnHigh { get; set; } = true;
    public List<string> NotificationRecipients { get; set; } = new();
}
