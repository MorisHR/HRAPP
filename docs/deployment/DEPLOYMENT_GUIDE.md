# 🚀 HRMS DEPLOYMENT GUIDE - Security Fixes
## Production Deployment Checklist
**Date:** 2025-11-02
**Version:** 1.0.0 (Security Hardened)

---

## ✅ PRE-DEPLOYMENT CHECKLIST

### 1. Code Compilation
- [✅] Build successful - No compilation errors
- [✅] All 12 security fixes implemented
- [✅] AspNetCoreRateLimit package installed
- [✅] Using directive added for rate limiting

### 2. Database Migrations Required
- [⚠️] **IMPORTANT:** Must run migrations before deployment
- [⚠️] AdminUser table needs lockout fields
- [⚠️] Employee table needs lockout fields

---

## 📋 STEP-BY-STEP DEPLOYMENT

### **STEP 1: Database Migrations (REQUIRED)**

#### Option A: Using EF Core Migrations (Recommended)

```bash
# Navigate to the API project directory
cd /workspaces/HRAPP/src/HRMS.API

# Create migrations for lockout fields
dotnet ef migrations add AddAccountLockoutFields \
  --context MasterDbContext \
  --output-dir ../HRMS.Infrastructure/Migrations/Master

dotnet ef migrations add AddEmployeeLockoutFields \
  --context TenantDbContext \
  --output-dir ../HRMS.Infrastructure/Migrations/Tenant

# Apply migrations to database
dotnet ef database update --context MasterDbContext
dotnet ef database update --context TenantDbContext
```

#### Option B: Manual SQL (If EF Migrations Fail)

**For MasterDbContext (AdminUser table):**
```sql
-- Run this on the master schema
ALTER TABLE master.admin_users
ADD COLUMN lockout_enabled BOOLEAN NOT NULL DEFAULT true,
ADD COLUMN lockout_end TIMESTAMP NULL,
ADD COLUMN access_failed_count INTEGER NOT NULL DEFAULT 0;

-- Verify the columns were added
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'master'
  AND table_name = 'admin_users'
  AND column_name IN ('lockout_enabled', 'lockout_end', 'access_failed_count');
```

**For TenantDbContext (Employee table):**
```sql
-- Run this for EACH tenant schema (replace tenant_1 with actual schema names)
ALTER TABLE tenant_1.employees
ADD COLUMN lockout_enabled BOOLEAN NOT NULL DEFAULT true,
ADD COLUMN lockout_end TIMESTAMP NULL,
ADD COLUMN access_failed_count INTEGER NOT NULL DEFAULT 0;

ALTER TABLE tenant_2.employees
ADD COLUMN lockout_enabled BOOLEAN NOT NULL DEFAULT true,
ADD COLUMN lockout_end TIMESTAMP NULL,
ADD COLUMN access_failed_count INTEGER NOT NULL DEFAULT 0;

-- Verify for each tenant
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'tenant_1'
  AND table_name = 'employees'
  AND column_name IN ('lockout_enabled', 'lockout_end', 'access_failed_count');
```

---

### **STEP 2: Configuration Updates**

#### Update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_PRODUCTION_CONNECTION_STRING"
  },
  "JwtSettings": {
    "Secret": "YOUR_STRONG_JWT_SECRET_MIN_32_CHARS",
    "Issuer": "HRMS.API",
    "Audience": "HRMS.Client",
    "ExpirationMinutes": 60
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "HttpStatusCode": 429,
    "IpWhitelist": [],
    "EndpointWhitelist": ["get:/health", "get:/health/ready", "get:/"],
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 1000
      }
    ]
  },
  "IpRateLimitPolicies": {
    "IpRules": [
      {
        "Ip": "*",
        "Rules": [
          {
            "Endpoint": "POST:/api/auth/login",
            "Period": "15m",
            "Limit": 5
          }
        ]
      }
    ]
  },
  "Security": {
    "RequireHttpsMetadata": true
  },
  "Cors": {
    "AllowedOrigins": [
      "https://your-frontend-domain.com"
    ]
  }
}
```

---

### **STEP 3: Build for Production**

```bash
# Clean previous builds
dotnet clean HRMS.sln

# Build in Release mode
dotnet build HRMS.sln --configuration Release

# Expected output: Build succeeded with 0 Errors
```

---

### **STEP 4: Run Security Tests**

#### Test 1: Rate Limiting on Login
```bash
# Run 6 login attempts with wrong password
for i in {1..6}; do
  echo "Attempt $i:"
  curl -X POST http://localhost:5000/api/admin/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@hrms.com","password":"wrongpassword"}'
  echo -e "\n---"
