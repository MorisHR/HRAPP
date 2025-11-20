# HRMS Frontend - Emergency Rollback Procedure

**Version**: 1.0
**Last Updated**: November 17, 2025
**Owner**: DevOps Team
**Emergency Hotline**: PagerDuty → #production-incidents

---

## 🚨 Emergency Rollback Decision Matrix

Use this matrix to determine if rollback is necessary:

| Issue | Severity | Action | Response Time |
|-------|----------|--------|---------------|
| Error rate > 5% | **CRITICAL** | ROLLBACK IMMEDIATELY | < 2 minutes |
| Error rate 1-5% | **HIGH** | ROLLBACK IMMEDIATELY | < 5 minutes |
| Error rate 0.5-1% | **MEDIUM** | Reduce feature flag to 0%, investigate | < 10 minutes |
| Response time > 2s | **HIGH** | ROLLBACK IMMEDIATELY | < 5 minutes |
| Response time 1-2s | **MEDIUM** | Reduce feature flag, investigate | < 10 minutes |
| Site completely down | **CRITICAL** | ROLLBACK IMMEDIATELY | < 1 minute |
| Visual bugs | **LOW** | Feature flag to 0%, fix forward | < 30 minutes |
| Console errors (non-blocking) | **LOW** | Monitor, consider feature flag reduction | < 1 hour |
| Customer complaints > 5 | **HIGH** | ROLLBACK IMMEDIATELY | < 5 minutes |
| Security vulnerability detected | **CRITICAL** | ROLLBACK + INCIDENT RESPONSE | < 2 minutes |

---

## Table of Contents

