# Deployment Checklist - Fortune 500 Grade
## HRMS Application - Comprehensive Deployment Verification

**Version:** 1.0.0
**Last Updated:** 2025-11-17
**Owner:** DevOps Engineering Team

---

## Overview

This checklist ensures all critical aspects of deployment are verified before, during, and after deploying the HRMS application to production. Use this for every production deployment.

---

## Pre-Deployment Verification

### Code & Build Quality

- [ ] **All CI/CD pipeline checks passed**
  - Build stage: ✅ Green
  - Test stage: ✅ Green
  - Quality stage: ✅ Green
  - Coverage: >= 80%

- [ ] **Code review completed**
  - Minimum 2 approvals received
  - All conversations resolved
  - Security review completed (if applicable)
  - Performance review completed (if applicable)

- [ ] **Version tagging**
  - Git tag created: `v{major}.{minor}.{patch}`
  - Release notes prepared
  - CHANGELOG.md updated

- [ ] **Dependencies verified**
  - No critical/high security vulnerabilities
  - All dependencies up to date
  - License compliance verified
  - No deprecated packages

### Database Migration Verification

- [ ] **Migration scripts reviewed**
  - All migration files reviewed and approved
  - Rollback scripts created and tested
  - Idempotency verified
  - No breaking schema changes (or properly communicated)

- [ ] **Database backup completed**
  ```bash
  # Verify backup exists
  pg_dump -h $DB_HOST -U $DB_USER -d $DB_NAME \
    -f backup_$(date +%Y%m%d_%H%M%S).sql

  # Verify backup file size and integrity
  ls -lh backup_*.sql
  ```

- [ ] **Migration tested in staging**
  - Forward migration successful
  - Rollback migration successful
  - Data integrity verified
  - Performance impact measured

- [ ] **Migration time estimated**
  - Estimated downtime: _______ minutes
  - Maintenance window scheduled: _______
  - Stakeholders notified: ✅

### Environment Variable Verification

- [ ] **Configuration files reviewed**
  - `appsettings.Production.json` reviewed
  - Environment variables documented
  - Secrets rotated (if needed)
  - No hardcoded secrets

- [ ] **Required environment variables set**
  ```bash
  # Backend (.NET)
  - ConnectionStrings__DefaultConnection
  - JwtSettings__Secret
  - JwtSettings__Issuer
  - JwtSettings__Audience
  - Email__SmtpServer
  - Email__SmtpPort
  - Email__FromEmail
  - Email__ApiKey
  - Hangfire__DashboardPath
  - Redis__ConnectionString (if used)
  - Logging__LogLevel
  - ASPNETCORE_ENVIRONMENT=Production

  # Frontend (Angular)
  - NG_APP_API_URL
  - NG_APP_ENVIRONMENT=production
  - NG_APP_VERSION
  ```

- [ ] **Secrets management verified**
  - Secrets stored in Google Secret Manager / AWS Secrets Manager
  - Service accounts have correct permissions
  - Secrets versioning enabled
  - Audit logging enabled for secret access

### Security Configuration Verification

- [ ] **SSL/TLS certificates valid**
  ```bash
  # Check certificate expiry
  echo | openssl s_client -servername hrms.example.com \
    -connect hrms.example.com:443 2>/dev/null | \
    openssl x509 -noout -dates

  # Verify certificate chain
  echo | openssl s_client -servername hrms.example.com \
    -connect hrms.example.com:443 -showcerts
  ```
  - Certificate expires after: _______
  - Certificate issuer: _______
  - Wildcard/SAN covers all domains: ✅

- [ ] **Security headers configured**
  - Content-Security-Policy
  - X-Frame-Options
  - X-Content-Type-Options
  - Strict-Transport-Security
  - Referrer-Policy

- [ ] **CORS configuration verified**
  - Allowed origins restricted to production domains
  - No wildcard (*) in production
  - Credentials handling configured correctly

- [ ] **Rate limiting enabled**
  - Login endpoint: 5 requests per 15 minutes
  - API endpoints: 100 requests per minute
  - IP-based rate limiting active
  - Rate limit monitoring configured

- [ ] **Authentication & authorization**
  - JWT secret rotated
  - Token expiration configured (60 minutes)
  - Refresh token mechanism verified
  - MFA enabled for admin users
  - Account lockout configured (5 failed attempts)

