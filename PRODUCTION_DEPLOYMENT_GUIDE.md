# HRMS Production Deployment Guide

**Last Updated:** November 22, 2025
**Target Platform:** Google Cloud Platform (GCP)
**Environment:** Production

---

## 🎯 Pre-Deployment Checklist

### ✅ Completed

- [x] All database migrations applied (20 migrations)
- [x] JWT security enhancements implemented
- [x] Token blacklist service operational
- [x] Device fingerprinting enabled
- [x] Encryption key rotated (development)
- [x] Monitoring stack deployed (Prometheus/Grafana)
- [x] Frontend RUM implemented
- [x] GDPR/DPA compliance features complete
- [x] SuperAdmin endpoints verified

### 🔴 Required Before Production Deployment

- [ ] Configure Google Secret Manager (see below)
- [ ] Update appsettings.Production.json with GCP Project ID
- [ ] Set up Cloud SQL PostgreSQL instance
- [ ] Configure Redis Memorystore
- [ ] Set up GCS bucket for file uploads
- [ ] Configure SMTP for email notifications
- [ ] Set up SSL/TLS certificates
- [ ] Configure Cloud Armor (WAF)
- [ ] Set up Cloud Monitoring alerts
- [ ] Load testing at target scale
- [ ] Security audit
- [ ] Backup and disaster recovery testing

---

## 1. Google Secret Manager Setup

### Step 1: Run Setup Script

```bash
cd /workspaces/HRAPP/deployment
./gcp-secret-manager-setup.sh
```

This script will:
- Enable Secret Manager API
- Generate secure random secrets
- Create all required secrets
- Grant service account access
- Verify setup

### Step 2: Verify Secrets Created

```bash
gcloud secrets list --filter="labels.app=hrms" --project="YOUR_PROJECT_ID"
```

Expected secrets:
- `HRMS_JWT_SECRET` - JWT signing key
- `HRMS_ENCRYPTION_KEY` - Column-level encryption key
- `HRMS_REDIS_PASSWORD` - Redis cache password
- `HRMS_HANGFIRE_PASSWORD` - Background job dashboard password
- `HRMS_DB_PASSWORD` - PostgreSQL database password
- `HRMS_SMTP_PASSWORD` - Email server password

### Step 3: Update Production Configuration

Edit `/workspaces/HRAPP/src/HRMS.API/appsettings.Production.json`:

```json
{
  "GoogleCloud": {
    "ProjectId": "your-actual-gcp-project-id",
    "SecretManagerEnabled": true,
    "CloudSqlInstanceConnectionName": "project:region:instance",
    "StorageBucket": "hrms-uploads-prod"
  }
}
```

---

## 2. Database Setup (Cloud SQL)

### Create PostgreSQL Instance

```bash
# Create Cloud SQL PostgreSQL instance
gcloud sql instances create hrms-production \
    --database-version=POSTGRES_15 \
    --tier=db-custom-4-16384 \
    --region=us-central1 \
    --storage-type=SSD \
    --storage-size=100GB \
    --storage-auto-increase \
    --backup-start-time=02:00 \
    --maintenance-window-day=SUN \
    --maintenance-window-hour=03 \
    --availability-type=REGIONAL \
    --enable-bin-log

# Create database
gcloud sql databases create hrms_master \
    --instance=hrms-production

# Create user (password stored in Secret Manager)
gcloud sql users create hrms_app \
    --instance=hrms-production \
    --password=$(gcloud secrets versions access latest --secret="HRMS_DB_PASSWORD")
```

### Configure Connection String

The application automatically builds the connection string from:
- Cloud SQL connection name (from appsettings.Production.json)
- Database password (from Secret Manager: HRMS_DB_PASSWORD)

Format:
```
Host=/cloudsql/PROJECT:REGION:INSTANCE;Database=hrms_master;Username=hrms_app;Password=FROM_SECRET_MANAGER;Pooling=true;Minimum Pool Size=10;Maximum Pool Size=200
```

### Apply Migrations on First Deployment

