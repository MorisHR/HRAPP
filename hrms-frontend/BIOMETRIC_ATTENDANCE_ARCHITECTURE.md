# Biometric Attendance System - Architecture Diagrams

## System Architecture Overview

```
┌────────────────────────────────────────────────────────────────────────────┐
│                           BIOMETRIC DEVICES LAYER                           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ ZKTeco   │  │ Face Rec │  │Fingerprint│  │  Card    │  │  Mobile  │   │
│  │ Device   │  │ Device   │  │ Scanner   │  │ Reader   │  │   App    │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
│       │             │              │              │              │          │
│       └─────────────┴──────────────┴──────────────┴──────────────┘          │
│                                    │                                        │
└────────────────────────────────────┼────────────────────────────────────────┘
                                     │
                    TCP/IP │ WebSocket │ REST API
                                     │
┌────────────────────────────────────┼────────────────────────────────────────┐
│                        API GATEWAY & SECURITY LAYER                         │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  • Device API Key Authentication                                     │  │
│  │  • Rate Limiting (60 req/min per device)                            │  │
│  │  • IP Whitelisting                                                   │  │
│  │  • TLS 1.3 Encryption                                               │  │
│  │  • Request Validation & Sanitization                                │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────┼────────────────────────────────────────┘
                                     │
┌────────────────────────────────────┼────────────────────────────────────────┐
│                         APPLICATION LAYER (ASP.NET Core)                    │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                         Controllers                                   │  │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐        │  │
│  │  │ BiometricPunch │  │AttendanceRules │  │  DeviceHealth  │        │  │
│  │  │   Controller   │  │   Controller   │  │   Controller   │        │  │
│  │  └───────┬────────┘  └───────┬────────┘  └───────┬────────┘        │  │
│  └──────────┼───────────────────┼───────────────────┼──────────────────┘  │
│             │                   │                   │                       │
│  ┌──────────┼───────────────────┼───────────────────┼──────────────────┐  │
│  │          │        Services Layer                 │                   │  │
│  │  ┌───────▼────────┐  ┌───────▼────────┐  ┌──────▼──────┐          │  │
│  │  │ PunchProcessing│  │  RulesEngine   │  │   Device    │          │  │
│  │  │    Service     │  │    Service     │  │  Monitoring │          │  │
│  │  └───────┬────────┘  └───────┬────────┘  └──────┬──────┘          │  │
│  │  ┌───────▼────────┐  ┌───────▼────────┐  ┌──────▼──────┐          │  │
│  │  │  GeoFencing    │  │   ApiKey       │  │    Photo    │          │  │
│  │  │    Service     │  │   Service      │  │   Storage   │          │  │
│  │  └────────────────┘  └────────────────┘  └─────────────┘          │  │
│  └────────────────────────────────┬──────────────────────────────────┘  │
└────────────────────────────────────┼────────────────────────────────────────┘
                                     │
┌────────────────────────────────────┼────────────────────────────────────────┐
│                         DATA PERSISTENCE LAYER                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                      PostgreSQL Database                              │  │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐        │  │
│  │  │ BiometricPunch │  │  Attendances   │  │AttendanceRules │        │  │
│  │  │    Records     │  │                │  │                │        │  │
│  │  └────────────────┘  └────────────────┘  └────────────────┘        │  │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐        │  │
│  │  │ DeviceHealth   │  │ GeoFenceZones  │  │  DeviceApiKeys │        │  │
│  │  │    Metrics     │  │                │  │                │        │  │
│  │  └────────────────┘  └────────────────┘  └────────────────┘        │  │
│  │                                                                       │  │
│  │  Features:                                                            │  │
│  │  • Time-series partitioning (monthly)                                │  │
│  │  • Optimized indexes for high-volume queries                         │  │
│  │  • Immutable audit trail with hash chain                            │  │
│  │  • Point-in-time recovery (PITR)                                    │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         BACKGROUND JOBS LAYER (Hangfire)                    │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐              │
│  │PunchProcessing │  │ DeviceHealth   │  │  PhotoCleanup  │              │
│  │   (5 min)      │  │   (1 min)      │  │    (daily)     │              │
│  └────────────────┘  └────────────────┘  └────────────────┘              │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐              │
│  │AnomalyAutoResolve│ │  DeviceSync   │  │   Integrity    │              │
│  │   (hourly)     │  │  (15 min)      │  │   (daily)      │              │
│  └────────────────┘  └────────────────┘  └────────────────┘              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                      REAL-TIME LAYER (SignalR)                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                       AttendanceHub                                   │  │
│  │  • PunchReceived event                                                │  │
│  │  • AnomalyDetected event                                             │  │
│  │  • DeviceOffline event                                               │  │
│  │  • AttendanceProcessed event                                         │  │
│  └────────────────────────┬─────────────────────────────────────────────┘  │
│                           │                                                 │
│  ┌────────────────────────┼─────────────────────────────────────────────┐  │
│  │        Redis Backplane (for scaled deployments)                      │  │
│  │  • Message distribution across multiple servers                      │  │
│  │  • Connection state management                                       │  │
│  │  • Rate limiting data                                                │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────┬─────────────────────────────────────────────────┘
                            │
┌───────────────────────────┼─────────────────────────────────────────────────┐
│                    FRONTEND LAYER (Angular 20)                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Real-Time Dashboard                                │  │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐    │  │
│  │  │ Live Punch │  │  Anomaly   │  │   Device   │  │  Today's   │    │  │
│  │  │    Feed    │  │   Alerts   │  │   Status   │  │   Stats    │    │  │
│  │  └────────────┘  └────────────┘  └────────────┘  └────────────┘    │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Admin Interfaces                                   │  │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐    │  │
│  │  │   Punch    │  │  GeoFence  │  │    Rules   │  │   Device   │    │  │
│  │  │  Approval  │  │   Zones    │  │  Builder   │  │   Health   │    │  │
│  │  └────────────┘  └────────────┘  └────────────┘  └────────────┘    │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                    EXTERNAL SERVICES                                        │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐              │
│  │ Google Cloud   │  │   Map Service  │  │  Email/SMS     │              │
│  │    Storage     │  │ (Google/Leaflet)│ │   Service      │              │
│  │  (Photos)      │  │  (Geo-Fencing) │  │  (Alerts)      │              │
│  └────────────────┘  └────────────────┘  └────────────────┘              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Punch Processing Flow

```
┌──────────────┐
│   Device     │
│  Captures    │
│    Punch     │
└──────┬───────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  1. Device API Key Authentication            │
│     • Validate API key                       │
│     • Check device is active                 │
│     • Verify IP whitelist                    │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  2. Rate Limiting Check                      │
│     • Check requests per minute              │
│     • Return 429 if exceeded                 │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  3. Input Validation                         │
│     • Validate employee exists               │
│     • Validate device exists                 │
│     • Validate timestamp                     │
│     • Validate photo (if present)            │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  4. Fraud Detection Rules                    │
│     • Check duplicate punch                  │
│     • Validate geo-fencing                   │
│     • Check impossible travel                │
│     • Verify device authorization            │
│     • Validate time window                   │
│     • Check verification quality             │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  5. Calculate Anomaly Score                  │
│     • Sum violation weights                  │
│     • Critical = 40 points                   │
│     • High = 25 points                       │
│     • Medium = 15 points                     │
│     • Low = 5 points                         │
│     • Max score = 100                        │
└──────┬───────────────────────────────────────┘
       │
       ▼
       Score >= 80?
       │
       ├─── YES ──────────────────────────────┐
       │                                       │
       ▼                                       ▼