done

# Expected Results:
# Attempts 1-5: HTTP 401 Unauthorized
# Attempt 6: HTTP 429 Too Many Requests
```

#### Test 2: Account Lockout
```bash
# After 5 failed login attempts, try correct password
curl -X POST http://localhost:5000/api/admin/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@hrms.com","password":"Admin@123"}'

# Expected: HTTP 423 Locked
# Message: "Account is locked until {timestamp}"
```

#### Test 3: Tenant Isolation
```bash
# Request without tenant context
curl -X GET http://localhost:5000/api/employees

# Expected: HTTP 400 Bad Request
# Message: "Tenant context required"
```

#### Test 4: Payslip Access Control
```bash
# Employee accessing own payslip (should work)
curl -X GET http://tenant1.localhost:5000/api/payroll/payslips/{own-id} \
  -H "Authorization: Bearer {EMPLOYEE_TOKEN}"
# Expected: HTTP 200 OK

# Employee accessing other's payslip (should fail)
curl -X GET http://tenant1.localhost:5000/api/payroll/payslips/{other-id} \
  -H "Authorization: Bearer {EMPLOYEE_TOKEN}"
# Expected: HTTP 403 Forbidden
```

#### Test 5: RBAC - Tenant Deletion
```bash
# Regular user attempting to delete tenant (should fail)
curl -X DELETE http://admin.localhost:5000/api/tenants/{id}/soft \
  -H "Authorization: Bearer {EMPLOYEE_TOKEN}"
# Expected: HTTP 403 Forbidden

# SuperAdmin deleting tenant (should work)
curl -X DELETE http://admin.localhost:5000/api/tenants/{id}/soft \
  -H "Authorization: Bearer {SUPERADMIN_TOKEN}"
# Expected: HTTP 200 OK
```

---

### **STEP 5: Deploy to Staging**

```bash
# Publish the application
dotnet publish src/HRMS.API/HRMS.API.csproj \
  --configuration Release \
  --output ./publish

# Deploy to staging server
# (Method depends on your infrastructure - Docker, IIS, Kestrel, etc.)
```

---

### **STEP 6: Post-Deployment Verification**

#### Check Application Logs
```bash
# Monitor logs for security warnings
tail -f Logs/hrms-*.log | grep "SECURITY"

# Look for:
# - "SECURITY: Employee {id} attempted to access..."
# - "Account lockout for email: {email}"
# - "Rate limit exceeded..."
```

#### Verify Health Endpoints
```bash
# Check basic health
curl http://your-domain.com/health

# Check detailed health (requires authentication)
curl http://your-domain.com/health/detailed \
  -H "Authorization: Bearer {ADMIN_TOKEN}"
```

#### Test Rate Limiting is Active
```bash
# Check rate limit headers in response
curl -v http://your-domain.com/api/employees \
  -H "Authorization: Bearer {TOKEN}"

# Look for headers:
# X-Rate-Limit-Limit: 100
# X-Rate-Limit-Remaining: 99
# X-Rate-Limit-Reset: {timestamp}
```

---

## 🔍 MONITORING & ALERTS

### Metrics to Track

1. **Security Metrics:**
   - Failed login attempts per hour
   - Account lockout events per day
   - Rate limit hits per endpoint
   - Unauthorized access attempts (403 responses)
   - Cross-tenant access attempts

2. **Performance Metrics:**
   - API response times
   - Database query performance
   - Memory usage (rate limiting cache)
   - Request throughput

3. **Application Health:**
   - Database connection status
   - Redis connection status (if used)
   - Background job execution
   - Error rates

### Recommended Alerts

```yaml
# Example alerting rules (adapt to your monitoring tool)

- Alert: HighFailedLoginRate
  Expression: failed_logins_per_minute > 10
  Severity: Warning
  Action: Notify security team

- Alert: MultipleAccountLockouts
  Expression: account_lockouts_per_hour > 5
  Severity: Critical
  Action: Investigate potential brute force attack

- Alert: UnauthorizedAccessSpike
  Expression: http_403_responses_per_minute > 20
  Severity: Warning
  Action: Review access logs

- Alert: RateLimitExceeded
  Expression: rate_limit_hits_per_minute > 100
  Severity: Info
  Action: Monitor for DDoS
```

---

## 🐛 TROUBLESHOOTING

### Issue: Rate Limiting Not Working

**Symptoms:** No 429 responses even after many requests

**Solution:**
```bash
# Verify AspNetCoreRateLimit package is installed
dotnet list src/HRMS.API/HRMS.API.csproj package | grep AspNetCoreRateLimit

