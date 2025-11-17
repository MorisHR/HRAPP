# Kubernetes Cost Optimization - Deployment Checklist

## Pre-Deployment Checklist

### Environment Setup
- [ ] GCP Project ID confirmed: `__________________`
- [ ] GKE Cluster name confirmed: `__________________`
- [ ] Region confirmed: `__________________`
- [ ] kubectl connected to cluster
- [ ] gcloud authenticated
- [ ] Namespace `hrms-production` exists

### Prerequisites Verified
- [ ] Kubernetes version 1.27+ confirmed
- [ ] Metrics Server installed
- [ ] Workload Identity enabled on cluster
- [ ] Cloud SQL instances created (master + replica)
- [ ] Service account for Cloud SQL Proxy created

### Configuration Files Updated
- [ ] PROJECT_ID replaced in all YAML files
- [ ] REGION replaced in all YAML files
- [ ] Cloud SQL instance names updated
- [ ] Database credentials configured as secrets

## Deployment Checklist

### Phase 1: Infrastructure (15 min)
- [ ] Resource quotas deployed
  ```bash
  kubectl apply -f resource-management/resource-quotas.yaml
  kubectl describe resourcequota -n hrms-production
  ```
- [ ] Cloud SQL Proxy service account configured
  ```bash
  gcloud iam service-accounts create cloudsql-proxy
  kubectl annotate serviceaccount cloudsql-proxy-sa
  ```
- [ ] Cloud SQL Proxy deployed
  ```bash
  kubectl apply -f infrastructure/cloudsql-proxy-deployment.yaml
  kubectl rollout status deployment/cloudsql-proxy -n hrms-production
  ```
- [ ] ConfigMaps deployed
  ```bash
  kubectl apply -f config/cost-optimization-config.yaml
  kubectl get configmap -n hrms-production
  ```

### Phase 2: Node Pool (20 min)
- [ ] Preemptible node pool created
  ```bash
  gcloud container node-pools create preemptible-jobs-pool ...
  gcloud container node-pools list --cluster=hrms-cluster
  ```
- [ ] Node labels verified
  ```bash
  kubectl get nodes -l workload-type=background-jobs --show-labels
  ```
- [ ] Node taints verified
  ```bash
  kubectl get nodes -l workload-type=background-jobs -o custom-columns=NAME:.metadata.name,TAINTS:.spec.taints
  ```

### Phase 3: Workloads (15 min)
- [ ] Hangfire deployment created
  ```bash
  kubectl apply -f workloads/hangfire-deployment.yaml
  kubectl rollout status deployment/hangfire-jobs -n hrms-production
  ```
- [ ] Pods on preemptible nodes verified
  ```bash
  kubectl get pods -n hrms-production -l app=hangfire-jobs -o wide
  ```

### Phase 4: Autoscaling (10 min)
- [ ] Metrics Server running
  ```bash
  kubectl get deployment metrics-server -n kube-system
  ```
- [ ] API HPA deployed
  ```bash
  kubectl apply -f autoscaling/hpa-api.yaml
  kubectl get hpa hrms-api-hpa -n hrms-production
  ```
- [ ] Frontend HPA deployed
  ```bash
  kubectl apply -f autoscaling/hpa-frontend.yaml
  kubectl get hpa hrms-frontend-hpa -n hrms-production
  ```
- [ ] Background Jobs HPA deployed
  ```bash
  kubectl apply -f autoscaling/hpa-background-jobs.yaml
  kubectl get hpa hrms-background-jobs-hpa -n hrms-production
  ```

## Validation Checklist

### Automated Validation
- [ ] Run validation script
  ```bash
  ./scripts/validate-cost-optimization.sh
  ```
- [ ] All tests passed (green checkmarks)

### Manual Validation
- [ ] HPA metrics showing
  ```bash
  kubectl get hpa -n hrms-production
  # Should show CPU/Memory percentages, not "unknown"
  ```
- [ ] Cloud SQL Proxy connectivity
  ```bash
  kubectl exec -it deployment/cloudsql-proxy -n hrms-production -- nc -zv localhost 5432
  kubectl exec -it deployment/cloudsql-proxy -n hrms-production -- nc -zv localhost 5433
  ```