┌──────────────────┐              ┌────────────────────┐
│  Score 50-79?    │              │  6a. Flag for      │
└──────┬───────────┘              │      Manual Review │
       │                          │      Status:       │
       ├─── YES ───────┐          │      "Flagged"     │
       │                │          └────────────────────┘
       ▼                ▼
┌──────────────┐  ┌──────────────┐
│ Score < 50   │  │  6b. Warn    │
└──────┬───────┘  │  Status:     │
       │          │  "Warning"   │
       │          └──────────────┘
       ▼
┌──────────────────────────────────────────────┐
│  6c. Auto-Process                            │
│      Status: "Processed"                     │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  7. Store BiometricPunchRecord               │
│     • Generate record hash                   │
│     • Link to previous record hash           │
│     • Store encrypted photo URL              │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  8. Process Photo (if captured)              │
│     • Compress image                         │
│     • Upload to cloud storage                │
│     • Generate signed URL                    │
│     • Calculate photo hash                   │
└──────┬───────────────────────────────────────┘
       │
       ▼
       Status == "Processed"?
       │
       ├─── YES ──────────────────────────────┐
       │                                       │
       ▼                                       ▼
┌──────────────────┐              ┌────────────────────┐
│  Status !=       │              │  9. Create/Update  │
│  "Processed"     │              │     Attendance     │
└──────┬───────────┘              │     Record         │
       │                          │     • Link punch   │
       │                          │     • Calculate    │
       │                          │       hours        │
       │                          └────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  10. Create AttendanceAnomaly (if flagged)   │