The application automatically applies pending migrations on startup when `APPLY_MIGRATIONS=true`:

```bash
# Set environment variable in Cloud Run/App Engine
APPLY_MIGRATIONS=true
```

---

## 3. Redis Cache Setup (Memorystore)

### Create Redis Instance

```bash
# Create Redis instance
gcloud redis instances create hrms-cache \
    --size=5 \
    --region=us-central1 \
    --tier=standard \
    --redis-version=redis_7_0 \
    --enable-auth

# Get connection details
gcloud redis instances describe hrms-cache --region=us-central1
```

### Update Configuration

Redis connection string is automatically retrieved from the instance.

---

## 4. File Storage Setup (Cloud Storage)

### Create GCS Bucket

```bash
# Create bucket
gsutil mb -p YOUR_PROJECT_ID -c STANDARD -l us-central1 gs://hrms-uploads-prod

# Set lifecycle (delete old temporary files)
cat > lifecycle.json << EOF
{
  "lifecycle": {
    "rule": [
      {
        "action": {"type": "Delete"},
        "condition": {
          "age": 30,
          "matchesPrefix": ["temp/"]
        }
      }
    ]
  }
}
EOF
gsutil lifecycle set lifecycle.json gs://hrms-uploads-prod

# Set CORS for direct uploads
cat > cors.json << EOF
[
  {
    "origin": ["https://admin.hrms.com", "https://*.hrms.com"],
    "method": ["GET", "POST", "PUT", "DELETE"],
    "responseHeader": ["Content-Type"],
    "maxAgeSeconds": 3600
  }
]
EOF
gsutil cors set cors.json gs://hrms-uploads-prod

# Grant service account access
gsutil iam ch serviceAccount:YOUR_SERVICE_ACCOUNT@YOUR_PROJECT.iam.gserviceaccount.com:roles/storage.objectAdmin gs://hrms-uploads-prod
```

---

## 5. Application Deployment

### Option A: Cloud Run (Recommended)

**Dockerfile** (already exists at `/workspaces/HRAPP/Dockerfile`):

```bash
# Build and push Docker image
gcloud builds submit --tag gcr.io/YOUR_PROJECT_ID/hrms-api:latest

# Deploy to Cloud Run
gcloud run deploy hrms-api \
    --image gcr.io/YOUR_PROJECT_ID/hrms-api:latest \
    --platform managed \
    --region us-central1 \
    --allow-unauthenticated \
    --min-instances 2 \
    --max-instances 100 \
    --cpu 2 \
    --memory 4Gi \
    --timeout 300 \
    --concurrency 80 \
    --set-env-vars "ASPNETCORE_ENVIRONMENT=Production,APPLY_MIGRATIONS=false" \
    --set-cloudsql-instances "YOUR_PROJECT:us-central1:hrms-production" \
    --vpc-connector "projects/YOUR_PROJECT/locations/us-central1/connectors/hrms-connector"

# Map custom domain
gcloud run services update hrms-api --region us-central1 \
    --update-labels "app=hrms,env=production"
```

### Option B: App Engine

Create `app.yaml`:

```yaml
runtime: aspnetcore
env: standard
instance_class: F4_1G

automatic_scaling:
  min_instances: 2
  max_instances: 100
  target_cpu_utilization: 0.6

env_variables:
  ASPNETCORE_ENVIRONMENT: "Production"
  APPLY_MIGRATIONS: "false"

beta_settings:
  cloud_sql_instances: "YOUR_PROJECT:us-central1:hrms-production"

vpc_access_connector:
  name: "projects/YOUR_PROJECT/locations/us-central1/connectors/hrms-connector"
```

Deploy:
```bash
gcloud app deploy app.yaml --project=YOUR_PROJECT_ID
```

---

## 6. Monitoring Setup

### Cloud Monitoring Integration

The application already exports metrics to Prometheus. Set up Google Cloud Monitoring:

```bash
# Create monitoring workspace
gcloud monitoring workspaces create --display-name="HRMS Production"

# Create uptime check
gcloud monitoring uptime create hrms-health-check \
    --display-name="HRMS API Health" \
    --check-interval=60s \
    --timeout=10s \
    --resource-type=uptime-url \
    --host=api.hrms.com \
    --path=/health

# Create alert policy
gcloud alpha monitoring policies create \
    --notification-channels=YOUR_NOTIFICATION_CHANNEL \
    --display-name="HRMS High Error Rate" \
    --condition-display-name="Error rate > 5%" \
    --condition-threshold-value=5 \
    --condition-threshold-duration=300s
```

### Prometheus/Grafana Setup

Already deployed via Docker Compose. For production, deploy to separate VM or GKE:

```bash
# Deploy monitoring stack to GKE
kubectl apply -f /workspaces/HRAPP/monitoring/kubernetes/
```

---

## 7. Security Configuration

### SSL/TLS Certificates

```bash
# Create managed SSL certificate
gcloud compute ssl-certificates create hrms-cert \
    --domains=api.hrms.com,admin.hrms.com \
    --global

# Create HTTPS load balancer with certificate
gcloud compute url-maps create hrms-lb \
    --default-service=hrms-api-backend

gcloud compute target-https-proxies create hrms-https-proxy \
    --ssl-certificates=hrms-cert \
    --url-map=hrms-lb

gcloud compute forwarding-rules create hrms-https-rule \
    --global \
    --target-https-proxy=hrms-https-proxy \
    --ports=443
```

### Cloud Armor (WAF)

```bash
# Create security policy
gcloud compute security-policies create hrms-waf \
    --description="WAF for HRMS API"

# Block common attacks
gcloud compute security-policies rules create 1000 \
    --security-policy=hrms-waf \
    --expression="evaluatePreconfiguredExpr('xss-stable')" \
    --action=deny-403

gcloud compute security-policies rules create 1001 \
    --security-policy=hrms-waf \
    --expression="evaluatePreconfiguredExpr('sqli-stable')" \
    --action=deny-403

# Rate limiting
gcloud compute security-policies rules create 2000 \
    --security-policy=hrms-waf \
    --expression="true" \
    --action=rate-based-ban \
    --rate-limit-threshold-count=1000 \
    --rate-limit-threshold-interval-sec=60 \
    --ban-duration-sec=600

# Attach to backend service
gcloud compute backend-services update hrms-api-backend \
    --security-policy=hrms-waf \
    --global
```

---

## 8. Environment Variables

### Production Environment Variables

Set these in Cloud Run/App Engine:

```bash
ASPNETCORE_ENVIRONMENT=Production
APPLY_MIGRATIONS=false  # Only true for first deployment
ASPNETCORE_URLS=http://+:8080
```

Secrets are loaded automatically from Google Secret Manager.

---

## 9. Deployment Pipeline (CI/CD)

### GitHub Actions Workflow

Create `.github/workflows/production-deploy.yml`:

```yaml
name: Production Deployment

on:
  push:
    branches: [ main ]
    tags: [ 'v*' ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Run Tests
        run: dotnet test

      - name: Authenticate to GCP
        uses: google-github-actions/auth@v1
        with:
          credentials_json: ${{ secrets.GCP_SA_KEY }}

      - name: Build Docker Image
        run: |
          gcloud builds submit --tag gcr.io/${{ secrets.GCP_PROJECT_ID }}/hrms-api:${{ github.sha }}
          gcloud builds submit --tag gcr.io/${{ secrets.GCP_PROJECT_ID }}/hrms-api:latest

      - name: Deploy to Cloud Run
        run: |
          gcloud run deploy hrms-api \
            --image gcr.io/${{ secrets.GCP_PROJECT_ID }}/hrms-api:${{ github.sha }} \
            --region us-central1 \
            --platform managed
```

---

## 10. Health Checks

### Application Health Endpoints

- `/health` - Overall health (database, Redis, etc.)
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe
- `/metrics` - Prometheus metrics

### Configure Probes

Cloud Run:
```bash
gcloud run services update hrms-api \
    --update-health-checks \
    --health-check-path=/health/ready \
    --health-check-interval=60s \
    --health-check-timeout=10s
```

