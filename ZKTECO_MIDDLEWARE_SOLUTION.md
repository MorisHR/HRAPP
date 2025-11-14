# ZKTeco Device Middleware Solution
## For ZAM180_TFT Without Push Capabilities

**Date**: November 14, 2025
**Device**: ZKTeco ZAM180_TFT (Serial: PFE8240100360)
**Challenge**: Device doesn't support push webhooks
**Solution**: Middleware service that polls devices and pushes to API

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│  Your Network (Office/Branch)                               │
│                                                              │
│  ┌──────────────────┐                                       │
│  │ ZKTeco Device 1  │                                       │
│  │ 192.168.100.201  │                                       │
│  │ Port: 4370       │                                       │
│  └────────┬─────────┘                                       │
│           │                                                  │
│           │ SDK Connection (TCP)                            │
│           ↓                                                  │
│  ┌──────────────────────────────────────┐                   │
│  │  Middleware Service                  │                   │
│  │  (Windows/Linux/Docker)              │                   │
│  │                                      │                   │
│  │  - Connects to devices via SDK       │                   │
│  │  - Polls every 5 minutes             │                   │
│  │  - Fetches new attendance records    │                   │
│  │  - Pushes to API via HTTPS           │                   │
│  └──────────────┬───────────────────────┘                   │
│                 │                                            │
└─────────────────┼────────────────────────────────────────────┘
                  │
                  │ HTTPS
                  │
                  ↓
         ┌────────────────────┐
         │  Your API Server   │
         │  api.morishr.com   │
         │                    │
         │  Receives data &   │
         │  stores in DB      │
         └────────────────────┘