### Performance Baseline Establishment

- [ ] **Current performance metrics captured**
  ```bash
  # Run performance baseline script
  ./scripts/capture-performance-baseline.sh

  # Verify metrics
  cat performance-baseline.json
  ```

  **Baseline Metrics:**
  - API P50 response time: _______ ms
  - API P95 response time: _______ ms
  - API P99 response time: _______ ms
  - Database query P95: _______ ms
  - Frontend FCP: _______ ms
  - Frontend TTI: _______ ms
  - Lighthouse score: _______

- [ ] **Performance budgets verified**
  - Bundle size within budget: ✅
  - Lighthouse performance score >= 85: ✅
  - API response time P95 < 500ms: ✅

- [ ] **Load testing completed**
  - Concurrent users tested: _______
  - Requests per second: _______
  - Error rate under load: _______%
  - Auto-scaling verified: ✅

### Monitoring Setup Verification

- [ ] **Application Performance Monitoring (APM)**
  - Google Cloud Monitoring / New Relic / Datadog configured
  - Custom dashboards created
  - Log aggregation working
  - Error tracking enabled (Sentry / Rollbar)

- [ ] **Infrastructure monitoring**
  - CPU usage monitoring: ✅
  - Memory usage monitoring: ✅
  - Disk usage monitoring: ✅
  - Network monitoring: ✅
  - Database monitoring: ✅

- [ ] **Business metrics monitoring**
  - User login tracking: ✅
  - API endpoint usage: ✅
  - Feature usage analytics: ✅
  - Error rates by endpoint: ✅

- [ ] **Alerting configured**
  ```yaml
  Critical Alerts (Immediate):
    - Application down: ✅
    - Error rate > 1%: ✅
    - Response time P95 > 2s: ✅
    - Database connection failures: ✅
    - Disk usage > 85%: ✅

  Warning Alerts (15 min):
    - Error rate > 0.5%: ✅
    - Response time P95 > 1s: ✅
    - Memory usage > 80%: ✅
    - CPU usage > 80%: ✅
  ```

- [ ] **Alert channels configured**
  - Slack: #production-alerts: ✅
  - Email: devops-team@company.com: ✅
  - PagerDuty (critical only): ✅
  - SMS (critical only): ✅

### Rollback Plan Verification

- [ ] **Rollback strategy documented**
  - Rollback SOP created and reviewed
  - Rollback decision tree documented
  - Rollback team identified
  - Rollback authorization process defined

- [ ] **Rollback scripts tested**
  ```bash
  # Verify rollback scripts exist
  ls -l deployment/kubernetes/rollback-*.sh

  # Test in staging
  ./deployment/kubernetes/rollback-deployment.sh staging
  ```

- [ ] **Database rollback plan**
  - Rollback migration scripts tested
  - Data rollback strategy defined
  - Backup restoration tested
  - Rollback time estimated: _______ minutes

- [ ] **Blue/Green deployment ready**
  - Blue environment running current version: ✅
  - Green environment ready for new version: ✅
  - Load balancer configuration tested: ✅
  - Traffic switching tested: ✅

### Communication Plan

- [ ] **Stakeholders notified**
  - Deployment schedule communicated
  - Maintenance window announced (if needed)
  - Impact assessment shared
  - Contact information distributed

- [ ] **Communication channels ready**
  - Status page updated: https://status.hrms.example.com
  - Slack #deployment-updates channel: ✅
  - Email distribution list: ✅
  - Incident response team on standby: ✅

- [ ] **Deployment timeline communicated**
  | Time | Activity | Owner | Status |
  |------|----------|-------|--------|
  | T-24h | Final QA in staging | QA Team | ⏳ |
  | T-4h | Database backup | DBA | ⏳ |
  | T-1h | Freeze code changes | Tech Lead | ⏳ |
  | T-0 | Begin deployment | DevOps | ⏳ |
  | T+30m | Smoke tests | QA Team | ⏳ |
  | T+1h | Monitor & verify | DevOps | ⏳ |
  | T+2h | All-clear signal | Tech Lead | ⏳ |

---

## During Deployment

### Deployment Execution

- [ ] **Maintenance mode enabled (if needed)**
  ```bash
  # Enable maintenance page
  kubectl apply -f deployment/kubernetes/maintenance-page.yaml

  # Verify maintenance page is displayed
  curl https://hrms.example.com
  ```