1. [Quick Rollback Guide](#quick-rollback-guide)
2. [Detailed Rollback Procedures](#detailed-rollback-procedures)
3. [Post-Rollback Actions](#post-rollback-actions)
4. [Rollback Verification](#rollback-verification)
5. [Root Cause Analysis](#root-cause-analysis)
6. [Communication Templates](#communication-templates)

---

## Quick Rollback Guide

### 1-Minute Emergency Rollback

**For**: Site down, critical errors, customer impact

```bash
# Run this immediately
cd /workspaces/HRAPP/hrms-frontend
./scripts/rollback.sh
```

**What happens**:
1. Prompts for confirmation (type "ROLLBACK")
2. Switches Blue/Green environment automatically
3. Resets feature flags to 0%
4. Verifies rollback
5. Sends notifications to Slack/PagerDuty

**Total Time**: ~2 minutes

---

### 5-Minute Controlled Rollback

**For**: Non-critical issues, gradual degradation

```bash
# Step 1: Reduce feature flag to 0% (30 seconds)
curl -X POST "$FEATURE_FLAG_API_URL/phase1-migration" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -d '{"rolloutPercentage": 0, "enabled": false}'

# Step 2: Monitor for 2 minutes
# Check if issue persists

# Step 3: If issue persists, full rollback
./scripts/rollback.sh
```

---

## Detailed Rollback Procedures

### Rollback Procedure A: Blue/Green Switch

**When to use**: Standard production rollback
**Time**: 2-3 minutes
**Risk**: Low

#### Prerequisites

- [ ] SSH access to production server
- [ ] Rollback script available
- [ ] On-call engineer notified
- [ ] Incident created in PagerDuty

#### Steps

**Step 1: Initiate Rollback**
```bash
cd /workspaces/HRAPP/hrms-frontend

# Set environment variables if not already set
export PRODUCTION_URL="https://hrms.example.com"
export PRODUCTION_DEPLOY_PATH="/var/www/hrms-production"
export PRODUCTION_SSH_HOST="hrms.example.com"
export PRODUCTION_SSH_USER="deploy"
export FEATURE_FLAG_API_URL="https://api.hrms.example.com/api/feature-flags"
export FEATURE_FLAG_API_KEY="your-api-key"
export SLACK_WEBHOOK_URL="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"

# Execute rollback
./scripts/rollback.sh
```

**Step 2: Confirm Rollback**
```
When prompted, type: ROLLBACK
```

**Step 3: Monitor Rollback**
```bash
# Watch logs
tail -f logs/rollback-*.log

# Expected output:
# [CRITICAL] Rolling back to previous environment: green
# [SUCCESS] Traffic switched to green environment
# [INFO] Resetting feature flags to 0%...
# [SUCCESS] Feature flags reset to 0%
# [SUCCESS] Rollback verification passed (HTTP 200)
```

**Step 4: Verify**
```bash
# Run health check
./scripts/health-check.sh --environment production

# Check current environment
ssh deploy@hrms.example.com "readlink /var/www/hrms-production/current"
```

---

### Rollback Procedure B: Restore from Backup

**When to use**: Blue/Green environments both broken, data corruption
**Time**: 5-10 minutes
**Risk**: Medium

#### Prerequisites

- [ ] Valid backup identified
- [ ] Senior engineer approval
- [ ] Backup file verified
- [ ] Communication to stakeholders sent

#### Steps

**Step 1: List Available Backups**
```bash
ssh deploy@hrms.example.com "ls -lth /var/www/hrms-production/backups/"

# Example output:
# backup-20251117_143022.tar.gz  150M  Nov 17 14:30  (most recent)
# backup-20251117_120015.tar.gz  148M  Nov 17 12:00
# backup-20251116_203045.tar.gz  145M  Nov 16 20:30
```

**Step 2: Identify Target Backup**
```bash
# Select backup timestamp (format: YYYYMMDD_HHMMSS)
# Example: 20251117_120015
BACKUP_TIMESTAMP="20251117_120015"
```

**Step 3: Execute Backup Rollback**
```bash
./scripts/rollback.sh --to-backup $BACKUP_TIMESTAMP

# Type ROLLBACK when prompted
```

**Step 4: Verify Restoration**
```bash
# Check application version
curl https://hrms.example.com | grep -o 'main\.[a-z0-9]*\.js'

# Compare with known good version
# Run health checks
./scripts/health-check.sh --environment production
```

---

### Rollback Procedure C: Feature Flag Only

**When to use**: Specific feature causing issues, site otherwise stable
**Time**: 30 seconds
**Risk**: Very Low

#### Steps

**Step 1: Disable Feature Flag**
```bash
curl -X POST "$FEATURE_FLAG_API_URL/phase1-migration" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -d '{"rolloutPercentage": 0, "enabled": false}'

# Expected response:
# {"success": true, "rolloutPercentage": 0, "enabled": false}
```

**Step 2: Verify Impact**
```bash
# Wait 30 seconds for caches to clear
sleep 30

# Check error rate
# Navigate to DataDog dashboard
# Verify error rate returns to normal
```

**Step 3: Monitor for 5 Minutes**
```bash
# Watch metrics closely
# If issue persists → Full rollback (Procedure A)
# If issue resolved → Root cause analysis
```

---

### Rollback Procedure D: Manual Emergency Override

**When to use**: All automated procedures failed, manual intervention required
**Time**: 5-15 minutes
**Risk**: High (requires SSH and manual nginx manipulation)

#### Prerequisites

- [ ] **APPROVAL FROM CTO OR ENGINEERING DIRECTOR REQUIRED**
- [ ] Two engineers present (primary + backup)
- [ ] Screen sharing active
- [ ] All stakeholders notified

#### Steps

**Step 1: SSH to Production Server**
```bash
ssh deploy@hrms.example.com
```

**Step 2: Check Current State**
```bash
# Check current symlink
ls -la /var/www/hrms-production/current

# Example output:
# current -> /var/www/hrms-production/blue
```

**Step 3: Switch to Previous Environment**
```bash
# If current is blue, switch to green
sudo ln -sfn /var/www/hrms-production/green /var/www/hrms-production/current

# If current is green, switch to blue
sudo ln -sfn /var/www/hrms-production/blue /var/www/hrms-production/current
```

**Step 4: Reload Nginx**
```bash
# Test nginx configuration first
sudo nginx -t

# If test passes, reload
sudo systemctl reload nginx

# If reload fails, restart (brief downtime)
sudo systemctl restart nginx
```

**Step 5: Verify Switch**
```bash
# Check symlink
ls -la /var/www/hrms-production/current

# Test locally
curl http://localhost

# Test externally
exit  # Exit SSH
curl https://hrms.example.com
```

**Step 6: Reset Feature Flags Manually**
```bash
# Use Postman or curl
curl -X POST "https://api.hrms.example.com/api/feature-flags/phase1-migration" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -d '{"rolloutPercentage": 0, "enabled": false}'
```

---

## Post-Rollback Actions

### Immediate Actions (0-15 minutes)

1. **Verify System Stability**
   ```bash
   # Run health checks
   ./scripts/health-check.sh --environment production

   # Monitor metrics for 15 minutes
   # DataDog → Error rate should be < 0.1%
   # DataDog → Response time p95 < 200ms
   ```

2. **Update Stakeholders**
   ```
   Post in #production-incidents:
   "✅ Rollback completed successfully
   - Issue: [brief description]
   - Rollback method: [Blue/Green switch / Backup restore]
   - Current state: Stable on previous version
   - Impact duration: [X minutes]
   - Customers affected: [estimate]
   - Next steps: Root cause analysis in progress
   "
   ```

3. **Create Incident Ticket**
   ```
   Title: Production Rollback - [Date] - [Issue]
   Priority: High
   Assignee: On-call engineer
   Description:
   - What happened
   - When it was detected
   - Impact assessment
   - Rollback method used
   - Current status
   - Next steps
   ```

### Short-Term Actions (15 minutes - 2 hours)

4. **Root Cause Analysis (Preliminary)**
   - Review deployment logs
   - Check git diff between versions
   - Examine error logs
   - Interview witnesses (who detected issue)
   - Create timeline of events

5. **Customer Communication**
   ```
   If customer-facing impact:
   - Draft status page update
   - Notify customer success team
   - Prepare customer communication
   - Send if impact > 10 minutes
   ```

6. **Preserve Evidence**
   ```bash
   # Save logs
   ssh deploy@hrms.example.com "
     mkdir -p /var/log/incidents/rollback-$(date +%Y%m%d_%H%M%S)
     cp /var/log/nginx/error.log /var/log/incidents/rollback-$(date +%Y%m%d_%H%M%S)/
     cp /var/log/nginx/access.log /var/log/incidents/rollback-$(date +%Y%m%d_%H%M%S)/
   "

   # Save deployment artifacts
   cp logs/deploy-production-*.log /path/to/incident-archive/
   cp logs/rollback-*.log /path/to/incident-archive/
   ```

### Long-Term Actions (2-24 hours)

7. **Full Root Cause Analysis**
   - Complete incident report
   - Identify root cause
   - Propose fix
   - Create action items

8. **Preventive Measures**
   - Update tests to catch issue
   - Add monitoring for similar issues
   - Update deployment procedures
   - Schedule fix deployment

---

## Rollback Verification

### Automated Verification

```bash
# Run full health check suite
./scripts/health-check.sh --environment production --verbose

# Expected results:
# [✓ PASS] Homepage (HTTP 200)
# [✓ PASS] Subdomain login page (HTTP 200)
# [✓ PASS] Admin login page (HTTP 200)
# [✓ PASS] Main JavaScript bundle (HTTP 200)
# [✓ PASS] Response time is acceptable (<2s)
# [✓ PASS] SSL certificate valid
# [✓ PASS] API health endpoint responding
```

### Manual Verification

**Critical Path Testing** (5 minutes)

1. **Homepage Load**
   - Navigate to https://hrms.example.com
   - ✅ Loads within 2 seconds
   - ✅ No console errors
   - ✅ All assets load

2. **Authentication Flow**
   - Go to /auth/subdomain
   - ✅ Form displays correctly
   - ✅ Can type in fields
   - ✅ No JavaScript errors

3. **Admin Portal**
   - Go to /admin/login
   - ✅ Login form displays
   - ✅ Styling correct
   - ✅ No errors

4. **API Connectivity**
   ```bash
   curl https://api.hrms.example.com/api/health
   # Should return: {"status":"healthy"}
   ```

### Monitoring Verification

**Check These Dashboards** (monitor for 30 minutes)

1. **DataDog**
   - Error rate < 0.1%
   - Response time p95 < 200ms
   - No 5xx errors
   - No client-side errors

2. **Grafana**
   - Normal traffic patterns
   - No alert triggers
   - API response times normal

3. **Server Logs**
   ```bash
   # Should see normal access patterns, no errors
   ssh deploy@hrms.example.com "tail -f /var/log/nginx/access.log"
   ```

---

## Root Cause Analysis

### RCA Template

```markdown
# Incident Report: Production Rollback - [Date]

## Executive Summary
[1-2 sentences describing what happened]

## Impact
- **Duration**: [X minutes/hours]
- **Users Affected**: [number or percentage]
- **Services Affected**: HRMS Frontend Production
- **Data Loss**: [None / Describe if any]
- **Revenue Impact**: [None / Estimate]

## Timeline
- **[Time]**: Issue first detected
- **[Time]**: On-call engineer alerted
- **[Time]**: Rollback initiated
- **[Time]**: Rollback completed
- **[Time]**: System verified stable
- **[Time]**: Incident closed

## Root Cause
[Detailed explanation of what caused the issue]

## Resolution
[What was done to resolve the issue]

## Action Items
1. [Immediate fix] - Owner: [Name] - Due: [Date]
2. [Test improvement] - Owner: [Name] - Due: [Date]
3. [Process improvement] - Owner: [Name] - Due: [Date]

## Lessons Learned
1. What went well
2. What could be improved
3. What we learned
```

---

## Communication Templates

### Template: Rollback Initiated

```
SUBJECT: 🚨 Production Rollback in Progress - HRMS Frontend

Team,

We have initiated a production rollback for the HRMS Frontend application.

DETAILS:
- Issue: [Brief description]
- Detected at: [Time]
- Rollback initiated: [Time]
- ETA for completion: 2-5 minutes
- Impact: [Description of user impact]

STATUS: IN PROGRESS

We will update this thread as soon as the rollback is complete.

On-call engineer: [Name]
```

### Template: Rollback Complete

```
SUBJECT: ✅ Production Rollback Complete - HRMS Frontend

Team,

The production rollback has been completed successfully.

SUMMARY:
- Rollback completed: [Time]
- Total downtime/impact: [X minutes]
- Current status: Stable on previous version
- Users can now access the application normally

NEXT STEPS:
- Root cause analysis in progress
- Fix will be developed and tested
- New deployment planned for [Date/Time]

Incident ticket: [JIRA-123]

Thank you to everyone involved in the quick resolution.
```

### Template: Customer Communication

```
SUBJECT: Brief Service Disruption - Resolved

Dear Valued Customer,

We experienced a brief service disruption today from [Start Time] to [End Time] affecting the HRMS application.

WHAT HAPPENED:
We deployed an update that caused [brief issue description]. Our monitoring systems detected the issue immediately, and we rolled back to the previous stable version within [X] minutes.

RESOLUTION:
The service is now fully operational and running on the previous stable version. No data was lost, and all functionality has been restored.

WE'RE SORRY:
We sincerely apologize for any inconvenience this may have caused. We take service reliability very seriously and are conducting a thorough review to prevent similar issues in the future.

If you experienced any issues or have questions, please don't hesitate to contact our support team.

Thank you for your patience and understanding.

HRMS Support Team
```

---

## Appendix: Common Issues and Solutions

### Issue: Rollback Script Fails

**Error**: `rollback.sh: line 123: SSH connection failed`

**Solution**:
```bash
# Test SSH manually
ssh deploy@hrms.example.com "echo test"

# If fails, check:
# 1. SSH key permissions (should be 600)
chmod 600 ~/.ssh/id_rsa

# 2. SSH agent
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/id_rsa

# 3. Host connectivity
ping hrms.example.com
```

### Issue: Health Checks Still Failing After Rollback

**Solution**:
```bash
# 1. Check if symlink actually changed
ssh deploy@hrms.example.com "ls -la /var/www/hrms-production/current"

# 2. Check nginx status
ssh deploy@hrms.example.com "sudo systemctl status nginx"

# 3. Restart nginx (last resort)
ssh deploy@hrms.example.com "sudo systemctl restart nginx"

# 4. If still failing, escalate to Level 2 (DevOps Lead)
```

### Issue: Feature Flags Not Resetting

**Solution**:
```bash
# Manual feature flag reset
curl -X POST "$FEATURE_FLAG_API_URL/phase1-migration" \
  -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"rolloutPercentage": 0, "enabled": false}' \
  -v

# Check response - should be 200 OK
# If 401/403: Check API key
# If 500: Contact backend team immediately
```

---

**Document Version**: 1.0
**Last Updated**: November 17, 2025
**Next Review**: December 17, 2025
**Maintained By**: DevOps Team
**Emergency Contact**: PagerDuty → #production-incidents
