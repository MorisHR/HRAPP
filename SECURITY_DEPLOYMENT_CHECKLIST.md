# Security Deployment Checklist
## Fortune 500-Grade HRMS Application

**Document Version:** 1.0
**Last Updated:** 2025-11-17
**Compliance:** GDPR, SOC2, ISO27001, PCI-DSS (if applicable), HIPAA (if applicable)

---

## Table of Contents

1. [Pre-Deployment Security Checklist](#pre-deployment-security-checklist)
2. [Application Security](#application-security)
3. [Infrastructure Security](#infrastructure-security)
4. [Data Security](#data-security)
5. [Authentication & Authorization](#authentication--authorization)
6. [Secrets Management](#secrets-management)
7. [Network Security](#network-security)
8. [Monitoring & Logging](#monitoring--logging)
9. [Incident Response](#incident-response)
10. [Compliance Verification](#compliance-verification)
11. [Post-Deployment Verification](#post-deployment-verification)

---

## Pre-Deployment Security Checklist

### Code Review & Static Analysis

- [ ] **Run SAST (Static Application Security Testing)**
  ```bash
  # .NET Security Scanning
  dotnet tool install -g security-scan
  security-scan /workspaces/HRAPP/src/HRMS.API/HRMS.API.csproj
  ```

- [ ] **SonarQube/SonarCloud Security Analysis**
  - Security Hotspots: 0 Critical
  - Vulnerabilities: 0 High/Critical
  - Code Smells: < 5% debt ratio
  - Security Rating: A

- [ ] **OWASP Dependency Check**
  ```bash
  dotnet list package --vulnerable --include-transitive
  ```
  - No critical vulnerabilities in dependencies
  - All packages up to date (< 6 months old)

- [ ] **Frontend Security Scan (Angular)**
  ```bash
  cd hrms-frontend
  npm audit --audit-level=high
  npm outdated
  ```
  - 0 high/critical vulnerabilities
  - All security patches applied

### Source Code Security Audit

- [ ] **No hardcoded secrets in codebase**
  ```bash
  # Search for potential secrets
  grep -r "password\s*=\s*['\"]" --include="*.cs" --include="*.ts" --exclude-dir=bin
  grep -r "api[_-]key" --include="*.cs" --include="*.ts"
  grep -r "secret\s*=\s*['\"]" --include="*.cs" --include="*.ts"
  ```

- [ ] **All sensitive data encrypted**
  - Database: AES-256-GCM column-level encryption ✅
  - Transit: TLS 1.2+ ✅
  - Storage: Encrypted backups ✅

- [ ] **Input validation on all endpoints**
  - FluentValidation implemented ✅
  - SQL injection prevention (parameterized queries) ✅
  - XSS prevention (output encoding) ✅

- [ ] **No SQL injection vulnerabilities**
  - No raw SQL queries with string concatenation
  - All queries use parameterized statements or EF Core LINQ

- [ ] **CSRF protection enabled**
  - ASP.NET Core anti-forgery tokens configured
  - SameSite cookie attribute set

---

## Application Security

### Web Application Security

- [ ] **Security headers configured** (See SECURITY_HEADERS_CONFIG.md)
  - Content-Security-Policy ✅
  - X-Frame-Options: DENY ✅
  - X-Content-Type-Options: nosniff ✅
  - Strict-Transport-Security (HSTS) ✅
  - Referrer-Policy ✅
  - Permissions-Policy ✅

- [ ] **HTTPS enforcement**
  - HTTP → HTTPS redirect enabled
  - HSTS header with preload
  - No mixed content warnings
  - TLS 1.2+ only (TLS 1.0/1.1 disabled)
  - Strong cipher suites configured

- [ ] **CORS policy configured**
  - Whitelist specific origins (no *)
  - Credentials allowed only for trusted domains
  - No preflight caching issues

  ```csharp
  // Verify in appsettings.json
  "Cors": {
    "AllowedOrigins": [
      "https://morishr.com",
      "https://*.morishr.com"
    ]
  }
  ```

- [ ] **Rate limiting enabled**
  - General API: 100 req/min, 1000 req/hour
  - Auth endpoints: 5 req/15min (login), 20 req/hour (all auth)
  - Tenant creation: 10 req/hour
  - Auto-blacklist after 10 violations

  ```bash
  # Test rate limiting
  curl -I https://morishr.com/api/auth/login
  # Check for X-RateLimit headers
  ```

- [ ] **Request size limits configured**
  - Max request body: 10MB (appsettings.json)
  - File upload limits enforced
  - Multipart form data limits

### API Security

- [ ] **All endpoints require authentication** (except public health checks)
  - [Authorize] attribute on controllers
  - AllowAnonymous only on login, register, health checks

- [ ] **Role-based access control (RBAC) implemented**
  - SuperAdmin role for master data
  - TenantEmployee role for tenant-scoped data
  - Manager role for subordinate data
  - HR role for employee management

- [ ] **API versioning configured**
  - URL-based: /api/v1/
  - Deprecated versions documented
  - Sunset headers for old versions

- [ ] **API documentation secured**
  - Swagger/OpenAPI disabled in production OR
  - Swagger requires authentication
  - No sensitive data in examples

### Session Management

- [ ] **JWT configuration secure**
  - Secret key: 256-bit minimum (32+ characters)
  - Expiration: 60 minutes (access token)
  - Refresh token: 7 days with rotation
  - Algorithm: HS256 (symmetric) or RS256 (asymmetric)
  - No sensitive data in JWT payload

- [ ] **Refresh token rotation enabled**
  - Old token revoked on refresh
  - Single-use refresh tokens
  - Session timeout: 30 minutes inactivity

- [ ] **Token storage secure**
  - HttpOnly cookies for web (prevents XSS)
  - Secure flag on cookies
  - SameSite=Strict or Lax
  - No tokens in localStorage (XSS risk)

---

## Infrastructure Security

### Server Configuration

- [ ] **Operating system hardened**
  - Latest security patches applied
  - Unnecessary services disabled
  - Firewall configured (allow only 80, 443)
  - SSH key-based auth only (password auth disabled)

- [ ] **Web server secure (Kestrel/IIS)**
  - Server header removed
  - Directory listing disabled
  - Error pages do not reveal stack traces
  - Default pages removed

- [ ] **Database security**
  - Postgres user: minimal privileges (no superuser)
  - SSL/TLS enabled for DB connections
  - Connection pooling configured
  - Row-level security (RLS) enabled for multi-tenancy

### Cloud Security (Google Cloud Platform)

- [ ] **GCP IAM configured**
  - Service accounts with least privilege
  - Workload Identity enabled (no service account keys)
  - Cloud SQL IAM authentication
  - Secret Manager access restricted

- [ ] **Cloud SQL security**
  - No public IP (private IP only)
  - Cloud SQL Proxy for connections
  - Automated backups enabled (7-day retention)
  - Point-in-time recovery enabled
  - SSL/TLS enforced

- [ ] **VPC & Firewall**
  - Private VPC configured
  - Firewall rules: deny all, allow specific
  - Cloud NAT for outbound traffic
  - VPC Service Controls enabled

- [ ] **Load balancer security**
  - Cloud Armor WAF enabled
  - DDoS protection active
  - IP whitelisting for admin endpoints
  - SSL certificate: Let's Encrypt or managed cert

---

## Data Security

### Encryption

- [ ] **Data at rest encrypted**
  - Database: Column-level encryption (AES-256-GCM)
  - Disk encryption: Google Cloud default encryption
  - Backups: Encrypted with customer-managed keys

- [ ] **Data in transit encrypted**
  - TLS 1.2+ for all connections
  - Certificate valid and not self-signed
  - No certificate warnings in browser
  - Perfect Forward Secrecy (PFS) enabled

- [ ] **Encryption key management**
  - Keys stored in Google Secret Manager
  - Key rotation policy: 90 days
  - Separate keys for dev/staging/production
  - Key backup and disaster recovery plan

### Data Privacy

- [ ] **PII data identified and protected**
  - Social Security Numbers (SSN): Encrypted
  - Bank Account Numbers: Encrypted
  - Salary/Compensation: Encrypted
  - Health information: Encrypted (if applicable)
  - Email addresses: Hashed for search, encrypted for storage

- [ ] **GDPR compliance**
  - Data retention policy: 7 years (or as required by law)
  - Right to erasure (delete user data)
  - Data portability (export user data)
  - Consent management
  - Privacy policy published

- [ ] **Data minimization**
  - Only collect necessary data
  - No excessive logging of PII
  - Automatic deletion of old audit logs (30 days)

---

## Authentication & Authorization

### Password Security

- [ ] **Password policy enforced**
  - Minimum length: 12 characters
  - Complexity: uppercase, lowercase, digit, special char
  - Password history: last 5 passwords blocked
  - Password expiration: 90 days
  - No common passwords (blacklist check)

- [ ] **Account lockout policy**
  - Failed attempts: 5
  - Lockout duration: 15 minutes
  - Auto-unlock after timeout
  - Admin can manually unlock

- [ ] **Password reset secure**
  - Reset token: cryptographically secure (GUID)
  - Token expiration: 1 hour
  - Single-use tokens
  - No password hints
  - Email verification required

### Multi-Factor Authentication (MFA)

- [ ] **MFA available for SuperAdmins**
  - TOTP (Google Authenticator, Authy)
  - Backup codes (10 single-use codes)
  - Recovery email/SMS (optional)

- [ ] **MFA enrollment**
  - QR code generation secure
  - Secret key encrypted in database
  - Backup codes hashed before storage

### IP Whitelisting & Time-Based Access

- [ ] **IP whitelisting for SuperAdmins** (optional but recommended)
  - CIDR notation support
  - Audit log for blocked IP attempts
  - Emergency override process documented

- [ ] **Login hours enforcement** (optional)
  - Business hours only for sensitive roles
  - After-hours login alerts

---

## Secrets Management

### Environment Variables & Configuration

- [ ] **No secrets in appsettings.json** (Production)
  - appsettings.Production.json: all secrets blank or "SET_IN_SECRET_MANAGER"
  - Connection strings: from Secret Manager
  - JWT secret: from Secret Manager
  - Encryption keys: from Secret Manager
  - SMTP credentials: from Secret Manager

- [ ] **Google Secret Manager configured**
  - Secrets created: DB_CONNECTION_STRING, JWT_SECRET, ENCRYPTION_KEY_V1
  - Version tracking enabled
  - Access logs enabled
  - IAM permissions: least privilege

- [ ] **Secret rotation plan**
  - Database password: quarterly
  - JWT secret: annually
  - Encryption keys: annually (with re-encryption)
  - API keys: quarterly

### API Keys & Credentials

- [ ] **Third-party API keys secured**
  - SMTP credentials (SMTP2GO): in Secret Manager
  - Google Cloud API keys: in Secret Manager
  - Device API keys: in Secret Manager, rotated quarterly

- [ ] **Service account keys**
  - No service account JSON keys committed to Git
  - Workload Identity used where possible
  - Keys stored in Secret Manager if required

---

## Network Security

### Firewall & Network Segmentation

- [ ] **Firewall rules configured**
  - Ingress: Allow 80/443 from internet, deny all others
  - Egress: Allow specific destinations (SMTP, external APIs)
  - Internal: Database only accessible from app servers

- [ ] **DDoS protection**
  - Cloud Armor configured
  - Rate limiting at edge
  - Auto-scaling to handle traffic spikes

- [ ] **Web Application Firewall (WAF)**
  - OWASP Top 10 rules enabled
  - SQL injection prevention
  - XSS prevention
  - Bot detection

### DNS & Domain Security

- [ ] **DNSSEC enabled**
  - Domain registrar: DNSSEC records configured
  - DNS provider: DNSSEC validation enabled

- [ ] **CAA records configured**
  ```dns
  morishr.com. CAA 0 issue "letsencrypt.org"
  morishr.com. CAA 0 issuewild "letsencrypt.org"
  morishr.com. CAA 0 iodef "mailto:security@morishr.com"
  ```

- [ ] **SPF, DKIM, DMARC configured** (for email security)
  ```dns
  morishr.com. TXT "v=spf1 include:_spf.smtp2go.com ~all"
  _dmarc.morishr.com. TXT "v=DMARC1; p=quarantine; rua=mailto:dmarc@morishr.com"
  ```

---

## Monitoring & Logging

### Security Logging

- [ ] **Audit logging enabled**
  - All authentication events (login, logout, failed attempts)
  - All authorization failures
  - All data modifications (CRUD operations)
  - All admin actions (user creation, role changes)
  - All security events (account lockouts, password resets)

- [ ] **Log retention policy**
  - Security logs: 1 year (compliance requirement)
  - Audit logs: 7 years (SOX/GDPR requirement)
  - Application logs: 30 days
  - Access logs: 90 days

- [ ] **Log protection**
  - Logs immutable (append-only)
  - PII masked in logs
  - Logs encrypted at rest
  - Access to logs restricted (RBAC)

### Security Monitoring

- [ ] **Real-time security alerts configured**
  - Failed login threshold: 5 attempts in 15 minutes
  - Account lockout alerts
  - Mass data export alerts (>100 records)
  - After-hours access alerts
  - Impossible travel detection

- [ ] **Alert channels configured**
  - Email: security@morishr.com
  - Slack: #security-alerts (optional)
  - SMS: critical alerts only (optional)
  - SIEM integration: Splunk/ELK (enterprise)

- [ ] **Anomaly detection enabled**
  - Failed login patterns
  - Concurrent sessions from different IPs
  - Rapid-fire actions (>10 in 60 seconds)
  - Salary change >50%
  - Bulk employee deletion

### Application Monitoring

- [ ] **Health checks enabled**
  - /health endpoint (basic liveness)
  - /health/ready endpoint (readiness check)
  - Database connectivity check
  - Redis connectivity check
  - External API checks

- [ ] **Performance monitoring**
  - Application Insights / New Relic / Datadog
  - Response time tracking
  - Error rate monitoring
  - Resource utilization (CPU, memory, disk)

---

## Incident Response

### Incident Response Plan

- [ ] **Incident response team identified**
  - Security Lead: [Name, Contact]
  - DevOps Lead: [Name, Contact]
  - Legal Counsel: [Name, Contact]
  - Executive Sponsor: [Name, Contact]

- [ ] **Incident severity levels defined**
  - P0 (Critical): Data breach, system compromise
  - P1 (High): Service outage, auth bypass
  - P2 (Medium): Performance degradation, minor vulnerability
  - P3 (Low): Non-critical bugs, enhancement requests

- [ ] **Incident response runbooks**
  - Data breach response
  - DDoS attack response
  - Ransomware response
  - Account compromise response

- [ ] **Communication plan**
  - Internal: Slack, Email
  - External: Status page, customer notifications
  - Regulatory: GDPR breach notification (72 hours)

### Backup & Disaster Recovery

- [ ] **Database backups configured**
  - Frequency: Daily (full), Hourly (incremental)
  - Retention: 30 days
  - Offsite storage: Google Cloud Storage (different region)
  - Encryption: Customer-managed keys

- [ ] **Backup restoration tested**
  - Last test date: [Date]
  - RTO (Recovery Time Objective): 4 hours
  - RPO (Recovery Point Objective): 1 hour
  - Restore test successful: ✅

- [ ] **Disaster recovery plan**
  - Multi-region deployment (optional)
  - Failover process documented
  - Failover test: quarterly

---

## Compliance Verification

### GDPR Compliance

- [ ] **Data protection impact assessment (DPIA) completed**
- [ ] **Data processing agreement (DPA) with customers**
- [ ] **Privacy policy published and accessible**
- [ ] **Cookie consent banner (if applicable)**
- [ ] **Right to erasure implemented** (delete user data)
- [ ] **Right to portability implemented** (export user data)
- [ ] **Data breach notification process (72 hours)**
- [ ] **Data Protection Officer (DPO) appointed** (if required)

### SOC 2 Type II Compliance

- [ ] **Access controls documented**
  - User provisioning/de-provisioning process
  - Role-based access control (RBAC)
  - Privileged access management

- [ ] **Change management process**
  - All code changes reviewed
  - Deployment approvals required
  - Rollback plan for each deployment

- [ ] **Security monitoring and incident response**
  - 24/7 monitoring (or business hours)
  - Incident response plan tested
  - Security awareness training

- [ ] **Vendor management**
  - Third-party risk assessment
  - Vendor security questionnaires
  - Data processing agreements (DPAs)

### ISO 27001:2022 Compliance

- [ ] **Information security policy published**
- [ ] **Risk assessment completed**
  - Asset inventory
  - Threat modeling
  - Risk treatment plan

- [ ] **Security controls implemented**
  - A.5: Organizational controls
  - A.6: People controls
  - A.7: Physical controls
  - A.8: Technological controls

- [ ] **Internal audit scheduled**
  - Frequency: Quarterly
  - Scope: All critical systems
  - Findings remediated within 30 days

### PCI-DSS (If Processing Payments)

- [ ] **SAQ (Self-Assessment Questionnaire) completed**
- [ ] **Network segmentation** (isolate payment processing)
- [ ] **No storage of CVV/CVC codes**
- [ ] **PAN (Primary Account Number) encrypted**
- [ ] **Quarterly vulnerability scans** (Approved Scanning Vendor)
- [ ] **Annual penetration test**

---

## Post-Deployment Verification

### Security Testing (Day 1 After Deployment)

- [ ] **Security headers test**
  ```bash
  # Test with SecurityHeaders.com
  https://securityheaders.com/?q=https://morishr.com
  # Target score: A+
  ```

- [ ] **SSL/TLS test**
  ```bash
  # Test with SSL Labs
  https://www.ssllabs.com/ssltest/analyze.html?d=morishr.com
  # Target score: A+
  ```

- [ ] **OWASP ZAP active scan**
  ```bash
  docker run -t owasp/zap2docker-stable zap-baseline.py -t https://morishr.com
  # 0 high-risk alerts
  ```

- [ ] **Penetration testing** (if budget allows)
  - External penetration test
  - Internal penetration test
  - Social engineering test
  - Report reviewed and vulnerabilities remediated

### Functional Security Testing

- [ ] **Authentication flows tested**
  - Login with valid credentials ✅
  - Login with invalid credentials (account locked after 5 attempts) ✅
  - Password reset flow ✅
  - MFA enrollment and verification ✅
  - Token refresh ✅
  - Logout ✅

- [ ] **Authorization tested**
  - Role-based access control (RBAC) ✅
  - Tenant isolation (can't access other tenant data) ✅
  - Horizontal privilege escalation prevented ✅
  - Vertical privilege escalation prevented ✅

- [ ] **Rate limiting tested**
  ```bash
  # Simulate 100 requests in 1 minute
  for i in {1..100}; do curl https://morishr.com/api/auth/login; done
  # Should return 429 Too Many Requests after limit exceeded
  ```

- [ ] **Input validation tested**
  - SQL injection attempts blocked ✅
  - XSS payloads sanitized ✅
  - File upload validation (type, size, content) ✅
  - Malicious file upload blocked ✅

### Monitoring Verification

- [ ] **Alerts tested**
  - Trigger failed login alert (5 attempts) ✅
  - Trigger account lockout alert ✅
  - Trigger mass export alert (>100 records) ✅
  - Verify alert delivery (email, Slack, SMS) ✅

- [ ] **Logs verified**
  - Authentication logs present ✅
  - Audit logs present ✅
  - Security events logged ✅
  - PII masked in logs ✅

- [ ] **Health checks verified**
  ```bash
  curl https://morishr.com/health
  # Should return 200 OK with status: healthy
  ```

### Performance & Availability

- [ ] **Load testing completed**
  - Concurrent users: 1000+ (target)
  - Response time: <500ms (95th percentile)
  - Error rate: <0.1%
  - No memory leaks under sustained load

- [ ] **Availability target**
  - SLA: 99.9% uptime (8.77 hours downtime/year)
  - Monitoring: Uptime Robot, Pingdom
  - Escalation: PagerDuty for on-call

---

## Sign-Off & Approval

### Pre-Deployment Approval

**Security Team Lead:** _________________________ Date: _________

**DevOps Lead:** _________________________ Date: _________

**CTO/CISO:** _________________________ Date: _________

**CEO/Executive Sponsor:** _________________________ Date: _________

### Post-Deployment Verification

**Security Testing Completed:** ☐ Yes ☐ No
**All Critical Issues Resolved:** ☐ Yes ☐ No
**Monitoring Verified:** ☐ Yes ☐ No

**Deployment Approved for Production:** ☐ Yes ☐ No

**Signature:** _________________________ Date: _________

---

## Quarterly Security Review

**Next Review Date:** [3 months from deployment]

**Review Checklist:**
- [ ] Dependency updates applied
- [ ] Security patches applied
- [ ] Penetration test results reviewed
- [ ] Audit log review completed
- [ ] Incident response plan tested
- [ ] Backup restoration tested
- [ ] Compliance certifications renewed

---

## Emergency Contacts

**Security Incidents:** security@morishr.com
**System Outages:** devops@morishr.com
**Customer Support:** support@morishr.com
**Legal/Compliance:** legal@morishr.com

**On-Call Phone:** +1-XXX-XXX-XXXX (24/7)

---

## Additional Resources

- [OWASP Web Security Testing Guide](https://owasp.org/www-project-web-security-testing-guide/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
- [CIS Controls](https://www.cisecurity.org/controls/)
- [Microsoft Security Development Lifecycle](https://www.microsoft.com/en-us/securityengineering/sdl/)
- [Google Cloud Security Best Practices](https://cloud.google.com/security/best-practices)

---

**Document Classification:** CONFIDENTIAL - Internal Use Only
**Distribution:** Security Team, DevOps Team, Executive Leadership
**Retention Period:** 7 years (compliance requirement)

---

**END OF CHECKLIST**