- [ ] Preemptible nodes active
  ```bash
  kubectl get nodes -l workload-type=background-jobs
  # Should show 1-5 nodes
  ```
- [ ] Resource quotas enforced
  ```bash
  kubectl describe resourcequota hrms-production-quota -n hrms-production
  # Should show used/hard values
  ```

### Functional Testing
- [ ] Load test HPA scaling
  ```bash
  # Generate load and watch scaling
  kubectl get hpa -n hrms-production -w
  ```
- [ ] Verify pod distribution
  ```bash
  kubectl get pods -n hrms-production -o wide
  ```
- [ ] Check application health
  ```bash
  kubectl get pods -n hrms-production
  # All pods should be Running
  ```

## Monitoring Setup

### Grafana Dashboard
- [ ] Import cost-dashboard.json to Grafana
- [ ] Verify all panels showing data
- [ ] Set up alerts for cost overruns

### Ongoing Monitoring
- [ ] Set up kubectl watch
  ```bash
  watch kubectl get hpa -n hrms-production
  ```
- [ ] Schedule weekly cost reports
  ```bash
  crontab -e
  # 0 9 * * 1 cd /path/to/deployment/kubernetes && ./scripts/cost-report.sh
  ```

## Cost Verification

### Week 1 Checkpoint
- [ ] Generate first cost report
  ```bash
  ./scripts/cost-report.sh > week1-report.txt
  ```
- [ ] Review HPA scaling patterns
- [ ] Check preemptible node eviction rate (< 5%)
- [ ] Monitor application performance

### Month 1 Checkpoint
- [ ] Compare GCP billing before/after
- [ ] Verify $720/month savings achieved
- [ ] Document any issues encountered
- [ ] Optimize HPA thresholds if needed

## Rollback Plan

### Rollback Triggers
- [ ] HPA causing instability (frequent scaling)
- [ ] Preemptible evictions > 10% per day
- [ ] Cloud SQL Proxy connection issues
- [ ] Application performance degradation

### Rollback Procedure
- [ ] Run rollback script
  ```bash
  ./scripts/rollback.sh
  ```
- [ ] Verify applications stable
- [ ] Document reason for rollback
- [ ] Plan remediation

## Documentation

### Team Handoff
- [ ] README.md reviewed by team
- [ ] Quick Start guide tested
- [ ] Runbooks updated
- [ ] On-call procedures updated

### Knowledge Transfer
- [ ] Team training completed
- [ ] Monitoring dashboards shared
- [ ] Escalation procedures documented
- [ ] Troubleshooting guide reviewed

## Success Criteria

### Technical Success
- [x] All 9 configuration files deployed
- [ ] All HPAs active and scaling
- [ ] Preemptible node pool running (1-5 nodes)
- [ ] Cloud SQL Proxy operational (2 replicas)
- [ ] Resource quotas enforced
- [ ] All validation tests passing

### Business Success
- [ ] $720/month cost savings achieved
- [ ] Application performance maintained
- [ ] No increase in error rates
- [ ] Team confident with new setup

## Post-Deployment

### Week 1 Tasks
- [ ] Daily HPA monitoring
- [ ] Check preemptible node stability
- [ ] Review application logs
- [ ] Generate cost report

### Week 2-4 Tasks
- [ ] Fine-tune HPA thresholds
- [ ] Optimize resource requests/limits
- [ ] Document lessons learned
- [ ] Plan Phase 2 optimizations

### Month 2+ Tasks
- [ ] Implement custom metrics
- [ ] Explore vertical pod autoscaling
- [ ] Evaluate spot VMs
- [ ] Automate cost reporting

## Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| DevOps Lead | | | |
| Platform Engineer | | | |
| SRE Lead | | | |
| Engineering Manager | | | |

---

**Deployment Date**: _______________
**Deployed By**: _______________
**Validated By**: _______________
**Approved By**: _______________

**Monthly Savings Target**: $720
**Deployment Time**: 90 minutes
**Status**: [ ] In Progress  [ ] Completed  [ ] Rolled Back