│      • Store anomaly details                 │
│      • Set resolution status = Pending       │
│      • Assign to HR queue                    │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  11. Real-Time Notifications (SignalR)       │
│      • Broadcast PunchReceived               │
│      • Broadcast AnomalyDetected (if flagged)│
│      • Update dashboard counters             │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  12. Update Device Metrics                   │
│      • Increment punch counter               │
│      • Update last activity time             │
│      • Record processing time                │
└──────┬───────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────┐
│  13. Return Response to Device               │
│      • Punch ID                              │
│      • Processing status                     │
│      • Warnings/errors                       │
│      • Next sync time (if applicable)        │
└──────────────────────────────────────────────┘
```

---

## Geo-Fencing Validation Flow

```
┌──────────────────────────┐
│  Punch with GPS coords   │
│  Lat: -20.1609          │
│  Lng: 57.5012           │
└────────┬─────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  1. Get Location from Device           │
│     device.LocationId = "LOC-001"      │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  2. Query Active GeoFence Zones        │
│     WHERE LocationId = "LOC-001"       │
│     AND IsActive = true                │
│     AND (time-based rules match)       │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  3. For Each Zone:                     │
│     Calculate Distance                 │
│                                        │
│     Using Haversine Formula:           │
│     distance = 2R × arcsin(√(         │
│       sin²(Δlat/2) +                  │
│       cos(lat1) × cos(lat2) ×         │
│       sin²(Δlon/2)                    │
│     ))                                 │
│                                        │
│     Where R = 6371 km (Earth radius)   │
└────────┬───────────────────────────────┘
         │
         ▼
         Distance <= Radius?
         │
         ├─── YES ───────────────────────┐
         │                                │
         ▼                                ▼
┌────────────────┐          ┌─────────────────────┐
│  Try Next Zone │          │  VALID              │
└────────┬───────┘          │  IsGeoFenced = true │
         │                  │  ZoneId = zone.Id   │
         ▼                  │  Distance = 45m     │
  All Zones Checked?        └─────────────────────┘
         │
         ├─── YES ─────────────────────┐
         │                              │
         ▼                              ▼
┌────────────────────┐      ┌──────────────────────┐
│  More Zones?       │      │  INVALID             │
└────────┬───────────┘      │  IsGeoFenced = false │
         │                  │  Violation:          │
         NO                 │  "Outside all zones" │
         │                  └──────────────────────┘
         ▼
