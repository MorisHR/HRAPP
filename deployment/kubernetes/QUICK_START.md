# Kubernetes Cost Optimization - Quick Start Guide

**Target Savings**: $720/month in 90 minutes

---

## Prerequisites Checklist

- [ ] GCP Project ID: `_________________`
- [ ] GKE Cluster Name: `_________________`
- [ ] Region: `_________________`
- [ ] kubectl configured and connected
- [ ] gcloud CLI installed and authenticated
- [ ] Namespace `hrms-production` exists

---

## 5-Minute Setup

### Step 1: Set Environment Variables
```bash
export GCP_PROJECT_ID="your-project-id"
export GCP_REGION="us-central1"
export GKE_CLUSTER_NAME="hrms-cluster"

# Verify connection
kubectl cluster-info
gcloud config set project $GCP_PROJECT_ID
```

### Step 2: Navigate to Deployment Directory
```bash
cd /workspaces/HRAPP/deployment/kubernetes
```

### Step 3: Dry Run (Optional but Recommended)
```bash
./deploy.sh --dry-run
```

### Step 4: Deploy
```bash
./deploy.sh
```

**Expected Output**:
```
=========================================
HRMS Kubernetes Cost Optimization Deploy
=========================================

[INFO] Checking prerequisites...
[SUCCESS] Prerequisites check passed
[INFO] Deploying resource management...
[SUCCESS] Resource management deployed
[INFO] Deploying Cloud SQL Proxy...
[SUCCESS] Cloud SQL Proxy deployed
...
[SUCCESS] Deployment completed successfully

Expected Monthly Savings: $720
  - HPA: $400/month
  - Preemptible VMs: $240/month
  - Cloud SQL Proxy: $80/month
```

---

## Validation (5 minutes)

### Quick Validation
```bash
./scripts/validate-cost-optimization.sh
```

**Expected Output**: All tests pass (green checkmarks)

### Manual Checks
```bash
# 1. Check HPAs
kubectl get hpa -n hrms-production

# 2. Check preemptible nodes
kubectl get nodes -l workload-type=background-jobs

# 3. Check Cloud SQL Proxy
kubectl get deployment cloudsql-proxy -n hrms-production

# 4. Check all pods
kubectl get pods -n hrms-production
```

---

## Testing (10 minutes)

### Test 1: Cloud SQL Proxy Connectivity
```bash
kubectl run -it --rm psql-test --image=postgres:15 \
  --restart=Never -n hrms-production -- \
  psql -h cloudsql-proxy.hrms-production.svc.cluster.local \
       -p 5432 -U hrms_user -d hrms_master -c "SELECT 1;"
```

**Expected**: `(1 row)` output

### Test 2: Autoscaling
```bash
# Generate load
kubectl run -it --rm load-test --image=busybox \
  --restart=Never -n hrms-production -- \
  /bin/sh -c "while true; do wget -q -O- http://hrms-api:8080/health; done"

# Watch scaling (in another terminal)
kubectl get hpa -n hrms-production -w
```

**Expected**: Pods increase from 2 to 4+ within 30 seconds

### Test 3: Preemptible Nodes
```bash
# Check Hangfire pods are on preemptible nodes
kubectl get pods -n hrms-production -l app=hangfire-jobs -o wide
```

**Expected**: Pods running on nodes with `workload-type=background-jobs` label

---

## Monitoring

### View Dashboard
```bash
# Import to Grafana
cat monitoring/cost-dashboard.json
# Upload to Grafana UI at: http://grafana:3000/dashboard/import
```

### Generate Cost Report
```bash
./scripts/cost-report.sh
```

### Watch HPA Scaling
```bash
watch kubectl get hpa -n hrms-production
```

### Monitor Resource Usage
```bash
kubectl top pods -n hrms-production --sort-by=cpu
kubectl top nodes
```

---

## Troubleshooting

### Issue: HPAs show "unknown" metrics
**Solution**:
```bash
# Install metrics server
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# Wait 60 seconds, then check
kubectl get hpa -n hrms-production
```

### Issue: Cloud SQL Proxy not connecting
**Solution**:
```bash
# Check logs
kubectl logs deployment/cloudsql-proxy -n hrms-production --tail=50

# Verify service account
kubectl get serviceaccount cloudsql-proxy-sa -n hrms-production -o yaml
```

### Issue: No preemptible nodes
**Solution**:
```bash
# Check node pool
gcloud container node-pools list --cluster=$GKE_CLUSTER_NAME --region=$GCP_REGION

# If missing, create manually
gcloud container node-pools create preemptible-jobs-pool \
  --cluster=$GKE_CLUSTER_NAME \
  --region=$GCP_REGION \
  --machine-type=n1-standard-2 \
  --preemptible \
  --num-nodes=2 \
  --min-nodes=1 \
  --max-nodes=5 \
  --enable-autoscaling
```

---

## Rollback

### Quick Rollback
```bash
./scripts/rollback.sh
```

### Manual Rollback
```bash
# Remove HPAs
kubectl delete hpa --all -n hrms-production

# Set fixed replicas
kubectl scale deployment hrms-api --replicas=5 -n hrms-production
kubectl scale deployment hangfire-jobs --replicas=2 -n hrms-production
```

---

## Cost Savings Verification

### Week 1 Checkpoint
```bash
./scripts/cost-report.sh > week1-report.txt
```

**Review**:
- HPA scaling frequency
- Preemptible node eviction rate (should be < 5%)
- Cloud SQL connection efficiency

### Month 1 Checkpoint
```bash
# Compare GCP billing
gcloud beta billing budgets list --billing-account=BILLING_ACCOUNT_ID

# Generate comprehensive report
./scripts/cost-report.sh > month1-report.txt
```

**Target**: $720/month savings visible in GCP billing

---

## Next Steps

### Week 1
- [ ] Monitor HPA scaling patterns
- [ ] Check preemptible node stability
- [ ] Validate application performance
- [ ] Review error rates

### Week 2-4
- [ ] Fine-tune HPA thresholds
- [ ] Optimize resource requests/limits
- [ ] Generate first cost report
- [ ] Document any issues

### Month 2+
- [ ] Implement custom metrics
- [ ] Explore vertical pod autoscaling
- [ ] Consider spot VMs (more stable than preemptible)
- [ ] Automate monthly reporting

---

## Support

### Documentation
- **Comprehensive Guide**: `README.md` (500+ lines)
- **Full Report**: `/workspaces/HRAPP/GCP_KUBERNETES_COST_OPTIMIZATION_REPORT.md`

### Commands Quick Reference
```bash
# Status check
kubectl get all -n hrms-production

# HPA status
kubectl get hpa -n hrms-production -w

# Resource usage
kubectl top pods -n hrms-production

# Validation
./scripts/validate-cost-optimization.sh

# Cost report
./scripts/cost-report.sh

# Rollback
./scripts/rollback.sh
```

### Getting Help
- Review logs: `kubectl logs deployment/NAME -n hrms-production`
- Check events: `kubectl get events -n hrms-production --sort-by='.lastTimestamp'`
- Describe resources: `kubectl describe hpa/pod/deployment NAME -n hrms-production`

---

## Success Criteria

✅ All HPAs active and collecting metrics
✅ Preemptible node pool created with 1-5 nodes
✅ Cloud SQL Proxy running with 2 replicas
✅ Hangfire pods on preemptible nodes
✅ Resource quotas enforced
✅ All validation tests pass
✅ $720/month cost reduction visible in billing

---

**Deployment Time**: 90 minutes (including testing)
**Savings**: $720/month ($8,640/year)
**ROI**: Immediate

Ready to deploy? Run: `./deploy.sh`
