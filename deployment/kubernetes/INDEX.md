# Kubernetes Cost Optimization - File Index

## Quick Links

| Document | Purpose | Lines |
|----------|---------|-------|
| [QUICK_START.md](QUICK_START.md) | Get started in 5 minutes | 250+ |
| [README.md](README.md) | Comprehensive deployment guide | 500+ |
| [Main Report](../../GCP_KUBERNETES_COST_OPTIMIZATION_REPORT.md) | Full deployment report | 950+ |

## Configuration Files

### Autoscaling (HPA)
| File | Workload | Min | Max | CPU Target | Savings |
|------|----------|-----|-----|------------|---------|
| [hpa-api.yaml](autoscaling/hpa-api.yaml) | API Service | 2 | 20 | 70% | $200/mo |
| [hpa-frontend.yaml](autoscaling/hpa-frontend.yaml) | Frontend | 2 | 15 | 75% | $150/mo |
| [hpa-background-jobs.yaml](autoscaling/hpa-background-jobs.yaml) | Background Jobs | 1 | 10 | 80% | $50/mo |

**Total HPA Savings**: $400/month

### Node Pools
| File | Machine Type | Nodes | Cost Savings | Use Case |
|------|--------------|-------|--------------|----------|
| [preemptible-jobs-pool.yaml](node-pools/preemptible-jobs-pool.yaml) | n1-standard-2 | 1-5 | 60-80% | Background jobs |

**Total Node Pool Savings**: $240/month

### Infrastructure
| File | Component | Replicas | Ports | Savings |
|------|-----------|----------|-------|---------|
| [cloudsql-proxy-deployment.yaml](infrastructure/cloudsql-proxy-deployment.yaml) | Cloud SQL Proxy | 2 | 5432, 5433 | $80/mo |

**Total Infrastructure Savings**: $80/month

### Workloads
| File | Component | Node Type | Features |
|------|-----------|-----------|----------|
| [hangfire-deployment.yaml](workloads/hangfire-deployment.yaml) | Background Jobs | Preemptible | Auto-scaling, HA |

### Resource Management
| File | Purpose | Components |
|------|---------|------------|
| [resource-quotas.yaml](resource-management/resource-quotas.yaml) | Resource governance | Quotas, limits, priority classes |

### Configuration
| File | ConfigMaps | Purpose |
|------|------------|---------|
| [cost-optimization-config.yaml](config/cost-optimization-config.yaml) | 3 | Feature flags, DB config, monitoring |

## Automation Scripts

| Script | Purpose | Runtime | Output |
|--------|---------|---------|--------|
| [deploy.sh](deploy.sh) | Automated deployment | 60 min | Deployment summary |
| [validate-cost-optimization.sh](scripts/validate-cost-optimization.sh) | Validation tests | 5 min | Pass/Fail report |
| [cost-report.sh](scripts/cost-report.sh) | Monthly cost analysis | 2 min | Cost report file |
| [rollback.sh](scripts/rollback.sh) | Safe rollback | 15 min | Interactive prompts |

## Monitoring

| File | Type | Panels | Metrics |
|------|------|--------|---------|
| [cost-dashboard.json](monitoring/cost-dashboard.json) | Grafana Dashboard | 12 | HPA, nodes, SQL, resources |

## File Size Summary

```
Total Configuration Files: 9
Total Scripts: 4
Total Documentation: 3
Total Size: ~100 KB
```

## Usage Patterns

### First Time Deployment
1. Read [QUICK_START.md](QUICK_START.md)
2. Run `./deploy.sh --dry-run`
3. Run `./deploy.sh`
4. Run `./scripts/validate-cost-optimization.sh`

### Regular Operations
1. Monitor: `kubectl get hpa -n hrms-production -w`
2. Report: `./scripts/cost-report.sh`
3. Validate: `./scripts/validate-cost-optimization.sh`

### Troubleshooting
1. Check [README.md](README.md) Troubleshooting section
2. Review logs: `kubectl logs deployment/NAME -n hrms-production`
3. Check events: `kubectl get events -n hrms-production --sort-by='.lastTimestamp'`

### Rollback
1. Run `./scripts/rollback.sh` (interactive)
2. Follow prompts for each component