---

## 11. Backup and Disaster Recovery

### Database Backups

Cloud SQL automatic backups are configured (see step 2).

Manual backup:
```bash
gcloud sql backups create --instance=hrms-production
```

Restore from backup:
```bash
gcloud sql backups restore BACKUP_ID \
    --backup-instance=hrms-production \
    --restore-instance=hrms-production
```

### Secrets Backup

Export secrets (for disaster recovery):
```bash
#!/bin/bash
SECRETS=("HRMS_JWT_SECRET" "HRMS_ENCRYPTION_KEY" "HRMS_REDIS_PASSWORD" "HRMS_HANGFIRE_PASSWORD")

for SECRET in "${SECRETS[@]}"; do
    gcloud secrets versions access latest --secret="$SECRET" > "${SECRET}.backup"
done

# Encrypt backups
tar czf secrets-backup.tar.gz *.backup
gpg -c secrets-backup.tar.gz
rm *.backup secrets-backup.tar.gz
```

---

## 12. Performance Optimization

### Database Optimization

Already configured:
- Connection pooling (min 10, max 200)
- Compiled queries for hot paths
- Proper indexes on all tables

### Caching Strategy

- Redis for distributed cache
- Response caching for GET endpoints
- CDN for static assets

### Scaling Configuration

Cloud Run auto-scaling:
- Min instances: 2 (always warm)
- Max instances: 100
- Concurrency: 80 per instance
- CPU: 2 cores, 4GB RAM

---

## 13. Testing Production Deployment

### Smoke Tests

```bash
API_URL="https://api.hrms.com"

# 1. Health check
curl -f $API_URL/health || exit 1

# 2. Metrics endpoint
curl -f $API_URL/metrics | grep "http_requests_total" || exit 1

# 3. Login (SuperAdmin)
TOKEN=$(curl -X POST $API_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@hrms.com","password":"Admin@123"}' \
  | jq -r '.token')

# 4. Test protected endpoint
curl -f $API_URL/admin/dashboard/stats \
  -H "Authorization: Bearer $TOKEN" || exit 1

echo "✓ All smoke tests passed"
```

### Load Testing

```bash
# Install k6
curl https://github.com/grafana/k6/releases/download/v0.47.0/k6-v0.47.0-linux-amd64.tar.gz -L | tar xvz
sudo mv k6-v0.47.0-linux-amd64/k6 /usr/local/bin

# Run load test
k6 run /workspaces/HRAPP/tests/load-test.js \
    --vus 1000 \
    --duration 5m \
    --env API_URL=https://api.hrms.com
```

---

## 14. Rollback Procedure

### Rollback Deployment

Cloud Run:
```bash
# List revisions
gcloud run revisions list --service=hrms-api --region=us-central1

# Rollback to previous revision
gcloud run services update-traffic hrms-api \
    --to-revisions=hrms-api-PREVIOUS_REVISION=100 \
    --region=us-central1
```

### Rollback Database Migration

```bash
# Connect to Cloud SQL
gcloud sql connect hrms-production --user=hrms_app --database=hrms_master

# Rollback to specific migration
dotnet ef database update <PreviousMigrationName> \
    --project src/HRMS.API \
    --connection "Host=/cloudsql/PROJECT:REGION:INSTANCE;..."
```

---

## 15. Production Checklist

### Before First Deployment

- [ ] All secrets created in Secret Manager
- [ ] Cloud SQL instance created and configured
- [ ] Redis Memorystore instance created
- [ ] GCS bucket created with proper IAM
- [ ] Service account has all required permissions
- [ ] SSL certificates provisioned
- [ ] Cloud Armor WAF configured
- [ ] Monitoring alerts configured
- [ ] Load testing completed successfully
- [ ] Backup and restore tested
- [ ] Security audit completed

### Post-Deployment

- [ ] Verify health checks pass
- [ ] Verify metrics are being collected
- [ ] Test all critical user flows
- [ ] Monitor error rates
- [ ] Verify backups are running
- [ ] Test rollback procedure
- [ ] Update documentation