- [ ] **Database migrations executed**
  ```bash
  # Run migrations
  kubectl exec -n production deployment/hrms-api -- \
    dotnet ef database update --no-build

  # Verify migration status
  kubectl exec -n production deployment/hrms-api -- \
    dotnet ef migrations list
  ```
  - Migration started: _______ (time)
  - Migration completed: _______ (time)
  - Duration: _______ minutes

- [ ] **Application deployed**
  ```bash
  # Deploy to green environment
  kubectl apply -f deployment/kubernetes/deployment-green.yaml

  # Wait for rollout
  kubectl rollout status deployment/hrms-frontend-green -n production
  kubectl rollout status deployment/hrms-api-green -n production
  ```
  - Deployment started: _______ (time)
  - Deployment completed: _______ (time)
  - Duration: _______ minutes

- [ ] **Health checks passing**
  ```bash
  # Check API health
  curl https://hrms.example.com/health
  # Expected: {"status":"Healthy"}

  # Check database connectivity
  curl https://hrms.example.com/health/ready
  # Expected: {"status":"Ready"}
  ```

### Smoke Tests Execution

- [ ] **API smoke tests**
  ```bash
  ./scripts/smoke-tests-production.sh
  ```
  - [ ] Authentication endpoints working
  - [ ] Employee CRUD operations working
  - [ ] Leave management working
  - [ ] Payroll calculations working
  - [ ] Reports generation working
  - [ ] Biometric sync working
  - [ ] Background jobs running

- [ ] **Frontend smoke tests**
  - [ ] Login page loads
  - [ ] Dashboard loads
  - [ ] Employee list loads
  - [ ] Leave application works
  - [ ] Reports display correctly
  - [ ] No console errors
  - [ ] No 404 errors for static assets

- [ ] **Integration smoke tests**
  - [ ] Email sending works
  - [ ] SignalR real-time updates work
  - [ ] File uploads work
  - [ ] External API integrations work
  - [ ] Biometric device communication works

### Traffic Switching

- [ ] **Gradual traffic migration**
  ```bash
  # Switch 10% traffic to green
  kubectl patch service hrms-frontend-live -p \
    '{"spec":{"selector":{"weight":"10"}}}'

  # Monitor for 5 minutes
  ./scripts/monitor-error-rate.sh

  # If stable, switch 50% traffic
  kubectl patch service hrms-frontend-live -p \
    '{"spec":{"selector":{"weight":"50"}}}'

  # Monitor for 5 minutes
  ./scripts/monitor-error-rate.sh

  # If stable, switch 100% traffic
  kubectl patch service hrms-frontend-live -p \
    '{"spec":{"selector":{"color":"green"}}}'
  ```

  - 10% traffic switched: _______ (time)
  - Error rate at 10%: _______%
  - 50% traffic switched: _______ (time)
  - Error rate at 50%: _______%
  - 100% traffic switched: _______ (time)

- [ ] **Monitoring during traffic switch**
  - Error rate < 0.1%: ✅
  - Response time P95 < 500ms: ✅
  - No 500 errors: ✅
  - Database queries normal: ✅
  - Memory usage normal: ✅
  - CPU usage normal: ✅

---

## Post-Deployment Verification

### Functional Verification

- [ ] **Critical user journeys tested**
  - [ ] User registration and login
  - [ ] Employee profile management
  - [ ] Leave application and approval
  - [ ] Timesheet submission
  - [ ] Payroll viewing
  - [ ] Report generation
  - [ ] Admin panel access

- [ ] **Data integrity verified**
  ```sql
  -- Verify record counts match
  SELECT COUNT(*) FROM employees;
  SELECT COUNT(*) FROM leave_applications;
  SELECT COUNT(*) FROM payslips;

  -- Verify no data corruption
  SELECT * FROM employees WHERE email IS NULL OR email = '';
  SELECT * FROM leave_applications WHERE status NOT IN ('Pending', 'Approved', 'Rejected');
  ```

- [ ] **Permissions verified**
  - SuperAdmin can access all features: ✅
  - Tenant Admin can manage tenant: ✅
  - HR can manage employees: ✅
  - Manager can approve leaves: ✅
  - Employee can view own data only: ✅

### Performance Verification

