# HRMS Frontend - DevOps Quick Start Guide

**For**: DevOps Engineers, On-Call Engineers, Release Managers
**Purpose**: Quick reference for common deployment operations
**Time to Read**: 5 minutes

---

## 🚀 Quick Commands

### Deploy to Staging
```bash
cd /workspaces/HRAPP/hrms-frontend
./scripts/deploy-staging.sh
```

### Deploy to Production
```bash
cd /workspaces/HRAPP/hrms-frontend
./scripts/deploy-production.sh
# Type "yes" when prompted
```

### Emergency Rollback
```bash
cd /workspaces/HRAPP/hrms-frontend
./scripts/rollback.sh
# Type "ROLLBACK" when prompted
```

### Health Check
```bash
./scripts/health-check.sh --environment production --verbose
```

---

## 📁 File Locations

### Deployment Scripts
```
/workspaces/HRAPP/hrms-frontend/scripts/
├── deploy-staging.sh       - Staging deployment
├── deploy-production.sh    - Production deployment (Blue/Green)
├── rollback.sh            - Emergency rollback
└── health-check.sh        - Health checks
```

### Documentation
```
/workspaces/HRAPP/hrms-frontend/
├── DEVOPS_AUDIT_REPORT.md          - Complete infrastructure audit
├── DEPLOYMENT_RUNBOOK.md           - Detailed deployment guide
├── ROLLBACK_PROCEDURE.md           - Emergency procedures
├── MONITORING_SETUP.md             - Monitoring configuration
└── DEVOPS_DEPLOYMENT_COMPLETE.md   - Executive summary
```

### Infrastructure
```
/workspaces/HRAPP/hrms-frontend/
├── Dockerfile              - Production container
├── .dockerignore          - Docker build exclusions
└── nginx/
    ├── nginx.conf         - Main nginx config
    └── default.conf       - Server block config
```

---

## 🔍 Common Operations

### Check Current Production Environment
```bash
ssh deploy@hrms.example.com "readlink /var/www/hrms-production/current"
# Output: /var/www/hrms-production/blue  (or green)
```

### View Recent Logs
```bash
tail -f logs/deploy-production-*.log
# Or
ssh deploy@hrms.example.com "tail -f /var/log/nginx/access.log"
```

### Check Feature Flag Status
```bash
curl -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  "$FEATURE_FLAG_API_URL/phase1-migration"
```

### Update Feature Flag
```bash
# Set to 10%
curl -X POST "$FEATURE_FLAG_API_URL/phase1-migration" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"rolloutPercentage": 10, "enabled": true}'
```

---

## 🚨 Emergency Decision Tree

```
Is the site down?
├─ YES → Run ./scripts/rollback.sh IMMEDIATELY
└─ NO  → Continue...

Is error rate > 1%?
├─ YES → Run ./scripts/rollback.sh
└─ NO  → Continue...

Is error rate 0.5-1%?
├─ YES → Set feature flag to 0%, monitor for 5 min
│        └─ If persists → ./scripts/rollback.sh
└─ NO  → Continue monitoring

Is response time > 2s?
├─ YES → Investigate, consider rollback
└─ NO  → Normal operations
```

---

## 📊 Monitoring Dashboards

### DataDog (Primary)
```
URL: https://app.datadoghq.com/dashboard/hrms-frontend
Watch for:
- Error rate < 0.1%
- Response time p95 < 200ms
- Active users
```

### Grafana (Infrastructure)
```
URL: https://grafana.hrms.example.com
Watch for:
- Server CPU < 80%
- Memory usage < 80%
- Nginx requests/sec
```

---

## 🔔 Alert Channels

- **Critical**: PagerDuty (immediate page)
- **High**: Slack #production-incidents
- **Medium**: Slack #production-deploys
- **Low**: Email devops@hrms.example.com

---

## 📞 Emergency Contacts

1. **On-Call DevOps**: PagerDuty → Auto-escalates
2. **DevOps Lead**: Escalated after 10 minutes
3. **Engineering Director**: Escalated after 20 minutes

---

## 📚 Detailed Documentation

For detailed procedures, see:

- **DEPLOYMENT_RUNBOOK.md** - Complete deployment guide
- **ROLLBACK_PROCEDURE.md** - All rollback scenarios
- **MONITORING_SETUP.md** - Monitoring configuration
- **DEVOPS_AUDIT_REPORT.md** - Infrastructure audit

---

## ✅ Pre-Deployment Checklist

- [ ] All tests passing in CI/CD
- [ ] Code coverage ≥ 85%
- [ ] Bundle size < 500KB
- [ ] Staging deployment successful
- [ ] Security scan passed
- [ ] Stakeholder approval received

---

**Last Updated**: November 17, 2025
**Maintained By**: DevOps Team
