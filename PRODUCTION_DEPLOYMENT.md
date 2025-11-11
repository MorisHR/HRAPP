# Production Deployment Guide
**MorisHR - Enterprise HRMS Platform**

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Environment Configuration](#environment-configuration)
3. [Google Cloud Setup](#google-cloud-setup)
4. [Email Configuration](#email-configuration)
5. [Database Setup](#database-setup)
6. [Application Deployment](#application-deployment)
7. [Post-Deployment Verification](#post-deployment-verification)
8. [Monitoring & Observability](#monitoring--observability)
9. [Security Checklist](#security-checklist)
10. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Accounts & Services
- [ ] Google Cloud Platform account with billing enabled
- [ ] PostgreSQL database (Cloud SQL or managed instance)
- [ ] Redis instance (for caching and rate limiting)
- [ ] Email provider account (Gmail with App Password / SendGrid / AWS SES)
- [ ] Domain configured with DNS access
- [ ] SSL/TLS certificates (Let's Encrypt or purchased)

### Required Tools
```bash
# Install .NET 9.0 SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# Install gcloud CLI
curl https://sdk.cloud.google.com | bash
exec -l $SHELL

# Install PostgreSQL client
sudo apt-get install postgresql-client
```

---

## Environment Configuration

### 1. Set Environment Variables

Create a `.env.production` file (DO NOT commit to git):

```bash
# Application Environment
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080

# Google Cloud Project
GCP_PROJECT_ID=your-project-id
GCP_REGION=us-central1

# Connection Strings (managed via Secret Manager)
# DO NOT hardcode - these are loaded from Secret Manager
# Listed here for reference only
# - ConnectionStrings__DefaultConnection
# - JwtSettings__Secret
# - EmailSettings__SmtpPassword
# - Redis__ConnectionString
```

### 2. Application Settings Hierarchy

Settings are loaded in this order (later overrides earlier):
1. `appsettings.json` (base configuration)
2. `appsettings.Production.json` (production overrides)
3. Google Secret Manager (sensitive values)
4. Environment variables (highest priority)

---

## Google Cloud Setup

### 1. Enable Required APIs

```bash
gcloud config set project YOUR_PROJECT_ID

# Enable required APIs
gcloud services enable \
  secretmanager.googleapis.com \
  sqladmin.googleapis.com \
  cloudrun.googleapis.com \
  cloudlogging.googleapis.com \
  cloudmonitoring.googleapis.com \
  cloudscheduler.googleapis.com
```

### 2. Configure Google Secret Manager

#### Create Service Account
```bash
# Create service account for the application
gcloud iam service-accounts create hrms-app \
  --display-name="HRMS Application Service Account"

# Grant Secret Manager access
gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
  --member="serviceAccount:hrms-app@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"
```

#### Create Secrets

```bash
# Database Connection String
echo -n "Host=YOUR_SQL_INSTANCE_IP;Port=5432;Database=hrms_prod;Username=hrms_user;Password=YOUR_DB_PASSWORD;Pooling=true;Minimum Pool Size=10;Maximum Pool Size=200;Connection Lifetime=300;Connection Idle Lifetime=60;" | \
gcloud secrets create ConnectionStrings__DefaultConnection \
  --data-file=-

# JWT Secret (generate a secure random secret)
openssl rand -base64 64 | tr -d '\n' | \
gcloud secrets create JwtSettings__Secret \
  --data-file=-

# SMTP Password
echo -n "YOUR_SMTP_PASSWORD" | \
gcloud secrets create EmailSettings__SmtpPassword \
  --data-file=-

# Redis Connection String
echo -n "YOUR_REDIS_HOST:6379,password=YOUR_REDIS_PASSWORD,ssl=true,abortConnect=false" | \
gcloud secrets create Redis__ConnectionString \
  --data-file=-
```

#### Update Secrets (when needed)
```bash
# Update a secret with new value
echo -n "NEW_VALUE" | gcloud secrets versions add SECRET_NAME --data-file=-

# List all secrets
gcloud secrets list

# View secret metadata (not the actual value)
gcloud secrets describe SECRET_NAME

# Delete old secret versions (keep last 3)
gcloud secrets versions destroy VERSION_NUMBER --secret=SECRET_NAME
```

### 3. Configure Application to Use Secret Manager

In `appsettings.Production.json`:
```json
{
  "GoogleCloud": {
    "ProjectId": "your-project-id",
    "SecretManagerEnabled": true,
    "CloudSqlInstanceConnectionName": "project-id:region:instance-name",
    "StorageBucket": "hrms-uploads-prod"
  }
}
```

---

## Email Configuration

### Option 1: Gmail with App Password (Recommended for Small Scale)

#### Step 1: Enable 2-Factor Authentication
1. Go to Google Account settings: https://myaccount.google.com/security
2. Enable 2-Step Verification

#### Step 2: Generate App Password
1. Go to: https://myaccount.google.com/apppasswords
2. Select "Mail" and "Other (Custom name)"
3. Name it "MorisHR Production"
4. Copy the 16-character password

#### Step 3: Configure Email Settings
```bash
# Create secret for SMTP password
echo -n "YOUR_16_CHAR_APP_PASSWORD" | \
gcloud secrets create EmailSettings__SmtpPassword --data-file=-

# Create secret for SMTP username
echo -n "your-email@gmail.com" | \
gcloud secrets create EmailSettings__SmtpUsername --data-file=-
```

#### Step 4: Update appsettings.Production.json
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "MorisHR",
    "EnableSsl": true,
    "UseDefaultCredentials": false
  }
}
```

**Gmail Limitations:**
- Max 500 emails per day
- Max 500 recipients per email
- Subject to Gmail sending policies

---

### Option 2: SendGrid (Recommended for Production)

#### Step 1: Create SendGrid Account
1. Sign up at: https://signup.sendgrid.com/
2. Verify your sender identity (domain or single sender)

#### Step 2: Generate API Key
1. Go to Settings > API Keys
2. Create API Key with "Mail Send" permission
3. Copy the API key (you won't see it again)

#### Step 3: Configure Email Settings
```bash
# Create secret for SendGrid API Key
echo -n "SG.your_api_key_here" | \
gcloud secrets create EmailSettings__SmtpPassword --data-file=-

echo -n "apikey" | \
gcloud secrets create EmailSettings__SmtpUsername --data-file=-
```

#### Step 4: Update appsettings.Production.json
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "MorisHR",
    "EnableSsl": true,
    "UseDefaultCredentials": false
  }
}
```

**SendGrid Benefits:**
- Free tier: 100 emails/day
- Essentials plan: $19.95/mo for 50,000 emails/month
- Email analytics and delivery insights
- Dedicated IP addresses (higher tiers)

---

### Option 3: AWS SES (Best for High Volume)

#### Step 1: Set Up AWS SES
```bash
# Install AWS CLI
pip install awscli

# Configure AWS credentials
aws configure

# Verify sender email
aws ses verify-email-identity --email-address noreply@yourdomain.com --region us-east-1
```

#### Step 2: Create SMTP Credentials
1. Go to AWS SES Console > SMTP Settings
2. Click "Create My SMTP Credentials"
3. Download credentials (username and password)

#### Step 3: Configure Email Settings
```bash
# Create secrets
echo -n "YOUR_SMTP_USERNAME" | \
gcloud secrets create EmailSettings__SmtpUsername --data-file=-

echo -n "YOUR_SMTP_PASSWORD" | \
gcloud secrets create EmailSettings__SmtpPassword --data-file=-
```

#### Step 4: Update appsettings.Production.json
```json
{
  "EmailSettings": {
    "SmtpServer": "email-smtp.us-east-1.amazonaws.com",
    "SmtpPort": 587,
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "MorisHR",
    "EnableSsl": true,
    "UseDefaultCredentials": false
  }
}
```

**AWS SES Benefits:**
- $0.10 per 1,000 emails
- Extremely high deliverability
- 62,000 free emails per month (if sending from EC2)
- Scales to millions of emails

---

### Email Testing

After configuration, test email delivery:

```bash
# Using curl to test via API
curl -X POST https://your-domain.com/api/admin/emailtest/send-test \
  -H "Authorization: Bearer YOUR_SUPERADMIN_JWT" \
  -H "Content-Type: application/json" \
  -d '{"toEmail":"admin@yourdomain.com"}'
```

Or use the admin portal:
1. Login as SuperAdmin
2. Navigate to System > Email Testing
3. Click "Send Test Email"
4. Check email delivery and verify template rendering

---

## Database Setup

### 1. Create Cloud SQL Instance (PostgreSQL)

```bash
# Create PostgreSQL instance
gcloud sql instances create hrms-prod-db \
  --database-version=POSTGRES_15 \
  --tier=db-custom-2-8192 \
  --region=us-central1 \
  --storage-type=SSD \
  --storage-size=20GB \
  --backup \
  --backup-start-time=03:00 \
  --maintenance-window-day=SUN \
  --maintenance-window-hour=04 \
  --database-flags=max_connections=200

# Create database
gcloud sql databases create hrms_prod \
  --instance=hrms-prod-db

# Create database user
gcloud sql users create hrms_user \
  --instance=hrms-prod-db \
  --password=GENERATE_SECURE_PASSWORD
```

### 2. Configure Connection Pooling

Update the connection string in Secret Manager:
```bash
# Production-optimized connection string
Host=YOUR_INSTANCE_IP;Port=5432;Database=hrms_prod;Username=hrms_user;Password=YOUR_PASSWORD;
Pooling=true;
Minimum Pool Size=10;
Maximum Pool Size=200;
Connection Lifetime=300;
Connection Idle Lifetime=60;
Command Timeout=30;
Keepalive=30;
```

### 3. Run Database Migrations

```bash
# Clone repository
git clone https://github.com/yourusername/HRAPP.git
cd HRAPP/src/HRMS.API

# Update connection string temporarily for migration
export ConnectionStrings__DefaultConnection="YOUR_CONNECTION_STRING"

# Run migrations
dotnet ef database update --context MasterDbContext
dotnet ef database update --context TenantDbContext

# Verify migrations
dotnet ef migrations list --context MasterDbContext
```

### 4. Create Initial SuperAdmin User

```bash
# Call setup endpoint (only works if no admin exists)
curl -X POST https://your-domain.com/api/admin/setup/create-first-admin \
  -H "Content-Type: application/json"

# Response will contain temporary credentials
# Save these securely and change password immediately
```

---

## Application Deployment

### Option 1: Docker Deployment

#### Build Docker Image
```bash
cd /workspaces/HRAPP

# Build production image
docker build -t gcr.io/YOUR_PROJECT_ID/hrms-api:latest \
  -f src/HRMS.API/Dockerfile .

# Push to Google Container Registry
docker push gcr.io/YOUR_PROJECT_ID/hrms-api:latest
```

#### Deploy to Cloud Run
```bash
gcloud run deploy hrms-api \
  --image gcr.io/YOUR_PROJECT_ID/hrms-api:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --service-account hrms-app@YOUR_PROJECT_ID.iam.gserviceaccount.com \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --memory 2Gi \
  --cpu 2 \
  --max-instances 10 \
  --min-instances 1 \
  --timeout 300 \
  --concurrency 80
```

### Option 2: VM Deployment (Ubuntu)

```bash
# Install .NET Runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-9.0

# Create application directory
sudo mkdir -p /var/www/hrms
sudo chown $USER:$USER /var/www/hrms

# Deploy application files
dotnet publish -c Release -o /var/www/hrms

# Create systemd service
sudo nano /etc/systemd/system/hrms-api.service
```

**Service File Content:**
```ini
[Unit]
Description=MorisHR API
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/hrms
ExecStart=/usr/bin/dotnet /var/www/hrms/HRMS.API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=hrms-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
# Enable and start service
sudo systemctl enable hrms-api
sudo systemctl start hrms-api
sudo systemctl status hrms-api
```

### Configure Nginx Reverse Proxy

```bash
sudo apt-get install nginx

# Create Nginx configuration
sudo nano /etc/nginx/sites-available/hrms
```

**Nginx Configuration:**
```nginx
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;

    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # API proxy
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;

        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Health check endpoint
    location /health {
        proxy_pass http://localhost:5000/health;
        access_log off;
    }
}
```

```bash
# Enable site and reload Nginx
sudo ln -s /etc/nginx/sites-available/hrms /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

---

## Post-Deployment Verification

### 1. Health Checks

```bash
# Check API health
curl https://your-domain.com/health

# Expected response:
{
  "status": "Healthy",
  "results": {
    "database": "Healthy",
    "redis": "Healthy",
    "hangfire": "Healthy"
  }
}
```

### 2. Verify Background Jobs (Hangfire)

```bash
# Check Hangfire dashboard (SuperAdmin only)
# Navigate to: https://your-domain.com/hangfire

# Verify scheduled jobs:
# - Subscription expiry checker (daily at 00:00 UTC)
# - Email queue processor (every 5 minutes)
# - Audit log cleanup (weekly)
```

### 3. Test Email Delivery

```bash
# Send test email via API
curl -X POST https://your-domain.com/api/admin/emailtest/send-test \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{"toEmail":"test@yourdomain.com"}'

# Check email received and verify:
# - Proper formatting
# - Images load correctly
# - Links work
# - Responsive design on mobile
```

### 4. Verify Database Connectivity

```bash
# Test database connection
curl -X GET https://your-domain.com/api/admin/setup/health \
  -H "Authorization: Bearer YOUR_JWT"
```

### 5. Check Application Logs

```bash
# For VM deployment
sudo journalctl -u hrms-api -f

# For Cloud Run deployment
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=hrms-api" \
  --limit 50 \
  --format json
```

---

## Monitoring & Observability

### 1. Application Insights (Google Cloud)

```bash
# Enable Cloud Logging and Monitoring
gcloud services enable logging.googleapis.com monitoring.googleapis.com

# Install monitoring agent (for VM deployments)
curl -sSO https://dl.google.com/cloudagents/add-google-cloud-ops-agent-repo.sh
sudo bash add-google-cloud-ops-agent-repo.sh --also-install
```

### 2. Key Metrics to Monitor

**Application Performance:**
- Request latency (p50, p95, p99)
- Error rate (target: <0.1%)
- Request throughput (requests/second)
- Memory usage
- CPU utilization

**Database Performance:**
- Connection pool utilization
- Query execution time
- Deadlocks
- Database size growth

**Email Delivery:**
- Email send success rate
- Email bounce rate
- Email delivery time

### 3. Set Up Alerts

Create `monitoring-alerts.yaml`:
```yaml
# High error rate alert
displayName: "High API Error Rate"
conditions:
  - displayName: "Error rate > 5%"
    conditionThreshold:
      filter: 'resource.type="cloud_run_revision" AND metric.type="run.googleapis.com/request_count" AND metric.labels.response_code_class="5xx"'
      comparison: COMPARISON_GT
      thresholdValue: 5
      duration: 300s

# Database connection pool exhaustion
displayName: "Database Connection Pool Low"
conditions:
  - displayName: "Available connections < 10"
    conditionThreshold:
      filter: 'metric.type="custom.googleapis.com/database/pool_available"'
      comparison: COMPARISON_LT
      thresholdValue: 10
      duration: 60s
```

```bash
# Create alerts
gcloud alpha monitoring policies create --policy-from-file=monitoring-alerts.yaml
```

### 4. Log Aggregation

Configure structured logging in `appsettings.Production.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Hangfire": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "GoogleCloudLogging",
        "Args": {
          "projectId": "your-project-id",
          "resourceType": "cloud_run_revision",
          "logName": "hrms-api"
        }
      }
    ]
  }
}
```

---

## Security Checklist

### Pre-Deployment Security Review

- [ ] **Secrets Management**
  - [ ] All secrets stored in Google Secret Manager
  - [ ] No hardcoded credentials in code or config files
  - [ ] Service account has least-privilege access
  - [ ] Secrets rotated regularly (90-day cycle)

- [ ] **Authentication & Authorization**
  - [ ] JWT secret is cryptographically secure (64+ characters)
  - [ ] Token expiration configured (60 minutes)
  - [ ] Refresh token rotation enabled
  - [ ] Role-based access control (RBAC) enforced
  - [ ] SuperAdmin account secured with strong password

- [ ] **Network Security**
  - [ ] HTTPS enforced (HTTP redirects to HTTPS)
  - [ ] TLS 1.2+ only
  - [ ] HSTS header configured
  - [ ] Cloud SQL instance not publicly accessible
  - [ ] Firewall rules restrict access to known IPs

- [ ] **Application Security**
  - [ ] Rate limiting enabled
  - [ ] Input validation on all endpoints
  - [ ] CORS configured with explicit origins
  - [ ] SQL injection protection (parameterized queries)
  - [ ] XSS prevention (sanitized outputs)
  - [ ] CSRF protection enabled

- [ ] **Data Security**
  - [ ] Database encryption at rest
  - [ ] Database encryption in transit (SSL)
  - [ ] Redis requires authentication
  - [ ] Sensitive data encrypted (passwords hashed with bcrypt)
  - [ ] Personal data handling complies with GDPR/privacy laws

- [ ] **Audit & Compliance**
  - [ ] Audit logging enabled for all critical operations
  - [ ] Log retention policy configured (1 year minimum)
  - [ ] Anomaly detection active
  - [ ] Security alerting configured
  - [ ] Regular security scans scheduled

- [ ] **Backup & Recovery**
  - [ ] Database automated backups enabled (daily)
  - [ ] Backup retention: 30 days
  - [ ] Disaster recovery plan documented
  - [ ] Backup restore tested

### Post-Deployment Security Verification

```bash
# 1. Verify HTTPS redirect
curl -I http://your-domain.com
# Should return: 301 Moved Permanently, Location: https://

# 2. Check security headers
curl -I https://your-domain.com
# Should include:
# - Strict-Transport-Security
# - X-Frame-Options
# - X-Content-Type-Options
# - X-XSS-Protection

# 3. Test rate limiting
for i in {1..10}; do curl https://your-domain.com/api/auth/login; done
# Should return 429 Too Many Requests after threshold

# 4. Verify authentication
curl https://your-domain.com/api/employees
# Should return: 401 Unauthorized (without token)

# 5. Test SQL injection protection
curl "https://your-domain.com/api/employees?search='; DROP TABLE employees;--"
# Should return safe results or validation error
```

---

## Troubleshooting

### Common Issues

#### 1. Email Not Sending

**Symptoms:** Test email endpoint returns success but email not received

**Diagnosis:**
```bash
# Check application logs
sudo journalctl -u hrms-api | grep -i "email"

# Check SMTP connection
telnet smtp.gmail.com 587
# Should connect successfully
```

**Solutions:**
- Verify SMTP credentials in Secret Manager
- Check sender email is verified with provider
- Review email provider logs for bounces
- Ensure firewall allows outbound port 587
- Check spam folder

#### 2. Database Connection Failures

**Symptoms:** API returns 500 errors, logs show "connection pool exhausted"

**Diagnosis:**
```bash
# Check active connections
SELECT count(*) FROM pg_stat_activity WHERE datname='hrms_prod';

# Check connection settings
SELECT name, setting FROM pg_settings WHERE name LIKE '%max_connections%';
```

**Solutions:**
- Increase `Maximum Pool Size` in connection string
- Increase `max_connections` in PostgreSQL
- Check for connection leaks (review application code)
- Scale up database instance

#### 3. Hangfire Jobs Not Running

**Symptoms:** Scheduled jobs (email reminders) not executing

**Diagnosis:**
```bash
# Check Hangfire dashboard
# Navigate to: https://your-domain.com/hangfire

# Check logs
sudo journalctl -u hrms-api | grep -i "hangfire"
```

**Solutions:**
- Verify Redis connection
- Restart application service
- Check worker count configuration
- Review job error logs in Hangfire dashboard

#### 4. High Memory Usage

**Symptoms:** Application crashes or restarts frequently

**Diagnosis:**
```bash
# Check memory usage
docker stats (for containers)
# OR
top -p $(pgrep dotnet)
```

**Solutions:**
- Increase container/VM memory allocation
- Review connection pool sizes (reduce if too high)
- Enable response caching
- Implement pagination on large data endpoints
- Profile application for memory leaks

#### 5. Secret Manager Access Denied

**Symptoms:** Application fails to start, logs show "Permission denied" for secrets

**Diagnosis:**
```bash
# Check service account permissions
gcloud projects get-iam-policy YOUR_PROJECT_ID \
  --flatten="bindings[].members" \
  --filter="bindings.members:serviceAccount:hrms-app@*"
```

**Solutions:**
```bash
# Grant Secret Manager access
gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
  --member="serviceAccount:hrms-app@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

# Verify secrets exist
gcloud secrets list
```

---

## Maintenance Procedures

### Weekly Tasks
- [ ] Review application logs for errors
- [ ] Check email delivery success rate
- [ ] Monitor database size and growth
- [ ] Review security alerts and anomalies

### Monthly Tasks
- [ ] Review and rotate access credentials
- [ ] Test backup restoration procedure
- [ ] Update dependencies and security patches
- [ ] Review and optimize slow queries
- [ ] Audit user access and permissions

### Quarterly Tasks
- [ ] Full security audit
- [ ] Disaster recovery drill
- [ ] Review and update monitoring alerts
- [ ] Capacity planning and scaling review
- [ ] Compliance audit (GDPR, SOC2, etc.)

---

## Support & Escalation

### Contact Information
- **Technical Support:** support@morishr.com
- **Security Issues:** security@morishr.com
- **Emergency (24/7):** +230 5XXX XXXX

### Escalation Path
1. Level 1: DevOps Team (Response: 1 hour)
2. Level 2: Engineering Lead (Response: 30 minutes)
3. Level 3: CTO (Response: 15 minutes for critical issues)

### Incident Severity Levels

**P0 (Critical):** Complete service outage, data breach
- Response: Immediate
- All hands on deck
- Customer notification required

**P1 (High):** Partial outage, security vulnerability
- Response: 30 minutes
- Senior engineer assigned
- Status updates every hour

**P2 (Medium):** Performance degradation, non-critical bugs
- Response: 2 hours
- Regular engineer assigned
- Status updates every 4 hours

**P3 (Low):** Minor issues, enhancement requests
- Response: 24 hours
- Scheduled maintenance window

---

## Additional Resources

- **API Documentation:** https://your-domain.com/swagger
- **Architecture Diagrams:** `/docs/architecture/`
- **Database Schema:** `/docs/database-schema.md`
- **Runbook:** `/docs/runbook.md`
- **Change Log:** `/CHANGELOG.md`

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2025-01-11 | DevOps Team | Initial production deployment guide |

---

**Last Updated:** 2025-01-11
**Next Review Date:** 2025-04-11
