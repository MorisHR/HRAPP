# DEVELOPMENT TESTING PLAN
## Biometric Device API System - Pre-Production Validation

**Date**: November 14, 2025
**Environment**: Development (GitHub Codespaces)
**Purpose**: Comprehensive testing before production deployment

---

## Overview

This document outlines the complete testing strategy for the Biometric Device API system before launching to production. It includes both automated and manual testing procedures.

---

## System Architecture

### Components to Test:

1. **PostgreSQL Database** (Port 5432)
   - Schema: `tenant_default`
   - Tables: `DeviceApiKeys`, `AttendanceMachines`, `BiometricPunchRecords`
   - 18-column DeviceApiKeys table with soft-delete support

2. **Backend API (.NET 9)** (Port 5090)
   - Two controllers:
     - `/api/biometric-devices` - Device management (requires JWT auth)
     - `/api/device-webhook` - Webhook endpoints (uses API key auth)

3. **Frontend (Angular 20)** (Port 4200)
   - Biometric Devices management UI
   - API key generation interface

---

## Testing Checklist

### ✅ Phase 1: Infrastructure & Database (AUTOMATED)

**Script**: `/tmp/automated_api_tests.sh`

| #  | Test Case | Expected Result | Status |
|----|-----------|----------------|--------|
| 1  | PostgreSQL Connection | Database accepting connections | ✅ PASS |
| 2  | Backend API Health | `/api/device-webhook/ping` returns 200 | ✅ PASS |
| 3  | DeviceApiKeys Table Exists | Table found in `tenant_default` schema | ✅ PASS |
| 4  | Table Schema Validation | 18 columns present including `IsDeleted` | ✅ PASS |
| 5  | Primary Key Constraint | `PK_DeviceApiKeys` on `Id` column | ✅ PASS |
| 6  | Foreign Key Constraints | FK to `AttendanceMachines` table | ✅ PASS |
| 7  | Performance Indexes | 8 indexes (7 + PK) present | ✅ PASS |
| 8  | Soft Delete Column | `IsDeleted` boolean with default false | ✅ PASS |

**Run Command**:
```bash
bash /tmp/automated_api_tests.sh
```

---

### ⏳ Phase 2: Device Webhook Endpoints (AUTOMATED)

These endpoints do NOT require JWT authentication (designed for IoT devices).

| #  | Test Case | Endpoint | Method | Status |
|----|-----------|----------|--------|--------|
| 9  | Ping Endpoint | `/api/device-webhook/ping` | GET | ✅ PASS |
| 10 | Attendance Webhook | `/api/device-webhook/attendance` | POST | ⏳ MANUAL |
| 11 | Heartbeat Webhook | `/api/device-webhook/heartbeat` | POST | ⏳ MANUAL |

**Sample Test (Ping)**:
```bash
curl http://localhost:5090/api/device-webhook/ping
# Expected: {"success":true,"message":"Device webhook endpoint is reachable",...}
```

---

### 🔐 Phase 3: Authenticated Device Management (MANUAL - Requires JWT)

These endpoints require JWT authentication (Admin/HR/Manager roles).

**Base URL**: `/api/biometric-devices`
**Authentication**: Required (JWT Token in Authorization header)

#### Prerequisites:
1. Login to frontend: `http://localhost:4200`
2. Obtain JWT token from browser localStorage or network tab
3. Use token in API requests:
   ```bash
   curl -H "Authorization: Bearer YOUR_JWT_TOKEN" http://localhost:5090/api/biometric-devices
   ```

| #  | Test Case | Endpoint | Method | Manual Steps |
|----|-----------|----------|--------|--------------|
| 12 | List All Devices | `/api/biometric-devices` | GET | Login → Navigate to Biometric Devices page |
| 13 | Get Device by ID | `/api/biometric-devices/{id}` | GET | Click on a device to view details |
| 14 | Create New Device | `/api/biometric-devices` | POST | Click "Add Device" button, fill form, submit |
| 15 | Update Device | `/api/biometric-devices/{id}` | PUT | Edit device details, save changes |
| 16 | Delete Device | `/api/biometric-devices/{id}` | DELETE | Click delete icon, confirm deletion |
| 17 | Test Connection | `/api/biometric-devices/test-connection` | POST | Enter IP/Port, click "Test Connection" button |

---