```

---

## Solution Options

You have **3 options** for implementing this middleware:

### Option 1: Use Existing Software (Easiest) ⭐ RECOMMENDED

**ZKBio Time Attendance Software** (Official from ZKTeco)

**Pros:**
- ✅ Official software from ZKTeco
- ✅ GUI interface for configuration
- ✅ No coding required
- ✅ Supports multiple devices
- ✅ Has API export capabilities

**Cons:**
- ⚠️ Runs on Windows only
- ⚠️ May require license fee
- ⚠️ Limited customization

**How it works:**
1. Install ZKBio Time software on Windows PC/Server
2. Add your devices to ZKBio
3. ZKBio polls devices automatically
4. Configure ZKBio to call your API webhook
5. Done!

**Where to get it:**
- Download from: https://www.zkteco.com/en/Software/Free
- Or contact ZKTeco distributor in Mauritius

---

### Option 2: Custom .NET Service (Most Flexible) ⭐ FOR DEVELOPERS

**Build a custom middleware service** using ZKTeco SDK

**Pros:**
- ✅ Full control over logic
- ✅ Can run as Windows Service or Docker
- ✅ Fully integrated with your HRMS
- ✅ Can customize data transformation
- ✅ Free (no license fees)

**Cons:**
- ⚠️ Requires development time
- ⚠️ Need ZKTeco SDK DLL files
- ⚠️ Need .NET developer

**I can build this for you!** See implementation details below.

---

### Option 3: Use ADMS + Webhook Forwarder (Cloud-based)

**Use ZKTeco's ADMS cloud service** as intermediary

**Pros:**
- ✅ Cloud-based (no on-premise service needed)
- ✅ ZKTeco manages the device connections
- ✅ Web-based monitoring dashboard

**Cons:**
- ⚠️ Monthly subscription fee
- ⚠️ Data goes through ZKTeco servers first
- ⚠️ Requires ADMS-enabled device (need to verify if ZAM180 supports this)

**How to check:**
1. Access device web panel: http://192.168.100.201
2. Look for "ADMS" or "Cloud Service" in menu
3. If exists, you can enable it

---

## RECOMMENDED SOLUTION: Custom .NET Middleware

I recommend **Option 2** because:
- You already have .NET infrastructure
- Full control and no licensing fees
- Can handle all 20 devices from one service
- Integrates perfectly with your existing HRMS

---

## How the .NET Middleware Works

### Components:

**1. Background Worker Service**
- Runs continuously as Windows Service or Docker container
- Polls devices at configurable interval (default: 5 minutes)
- Handles multiple devices concurrently

**2. ZKTeco SDK Integration**
- Uses `zkemkeeper.dll` (ZKTeco official SDK)
- Connects to device via TCP (port 4370)
- Fetches attendance logs
- Handles disconnections gracefully

**3. API Client**
- Pushes attendance data to your API
- Uses generated API keys for authentication
- Retries on failures
- Logs all operations

**4. Configuration**
- JSON-based configuration
- Add/remove devices without code changes
- Configure sync intervals
- Set API URLs and credentials

---

## Configuration File

Your middleware will use `appsettings.json`:

```json
{
  "SyncService": {
    "ApiBaseUrl": "https://api.morishr.com",
    "ApiKey": "YOUR_GENERATED_API_KEY_FROM_HRMS",
    "SyncIntervalMinutes": 5,
    "MaxRecordsPerSync": 1000,

    "Devices": [
      {
        "DeviceCode": "MAIN-OFFICE-001",
        "IpAddress": "192.168.100.201",
        "Port": 4370,
        "CommPassword": 0,
        "DeviceType": "ZKTeco",
        "IsEnabled": true
      },
      {
        "DeviceCode": "BRANCH-A-001",
        "IpAddress": "192.168.2.100",
        "Port": 4370,
        "CommPassword": 0,
        "DeviceType": "ZKTeco",
        "IsEnabled": true
      }
      // Add all 20 devices here
    ]
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

---

## What You Need

### To Use ZKBio Software (Option 1):

1. **Software**: Download ZKBio Time from ZKTeco website
2. **Windows PC/Server**: Runs on same network as devices
3. **Device Access**: Network access to all devices
4. **API Configuration**: Configure webhook in ZKBio to call your API

### To Build Custom Service (Option 2):

1. **ZKTeco SDK**:
   - Download from: https://www.zkteco.com/en/download_category/standalone-sdk
   - You need: `zkemkeeper.dll` (COM DLL for .NET)
   - Version: Latest standalone SDK

2. **Server/Host**:
   - Windows Server 2019/2022 (preferred)
   - Or: Linux with Mono/.NET
   - Or: Docker container
   - Min specs: 2GB RAM, 2 CPU cores

3. **Network Requirements**:
   - Access to all device IPs (port 4370)
   - Outbound HTTPS access to api.morishr.com
   - Static IP recommended (or use hostname)

4. **Development** (if building custom):
   - .NET 9 SDK
   - Visual Studio or VS Code
   - I can provide complete source code

---

## Deployment Options

### Deployment A: Windows Server

**Best for:**
- Already have Windows Server
- Familiar with Windows administration
- Need GUI for monitoring

**Steps:**
1. Install .NET 9 Runtime on Windows Server
2. Deploy middleware as Windows Service
3. Configure firewall (allow outbound HTTPS)
4. Set service to auto-start
5. Monitor via Windows Event Log

**Cost:** Windows Server license if you don't have one

---

### Deployment B: Docker Container

**Best for:**
- Modern cloud deployment
- Easy scaling
- Cross-platform

**Steps:**
1. Build Docker image with middleware
2. Deploy to:
   - Google Cloud Run
   - Docker on VM
   - Kubernetes cluster
3. Configure environment variables
4. Monitor via container logs

**Cost:** Cloud hosting fees (~$10-30/month)

---

### Deployment C: On-Premise PC/Workstation

**Best for:**
- Testing
- Small deployments
- Low budget

**Steps:**
1. Use any Windows PC on your network
2. Install .NET 9 Runtime
3. Run middleware as console app
4. Keep PC powered on 24/7

**Cost:** Free (use existing hardware)

---

## Step-by-Step Implementation

### Phase 1: Setup & Testing (Week 1)

**Day 1-2: Prepare Environment**
1. Download ZKTeco SDK
2. Install .NET 9 Runtime
3. Prepare configuration file with device IPs

**Day 3-4: Test Connection**
1. Test SDK connection to ONE device
2. Verify can fetch attendance logs
3. Confirm data format

**Day 5-7: Build Middleware**
1. Create .NET Worker Service
2. Implement device polling
3. Implement API push
4. Test end-to-end with ONE device

### Phase 2: Production Deployment (Week 2)

**Day 8-10: Add All Devices**
1. Configure all 20 devices in appsettings.json
2. Test connection to each device
3. Verify sync works for all

**Day 11-12: Deploy to Production**
1. Deploy to Windows Server or Docker
2. Configure as service (auto-start)
3. Setup monitoring/alerts

**Day 13-14: Monitor & Optimize**
1. Monitor logs for errors
2. Optimize sync intervals
3. Add error handling/retries

---

## Code Structure (If I Build It For You)

```
HRMS.DeviceSync/
├── Models/
│   ├── DeviceConfiguration.cs
│   ├── AttendanceRecord.cs
│   └── ApiResponse.cs
├── Services/
│   ├── ZKTecoDeviceService.cs      ← SDK integration
│   ├── AttendanceSyncService.cs    ← Background worker
│   └── HrmsApiClient.cs            ← API push client
├── appsettings.json                ← Configuration
├── Program.cs                      ← Entry point
└── SDK/
    └── zkemkeeper.dll              ← ZKTeco SDK (you provide)
```

---

## What Data Gets Synced

The middleware will fetch and sync:

**Attendance Records:**
- Employee ID / Enrollment Number
- Punch Date & Time
- Verification Method (Face=15, Fingerprint=1, Password=0)
- In/Out Status (0=In, 1=Out, 2=Break Out, 3=Break In)
- Device Code
- Work Code (if used)

**Pushed to your API as:**
```json
POST https://api.morishr.com/api/device-webhook/attendance
Headers: X-API-Key: YOUR_API_KEY

{
  "deviceId": "MAIN-OFFICE-001",
  "apiKey": "YOUR_API_KEY",
  "timestamp": "2025-11-14T10:00:00Z",
  "records": [
    {
      "employeeId": "EMP001",
      "enrollNumber": "1",
      "punchTime": "2025-11-14T09:00:00Z",
      "punchType": 0,
      "verifyMode": 15,
      "workCode": 0,
      "deviceRecordId": "20251114090000_1"
    }
  ]
}
```

---

## Monitoring & Maintenance

**Logs to Monitor:**
- Device connection status
- Records fetched count
- API push success/failure
- Sync timing (duration)
- Errors and retries

**Typical Log Output:**
```
[2025-11-14 10:00:00] INFO: Starting sync cycle...
[2025-11-14 10:00:01] INFO: Connecting to MAIN-OFFICE-001 (192.168.100.201:4370)
[2025-11-14 10:00:02] INFO: Connected successfully
[2025-11-14 10:00:03] INFO: Fetched 5 new attendance records
[2025-11-14 10:00:04] INFO: Pushing 5 records to API...
[2025-11-14 10:00:05] INFO: API push successful - 5 records saved
[2025-11-14 10:00:05] INFO: Disconnecting from device
[2025-11-14 10:00:06] INFO: Sync completed in 6 seconds
[2025-11-14 10:05:00] INFO: Next sync in 5 minutes...
```

---

## Cost Comparison

| Option | Setup Cost | Monthly Cost | Complexity |
|--------|------------|--------------|------------|
| **ZKBio Software** | $0-500 (license) | $0-50 | Low |
| **Custom .NET Service** | $0 (DIY) or $500-2000 (hire developer) | $10-30 (hosting) | Medium |
| **ADMS Cloud** | $0 | $10-50/device | Low |

---

## My Recommendation for You

**For morishr.com - I recommend:**

**Build Custom .NET Middleware** because:

1. ✅ **You already have .NET skills** - Your HRMS is .NET-based
2. ✅ **Free** - No licensing fees, just hosting costs
3. ✅ **Full control** - Can customize as needed
4. ✅ **Scalable** - Handle 20+ devices easily
5. ✅ **Integrated** - Perfect fit with existing HRMS architecture

**Deployment:**
- Start with Docker container on GCP (same as your API)
- Cost: ~$10-20/month for small VM or Cloud Run

---

## Next Steps

**Option A: I Build It For You** (Fastest)

1. You provide: ZKTeco SDK DLL files
2. I build: Complete middleware service (2-3 hours)
3. You deploy: To Windows Server or Docker
4. Timeline: Ready in 1 day

**Option B: You Build It** (Learning experience)

1. Download SDK from ZKTeco
2. Follow my code templates (I provide)
3. Test with your device
4. Deploy when ready
5. Timeline: 1-2 weeks depending on experience

**Option C: Use ZKBio Software** (Easiest)

1. Download ZKBio Time
2. Install on Windows PC
3. Configure devices
4. Setup webhook to your API
5. Timeline: 1 day

---

## What Do You Want to Do?

**Tell me your preference:**

1. **"Build it for me"** → I'll create complete working middleware service
2. **"Help me build it"** → I'll guide you step-by-step
3. **"Use ZKBio instead"** → I'll help you configure ZKBio software

**Or tell me:**
- Where you want to run it? (Windows Server, Docker, Cloud VM)
- Timeline? (Need it this week vs. can take 2 weeks)
- Technical comfort level? (Developer vs. need turnkey solution)

Once you decide, I'll provide exact next steps!

---

**Current Status:** ✅ Architecture designed, ready to implement
**Waiting for:** Your decision on which option to pursue
