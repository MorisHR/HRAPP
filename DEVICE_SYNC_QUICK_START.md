# HRMS Device Sync - Quick Start Guide

**⚡ Get Up and Running in 15 Minutes**

---

## 🎯 What You Need

1. ✅ **Device Information:**
   - IP Address: `192.168.100.201`
   - Port: `4370`
   - Device Code: `MAIN-OFFICE-001`

2. ✅ **API Access:**
   - API URL: `https://api.morishr.com`
   - API Key: (Generate from HRMS frontend)

3. ✅ **SDK Files:** (Already downloaded ✅)
   - Location: `/workspaces/HRAPP/src/HRMS.DeviceSync/SDK/`
   - Files: `zkemkeeper.dll` and supporting DLLs

---

## 🚀 Windows Quick Deploy (5 Steps)

### Step 1: Build & Publish
```powershell
cd C:\Projects\HRAPP\src\HRMS.DeviceSync
dotnet publish -c Release -o C:\HRMS\DeviceSync
```

### Step 2: Copy Configuration
```powershell
# Edit appsettings.json with your settings
notepad C:\HRMS\DeviceSync\appsettings.json
```

**Update these values:**
```json
{
  "SyncService": {
    "ApiBaseUrl": "https://api.morishr.com",
    "ApiKey": "YOUR_API_KEY_HERE",
    "Devices": [
      {
        "DeviceCode": "MAIN-OFFICE-001",
        "IpAddress": "192.168.100.201",
        "Port": 4370,
        "IsEnabled": true
      }
    ]
  }
}
```

### Step 3: Install Service
```powershell
.\deploy-windows-service.ps1 -Action Install
```

### Step 4: Start Service
```powershell
Start-Service "HRMS Device Sync Service"
```

### Step 5: Verify
```powershell
# Check service status
Get-Service "HRMS Device Sync Service"

# View logs
Get-Content C:\HRMS\DeviceSync\logs\device-sync-*.txt -Tail 50
```

**Done! 🎉** Check HRMS dashboard for attendance data.

---

## 🐧 Linux Quick Deploy (5 Steps)

### Step 1: Build & Publish
```bash
cd /workspaces/HRAPP/src/HRMS.DeviceSync
dotnet publish -c Release -o /opt/hrms/devicesync
```

### Step 2: Configure
```bash
nano /opt/hrms/devicesync/appsettings.json
```

### Step 3: Create Service User
```bash
sudo useradd -r -s /bin/false hrmsync
sudo chown -R hrmsync:hrmsync /opt/hrms/devicesync
```

### Step 4: Install systemd Service
```bash
sudo cp hrms-devicesync.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable hrms-devicesync
sudo systemctl start hrms-devicesync
```

### Step 5: Verify
```bash
sudo systemctl status hrms-devicesync
sudo journalctl -u hrms-devicesync -f
```

---

## 🐳 Docker Quick Deploy (3 Steps)

### Step 1: Configure
```bash
cd /workspaces/HRAPP/src/HRMS.DeviceSync
nano appsettings.json
```

### Step 2: Build & Run
```bash
docker-compose up -d
```

### Step 3: Verify
```bash
docker-compose logs -f
```

---

## ✅ Verification Checklist

After deployment, verify these:

```bash
# 1. Service is running
# Windows: Get-Service "HRMS Device Sync Service"
# Linux: sudo systemctl status hrms-devicesync
# Docker: docker ps | grep hrms-devicesync

# 2. API is reachable
curl https://api.morishr.com/api/device-webhook/ping

# 3. Device is reachable
ping 192.168.100.201

# 4. Logs show successful sync
# Look for: "✅ Successfully synced X records for device"
```

---

## 🔧 Common Commands

### Generate API Key
1. Login to HRMS → Organization → Biometric Devices
2. Select your device → API Keys tab
3. Click "Generate New Key"
4. Copy and paste into `appsettings.json`

### View Logs
```powershell
# Windows
Get-Content C:\HRMS\DeviceSync\logs\device-sync-*.txt -Tail 50 -Wait

# Linux
tail -f /opt/hrms/devicesync/logs/device-sync-*.txt

# Docker
docker-compose logs -f hrms-devicesync
```

### Restart Service
```powershell
# Windows
Restart-Service "HRMS Device Sync Service"

# Linux
sudo systemctl restart hrms-devicesync

# Docker
docker-compose restart hrms-devicesync
```

---

## ❌ Troubleshooting

### Service Won't Start
```bash
# Check .NET is installed
dotnet --version  # Should show 9.0.x

# Check SDK files exist
ls SDK/zkemkeeper.dll  # Should exist

# Check configuration is valid JSON
cat appsettings.json | jq .  # Should parse without errors
```

### Cannot Connect to Device
```bash
# Test network connectivity
ping 192.168.100.201

# Test port is open
telnet 192.168.100.201 4370
# Or on Windows:
Test-NetConnection -ComputerName 192.168.100.201 -Port 4370
```

### API Authentication Failed
```bash
# Regenerate API key
1. Login to HRMS
2. Organization → Biometric Devices
3. Select device → API Keys
4. Generate new key
5. Update appsettings.json
6. Restart service
```

---

## 📊 Expected Log Output

**Successful Startup:**
```
═══════════════════════════════════════════
  HRMS Device Sync Service v1.0
  For morishr.com
═══════════════════════════════════════════
Starting service...
API URL: https://api.morishr.com
Sync Interval: 5 minutes
Configured Devices: 1
  - MAIN-OFFICE-001 (192.168.100.201:4370)
Worker Ready - Starting Sync Cycles
```

**Successful Sync:**
```
🔄 Starting sync cycle at 11/14/2025 06:00:00
📡 Syncing device MAIN-OFFICE-001 (192.168.100.201:4370)
   Fetched 25 records from device MAIN-OFFICE-001
   ✅ Successfully synced 25 records for device MAIN-OFFICE-001
✅ Sync cycle completed. Next sync in 5 minutes
```

---

## 🎓 Next Steps

1. ✅ Monitor logs for first few cycles
2. ✅ Verify attendance data in HRMS dashboard
3. ✅ Configure multiple devices (if needed)
4. ✅ Set up monitoring/alerting
5. ✅ Document your deployment

---

## 📞 Need Help?

**Full Documentation:** `/workspaces/HRAPP/DEVICE_SYNC_DEPLOYMENT_GUIDE.md`
**Configuration:** Check `appsettings.json` syntax
**Logs:** Always check logs first!

---

**Quick Deploy Time:** 15 minutes ⚡
**Status:** ✅ Production Ready
**Last Updated:** 2025-11-14