### 🔑 Phase 4: API Key Management (MANUAL - Critical Feature)

**Priority**: HIGH - This is the core functionality that was previously failing.

#### Test Scenario 1: Generate API Key

**Steps**:
1. Open browser: `http://localhost:4200`
2. Login with admin credentials
3. Navigate to: **Organization → Biometric Devices**
4. Click on a device (or create new device if none exist)
5. Click **"Generate API Key"** button
6. Enter description: `"Development Testing API Key"` (must be 3-200 characters)
7. **Optional**: Set expiration date, IP whitelist, rate limit
8. Click **Generate**

**Expected Results**:
- ✅ API key displayed ONE TIME ONLY (48-byte, ~64 characters base64)
- ✅ Success message: "API key generated successfully"
- ✅ Key stored in database as SHA-256 hash (not plaintext)
- ✅ Key appears in "Active API Keys" list with:
  - Description
  - Created date
  - Last used (null initially)
  - Usage count (0 initially)
  - Status (Active)

**Database Verification**:
```sql
-- Check API key was created
SELECT
    "Id", "Description", "IsActive", "ExpiresAt",
    "UsageCount", "CreatedAt", "IsDeleted"
FROM tenant_default."DeviceApiKeys"
WHERE "Description" = 'Development Testing API Key';

-- Verify hash is stored (not plaintext)
SELECT LENGTH("ApiKeyHash") as hash_length
FROM tenant_default."DeviceApiKeys"
WHERE "Description" = 'Development Testing API Key';
-- Expected: hash_length should be 64 (SHA-256 hex)
```

#### Test Scenario 2: Use API Key for Authentication

**Steps**:
1. Copy the generated API key from Step 1
2. Test attendance submission with API key:

```bash
# Replace YOUR_API_KEY with actual generated key
curl -X POST http://localhost:5090/api/device-webhook/attendance \
  -H "Content-Type: application/json" \
  -H "X-API-Key: YOUR_API_KEY" \
  -d '{
    "deviceId": "DEV-001",
    "apiKey": "YOUR_API_KEY",
    "timestamp": "2025-11-14T10:00:00Z",
    "records": [
      {
        "employeeId": "EMP001",
        "punchTime": "2025-11-14T09:00:00Z",
        "punchType": 0,
        "verifyMode": 1,
        "deviceRecordId": "12345"
      }
    ]
  }'
```

**Expected Results**:
- ✅ HTTP 200 OK
- ✅ Response: `{"success": true, "recordsProcessed": 1, ...}`
- ✅ `UsageCount` incremented in database
- ✅ `LastUsedAt` timestamp updated

#### Test Scenario 3: Invalid API Key (Security Test)

**Steps**:
```bash
curl -X POST http://localhost:5090/api/device-webhook/attendance \
  -H "Content-Type: application/json" \
  -H "X-API-Key: INVALID_KEY_12345" \
  -d '{...}'
```

**Expected Results**:
- ✅ HTTP 401 Unauthorized
- ✅ Error message: "Invalid device credentials"

#### Test Scenario 4: Validation Errors

**Test 4.1 - Description Too Short**:
- Enter description: `"ab"` (2 characters)
- **Expected**: Validation error "Description must be between 3 and 200 characters"

**Test 4.2 - Description Too Long**:
- Enter description: 201 characters
- **Expected**: Validation error "Description must be between 3 and 200 characters"

**Test 4.3 - Empty Description**:
- Leave description blank
- **Expected**: Validation error "Description is required"

#### Test Scenario 5: Revoke API Key

**Steps**:
1. In the device detail page, find the API key list
2. Click "Revoke" or "Deactivate" button on an API key
3. Confirm revocation

**Expected Results**:
- ✅ Key marked as inactive (`IsActive = false`)
- ✅ Key no longer works for authentication (401 error)
- ✅ Soft delete: `IsDeleted = true`, `DeletedAt` timestamp set

---

### 🚀 Phase 5: Rate Limiting (MANUAL)

**Purpose**: Verify rate limiting prevents abuse (default: 60 requests/minute)

**Test Script**:
```bash
# Generate API key first via frontend
API_KEY="YOUR_GENERATED_API_KEY"

# Send 70 requests rapidly
for i in {1..70}; do
  echo "Request $i..."
  curl -s -X POST "http://localhost:5090/api/device-webhook/attendance" \
    -H "X-API-Key: $API_KEY" \
    -H "Content-Type: application/json" \
    -d '{"deviceId":"DEV-001","records":[]}' | grep -o "429\|success"

  if [ $? -eq 0 ]; then
    echo "Rate limit hit at request $i"
    break
  fi
done
```