## Cost Savings Map

```
Total Monthly Savings: $720
├── HPA (Dynamic Scaling): $400
│   ├── API: $200
│   ├── Frontend: $150
│   └── Background Jobs: $50
├── Preemptible VMs: $240
│   └── Background job nodes (60-80% cheaper)
└── Cloud SQL Proxy: $80
    └── Connection pooling efficiency
```

## File Dependencies

```
deploy.sh
├── autoscaling/*.yaml
├── node-pools/*.yaml
├── workloads/*.yaml
├── infrastructure/*.yaml
├── resource-management/*.yaml
└── config/*.yaml

validate-cost-optimization.sh
├── Checks all deployed resources
└── Validates configuration correctness

cost-report.sh
├── Analyzes HPA metrics
├── Checks node pool usage
├── Reviews resource quotas
└── Calculates savings

rollback.sh
├── Removes HPAs
├── Migrates off preemptible nodes
├── Removes Cloud SQL Proxy
└── Cleans up ConfigMaps
```

## Testing Matrix

| Test | File | Command | Expected Result |
|------|------|---------|-----------------|
| HPA Functionality | All HPAs | `kubectl get hpa -n hrms-production` | 3+ HPAs active |
| Preemptible Nodes | preemptible-jobs-pool.yaml | `kubectl get nodes -l workload-type=background-jobs` | 1-5 nodes |
| Cloud SQL Proxy | cloudsql-proxy-deployment.yaml | `kubectl get deployment cloudsql-proxy` | 2/2 ready |
| Resource Quotas | resource-quotas.yaml | `kubectl describe resourcequota` | Quotas enforced |
| Hangfire Placement | hangfire-deployment.yaml | `kubectl get pods -l app=hangfire-jobs -o wide` | On preemptible nodes |

## Deployment Phases

### Phase 1: Infrastructure (15 min)
- [x] resource-quotas.yaml
- [x] cloudsql-proxy-deployment.yaml
- [x] cost-optimization-config.yaml

### Phase 2: Node Pool (20 min)
- [x] preemptible-jobs-pool.yaml

### Phase 3: Workloads (15 min)
- [x] hangfire-deployment.yaml

### Phase 4: Autoscaling (10 min)
- [x] hpa-api.yaml
- [x] hpa-frontend.yaml
- [x] hpa-background-jobs.yaml

## Key Metrics to Monitor

### HPA Metrics
- Current vs Desired Replicas
- CPU Utilization (target: 70-80%)
- Memory Utilization (target: 80-85%)
- Scaling Events per hour

### Node Pool Metrics
- Active Preemptible Nodes (1-5)
- Eviction Rate (should be < 5%)
- Pod Rescheduling Time
- Node Autoscaling Events

### Cloud SQL Proxy Metrics
- Active Connections
- Connection Errors
- Query Latency
- Connection Pool Efficiency

### Cost Metrics
- Current vs Baseline Pod Count
- Preemptible Node Savings
- Monthly Cost Trend
- Savings vs Target ($720)

## Support Resources

### Documentation
- [Main Report](../../GCP_KUBERNETES_COST_OPTIMIZATION_REPORT.md) - Complete deployment documentation
- [README.md](README.md) - Comprehensive technical guide
- [QUICK_START.md](QUICK_START.md) - Fast deployment guide

### External Links
- [GKE Autoscaling](https://cloud.google.com/kubernetes-engine/docs/how-to/horizontal-pod-autoscaling)
- [Preemptible VMs](https://cloud.google.com/compute/docs/instances/preemptible)
- [Cloud SQL Proxy](https://cloud.google.com/sql/docs/postgres/sql-proxy)
- [Resource Management](https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/)

## Version Information

- **Created**: 2025-11-17
- **Version**: 1.0
- **Kubernetes**: 1.27+
- **GKE**: Regional clusters
- **Target Savings**: $720/month

---

**Quick Start**: `cd /workspaces/HRAPP/deployment/kubernetes && ./deploy.sh`

**Validate**: `./scripts/validate-cost-optimization.sh`

**Monitor**: `kubectl get hpa -n hrms-production -w`

**Report**: `./scripts/cost-report.sh`