┌────────────────────────────────────────┐
│  Check Enforcement Level               │
│  • Block: Reject punch                 │
│  • Warn: Accept with warning           │
│  • Log: Accept, log for audit          │
└────────────────────────────────────────┘
```

---

## Device Health Monitoring Flow

```
┌──────────────────────────┐
│   Device                 │
│   Sends Heartbeat        │
│   Every 60 seconds       │
└────────┬─────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  1. Receive Heartbeat                  │
│     {                                  │
│       deviceId: "xxx",                 │
│       timestamp: "2025-11-11T10:00",  │
│       metrics: {                       │
│         cpuUsage: 45.2,               │
│         memoryUsage: 67.8,            │
│         errorCount: 0,                │
│         dailyPunchCount: 245          │
│       }                                │
│     }                                  │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  2. Update Device Record               │
│     • LastHeartbeat = now              │
│     • DeviceStatus = "Online"          │
│     • Update metrics                   │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  3. Store Health Metrics               │
│     • Create DeviceHealthMetrics       │
│     • Time-series data point           │
└────────┬───────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────┐
│  4. Check Thresholds                   │
│     • CPU > 90%?                       │
│     • Memory > 90%?                    │
│     • Error rate high?                 │
│     • Response time slow?              │
└────────┬───────────────────────────────┘
         │
         ▼
         Threshold Exceeded?
         │
         ├─── YES ──────────────────────┐
         │                               │
         ▼                               ▼
┌────────────────┐          ┌────────────────────┐
│  No Alert      │          │  5. Create Alert   │
│  Continue      │          │     • AlertType    │
└────────────────┘          │     • Severity     │
                            │     • Send to HR   │
                            └────────┬───────────┘
                                     │
                                     ▼
                            ┌────────────────────┐
                            │  6. Notify         │
                            │     • SignalR      │
                            │     • Email        │
                            │     • SMS (critical)│
                            └────────────────────┘

┌─────────────────────────────────────────────┐
│   Background Job (Every Minute)             │
│                                             │
│   DeviceHealthCheckJob                      │
└─────────┬───────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────┐
│  1. Query All Devices                       │
│     WHERE IsActive = true                   │
└─────────┬───────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────┐
│  2. For Each Device:                        │
│     Check Last Heartbeat                    │
│                                             │
│     timeSinceHeartbeat =                    │
│       now - device.LastHeartbeat            │
└─────────┬───────────────────────────────────┘
          │
          ▼
          timeSince > OfflineThreshold?
          │
          ├─── YES ─────────────────────────┐
          │                                  │
          ▼                                  ▼
┌─────────────────┐           ┌──────────────────────┐
│  Device Online  │           │  3. Mark as Offline  │
│  No Action      │           │     • DeviceStatus   │
└─────────────────┘           │       = "Offline"    │
                              │     • Create alert   │
                              └──────┬───────────────┘
                                     │
                                     ▼
                              ┌──────────────────────┐
                              │  4. Notify HR/IT     │
                              │     • SignalR event  │
                              │     • Email if       │
                              │       > 30 min       │
                              └──────────────────────┘
```

---

## Data Flow - Creating Attendance from Punches

```
┌──────────────────────────────────────────────┐
│  Scenario: Employee Check-In and Check-Out  │
│  Employee: John Doe (EMP001)                │
│  Date: 2025-11-11                           │
└────────┬─────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│  08:45 AM - Check-In Punch                         │
│  ┌──────────────────────────────────────────────┐  │
│  │ Device: Main Entrance (DEV001)               │  │
│  │ Method: Fingerprint                          │  │
│  │ Quality: 92                                  │  │
│  │ Location: -20.1609, 57.5012                 │  │
│  │ Photo: [Captured]                           │  │
│  └──────────────────────────────────────────────┘  │
└────────┬───────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│  Fraud Detection Results:                          │
│  • Duplicate Check: PASS (no punch in 15 min)      │
│  • Geo-Fence: PASS (within 45m of zone)           │
│  • Device Auth: PASS (primary location)            │
│  • Time Window: PASS (06:00-22:00 allowed)        │
│  • Quality Check: PASS (92 >= 70 threshold)        │
│                                                    │
│  Anomaly Score: 0                                  │
│  Status: PROCESSED                                 │
└────────┬───────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│  BiometricPunchRecord Created:                     │
│  ┌──────────────────────────────────────────────┐  │
│  │ Id: PR-001                                   │  │
│  │ EmployeeId: EMP001                           │  │
│  │ DeviceId: DEV001                             │  │
│  │ PunchTime: 2025-11-11 08:45:00              │  │
│  │ PunchType: CheckIn                           │  │
│  │ ProcessingStatus: Processed                  │  │
│  │ AnomalyScore: 0                              │  │
│  │ PhotoUrl: gs://bucket/punches/PR-001.jpg    │  │
│  │ RecordHash: sha256(...PR-001...)             │  │
│  └──────────────────────────────────────────────┘  │
└────────┬───────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│  Attendance Record Created (Partial):              │
│  ┌──────────────────────────────────────────────┐  │
│  │ Id: ATT-001                                  │  │
│  │ EmployeeId: EMP001                           │  │
│  │ Date: 2025-11-11                            │  │
│  │ CheckInTime: 08:45:00                       │  │
│  │ CheckOutTime: null                          │  │
│  │ CheckInPunchRecordId: PR-001                │  │
│  │ Status: Present                              │  │
│  │ WorkingHours: 0 (pending checkout)          │  │
│  └──────────────────────────────────────────────┘  │
└────────┬───────────────────────────────────────────┘
         │
         ▼
    [Wait 8 hours...]
         │
         ▼