**Expected Results**:
- ✅ First 60 requests succeed (within 1 minute)
- ✅ Request 61+ returns HTTP 429 Too Many Requests
- ✅ Error message: "Rate limit exceeded"

---

### 🔒 Phase 6: Security Validation (DATABASE)

**Test 6.1 - API Keys Never Stored in Plaintext**:
```sql
-- Verify all API keys are hashed
SELECT
    "Id",
    "Description",
    LENGTH("ApiKeyHash") as hash_length,
    "ApiKeyHash"
FROM tenant_default."DeviceApiKeys";
-- Hash should be 64 chars (SHA-256 hex) or 44 chars (base64)
-- Never matches the plaintext key you generated
```

**Test 6.2 - Tenant Isolation**:
```sql
-- Verify TenantId is set (not Guid.Empty)
SELECT
    "Id",
    "TenantId",
    "DeviceId",
    "Description"
FROM tenant_default."DeviceApiKeys"
WHERE "TenantId" = '00000000-0000-0000-0000-000000000000';
-- Should return 0 rows (or only test data)
```

**Test 6.3 - Soft Delete Pattern**:
```sql
-- Verify soft delete columns exist and work
SELECT
    "IsDeleted",
    "DeletedAt",
    "DeletedBy",
    "Description"
FROM tenant_default."DeviceApiKeys"
WHERE "IsDeleted" = true;
-- Should show only deleted/revoked API keys
```

---

### 📊 Phase 7: End-to-End Workflow (MANUAL)

**Complete User Journey**:

1. **Administrator Login**
   - Navigate to `http://localhost:4200`
   - Login with admin credentials
   - Verify dashboard loads successfully

2. **Create Biometric Device**
   - Go to: Organization → Biometric Devices
   - Click "Add New Device"
   - Fill form:
     - Device Code: `DEV-TEST-001`
     - Device Type: `ZKTeco`
     - IP Address: `192.168.1.100`
     - Port: `4370`
     - Location: Office entrance
     - Description: Test device
   - Click "Save"
   - **Verify**: Device appears in list

3. **Generate API Key**
   - Click on newly created device
   - Click "Generate API Key"
   - Description: `"Production API Key - Office Entrance"`
   - Expiration: 1 year from now
   - IP Whitelist: `192.168.1.0/24`
   - Rate Limit: `60` req/min
   - Click "Generate"
   - **IMPORTANT**: Copy the API key (shown once only)

4. **Test Connection** (Expected to timeout in cloud environment)
   - In device detail page
   - Click "Test Connection"
   - **Expected**: Timeout (normal for cloud → local network)
   - **Note**: Will work in production when API is on same network

5. **Simulate Device Attendance Submission**
   - Use copied API key with curl:
   ```bash
   curl -X POST http://localhost:5090/api/device-webhook/attendance \
     -H "X-API-Key: YOUR_COPIED_API_KEY" \
     -H "Content-Type: application/json" \
     -d '{
       "deviceId": "DEV-TEST-001",
       "apiKey": "YOUR_COPIED_API_KEY",
       "timestamp": "2025-11-14T10:00:00Z",
       "records": [{
         "employeeId": "EMP001",
         "punchTime": "2025-11-14T09:00:00Z",
         "punchType": 0,
         "verifyMode": 1,
         "deviceRecordId": "12345"
       }]
     }'
   ```
   - **Verify**: HTTP 200 OK response

6. **Check Attendance Record in Database**
   ```sql
   SELECT * FROM tenant_default."BiometricPunchRecords"
   WHERE "DeviceRecordId" = '12345'
   ORDER BY "CreatedAt" DESC LIMIT 1;
   ```
   - **Verify**: Record exists with correct employee, punch time, device

7. **View API Key Usage Statistics**
   - Go back to device detail page
   - View API key list
   - **Verify**:
     - Usage count: 1
     - Last used: Current timestamp
     - Status: Active

---

## Known Limitations (Development Environment)

### ⚠️ Connection Test Will Timeout

**Issue**: When testing connection to device at `192.168.1.100:4370`, you'll get a timeout.