# Check middleware order in Program.cs
# Rate limiting must be BEFORE authentication
app.UseIpRateLimiting();  // Should be here
app.UseAuthentication();   // After rate limiting
```

### Issue: Account Lockout Not Triggering

**Symptoms:** Users can attempt unlimited logins

**Solution:**
```sql
# Verify lockout fields exist in database
SELECT lockout_enabled, lockout_end, access_failed_count
FROM master.admin_users
WHERE email = 'admin@hrms.com';

# If columns don't exist, run migrations again
```

### Issue: Tenant Isolation Bypass

**Symptoms:** Users can access other tenants' data

**Solution:**
```bash
# Verify TenantContextValidationMiddleware is registered
# Check Program.cs line ~494
grep "UseTenantContextValidation" src/HRMS.API/Program.cs

# Verify middleware order:
# 1. UseTenantResolution
# 2. UseTenantContextValidation
# 3. UseAuthentication
```

### Issue: Payslip Access Not Restricted

**Symptoms:** Employees can view others' payslips

**Solution:**
```csharp
// Verify GetPayslip method includes ownership check
// File: src/HRMS.API/Controllers/PayrollController.cs:199-235

// Should include:
if (!isAdminOrHR)
{
    var currentEmployeeId = GetEmployeeIdFromToken();
    if (payslip.EmployeeId != currentEmployeeId)
    {
        return Forbid();
    }
}
```

---

## 📊 ROLLBACK PLAN

If issues are discovered in production:

### Step 1: Immediate Rollback
```bash
# Stop the application
systemctl stop hrms-api  # Or your process manager

# Restore previous version
cp /backup/hrms-api-previous /app/hrms-api

# Start previous version
systemctl start hrms-api
```

### Step 2: Database Rollback (If Needed)
```sql
-- Remove lockout columns if causing issues
ALTER TABLE master.admin_users
DROP COLUMN IF EXISTS lockout_enabled,
DROP COLUMN IF EXISTS lockout_end,
DROP COLUMN IF EXISTS access_failed_count;

ALTER TABLE tenant_1.employees
DROP COLUMN IF EXISTS lockout_enabled,
DROP COLUMN IF EXISTS lockout_end,
DROP COLUMN IF EXISTS access_failed_count;
```

### Step 3: Configuration Rollback
```bash
# Restore previous appsettings.json
cp /backup/appsettings.Production.json.bak /app/appsettings.Production.json

# Restart application
systemctl restart hrms-api
```

---

## ✅ DEPLOYMENT SIGN-OFF

Before marking deployment as complete, verify:

- [ ] Database migrations applied successfully
- [ ] Application builds without errors
- [ ] All security tests pass
- [ ] Rate limiting is active and working
- [ ] Account lockout triggers after 5 attempts
- [ ] Tenant isolation enforced
- [ ] Payslip access restricted to owners
- [ ] RBAC enforced on sensitive endpoints
- [ ] Monitoring and alerts configured
- [ ] Logs are being written correctly
- [ ] Backup/rollback plan tested

---

## 📞 SUPPORT & ESCALATION

### If Deployment Issues Occur:

1. **Check application logs:**
   ```bash
   tail -f Logs/hrms-*.log
   ```

2. **Review security audit reports:**
   - `CRITICAL_SECURITY_AUDIT_REPORT.md`
   - `PHASE2_SECURITY_FIXES_COMPLETE.md`

3. **Database issues:**
   - Verify connection string
   - Check migration status: `dotnet ef migrations list`
   - Review database logs

4. **Performance issues:**
   - Monitor memory usage
   - Check database connection pool
   - Review rate limiting cache size

---

## 🎉 SUCCESS CRITERIA

Deployment is successful when:

✅ **All 12 Security Fixes Active:**
1. Rate limiting prevents brute force
2. Account lockout after 5 failed logins
3. Tenant isolation enforced
4. Payslip access restricted
5. Leave approval requires authorization
6. Tenant deletion requires SuperAdmin
7. Employee management requires HR/Admin
8. Reports require Admin/HR/Manager
9. Setup reset requires SuperAdmin
10. X-Tenant-Subdomain header disabled in production
11. System builds without errors
12. All tests pass

✅ **Monitoring Active:**
- Security metrics tracked
- Alerts configured
- Logs reviewed

✅ **Performance Acceptable:**
- API response times < 200ms (p95)
- No memory leaks
- Database queries optimized

---

**Deployment Owner:** _________________
**Date Deployed:** _________________
**Sign-Off:** _________________

---

**END OF DEPLOYMENT GUIDE**