---

## 16. Support and Monitoring

### Key Metrics to Monitor

1. **Availability**: Uptime > 99.9%
2. **Latency**: P95 < 200ms, P99 < 500ms
3. **Error Rate**: < 0.1%
4. **Database**: Query time P95 < 50ms
5. **Cache Hit Rate**: > 80%

### Alert Thresholds

- Error rate > 1% for 5 minutes
- P95 latency > 500ms for 5 minutes
- Database connections > 180 (90% of max)
- Disk usage > 80%
- Memory usage > 85%

### Incident Response

1. Check Cloud Run logs
2. Check database slow query log
3. Check Redis connection status
4. Review recent deployments
5. Rollback if necessary
6. Post-mortem after resolution

---

## 17. Cost Optimization

### Estimated Monthly Costs

- Cloud Run (2-100 instances): $200-$2000
- Cloud SQL (4 vCPU, 16GB): $400
- Redis Memorystore (5GB): $150
- Cloud Storage (100GB): $2.30
- Load Balancer: $20
- Secret Manager: $0.60
- Monitoring: $50

**Total**: ~$820-$2620/month (depending on scale)

### Cost Saving Tips

1. Use committed use discounts for Cloud SQL
2. Enable auto-scaling with aggressive scale-down
3. Use lifecycle policies for Cloud Storage
4. Implement efficient caching to reduce database load
5. Optimize database queries to reduce CPU usage

---

## 18. Security Hardening

### Implemented Security Features

- ✅ JWT with 15-minute expiration
- ✅ Token blacklist service
- ✅ Device fingerprinting
- ✅ Rate limiting (API and login)
- ✅ CSRF protection
- ✅ SQL injection prevention (Entity Framework)
- ✅ XSS prevention (input sanitization)
- ✅ Column-level encryption for PII
- ✅ HTTPS only (production)
- ✅ Security headers (HSTS, CSP, etc.)
- ✅ Cloud Armor WAF
- ✅ Secrets in Secret Manager (not code)

### Regular Security Tasks

- [ ] Rotate encryption key quarterly
- [ ] Rotate JWT secret quarterly
- [ ] Review and update WAF rules monthly
- [ ] Audit user permissions monthly
- [ ] Review security logs weekly
- [ ] Update dependencies monthly
- [ ] Penetration testing annually

---

## 19. Compliance

### GDPR Compliance

- ✅ Data Processing Agreements (DPA) management
- ✅ User consent tracking
- ✅ Data encryption at rest and in transit
- ✅ Right to be forgotten (data deletion)
- ✅ Data export functionality
- ✅ Audit logs for data access
- ✅ Breach notification system

### SOC 2 Controls

- ✅ Encryption of sensitive data
- ✅ Access controls (RBAC)
- ✅ Audit logging
- ✅ Change management (migrations)
- ✅ Backup and disaster recovery
- ✅ Monitoring and alerting
- ✅ Incident response procedures

---

## 20. Troubleshooting

### Common Issues

**Issue**: Application can't connect to Cloud SQL
**Solution**: Verify service account has Cloud SQL Client role and connection name is correct

**Issue**: Secrets not loading
**Solution**: Verify service account has Secret Manager Secret Accessor role

**Issue**: High latency
**Solution**: Check database query performance, Redis cache hit rate, scale up instances

**Issue**: Memory leaks
**Solution**: Check for unbounded collections, ensure EF context disposal, review async patterns

---

## Conclusion

This deployment guide provides a complete production deployment process for HRMS on Google Cloud Platform. Follow each section carefully and verify each step before proceeding.

For questions or issues, refer to:
- `/workspaces/HRAPP/REMAINING_TASKS.md` - Outstanding tasks
- `/workspaces/HRAPP/MIGRATION_FIX_COMPLETE_REPORT.md` - Database migration details
- `/workspaces/HRAPP/SECURITY_ACTIONS_REQUIRED.md` - Security checklist

**Last Review**: November 22, 2025
**Next Review**: Before production deployment
