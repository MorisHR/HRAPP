# HRMS Production Deployment Guide

**Document Version:** 1.0
**Last Updated:** November 14, 2025
**Target:** Fortune 500 Enterprise Production Environment

---

## Table of Contents

1. [Pre-Deployment Security Checklist](#pre-deployment-security-checklist)
2. [Environment Setup](#environment-setup)
3. [Secrets Management](#secrets-management)
4. [Database Configuration](#database-configuration)
5. [Infrastructure Requirements](#infrastructure-requirements)
6. [Deployment Steps](#deployment-steps)
7. [Post-Deployment Validation](#post-deployment-validation)
8. [Rollback Procedures](#rollback-procedures)
9. [Monitoring and Alerts](#monitoring-and-alerts)
10. [Security Hardening](#security-hardening)

---

## Pre-Deployment Security Checklist

**CRITICAL: Complete ALL items before deploying to production**

### Configuration Verification
- [ ] All hardcoded secrets removed from source code
- [ ] Environment variables configured (see `.env.production.template`)
- [ ] Google Secret Manager secrets created
- [ ] Database credentials rotated from default values
- [ ] JWT secret generated (minimum 256-bit)
- [ ] Encryption key generated (AES-256)
- [ ] SMTP credentials configured and tested
- [ ] CORS origins whitelisted for production domains only
- [ ] API keys generated for all biometric devices
- [ ] SuperAdmin secret path configured (server-side only)

### Security Configuration
- [ ] `ASPNETCORE_ENVIRONMENT` set to `Production`
- [ ] Swagger disabled (verify `/swagger` returns 404)
- [ ] Debug symbols removed from release build
- [ ] Correlation IDs not exposed in production error responses
- [ ] Sensitive data logging removed (no TOTP codes, secrets, or tokens in logs)
- [ ] API key validation enabled for device endpoints
- [ ] HTTPS enforced (no HTTP in production)
- [ ] SSL certificates valid and not expired

### Infrastructure Readiness
- [ ] Load balancer configured with health check endpoints
- [ ] Database backup strategy implemented and tested
- [ ] Redis cache configured for distributed sessions
- [ ] Log aggregation configured (ELK, Splunk, or Cloud Logging)
- [ ] Monitoring and alerting configured
- [ ] Incident response plan documented
- [ ] On-call rotation established

### Testing Verification
- [ ] All unit tests passing
- [ ] Integration tests passing
- [ ] Security tests completed (OWASP Top 10)
- [ ] Load testing completed (500+ concurrent users)
- [ ] Tenant isolation validated (no cross-tenant data access)
- [ ] Database migrations tested on production-like dataset
- [ ] Rollback procedure tested in staging

---

## Environment Setup

### Required Environment Variables

Set these environment variables in your production environment:

```bash
# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# Database Connection
# Format: Host={host};Port={port};Database={db};Username={user};Password={password};SSL Mode=Require
ConnectionStrings__DefaultConnection="REPLACE_WITH_GOOGLE_SECRET_OR_ENV_VAR"

# JWT Configuration
JwtSettings__Secret="REPLACE_WITH_256BIT_SECRET"
JwtSettings__Issuer="HRMS.Production"
JwtSettings__Audience="HRMS.Client"
JwtSettings__ExpirationMinutes=60
JwtSettings__RefreshTokenExpirationDays=7

# Encryption Configuration
Encryption__Key="REPLACE_WITH_256BIT_ENCRYPTION_KEY"
Encryption__KeyVersion="v1"
Encryption__Enabled=true

# Email Configuration (SMTP2GO or other provider)
EmailSettings__SmtpServer="mail.smtp2go.com"
EmailSettings__SmtpPort=2525
EmailSettings__SmtpUsername="REPLACE_WITH_SMTP_USERNAME"
EmailSettings__SmtpPassword="REPLACE_WITH_SMTP_PASSWORD"
EmailSettings__FromEmail="noreply@yourdomain.com"
EmailSettings__FromName="HRMS System"
EmailSettings__EnableSsl=true

# Authentication
Auth__SuperAdminSecretPath="REPLACE_WITH_RANDOM_UUID_PATH"

# Google Cloud Configuration
GoogleCloud__ProjectId="your-gcp-project-id"
GoogleCloud__SecretManagerEnabled=true
GoogleCloud__CloudSqlInstanceConnectionName="project:region:instance"
GoogleCloud__StorageBucket="hrms-uploads-prod"

# Redis Configuration (for distributed cache)
Redis__ConnectionString="your-redis-connection-string"
Redis__InstanceName="HRMS_PROD_"
Redis__DefaultCacheExpirationMinutes=60

# Security Settings
Security__RequireHttpsMetadata=true
Security__EnableApiKeyValidation=true

# CORS Configuration
Cors__AllowedOrigins__0="https://app.yourdomain.com"
Cors__AllowedOrigins__1="https://admin.yourdomain.com"

# Frontend URL
AppSettings__FrontendUrl="https://app.yourdomain.com"
AppSettings__ProductionUrl="https://yourdomain.com"
```

---

## Secrets Management

### Google Secret Manager Setup

1. **Install Google Cloud SDK:**
   ```bash
   curl https://sdk.cloud.google.com | bash
   gcloud init
   ```

2. **Create Required Secrets:**

   ```bash
   # Database Password
   echo -n "YOUR_SECURE_DB_PASSWORD" | gcloud secrets create DB_PASSWORD --data-file=-

   # JWT Secret (generate with: openssl rand -base64 32)
   openssl rand -base64 32 | gcloud secrets create JWT_SECRET --data-file=-

   # Encryption Key (generate with: openssl rand -base64 32)
   openssl rand -base64 32 | gcloud secrets create ENCRYPTION_KEY_V1 --data-file=-

   # SMTP Password
   echo -n "YOUR_SMTP_PASSWORD" | gcloud secrets create SMTP_PASSWORD --data-file=-

   # SuperAdmin Secret Path
   echo -n "system-$(uuidgen)" | gcloud secrets create SUPERADMIN_SECRET_PATH --data-file=-
   ```

3. **Grant Access to Service Account:**
   ```bash
   gcloud secrets add-iam-policy-binding DB_PASSWORD \
     --member="serviceAccount:YOUR_SERVICE_ACCOUNT@PROJECT.iam.gserviceaccount.com" \
     --role="roles/secretmanager.secretAccessor"

   # Repeat for all secrets
   ```

4. **Verify Access:**
   ```bash
   gcloud secrets versions access latest --secret="JWT_SECRET"
   ```

### Secret Rotation Schedule

- **Database Password:** Every 90 days
- **JWT Secret:** Every 180 days (requires token invalidation)
- **Encryption Key:** Every 365 days (requires data re-encryption)
- **API Keys:** Every 90 days per device
- **SMTP Password:** Every 180 days

---

## Database Configuration

### PostgreSQL Production Setup

1. **Create Production Database:**
   ```sql
   CREATE DATABASE hrms_production;
   CREATE USER hrms_prod_user WITH ENCRYPTED PASSWORD 'SECURE_PASSWORD';
   GRANT ALL PRIVILEGES ON DATABASE hrms_production TO hrms_prod_user;

   -- Enable required extensions
   \c hrms_production
   CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
   CREATE EXTENSION IF NOT EXISTS "pgcrypto";
   ```

2. **Configure Connection Pooling:**
   - MaxPoolSize: 200 (adjust based on load testing)
   - MinPoolSize: 20
   - Connection Lifetime: 300 seconds
   - SSL Mode: Require
   - Trust Server Certificate: false

3. **Run Database Migrations:**
   ```bash
   cd src/HRMS.API
   dotnet ef database update --environment Production
   ```

4. **Verify Migrations:**
   ```sql
   SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
   ```

5. **Create Initial Backups:**
   ```bash
   pg_dump -U hrms_prod_user -h localhost hrms_production > hrms_production_backup_$(date +%Y%m%d).sql
   ```

### Database Performance Tuning

```sql
-- Create indexes for frequently queried fields
CREATE INDEX idx_employees_tenant_id ON Employees(TenantId);
CREATE INDEX idx_audit_logs_timestamp ON AuditLogs(Timestamp);
CREATE INDEX idx_attendance_employee_date ON AttendanceRecords(EmployeeId, PunchDate);

-- Analyze tables for query optimization
ANALYZE Employees;
ANALYZE AttendanceRecords;
ANALYZE PayrollProcessingRecords;
```

---

## Infrastructure Requirements

### Minimum Server Requirements

**Application Server:**
- CPU: 4 cores minimum (8 cores recommended)
- RAM: 8GB minimum (16GB recommended)
- Storage: 50GB SSD
- Network: 1Gbps

**Database Server:**
- CPU: 8 cores minimum
- RAM: 16GB minimum (32GB recommended)
- Storage: 500GB SSD with RAID 10
- IOPS: 10,000+ provisioned IOPS

**Redis Cache Server:**
- CPU: 2 cores
- RAM: 4GB minimum
- Storage: 20GB SSD

### Network Configuration

1. **Firewall Rules:**
   ```
   ALLOW: HTTPS (443) from Load Balancer
   ALLOW: HTTP (5000) from Load Balancer (internal)
   ALLOW: PostgreSQL (5432) from Application Server only
   ALLOW: Redis (6379) from Application Server only
   DENY: All other inbound traffic
   ```

2. **Load Balancer Configuration:**
   - Health Check Path: `/health/ready`
   - Health Check Interval: 10 seconds
   - Unhealthy Threshold: 3 consecutive failures
   - Healthy Threshold: 2 consecutive successes
   - Timeout: 5 seconds

3. **SSL/TLS Configuration:**
   - TLS 1.2 minimum (TLS 1.3 recommended)
   - Strong cipher suites only
   - HSTS enabled (Strict-Transport-Security header)
   - Certificate from trusted CA (no self-signed)

---

## Deployment Steps

### Step 1: Build Release Package

```bash
# Clean previous builds
dotnet clean --configuration Release

# Build release version (without debug symbols)
dotnet build --configuration Release --no-restore

# Publish self-contained deployment
dotnet publish src/HRMS.API/HRMS.API.csproj \
  --configuration Release \
  --output ./publish \
  --self-contained true \
  --runtime linux-x64
```

### Step 2: Deploy to Production Server

```bash
# Stop existing service (if running)
sudo systemctl stop hrms-api

# Backup current deployment
sudo mv /var/www/hrms-api /var/www/hrms-api.backup.$(date +%Y%m%d_%H%M%S)

# Copy new deployment
sudo cp -r ./publish /var/www/hrms-api

# Set permissions
sudo chown -R www-data:www-data /var/www/hrms-api
sudo chmod -R 755 /var/www/hrms-api

# Copy environment configuration
sudo cp /secure/hrms/.env.production /var/www/hrms-api/.env
```

### Step 3: Configure Systemd Service

Create `/etc/systemd/system/hrms-api.service`:

```ini
[Unit]
Description=HRMS API Production Service
After=network.target postgresql.service redis.service

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/var/www/hrms-api
ExecStart=/var/www/hrms-api/HRMS.API
Restart=on-failure
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=hrms-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
EnvironmentFile=/var/www/hrms-api/.env

# Security hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/www/hrms-api/Logs

[Install]
WantedBy=multi-user.target
```

### Step 4: Start Service

```bash
# Reload systemd configuration
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable hrms-api

# Start service
sudo systemctl start hrms-api

# Check status
sudo systemctl status hrms-api

# View logs
sudo journalctl -u hrms-api -f
```

---

## Post-Deployment Validation

### Health Checks

1. **API Health Check:**
   ```bash
   curl https://api.yourdomain.com/health/ready
   # Expected: HTTP 200 with JSON response
   ```

2. **Database Connectivity:**
   ```bash
   curl https://api.yourdomain.com/health
   # Check that PostgreSQL status is "Healthy"
   ```

3. **Redis Connectivity:**
   ```bash
   # Verify Redis cache is working
   redis-cli -h your-redis-host ping
   ```

### Functional Testing

1. **User Authentication:**
   ```bash
   curl -X POST https://api.yourdomain.com/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","password":"TestPassword123!"}'
   ```

2. **Tenant Isolation:**
   - Login as user from Tenant A
   - Attempt to access Tenant B data
   - Verify 403 Forbidden response

3. **Rate Limiting:**
   ```bash
   # Send 101 requests in 1 minute (limit is 100)
   for i in {1..101}; do curl https://api.yourdomain.com/api/health; done
   # 101st request should return HTTP 429 (Too Many Requests)
   ```

4. **CORS Verification:**
   ```bash
   curl -H "Origin: https://unauthorized-domain.com" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS https://api.yourdomain.com/api/employees
   # Should NOT return Access-Control-Allow-Origin header
   ```

### Security Validation

1. **Swagger Disabled:**
   ```bash
   curl https://api.yourdomain.com/swagger
   # Expected: HTTP 404 Not Found
   ```

2. **HTTPS Enforced:**
   ```bash
   curl -I http://api.yourdomain.com
   # Expected: HTTP 301/308 redirect to HTTPS
   ```

3. **Debug Symbols Removed:**
   ```bash
   file /var/www/hrms-api/HRMS.API
   # Should NOT contain "with debug_info"
   ```

4. **Secrets Not in Logs:**
   ```bash
   sudo journalctl -u hrms-api | grep -i "password\|secret\|key" | wc -l
   # Should be 0 (no secrets in logs)
   ```

---

## Rollback Procedures

### Emergency Rollback (< 5 minutes)

```bash
# Stop current service
sudo systemctl stop hrms-api

# Restore previous deployment
sudo rm -rf /var/www/hrms-api
sudo mv /var/www/hrms-api.backup.YYYYMMDD_HHMMSS /var/www/hrms-api

# Restart service
sudo systemctl start hrms-api

# Verify health
curl https://api.yourdomain.com/health/ready
```

### Database Rollback

```bash
# Restore database from backup
pg_restore -U hrms_prod_user -d hrms_production -c hrms_production_backup_YYYYMMDD.sql

# Verify data integrity
psql -U hrms_prod_user -d hrms_production -c "SELECT COUNT(*) FROM Employees;"
```

---

## Monitoring and Alerts

### Key Metrics to Monitor

1. **Application Metrics:**
   - Request throughput (requests/second)
   - Response time (p50, p95, p99)
   - Error rate (4xx, 5xx errors)
   - Active connections
   - Memory usage
   - CPU usage

2. **Database Metrics:**
   - Connection pool usage
   - Query execution time
   - Deadlocks
   - Replication lag (if using replicas)
   - Disk space usage

3. **Security Metrics:**
   - Failed login attempts
   - Rate limit violations
   - Anomaly detection alerts
   - Unauthorized access attempts

### Alert Thresholds

```yaml
Critical Alerts:
  - Error rate > 5%
  - Response time p95 > 2 seconds
  - Database connection pool > 90% used
  - Disk space > 85% full
  - Failed login attempts > 50/minute
  - CPU usage > 90% for 5 minutes

Warning Alerts:
  - Error rate > 1%
  - Response time p95 > 1 second
  - Memory usage > 80%
  - Anomaly detection triggered
```

---

## Security Hardening

### Additional Security Measures

1. **Web Application Firewall (WAF):**
   - Enable OWASP ModSecurity rules
   - Block SQL injection attempts
   - Block XSS attempts
   - Rate limit by IP address

2. **DDoS Protection:**
   - Cloudflare or AWS Shield
   - Geographic restrictions if applicable
   - Behavioral analysis

3. **Security Scanning:**
   ```bash
   # Run OWASP Dependency Check
   dotnet list package --vulnerable

   # Run static code analysis
   dotnet format --verify-no-changes
   ```

4. **Audit Logging:**
   - All authentication attempts (success/failure)
   - All data modifications (create, update, delete)
   - All permission changes
   - All configuration changes
   - Retain logs for minimum 7 years (compliance)

5. **Penetration Testing:**
   - Schedule annual penetration testing
   - Address all findings before production deployment
   - Re-test after fixes

---

## Compliance Checklist

### GDPR Compliance
- [ ] Data encryption at rest (AES-256)
- [ ] Data encryption in transit (TLS 1.2+)
- [ ] Right to access implemented
- [ ] Right to be forgotten implemented
- [ ] Data breach notification procedures
- [ ] Privacy policy published
- [ ] Consent management implemented

### SOX Compliance
- [ ] Audit trail for all financial transactions
- [ ] Immutable audit logs
- [ ] Separation of duties
- [ ] Access controls for payroll data
- [ ] Change management procedures

### PCI-DSS (if processing payments)
- [ ] Cardholder data encrypted
- [ ] Strong access controls
- [ ] Secure network architecture
- [ ] Regular security testing
- [ ] Information security policy

---

## Support and Escalation

### On-Call Contacts

- **Level 1 Support:** support@yourdomain.com
- **Level 2 Engineering:** engineering@yourdomain.com
- **Security Team:** security@yourdomain.com
- **Emergency Hotline:** +1-XXX-XXX-XXXX

### Incident Response

1. **Severity 1 (Critical):** Response within 15 minutes
2. **Severity 2 (High):** Response within 1 hour
3. **Severity 3 (Medium):** Response within 4 hours
4. **Severity 4 (Low):** Response within 24 hours

---

**Document End**

For questions or issues, contact: engineering@morishr.com
