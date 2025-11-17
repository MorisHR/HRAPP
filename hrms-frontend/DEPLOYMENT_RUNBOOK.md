# HRMS Frontend - Deployment Runbook

**Version**: 1.0
**Last Updated**: November 17, 2025
**Maintained By**: DevOps Team
**On-Call Escalation**: PagerDuty → #devops-oncall

---

## Table of Contents

1. [Overview](#overview)
2. [Pre-Deployment Checklist](#pre-deployment-checklist)
3. [Deployment Procedures](#deployment-procedures)
4. [Post-Deployment Verification](#post-deployment-verification)
5. [Rollback Procedures](#rollback-procedures)
6. [Troubleshooting](#troubleshooting)
7. [Emergency Contacts](#emergency-contacts)

---

## Overview

This runbook provides step-by-step procedures for deploying the HRMS Frontend application to staging and production environments.

### Deployment Strategy

- **Staging**: Direct deployment with automated testing
- **Production**: Blue/Green deployment with manual approval gates
- **Rollout**: Feature flags control gradual rollout (0% → 10% → 25% → 50% → 100%)

### Key Principles

1. **Safety First**: Never deploy without approval
2. **Gradual Rollout**: Always start at 0% for production
3. **Monitor Actively**: Watch metrics for first 5 minutes
4. **Rollback Fast**: Don't hesitate if issues detected

---

## Pre-Deployment Checklist

### Code Quality Checklist

- [ ] All tests passing in CI/CD
- [ ] Code coverage ≥ 85%
- [ ] Lighthouse scores: Performance >90%, Accessibility 100%
- [ ] Bundle size within limits (main.js < 500KB)
- [ ] Security scan passed (no critical vulnerabilities)
- [ ] Peer review completed and approved

### Environment Checklist

- [ ] Staging deployment successful
- [ ] Smoke tests passed in staging
- [ ] Feature flags configured correctly
- [ ] API endpoints verified
- [ ] SSL certificates valid (>30 days)
- [ ] DNS configuration verified

### Stakeholder Checklist

- [ ] Product owner approval received
- [ ] QA sign-off completed
- [ ] Security team approval (if security changes)
- [ ] Customer success team notified
- [ ] Documentation updated

### Infrastructure Checklist

- [ ] Backup verification completed
- [ ] Monitoring dashboards accessible
- [ ] Alerting configured and tested
- [ ] On-call engineer identified
- [ ] Rollback plan reviewed

---

## Deployment Procedures

### Staging Deployment

**Estimated Time**: 15-20 minutes
**Approval Required**: No
**Automated**: Yes (via CI/CD)

#### Step 1: Trigger Deployment

**Option A: Automatic (via Git)**
```bash
git checkout develop
git pull origin develop
git push origin develop  # Triggers CI/CD
```

**Option B: Manual Script**
```bash
cd /workspaces/HRAPP/hrms-frontend

# Set environment variables
export STAGING_URL="https://staging.hrms.example.com"
export STAGING_DEPLOY_PATH="/var/www/hrms-staging"
export STAGING_SSH_HOST="staging.hrms.example.com"
export STAGING_SSH_USER="deploy"
export SLACK_WEBHOOK_URL="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"

# Run deployment script
./scripts/deploy-staging.sh
```

#### Step 2: Monitor Deployment

```bash
# Watch deployment logs
tail -f logs/deploy-staging-*.log

# Check CI/CD pipeline
# Navigate to: https://github.com/YOUR-ORG/hrms-frontend/actions
```

#### Step 3: Verify Deployment

```bash
# Run health checks
./scripts/health-check.sh --environment staging

# Manual verification
curl https://staging.hrms.example.com
curl https://staging.hrms.example.com/auth/subdomain
```

#### Step 4: Run Smoke Tests

```bash
# Critical user flows
1. Navigate to staging.hrms.example.com
2. Test subdomain login flow
3. Test admin login flow
4. Verify employee dashboard loads
5. Check for console errors (F12)
```

### Production Deployment

**Estimated Time**: 30-45 minutes
**Approval Required**: YES
**Automated**: Partial (requires manual approval)

#### Step 1: Pre-Deployment Preparation

```bash
cd /workspaces/HRAPP/hrms-frontend

# Ensure you're on main branch
git checkout main
git pull origin main

# Verify latest commit is desired release
git log --oneline -n 5

# Set environment variables
export PRODUCTION_URL="https://hrms.example.com"
export PRODUCTION_DEPLOY_PATH="/var/www/hrms-production"
export PRODUCTION_SSH_HOST="hrms.example.com"
export PRODUCTION_SSH_USER="deploy"
export FEATURE_FLAG_API_URL="https://api.hrms.example.com/api/feature-flags"
export FEATURE_FLAG_API_KEY="your-api-key"
export SLACK_WEBHOOK_URL="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
export DATADOG_API_KEY="your-datadog-api-key"
export PAGERDUTY_API_KEY="your-pagerduty-api-key"
```

#### Step 2: Communicate Deployment

```bash
# Send notification to Slack
# Message in #production-deploys channel:
"🚀 Starting production deployment for HRMS Frontend
- Release: [commit hash]
- Deployed by: [your name]
- ETA: 30-45 minutes
- Impact: None expected (0% feature flag rollout)
"
```

#### Step 3: Create Backup

```bash
# Backup is automatic in script, but verify manually if needed
ssh deploy@hrms.example.com "
  cd /var/www/hrms-production
  ls -lth backups/ | head -5
"
```

#### Step 4: Run Deployment Script

```bash
./scripts/deploy-production.sh
```

The script will:
1. ✅ Ask for approval (type "yes" to continue)
2. ✅ Check prerequisites
3. ✅ Build application
4. ✅ Determine Blue/Green target (automatic)
5. ✅ Create backup
6. ✅ Deploy to inactive environment
7. ✅ Run health checks
8. ✅ Switch traffic
9. ✅ Verify smoke tests
10. ✅ Set feature flags to 0%
11. ✅ Monitor for 5 minutes

#### Step 5: Monitor Initial Rollout

```bash
# Watch logs in real-time
tail -f logs/deploy-production-*.log

# Monitor DataDog/Grafana
# Navigate to: https://app.datadoghq.com/dashboard/hrms-frontend
# Watch for:
# - Error rate spikes
# - Response time degradation
# - Failed requests
```

#### Step 6: Gradual Rollout

**Hour 0-1: 0% → 10%**
```bash
# Update feature flag to 10%
curl -X POST "$FEATURE_FLAG_API_URL/phase1-migration" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -d '{"rolloutPercentage": 10, "enabled": true}'

# Monitor for 30 minutes
# ✅ Error rate < 0.1%
# ✅ Response time p95 < 200ms
# ✅ No customer complaints
```

**Hour 1-3: 10% → 25%**
```bash
# Update to 25%
curl -X POST "$FEATURE_FLAG_API_URL/phase1-migration" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -d '{"rolloutPercentage": 25, "enabled": true}'

# Monitor for 1 hour
```

**Hour 3-6: 25% → 50%**
```bash
# Update to 50%
curl -X POST "$FEATURE_FLAG_API_URL/phase1-migration" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -d '{"rolloutPercentage": 50, "enabled": true}'

# Monitor for 2 hours
```

**Hour 6-12: 50% → 100%**
```bash
# Full rollout
curl -X POST "$FEATURE_FLAG_API_URL/phase1-migration" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -d '{"rolloutPercentage": 100, "enabled": true}'

# Monitor for remainder of business day
```

---

## Post-Deployment Verification

### Automated Health Checks

```bash
# Run comprehensive health check
./scripts/health-check.sh --environment production --verbose

# Expected output:
# ✓ Homepage (HTTP 200)
# ✓ Admin login page (HTTP 200)
# ✓ Subdomain login page (HTTP 200)
# ✓ Main JavaScript bundle (HTTP 200)
# ✓ Main stylesheet (HTTP 200)
# ✓ SSL certificate valid (XX days remaining)
# ✓ Content compression enabled
# ✓ API health endpoint responding
```

### Manual Verification

1. **Homepage Test**
   ```
   Navigate to: https://hrms.example.com
   ✅ Page loads within 2 seconds
   ✅ No console errors
   ✅ Assets load correctly
   ```

2. **Authentication Test**
   ```
   Navigate to: https://hrms.example.com/auth/subdomain
   ✅ Login form displays correctly
   ✅ Can submit form (don't complete login)
   ✅ API endpoints responding
   ```

3. **Admin Portal Test**
   ```
   Navigate to: https://hrms.example.com/admin/login
   ✅ Admin login page loads
   ✅ Styling correct
   ✅ No JavaScript errors
   ```

4. **Service Worker Test**
   ```
   Open DevTools (F12) → Application → Service Workers
   ✅ Service Worker registered
   ✅ Status: Activated
   ✅ Cache populated
   ```

### Monitoring Checks

**DataDog Dashboard**
```
Navigate to: https://app.datadoghq.com/dashboard/hrms-frontend

Monitor for 30 minutes:
✅ Error rate < 0.1%
✅ Response time p95 < 200ms
✅ Response time p99 < 500ms
✅ No 5xx errors
✅ No client-side errors
```

**Grafana Dashboard**
```
Navigate to: https://grafana.hrms.example.com/d/frontend

Monitor:
✅ Active users trending normally
✅ Page load times within SLA
✅ API response times normal
✅ No alert triggers
```

---

## Rollback Procedures

**See**: [ROLLBACK_PROCEDURE.md](./ROLLBACK_PROCEDURE.md) for detailed rollback guide

### Quick Rollback (2 minutes)

```bash
# Emergency rollback script
./scripts/rollback.sh

# Type "ROLLBACK" when prompted
# Script will:
# 1. Switch to previous Blue/Green environment
# 2. Reset feature flags to 0%
# 3. Verify rollback
# 4. Send notifications
```

### Rollback Decision Tree

```
Issue Detected?
├─ YES → What type?
│   ├─ Error rate > 1% → ROLLBACK IMMEDIATELY
│   ├─ Response time > 1s → ROLLBACK IMMEDIATELY
│   ├─ Customer complaints → Investigate (2 min) → ROLLBACK if critical
│   ├─ Console errors → Investigate (5 min) → ROLLBACK if blocking users
│   └─ Visual bugs → Feature flag to 0% → Fix forward or rollback
└─ NO → Continue monitoring
```

---

## Troubleshooting

### Issue: Deployment Script Fails

**Symptom**: `deploy-production.sh` exits with error

**Diagnosis**:
```bash
# Check logs
tail -f logs/deploy-production-*.log

# Check SSH connectivity
ssh deploy@hrms.example.com "echo 'Connection OK'"

# Check disk space
ssh deploy@hrms.example.com "df -h"
```

**Resolution**:
1. Review error message in logs
2. Fix underlying issue
3. Re-run deployment script
4. If critical, rollback and deploy outside business hours

---

### Issue: Health Checks Failing

**Symptom**: `health-check.sh` reports failures

**Diagnosis**:
```bash
# Run with verbose output
./scripts/health-check.sh --environment production --verbose

# Check nginx status
ssh deploy@hrms.example.com "sudo systemctl status nginx"

# Check application logs
ssh deploy@hrms.example.com "tail -100 /var/log/nginx/error.log"
```

**Resolution**:
1. Identify failing check
2. Fix server configuration
3. Reload nginx: `sudo systemctl reload nginx`
4. Re-run health checks

---

### Issue: High Error Rate After Deployment

**Symptom**: DataDog shows error rate > 0.5%

**Diagnosis**:
```bash
# Check error logs
ssh deploy@hrms.example.com "tail -500 /var/log/nginx/error.log"

# Check browser console
# Navigate to site → F12 → Console
# Look for JavaScript errors
```

**Resolution**:
1. Reduce feature flag to 0%
2. Investigate errors
3. If critical: ROLLBACK
4. If non-critical: Fix forward

---

### Issue: SSL Certificate Issues

**Symptom**: SSL warnings in browser

**Diagnosis**:
```bash
# Check certificate expiry
echo | openssl s_client -servername hrms.example.com -connect hrms.example.com:443 2>/dev/null | openssl x509 -noout -dates
```

**Resolution**:
```bash
# Renew certificate (Let's Encrypt)
ssh deploy@hrms.example.com "sudo certbot renew"

# Reload nginx
ssh deploy@hrms.example.com "sudo systemctl reload nginx"
```

---

### Issue: Blue/Green Switch Failed

**Symptom**: Traffic not switching to new environment

**Diagnosis**:
```bash
# Check current symlink
ssh deploy@hrms.example.com "ls -la /var/www/hrms-production/current"

# Check nginx configuration
ssh deploy@hrms.example.com "sudo nginx -t"
```

**Resolution**:
```bash
# Manually switch symlink
ssh deploy@hrms.example.com "
  ln -sfn /var/www/hrms-production/blue /var/www/hrms-production/current
  sudo systemctl reload nginx
"

# Verify
curl -I https://hrms.example.com
```

---

## Emergency Contacts

### On-Call Rotation

| Role | Primary | Backup | Contact |
|------|---------|--------|---------|
| DevOps Lead | John Doe | Jane Smith | PagerDuty |
| Frontend Lead | Alice Johnson | Bob Williams | PagerDuty |
| Backend Lead | Charlie Brown | Diana Prince | PagerDuty |
| Security | Eve Adams | Frank Castle | PagerDuty |

### Escalation Path

1. **Level 1**: On-call DevOps Engineer (PagerDuty)
2. **Level 2**: DevOps Lead + Frontend Lead
3. **Level 3**: CTO + Engineering Director
4. **Level 4**: CEO (Critical customer impact only)

### Communication Channels

- **Slack**: #production-deploys (announcements)
- **Slack**: #production-incidents (active issues)
- **PagerDuty**: Automatic alerts for critical issues
- **Email**: devops@hrms.example.com
- **Phone**: +1-555-DEVOPS-1

---

## Appendix

### Useful Commands

```bash
# Check deployment status
ssh deploy@hrms.example.com "readlink /var/www/hrms-production/current"

# View nginx logs
ssh deploy@hrms.example.com "tail -f /var/log/nginx/access.log"

# Restart nginx (careful!)
ssh deploy@hrms.example.com "sudo systemctl restart nginx"

# Check disk space
ssh deploy@hrms.example.com "df -h"

# List backups
ssh deploy@hrms.example.com "ls -lth /var/www/hrms-production/backups"

# Check git commit
git log --oneline -n 5
```

### Dashboard Links

- **CI/CD**: https://github.com/YOUR-ORG/hrms-frontend/actions
- **DataDog**: https://app.datadoghq.com/dashboard/hrms-frontend
- **Grafana**: https://grafana.hrms.example.com
- **PagerDuty**: https://hrms.pagerduty.com
- **Slack**: https://hrms.slack.com/channels/production-deploys

---

**Document Version**: 1.0
**Last Updated**: November 17, 2025
**Next Review**: December 17, 2025
**Maintained By**: DevOps Team