┌────────────────────────────────────────────────────┐
│  05:15 PM - Check-Out Punch                        │
│  ┌──────────────────────────────────────────────┐  │
│  │ Device: Main Entrance (DEV001)               │  │
│  │ Method: Fingerprint                          │  │
│  │ Quality: 88                                  │  │
│  │ Location: -20.1611, 57.5010                 │  │
│  │ Photo: [Captured]                           │  │
│  └──────────────────────────────────────────────┘  │
└────────┬───────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│  BiometricPunchRecord Created:                     │
│  ┌──────────────────────────────────────────────┐  │
│  │ Id: PR-002                                   │  │
│  │ PunchType: CheckOut                          │  │
│  │ PunchTime: 2025-11-11 17:15:00              │  │
│  │ ProcessingStatus: Processed                  │  │
│  │ PreviousRecordHash: [PR-001 hash]           │  │
│  └──────────────────────────────────────────────┘  │
└────────┬───────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│  Attendance Record Updated (Complete):             │
│  ┌──────────────────────────────────────────────┐  │
│  │ Id: ATT-001                                  │  │
│  │ EmployeeId: EMP001                           │  │
│  │ Date: 2025-11-11                            │  │
│  │ CheckInTime: 08:45:00                       │  │
│  │ CheckOutTime: 17:15:00                      │  │
│  │ CheckInPunchRecordId: PR-001                │  │
│  │ CheckOutPunchRecordId: PR-002               │  │
│  │ WorkingHours: 7.5 (8.5 - 1 hour break)     │  │
│  │ OvertimeHours: 0 (calculated weekly)        │  │
│  │ Status: Present                              │  │
│  └──────────────────────────────────────────────┘  │
└────────┬───────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│  SignalR Notifications Sent:                       │
│  • PunchReceived (to location group)               │
│  • AttendanceProcessed (to HR dashboard)           │
│  • EmployeeStatusUpdated (to employee app)         │
└────────────────────────────────────────────────────┘
```

---

## Security Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    SECURITY LAYERS                       │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  Layer 1: Network Security                              │
│  ┌───────────────────────────────────────────────────┐  │
│  │ • TLS 1.3 for all communications                  │  │
│  │ • IP Whitelisting for devices                     │  │
│  │ • Firewall rules (ports 443, 4370 only)          │  │
│  │ • DDoS protection (Cloudflare/WAF)               │  │
│  └───────────────────────────────────────────────────┘  │
└────────┬────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│  Layer 2: API Gateway Security                          │
│  ┌───────────────────────────────────────────────────┐  │
│  │ • Device API Key Authentication                   │  │
│  │   - SHA-256 hashed keys                          │  │
│  │   - Prefix-based identification                  │  │
│  │   - Key expiration and rotation                  │  │
│  │                                                   │  │
│  │ • Rate Limiting                                   │  │
│  │   - 60 requests/minute per device                │  │
│  │   - 1000 requests/hour per device                │  │
│  │   - Redis-based distributed limiting             │  │
│  │                                                   │  │
│  │ • Request Validation                              │  │
│  │   - Input sanitization                           │  │
│  │   - Schema validation                            │  │
│  │   - Timestamp verification (clock drift < 5 min) │  │
│  └───────────────────────────────────────────────────┘  │
└────────┬────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│  Layer 3: Application Security                          │
│  ┌───────────────────────────────────────────────────┐  │
│  │ • JWT Authentication (for web users)              │  │
│  │   - Access token (15 min expiry)                 │  │
│  │   - Refresh token (7 days)                       │  │
│  │   - Role-based authorization                     │  │
│  │                                                   │  │
│  │ • Data Validation                                 │  │
│  │   - FluentValidation rules                       │  │
│  │   - Business rule validation                     │  │
│  │   - Anomaly detection                            │  │
│  │                                                   │  │
│  │ • Audit Logging                                   │  │
│  │   - All API calls logged                         │  │
│  │   - User actions tracked                         │  │
│  │   - Correlation IDs for tracing                  │  │
│  └───────────────────────────────────────────────────┘  │
└────────┬────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│  Layer 4: Data Security                                 │
│  ┌───────────────────────────────────────────────────┐  │
│  │ • Encryption at Rest                              │  │
│  │   - PostgreSQL encryption (AES-256)              │  │
│  │   - Encrypted backups                            │  │
│  │                                                   │  │
│  │ • Encryption in Transit                           │  │
│  │   - TLS 1.3 for all communications               │  │
│  │   - HTTPS only                                   │  │
│  │                                                   │  │
│  │ • Data Integrity                                  │  │
│  │   - SHA-256 hashes for punch records             │  │
│  │   - Blockchain-style hash chain                  │  │
│  │   - Photo hash verification                      │  │
│  │                                                   │  │
│  │ • PII Protection                                  │  │
│  │   - Biometric templates hashed                   │  │
│  │   - Photos stored with access control            │  │
│  │   - GDPR compliance features                     │  │
│  └───────────────────────────────────────────────────┘  │
└────────┬────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│  Layer 5: Monitoring & Alerting                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │ • Security Event Monitoring                       │  │
│  │   - Failed authentication attempts               │  │
│  │   - Rate limit violations                        │  │
│  │   - Anomalous behavior detection                 │  │
│  │                                                   │  │
│  │ • Real-Time Alerts                                │  │
│  │   - Critical security events (SMS)               │  │
│  │   - Suspicious activity (Email)                  │  │
│  │   - System health issues (Dashboard)             │  │
│  │                                                   │  │
│  │ • Compliance Reporting                            │  │
│  │   - Access logs                                  │  │
│  │   - Audit trail reports                          │  │
│  │   - Data integrity checks                        │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Scalability Architecture

```
┌─────────────────────────────────────────────────────────┐
│         HORIZONTAL SCALING (Multiple Servers)            │
└─────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│                    Load Balancer                          │
│              (Google Cloud Load Balancer)                 │
│  • Health checks every 5 seconds                         │
│  • Session affinity for SignalR                          │
│  • SSL termination                                       │
└────────┬──────────────┬──────────────┬───────────────────┘
         │              │              │
         ▼              ▼              ▼
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│   Server 1  │  │   Server 2  │  │   Server 3  │
│  (Primary)  │  │  (Worker)   │  │  (Worker)   │
│             │  │             │  │             │
│ • API       │  │ • API       │  │ • API       │
│ • SignalR   │  │ • SignalR   │  │ • SignalR   │
│ • Jobs      │  │             │  │             │
└─────┬───────┘  └─────┬───────┘  └─────┬───────┘
      │                │                │
      └────────────────┼────────────────┘
                       │
                       ▼
         ┌─────────────────────────────┐
         │      Redis Backplane         │
         │  • SignalR message routing   │
         │  • Distributed cache         │
         │  • Rate limiting data        │
         └─────────────┬───────────────┘
                       │
                       ▼
         ┌─────────────────────────────┐
         │   PostgreSQL Database        │
         │  • Master-slave replication  │
         │  • Read replicas (3x)        │
         │  • Auto-failover             │
         │  • Connection pooling        │
         └──────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│              PERFORMANCE OPTIMIZATIONS                    │