- [ ] **Response times within SLA**
  ```bash
  # Run performance check
  ./scripts/check-performance-production.sh

  # Compare with baseline
  diff performance-baseline.json performance-current.json
  ```

  | Metric | Baseline | Current | Status |
  |--------|----------|---------|--------|
  | API P50 | ___ms | ___ms | ✅/❌ |
  | API P95 | ___ms | ___ms | ✅/❌ |
  | API P99 | ___ms | ___ms | ✅/❌ |
  | DB Query P95 | ___ms | ___ms | ✅/❌ |
  | FCP | ___ms | ___ms | ✅/❌ |
  | TTI | ___ms | ___ms | ✅/❌ |

- [ ] **Load capacity verified**
  - Handles expected concurrent users: ✅
  - Auto-scaling triggers work: ✅
  - Resource limits not exceeded: ✅

### Security Verification

- [ ] **Security headers present**
  ```bash
  curl -I https://hrms.example.com

  # Verify headers:
  # Content-Security-Policy: ✅
  # X-Frame-Options: DENY ✅
  # Strict-Transport-Security: max-age=31536000 ✅
  ```

- [ ] **SSL/TLS working correctly**
  - Certificate valid: ✅
  - HTTPS enforced: ✅
  - No mixed content warnings: ✅
  - SSL Labs rating: A+ ✅

- [ ] **Authentication working**
  - Login with valid credentials works: ✅
  - Login with invalid credentials fails: ✅
  - Account lockout works after 5 attempts: ✅
  - MFA works for admin users: ✅
  - Token refresh works: ✅

- [ ] **Rate limiting active**
  ```bash
  # Test rate limiting
  for i in {1..10}; do
    curl -X POST https://hrms.example.com/api/auth/login \
      -H "Content-Type: application/json" \
      -d '{"email":"test@test.com","password":"wrong"}'
  done
  # Should get 429 after 5 attempts
  ```

### Monitoring Verification

- [ ] **Logs being collected**
  ```bash
  # Verify logs in Cloud Logging / ELK
  gcloud logging read "resource.type=k8s_container" --limit 50

  # Verify log levels
  # INFO: ✅
  # WARN: ✅
  # ERROR: ✅
  ```

- [ ] **Metrics being collected**
  - Application metrics visible: ✅
  - Infrastructure metrics visible: ✅
  - Custom business metrics visible: ✅
  - No gaps in metric collection: ✅

- [ ] **Dashboards updated**
  - Production dashboard shows new deployment: ✅
  - Deployment marked in graphs: ✅
  - All panels loading: ✅

- [ ] **Alerts functioning**
  ```bash
  # Test alert by triggering condition
  # (Do not do this in production without planning!)

  # Verify alert notifications received:
  # - Slack: ✅
  # - Email: ✅
  # - PagerDuty: ✅
  ```

### Business Verification

- [ ] **Feature flags verified**
  - New features toggled correctly: ✅
  - Gradual rollout configured (if applicable): ✅
  - Kill switch available for new features: ✅

- [ ] **A/B tests configured (if applicable)**
  - Experiment tracking working: ✅
  - Control/variant split correct: ✅
  - Metrics collection working: ✅

- [ ] **Analytics tracking**
  - Page views tracked: ✅
  - User actions tracked: ✅
  - Custom events tracked: ✅
  - No tracking errors: ✅

### Documentation Verification

- [ ] **Runbook updated**
  - New version documented: ✅
  - Known issues documented: ✅
  - Troubleshooting steps updated: ✅
  - Contact information current: ✅

- [ ] **CHANGELOG updated**
  - Release version tagged: ✅
  - Changes documented: ✅
  - Breaking changes highlighted: ✅
  - Migration notes included: ✅

- [ ] **API documentation updated**
  - Swagger/OpenAPI updated: ✅
  - New endpoints documented: ✅
  - Deprecated endpoints marked: ✅
  - Examples updated: ✅

### Communication

- [ ] **Deployment status communicated**
  - [ ] Slack #deployment-updates: Deployment complete ✅
  - [ ] Email to stakeholders: Success notification sent ✅
  - [ ] Status page updated: Resolved ✅
  - [ ] JIRA ticket updated: Closed ✅

