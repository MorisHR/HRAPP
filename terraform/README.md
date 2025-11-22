# HRMS Fortune 500 - GCP Deployment Guide

Complete guide for deploying the HRMS system to Google Cloud Platform using Terraform and GitHub Actions CI/CD.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Detailed Setup](#detailed-setup)
- [CI/CD Configuration](#cicd-configuration)
- [Post-Deployment](#post-deployment)
- [Monitoring](#monitoring)
- [Cost Optimization](#cost-optimization)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools

- [Google Cloud SDK](https://cloud.google.com/sdk/docs/install) (gcloud CLI)
- [Terraform](https://www.terraform.io/downloads) v1.0+
- [Docker](https://docs.docker.com/get-docker/)
- Git
- .NET 9 SDK

### Required Access

- GCP Project with billing enabled
- Owner or Editor role on the GCP project
- GitHub repository with Actions enabled

---

## Quick Start

### 1. Initial Setup (5 minutes)

```bash
# Clone the repository
git clone https://github.com/your-org/hrms.git
cd hrms

# Authenticate with Google Cloud
gcloud auth login
gcloud auth application-default login

# Set your project
gcloud config set project YOUR_PROJECT_ID

# Enable required APIs
gcloud services enable \
  compute.googleapis.com \
  run.googleapis.com \
  sql-component.googleapis.com \
  servicenetworking.googleapis.com \
  redis.googleapis.com \
  secretmanager.googleapis.com \
  storage-api.googleapis.com \
  cloudresourcemanager.googleapis.com
```

### 2. Configure Terraform (5 minutes)

```bash
cd terraform

# Copy example variables
cp terraform.tfvars.example terraform.tfvars

# Edit with your values
nano terraform.tfvars
```

**Required changes in terraform.tfvars:**
```hcl
project_id  = "your-gcp-project-id"  # Change this
domain      = "api.yourdomain.com"    # Change this
api_image   = "gcr.io/your-gcp-project-id/hrms-api:latest"  # Change project ID
```

### 3. Deploy Infrastructure (15-20 minutes)

```bash
# Initialize Terraform
terraform init

# Review the plan
terraform plan

# Apply (will ask for confirmation)
terraform apply

# Save outputs
terraform output > ../deployment-info.txt
```

### 4. Build and Deploy Application (10 minutes)

```bash
# Go back to root directory
cd ..

# Build Docker image
docker build -t gcr.io/YOUR_PROJECT_ID/hrms-api:latest \
  --build-arg BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ") \
  --build-arg VCS_REF=$(git rev-parse --short HEAD) \
  -f Dockerfile .

# Push to Google Container Registry
docker push gcr.io/YOUR_PROJECT_ID/hrms-api:latest

# Deploy to Cloud Run (will be auto-configured by Terraform)
gcloud run deploy production-hrms-api \
  --image gcr.io/YOUR_PROJECT_ID/hrms-api:latest \
  --region us-central1
```

### 5. Configure DNS (5 minutes)

```bash
# Get load balancer IP
terraform output load_balancer_ip

# Add A record to your DNS provider:
# Type: A
# Name: api.yourdomain.com
# Value: [IP from above]
# TTL: 300
```

### 6. Run Database Migrations (5 minutes)

```bash
# Get database password
gcloud secrets versions access latest --secret="production-db-password"

# Install Cloud SQL Proxy
wget https://dl.google.com/cloudsql/cloud_sql_proxy.linux.amd64 -O cloud_sql_proxy
chmod +x cloud_sql_proxy

# Get connection name
gcloud sql instances describe production-hrms-db --format='value(connectionName)'

# Start proxy
./cloud_sql_proxy -instances=YOUR_CONNECTION_NAME=tcp:5432 &

# Run migrations
cd src/HRMS.API
dotnet ef database update --project ../HRMS.Infrastructure --context MasterDbContext
```

---

## Detailed Setup

### Architecture Overview

The infrastructure consists of:

- **Cloud Run**: Serverless container platform (API)
- **Cloud SQL**: PostgreSQL 15 with High Availability
- **Cloud Memorystore**: Redis 7.0 for caching
- **Cloud Load Balancer**: Global HTTPS load balancer
- **Cloud Storage**: File uploads and backups
- **Secret Manager**: Secure secrets storage
- **VPC Network**: Private networking

### Infrastructure Components

#### 1. Cloud SQL (PostgreSQL)

**Configuration:**
- Version: PostgreSQL 15
- Tier: db-custom-4-16384 (4 vCPUs, 16GB RAM)
- Storage: 100GB SSD (auto-resize enabled)
- Backup: Daily at 2 AM UTC
- HA: Regional (production)

**Cost:** ~$400/month

#### 2. Cloud Memorystore (Redis)

**Configuration:**
- Version: Redis 7.0
- Tier: STANDARD_HA
- Memory: 5GB
- Replicas: 1 (production)

**Cost:** ~$150/month

#### 3. Cloud Run

**Configuration:**
- CPU: 2 vCPUs per instance
- Memory: 2GB per instance
- Min instances: 1
- Max instances: 100
- Timeout: 300s

**Cost:** ~$100/month (10 avg instances)

**Total Monthly Cost:** ~$700/month

---

## CI/CD Configuration

### GitHub Actions Setup

The deployment pipeline automatically:
1. Builds the .NET 9 application
2. Runs tests
3. Creates Docker image
4. Pushes to Google Container Registry
5. Deploys to Cloud Run
6. Runs database migrations
7. Performs health checks

### Required GitHub Secrets

Add these secrets to your GitHub repository:

```bash
# Get service account key
gcloud iam service-accounts create github-actions \
  --display-name="GitHub Actions CI/CD"

gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
  --member="serviceAccount:github-actions@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/owner"

gcloud iam service-accounts keys create key.json \
  --iam-account=github-actions@YOUR_PROJECT_ID.iam.gserviceaccount.com

# Add to GitHub Secrets:
# - GCP_PROJECT_ID: Your GCP project ID
# - GCP_SA_KEY: Contents of key.json
```

### Manual Deployment

To deploy manually without GitHub Actions:

```bash
# Build image
docker build -t gcr.io/YOUR_PROJECT_ID/hrms-api:latest .

# Push image
docker push gcr.io/YOUR_PROJECT_ID/hrms-api:latest

# Deploy to Cloud Run
gcloud run deploy production-hrms-api \
  --image gcr.io/YOUR_PROJECT_ID/hrms-api:latest \
  --region us-central1 \
  --platform managed \
  --allow-unauthenticated \
  --min-instances 1 \
  --max-instances 100 \
  --cpu 2 \
  --memory 2Gi
```

---

## Post-Deployment

### 1. Verify Deployment

```bash
# Check Cloud Run service
gcloud run services describe production-hrms-api --region us-central1

# Test health endpoint
curl https://api.yourdomain.com/health

# Check logs
gcloud run services logs read production-hrms-api --region us-central1
```

### 2. Create Initial SuperAdmin User

```bash
# Connect to database
./cloud_sql_proxy -instances=YOUR_CONNECTION_NAME=tcp:5432 &

# Use the initial setup endpoint or SQL
psql -h localhost -U postgres -d hrms_master
-- Run your initial user creation SQL
```

### 3. Configure Frontend

Update frontend configuration:

```typescript
// src/environments/environment.prod.ts
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.com'
};
```

Build and deploy frontend:

```bash
cd hrms-frontend
npm install
npm run build

# Deploy to Cloud Storage + CDN (recommended)
gsutil -m cp -r dist/* gs://your-frontend-bucket/
```

---

## Monitoring

### Cloud Logging

```bash
# View logs
gcloud logging read "resource.type=cloud_run_revision" --limit 50

# Follow logs in real-time
gcloud run services logs tail production-hrms-api --region us-central1
```

### Cloud Monitoring

```bash
# Create uptime check
gcloud alpha monitoring uptime create production-hrms-health \
  --resource-type=uptime-url \
  --host=api.yourdomain.com \
  --path=/health \
  --period=60

# Create alert policy
gcloud alpha monitoring policies create \
  --notification-channels=YOUR_CHANNEL_ID \
  --display-name="HRMS API Down" \
  --condition-display-name="Health check failed" \
  --condition-threshold-value=1 \
  --condition-threshold-duration=300s
```

### Performance Monitoring

Key metrics to monitor:

- **Request latency**: Target p95 < 500ms
- **Error rate**: Target < 1%
- **Database connections**: Monitor connection pool
- **Redis hit rate**: Target > 90%
- **CPU utilization**: Target < 70%
- **Memory usage**: Monitor for leaks

---

## Cost Optimization

### 1. Right-Sizing

```bash
# Monitor actual usage
gcloud logging read "resource.type=cloud_run_revision" \
  --format="value(protoPayload.response.status)" | sort | uniq -c

# Adjust based on traffic patterns
terraform apply -var="min_instances=0"  # Scale to zero during low traffic
```

### 2. Committed Use Discounts

For predictable workloads:
- Cloud SQL: 1-year commitment = 25% discount
- Cloud Memorystore: 1-year commitment = 25% discount

### 3. Storage Lifecycle

Already configured in Terraform:
- Uploads: Standard → Nearline (90 days) → Coldline (365 days)
- Backups: Nearline → Coldline (30 days) → Archive (90 days)

### 4. Database Optimization

```sql
-- Enable query insights
ALTER SYSTEM SET track_activity_query_size = 1024;
ALTER SYSTEM SET pg_stat_statements.track = all;

-- Review slow queries
SELECT * FROM pg_stat_statements
ORDER BY total_time DESC
LIMIT 20;
```

---

## Troubleshooting

### Common Issues

#### 1. Deployment Fails

```bash
# Check Cloud Run logs
gcloud run services logs read production-hrms-api --region us-central1 --limit=100

# Check build logs
gcloud builds log YOUR_BUILD_ID
```

#### 2. Database Connection Issues

```bash
# Verify Cloud SQL is running
gcloud sql instances describe production-hrms-db

# Check VPC connector
gcloud compute networks vpc-access connectors describe production-hrms-connector \
  --region us-central1

# Test connection
./cloud_sql_proxy -instances=YOUR_CONNECTION_NAME=tcp:5432
psql -h localhost -U postgres -d hrms_master
```

#### 3. SSL Certificate Not Provisioning

```bash
# Check certificate status
gcloud compute ssl-certificates describe production-hrms-cert

# Common causes:
# - DNS not configured correctly
# - DNS not propagated yet (wait 15 mins)
# - Domain ownership not verified

# Verify DNS
dig api.yourdomain.com
nslookup api.yourdomain.com
```

#### 4. High Costs

```bash
# Check current costs
gcloud billing accounts list
gcloud billing projects describe YOUR_PROJECT_ID

# View detailed breakdown
# Go to: https://console.cloud.google.com/billing/

# Identify expensive resources
gcloud compute instances list
gcloud sql instances list
gcloud redis instances list
```

### Getting Help

- **GCP Documentation**: https://cloud.google.com/docs
- **Terraform Docs**: https://registry.terraform.io/providers/hashicorp/google/latest/docs
- **Cloud Run Docs**: https://cloud.google.com/run/docs
- **Cloud SQL Docs**: https://cloud.google.com/sql/docs

### Support Contacts

- **GCP Support**: https://cloud.google.com/support
- **Community**: Stack Overflow (tag: google-cloud-platform)
- **Discord**: GCP Community Server

---

## Maintenance

### Regular Tasks

**Daily:**
- Check error logs
- Monitor request latency
- Review failed health checks

**Weekly:**
- Review database slow queries
- Check backup status
- Monitor costs

**Monthly:**
- Review security alerts
- Update dependencies
- Performance tuning
- Cost optimization review

### Updates and Patches

```bash
# Update Terraform
terraform init -upgrade

# Update Docker base images
docker pull mcr.microsoft.com/dotnet/aspnet:9.0
docker pull mcr.microsoft.com/dotnet/sdk:9.0

# Rebuild and deploy
docker build -t gcr.io/YOUR_PROJECT_ID/hrms-api:latest .
docker push gcr.io/YOUR_PROJECT_ID/hrms-api:latest
```

---

## Security Checklist

- [x] Use Secret Manager for all secrets
- [x] Enable VPC for private networking
- [x] Use private IP for Cloud SQL
- [x] Enable SSL/TLS everywhere
- [x] Non-root user in Docker
- [x] Least privilege IAM roles
- [x] Regular backups enabled
- [x] Logging enabled
- [x] HTTPS-only (HTTP redirects)
- [ ] WAF/DDoS protection (consider Cloud Armor)
- [ ] Vulnerability scanning
- [ ] Penetration testing

---

## Next Steps

1. **Production Hardening**
   - Enable Cloud Armor WAF
   - Set up Cloud CDN for static assets
   - Configure custom domain for frontend

2. **Monitoring & Alerting**
   - Set up PagerDuty/Opsgenie integration
   - Configure Slack notifications
   - Create custom dashboards

3. **Disaster Recovery**
   - Test backup restoration
   - Document runbook procedures
   - Set up multi-region failover

4. **Performance Optimization**
   - Enable Cloud CDN
   - Configure connection pooling
   - Implement Redis caching strategy

---

**Last Updated:** 2025-11-22
**Version:** 1.0.0
**Maintained By:** Platform Team