└──────────────────────────────────────────────────────────┘

Database Layer:
┌───────────────────────────────────────────────────────┐
│ • Table Partitioning (monthly)                        │
│   - Automatic partition management                    │
│   - Old partitions archived to cold storage          │
│                                                       │
│ • Indexing Strategy                                   │
│   - B-tree indexes for equality lookups              │
│   - GiST indexes for geo queries                     │
│   - Partial indexes for common filters               │
│                                                       │
│ • Query Optimization                                  │
│   - Materialized views for reports                   │
│   - Query result caching (Redis)                     │
│   - Prepared statements                              │
└───────────────────────────────────────────────────────┘

Application Layer:
┌───────────────────────────────────────────────────────┐
│ • Caching Strategy                                     │
│   - Rules cached for 10 minutes                       │
│   - Device configs cached for 5 minutes               │
│   - Location data cached for 1 hour                   │
│                                                       │
│ • Async Processing                                     │
│   - Non-critical operations queued                    │
│   - Background jobs for heavy tasks                   │
│   - Event-driven architecture                         │
│                                                       │
│ • Resource Management                                  │
│   - Connection pooling (min 10, max 100)             │
│   - Object pooling for frequent allocations          │
│   - Memory-efficient data structures                 │
└───────────────────────────────────────────────────────┘
```

---

## Deployment Architecture

```
┌─────────────────────────────────────────────────────────┐
│              PRODUCTION ENVIRONMENT                      │
└─────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│                 Google Cloud Platform                     │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │              Cloud Load Balancer                    │  │
│  │  • HTTPS (443)                                      │  │
│  │  • Health checks                                    │  │
│  │  • SSL certificates (auto-renew)                   │  │
│  └────────────┬───────────────────────────────────────┘  │
│               │                                           │
│  ┌────────────┴───────────────────────────────────────┐  │
│  │        Kubernetes Cluster (GKE)                     │  │
│  │                                                      │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │  HRMS API Pods (3 replicas)                  │  │  │
│  │  │  • Auto-scaling (2-10 pods)                  │  │  │
│  │  │  • Rolling updates                           │  │  │
│  │  │  • Health probes                             │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  │                                                      │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │  Background Jobs Pods (2 replicas)           │  │  │
│  │  │  • Hangfire dashboard                        │  │  │
│  │  │  • Job processing                            │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  │                                                      │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │    Cloud SQL (PostgreSQL)                          │  │
│  │    • High availability                             │  │
│  │    • Automatic backups (daily)                     │  │
│  │    • Point-in-time recovery                        │  │
│  │    • Read replicas (3)                             │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │    Cloud Memorystore (Redis)                       │  │
│  │    • 5GB RAM                                       │  │
│  │    • High availability                             │  │
│  │    • Automatic failover                            │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │    Cloud Storage                                    │  │
│  │    • Punch photos                                  │  │
│  │    • Lifecycle policies (90 days)                 │  │
│  │    • Versioning enabled                           │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │    Cloud Monitoring                                 │  │
│  │    • Application metrics                           │  │
│  │    • Custom dashboards                             │  │
│  │    • Alerts configuration                          │  │
│  └────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│               STAGING ENVIRONMENT                         │
│  • 1 API replica                                          │
│  • 1 Job worker                                           │
│  • Smaller database instance                             │
│  • Same architecture, reduced capacity                    │
└───────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│             DEVELOPMENT ENVIRONMENT                       │
│  • Local Docker Compose                                   │
│  • PostgreSQL container                                   │
│  • Redis container                                        │
│  • Hot reload enabled                                     │
└───────────────────────────────────────────────────────────┘
```

---

This architecture document provides visual representations of all key system flows and deployment configurations. For full implementation details, refer to `BIOMETRIC_ATTENDANCE_IMPLEMENTATION_PLAN.md`.