**Reason**: The API server is running in GitHub Codespaces (cloud), but the device IP is a private network address (RFC 1918). There's no network route from cloud to local network.

**This is NOT a bug** - the connection test feature is working correctly. It will succeed in production when:
- API server is deployed on-premises on same network as devices, OR
- Device has public IP address with port forwarding configured

**Test Response**:
```json
{
  "success": false,
  "message": "Connection timeout after 30 seconds",
  "responseTimeMs": 30000,
  "errorDetails": "Device at 192.168.1.100:4370 did not respond",
  "diagnostics": "Possible causes: Device offline, incorrect IP/port, network firewall",
  "testedAt": "2025-11-14T10:00:00Z"
}
```

---

## Test Results Template

Create a file `/tmp/manual_test_results.md` and document results:

```markdown
# Manual Test Results

**Tested By**: [Your Name]
**Date**: [Date]
**Environment**: Development

## Phase 3: Device Management
- [ ] List All Devices - PASS/FAIL
- [ ] Create New Device - PASS/FAIL
- [ ] Update Device - PASS/FAIL
- [ ] Delete Device - PASS/FAIL

## Phase 4: API Key Management
- [ ] Generate API Key - PASS/FAIL
- [ ] API Key Authentication - PASS/FAIL
- [ ] Invalid API Key Rejected - PASS/FAIL
- [ ] Validation Errors - PASS/FAIL
- [ ] Revoke API Key - PASS/FAIL

## Phase 5: Rate Limiting
- [ ] Rate limit enforced after 60 req/min - PASS/FAIL

## Phase 6: Security
- [ ] API keys hashed in database - PASS/FAIL
- [ ] Tenant isolation working - PASS/FAIL
- [ ] Soft delete pattern working - PASS/FAIL

## Phase 7: End-to-End
- [ ] Complete workflow successful - PASS/FAIL

## Issues Found
1. [List any issues discovered]
2. [Include error messages, screenshots]

## Overall Status
- [ ] READY FOR PRODUCTION
- [ ] ISSUES NEED RESOLUTION
```

---

## Success Criteria for Production Launch

### ✅ Must Pass (Critical):
1. Database schema complete (18 columns)
2. API key generation works
3. API key authentication works
4. Invalid API keys rejected (401)
5. API keys hashed in database (never plaintext)
6. Tenant isolation verified
7. Attendance submission successful with valid API key
8. Rate limiting enforced

### ✅ Should Pass (Important):
1. Frontend validation prevents bad input
2. Soft delete working for API key revocation
3. Usage statistics tracking (count, last used)
4. CORS properly configured for frontend-backend communication

### ⏳ Optional (Nice to Have):
1. Connection test (will work in production, not in dev cloud environment)
2. IP whitelisting validation
3. API key expiration enforcement
4. Heartbeat endpoint testing

---

## Quick Start - Run All Automated Tests

```bash
# 1. Verify services running
pg_isready -h localhost -p 5432
curl http://localhost:5090/api/device-webhook/ping
curl http://localhost:4200

# 2. Run automated database and API tests
bash /tmp/automated_api_tests.sh

# 3. Open frontend for manual testing
# Navigate to: http://localhost:4200
# Follow Phase 4, 5, 6, 7 manual test procedures
```

---

## Support & Documentation

**Files Created**:
- `/tmp/automated_api_tests.sh` - Automated infrastructure tests
- `/tmp/dev_test_biometric_api.sh` - Comprehensive test script (includes manual prompts)
- `/workspaces/HRAPP/DEVELOPMENT_TESTING_PLAN.md` - This document
- `/workspaces/HRAPP/FINAL_FIX_SUMMARY.md` - Summary of all issues fixed
- `/workspaces/HRAPP/CORS_FIX_INSTRUCTIONS.md` - CORS configuration guide

**Endpoints Reference**:
- Device Management: `/api/biometric-devices/*` (requires JWT)
- Device Webhooks: `/api/device-webhook/*` (uses API key)
- Health Check: `GET /api/device-webhook/ping`

**Database**:
- Schema: `tenant_default`
- Key Table: `DeviceApiKeys` (18 columns)
- Connection: `postgres@localhost:5432/hrms_db`

---

**Last Updated**: November 14, 2025
**Status**: Ready for Development Testing
**Next Step**: Execute manual tests in Phase 4-7, document results
