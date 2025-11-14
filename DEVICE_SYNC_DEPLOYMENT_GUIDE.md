# HRMS Device Sync - Complete Deployment Guide

**Production-Ready Biometric Device Integration**
**For morishr.com - Fortune 500 Standard**

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Deployment Options](#deployment-options)
5. [Configuration](#configuration)
6. [Windows Service Deployment](#windows-service-deployment)
7. [Linux systemd Deployment](#linux-systemd-deployment)
8. [Docker Deployment](#docker-deployment)
9. [Monitoring & Troubleshooting](#monitoring--troubleshooting)
10. [Security Best Practices](#security-best-practices)

---

## 🎯 Overview

The HRMS Device Sync middleware service provides **hybrid push/pull** architecture for biometric attendance devices:

### Supported Methods:

**🌐 Push Method** (Modern Devices)
- Devices push directly to API webhook
- Real-time data sync
- No middleware required

**🔄 Pull Method** (SDK-Only Devices like ZKTeco ZAM180)
- Middleware polls devices via SDK
- Pushes data to API
- Works with legacy devices

### Key Features:
- ✅ Enterprise-grade logging with Serilog
- ✅ Automatic retry and error handling
- ✅ Multi-device concurrent syncing
- ✅ Configurable sync intervals
- ✅ Production-ready monitoring
- ✅ Windows/Linux/Docker support

---

## 🏗️ Architecture

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│  ZKTeco Device  │◄────────┤  HRMS.DeviceSync │────────►│  API (morishr)  │
│  192.168.x.x    │  Poll   │   (Middleware)   │  Push   │  api.morishr.com│
│  Port: 4370     │ (SDK)   │                  │ (HTTPS) │                 │
└─────────────────┘         └──────────────────┘         └─────────────────┘
```

### Data Flow:
1. **Middleware connects** to device via TCP (ZKTeco SDK)
2. **Fetches attendance** records every N minutes
3. **Pushes to API** using device-specific API key
4. **Logs everything** to file and console
5. **Repeats cycle** based on configuration

---

## 📦 Prerequisites

### Hardware Requirements:
- **CPU:** 1 core (2+ recommended)
- **RAM:** 512 MB minimum (1 GB recommended)
- **Network:** Access to both device network and internet
- **Storage:** 10 GB for logs and binaries

### Software Requirements:

#### Windows Deployment:
- Windows Server 2019/2022 or Windows 10/11
- .NET 9.0 Runtime (included in SDK)
- ZKTeco SDK DLL files ✅ (Already downloaded)
- Administrator access

#### Linux Deployment:
- Ubuntu 22.04 LTS / Debian 12 / RHEL 9
- .NET 9.0 Runtime
- Wine (for COM DLL support - experimental)
- Root or sudo access

#### Docker Deployment:
- Docker 24.0+
- Docker Compose 2.0+
- Network access to devices

### Required Credentials:
- ✅ Device IP addresses and ports
- ✅ Device communication passwords
- ✅ API key from HRMS (generated from frontend)
- ✅ API base URL (https://api.morishr.com)

---

## ⚙️ Configuration

### Step 1: Get Your API Key

1. Login to HRMS as tenant admin
2. Navigate to: **Organization → Biometric Devices**
3. Select your device
4. Click "**API Keys**" tab
5. Click "**Generate New Key**"
6. Enter description: "Device Sync Middleware"
7. **Copy the generated API key** (shown once only!)

### Step 2: Configure appsettings.json

Edit `/opt/hrms/devicesync/appsettings.json` (or `C:\HRMS\DeviceSync\appsettings.json`):

```json
{
  "SyncService": {
    "ApiBaseUrl": "https://api.morishr.com",
    "ApiKey": "YOUR_GENERATED_API_KEY_HERE",
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
        "DeviceCode": "BRANCH-OFFICE-001",
        "IpAddress": "192.168.200.50",
        "Port": 4370,
        "CommPassword": 0,
        "DeviceType": "ZKTeco",
        "IsEnabled": true
      }
    ]
  },

  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Step 3: Configure Production Settings

Edit `appsettings.Production.json`:

```json
{
  "SyncService": {
    "ApiBaseUrl": "https://api.morishr.com",
    "SyncIntervalMinutes": 5
  },

  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "HRMS.DeviceSync": "Information"
      }
    }
  }
}
```

---

## 🪟 Windows Service Deployment

### Option 1: Using PowerShell Script (Recommended)

```powershell
# Step 1: Build and publish
cd C:\Projects\HRAPP\src\HRMS.DeviceSync
dotnet publish -c Release -o C:\HRMS\DeviceSync

# Step 2: Copy SDK files
Copy-Item SDK\*.dll C:\HRMS\DeviceSync\SDK\

# Step 3: Copy configuration
Copy-Item appsettings*.json C:\HRMS\DeviceSync\

# Step 4: Install as Windows Service
.\deploy-windows-service.ps1 -Action Install -BinaryPath "C:\HRMS\DeviceSync\HRMS.DeviceSync.exe"

# Step 5: Verify service is running
.\deploy-windows-service.ps1 -Action Status
```

### Option 2: Manual Installation

```powershell
# Create the service
sc.exe create "HRMS Device Sync Service" `
    binPath= "C:\HRMS\DeviceSync\HRMS.DeviceSync.exe" `
    start= auto `
    DisplayName= "HRMS Device Sync Service"

# Start the service
sc.exe start "HRMS Device Sync Service"

# Check status
sc.exe query "HRMS Device Sync Service"
```

### Management Commands

```powershell
# Start service
.\deploy-windows-service.ps1 -Action Start

# Stop service
.\deploy-windows-service.ps1 -Action Stop

# Restart service
.\deploy-windows-service.ps1 -Action Restart

# Check status
.\deploy-windows-service.ps1 -Action Status

# Uninstall
.\deploy-windows-service.ps1 -Action Uninstall

# View logs
Get-Content C:\HRMS\DeviceSync\logs\device-sync-*.txt -Tail 50 -Wait
```

---

## 🐧 Linux systemd Deployment

### Step 1: Build and Publish

```bash
cd /opt/hrms/HRAPP/src/HRMS.DeviceSync
dotnet publish -c Release -o /opt/hrms/devicesync
```

### Step 2: Create Service User

```bash
sudo useradd -r -s /bin/false hrmsync
sudo chown -R hrmsync:hrmsync /opt/hrms/devicesync
```

### Step 3: Install systemd Service

```bash
# Copy service file
sudo cp hrms-devicesync.service /etc/systemd/system/

# Reload systemd
sudo systemctl daemon-reload

# Enable service (start on boot)
sudo systemctl enable hrms-devicesync

# Start service
sudo systemctl start hrms-devicesync

# Check status
sudo systemctl status hrms-devicesync
```

### Management Commands

```bash
# Start service
sudo systemctl start hrms-devicesync

# Stop service
sudo systemctl stop hrms-devicesync

# Restart service
sudo systemctl restart hrms-devicesync

# View status
sudo systemctl status hrms-devicesync

# View logs (live)
sudo journalctl -u hrms-devicesync -f

# View logs (last 100 lines)
sudo journalctl -u hrms-devicesync -n 100

# View file logs
tail -f /opt/hrms/devicesync/logs/device-sync-*.txt
```

---

## 🐳 Docker Deployment

### Step 1: Build Docker Image

```bash
cd /workspaces/HRAPP/src/HRMS.DeviceSync

# Build image
docker build -t hrms-devicesync:latest .

# Or use docker-compose
docker-compose build
```

### Step 2: Configure Environment

```bash
# Edit appsettings.json with your configuration
nano appsettings.json
```

### Step 3: Run Container

**Using docker-compose (Recommended):**
```bash
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

**Using docker run:**
```bash
docker run -d \
  --name hrms-devicesync \
  --network host \
  -v $(pwd)/appsettings.json:/app/appsettings.json:ro \
  -v $(pwd)/appsettings.Production.json:/app/appsettings.Production.json:ro \
  -v $(pwd)/SDK:/app/SDK:ro \
  -v $(pwd)/logs:/app/logs \
  -e DOTNET_ENVIRONMENT=Production \
  hrms-devicesync:latest

# View logs
docker logs -f hrms-devicesync

# Stop
docker stop hrms-devicesync
```

### Management Commands

```bash
# View logs
docker-compose logs -f hrms-devicesync

# Restart
docker-compose restart hrms-devicesync

# Stop
docker-compose stop hrms-devicesync

# Start
docker-compose start hrms-devicesync

# Remove and recreate
docker-compose down
docker-compose up -d

# View container stats
docker stats hrms-devicesync
```

---

## 📊 Monitoring & Troubleshooting

### Log Locations

**Windows:**
```
C:\HRMS\DeviceSync\logs\device-sync-YYYYMMDD.txt
```

**Linux:**
```
/opt/hrms/devicesync/logs/device-sync-YYYYMMDD.txt
systemd journal: journalctl -u hrms-devicesync
```

**Docker:**
```
docker logs hrms-devicesync
./logs/device-sync-YYYYMMDD.txt (mounted volume)
```

### Common Issues

#### 1. Cannot Connect to Device

**Symptoms:**
```
⚠️  Failed to connect to device MAIN-OFFICE-001. Skipping.
```

**Solutions:**
- ✅ Verify device IP address and port (usually 4370)
- ✅ Check network connectivity: `ping 192.168.100.201`
- ✅ Ensure device is powered on
- ✅ Check firewall rules allow TCP port 4370
- ✅ Verify CommPassword is correct (usually 0)

#### 2. API Key Authentication Failed

**Symptoms:**
```
❌ Failed to push records to API for device MAIN-OFFICE-001
HTTP 401 Unauthorized
```

**Solutions:**
- ✅ Regenerate API key from HRMS frontend
- ✅ Update `ApiKey` in appsettings.json
- ✅ Restart service
- ✅ Ensure API key belongs to correct tenant

#### 3. SDK DLL Not Found

**Symptoms:**
```
warning MSB3245: Could not resolve this reference. Could not locate the assembly "zkemkeeper"
```

**Solutions:**
- ✅ Copy all DLL files from SDK folder
- ✅ Ensure zkemkeeper.dll is in SDK/ directory
- ✅ On Linux, install Wine (experimental)
- ✅ **Recommended:** Use Windows host for production

#### 4. No Records Being Synced

**Symptoms:**
```
No new attendance records for device MAIN-OFFICE-001
```

**Solutions:**
- ✅ Check if device has new records since last sync
- ✅ Verify employees have punched in/out
- ✅ Check device memory is not full
- ✅ Try clearing device records (after backup!)

### Health Checks

```bash
# Check if service is running
# Windows
sc.exe query "HRMS Device Sync Service"

# Linux
sudo systemctl status hrms-devicesync

# Docker
docker ps | grep hrms-devicesync

# Test API connectivity
curl https://api.morishr.com/api/device-webhook/ping

# Check recent logs for errors
# Windows
Get-Content C:\HRMS\DeviceSync\logs\device-sync-*.txt | Select-String "ERROR"

# Linux
grep "ERR" /opt/hrms/devicesync/logs/device-sync-*.txt

# Docker
docker logs hrms-devicesync | grep "ERR"
```

---

## 🔒 Security Best Practices

### 1. API Key Security
- ✅ **Never** commit API keys to version control
- ✅ Use environment variables or secrets manager
- ✅ Rotate keys every 90 days
- ✅ Use separate keys per environment (dev/staging/prod)

### 2. Network Security
- ✅ Run middleware on same network as devices
- ✅ Use VPN for remote device access
- ✅ Whitelist middleware IP in API key settings
- ✅ Enable firewall rules

### 3. Service Account Security
- ✅ Run service as non-admin user
- ✅ Grant minimum required permissions
- ✅ Use managed service accounts (Windows)
- ✅ Enable SELinux/AppArmor (Linux)

### 4. Log Security
- ✅ Rotate logs regularly (30-day retention)
- ✅ Encrypt logs at rest
- ✅ Restrict log file permissions (644)
- ✅ Monitor for sensitive data in logs

### 5. Update Management
- ✅ Keep .NET runtime updated
- ✅ Monitor security advisories
- ✅ Test updates in staging first
- ✅ Maintain rollback plan

---

## 📈 Performance Tuning

### Recommended Settings

**Small Deployment (1-5 devices):**
```json
{
  "SyncService": {
    "SyncIntervalMinutes": 5,
    "MaxRecordsPerSync": 1000
  }
}
```

**Medium Deployment (6-20 devices):**
```json
{
  "SyncService": {
    "SyncIntervalMinutes": 3,
    "MaxRecordsPerSync": 2000
  }
}
```

**Large Deployment (21+ devices):**
```json
{
  "SyncService": {
    "SyncIntervalMinutes": 2,
    "MaxRecordsPerSync": 5000
  }
}
```

### Resource Limits

**Docker:**
```yaml
deploy:
  resources:
    limits:
      cpus: '2.0'
      memory: 1G
    reservations:
      cpus: '0.5'
      memory: 256M
```

**systemd:**
```ini
[Service]
LimitNOFILE=65536
TasksMax=4096
MemoryMax=1G
CPUQuota=200%
```

---

## 🎓 Quick Start Checklist

- [ ] ✅ ZKTeco SDK downloaded and installed
- [ ] ✅ Middleware built and deployed
- [ ] ✅ API key generated from HRMS
- [ ] ✅ Configuration file updated
- [ ] ✅ Device IP addresses configured
- [ ] ✅ Service installed and running
- [ ] ✅ Logs showing successful sync
- [ ] ✅ Data appearing in HRMS dashboard
- [ ] ✅ Monitoring configured
- [ ] ✅ Backup and recovery tested

---

## 📞 Support

**Documentation:** https://docs.morishr.com
**Issues:** Contact your system administrator
**Emergency:** Check logs first, then contact support

---

## 📜 License

Copyright © 2025 morishr.com
Enterprise HRMS - Biometric Device Integration
All rights reserved.

---

**Last Updated:** 2025-11-14
**Version:** 1.0.0
**Status:** ✅ Production Ready