- [ ] **Post-deployment report created**
  ```markdown
  # Deployment Report - v{version}

  **Date:** _______
  **Duration:** _______ minutes
  **Downtime:** _______ minutes (if any)
  **Deployed By:** _______

  ## Summary
  - Features deployed: _______
  - Bug fixes: _______
  - Database migrations: Yes/No
  - Rollback required: Yes/No

  ## Metrics
  - Error rate: _______%
  - Performance: ✅/❌
  - All checks passed: ✅/❌

  ## Issues Encountered
  - None / List issues

  ## Follow-up Actions
  - [ ] Action item 1
  - [ ] Action item 2
  ```

---

## Rollback Decision Tree

### When to Rollback

**IMMEDIATE ROLLBACK if:**
- Error rate > 5%
- Complete service outage
- Data corruption detected
- Security breach detected
- Database migration failed

**ROLLBACK WITHIN 15 MINUTES if:**
- Error rate > 1%
- Performance degradation > 2x baseline
- Critical feature broken
- Widespread user complaints

**MONITOR CLOSELY if:**
- Error rate 0.1-1%
- Minor performance degradation
- Non-critical feature issues
- Isolated user reports

### Rollback Procedure

```bash
# STEP 1: Notify team
echo "ROLLBACK INITIATED" | slack #deployment-updates

# STEP 2: Switch traffic back to blue
kubectl patch service hrms-frontend-live -p \
  '{"spec":{"selector":{"color":"blue"}}}'
kubectl patch service hrms-api-live -p \
  '{"spec":{"selector":{"color":"blue"}}}'

# STEP 3: Verify rollback
curl https://hrms.example.com/health

# STEP 4: Rollback database (if needed)
kubectl exec -n production deployment/hrms-api-blue -- \
  dotnet ef database update PreviousMigrationName

# STEP 5: Monitor and verify
./scripts/monitor-error-rate.sh

# STEP 6: Communicate rollback
echo "ROLLBACK COMPLETE" | slack #deployment-updates
```

---

## Post-Deployment Monitoring Period

### First 15 Minutes (Critical)
- [ ] Monitor error rates every minute
- [ ] Check performance metrics every 2 minutes
- [ ] Review logs for errors
- [ ] Team on high alert

### First Hour (High Alert)
- [ ] Monitor error rates every 5 minutes
- [ ] Check performance metrics every 5 minutes
- [ ] Review user feedback
- [ ] Team available

### First 24 Hours (Monitoring)
- [ ] Monitor error rates every 15 minutes
- [ ] Check performance metrics hourly
- [ ] Review daily metrics summary
- [ ] On-call team designated

### First Week (Observation)
- [ ] Daily metrics review
- [ ] User feedback analysis
- [ ] Performance trend analysis
- [ ] Plan improvements

---

## Sign-Off

### Deployment Team

| Role | Name | Sign-Off | Date/Time |
|------|------|----------|-----------|
| DevOps Engineer | _______ | _______ | _______ |
| Tech Lead | _______ | _______ | _______ |
| QA Lead | _______ | _______ | _______ |
| DBA | _______ | _______ | _______ |
| Product Manager | _______ | _______ | _______ |

### Verification

- [ ] All pre-deployment checks completed
- [ ] Deployment executed successfully
- [ ] All post-deployment checks passed
- [ ] Monitoring confirmed working
- [ ] Documentation updated
- [ ] Stakeholders notified

### Final Approval

**Deployment approved for completion:**

Signature: _______________________
Name: _______________________
Role: _______________________
Date: _______________________

---

## Appendix

### Useful Commands

```bash
# Check deployment status
kubectl get deployments -n production

# View pod status
kubectl get pods -n production

# View logs
kubectl logs -f deployment/hrms-api -n production

# Describe pod
kubectl describe pod <pod-name> -n production

# Execute command in pod
kubectl exec -it <pod-name> -n production -- bash

# Port forward for debugging
kubectl port-forward deployment/hrms-api 5000:5000 -n production

# View resource usage
kubectl top pods -n production
kubectl top nodes
```

### Emergency Contacts

| Role | Name | Phone | Email |
|------|------|-------|-------|
| On-Call DevOps | _______ | _______ | _______ |
| Tech Lead | _______ | _______ | _______ |
| DBA | _______ | _______ | _______ |
| Product Manager | _______ | _______ | _______ |
| CTO | _______ | _______ | _______ |

---

**Document Version:** 1.0.0
**Last Updated:** 2025-11-17
**Next Review:** 2026-01-17
