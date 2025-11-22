# HRMS Security Testing Suite

Complete penetration testing infrastructure for Fortune 500-grade security validation.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Test Types](#test-types)
- [Running Tests](#running-tests)
- [Understanding Results](#understanding-results)
- [Security Checklist](#security-checklist)
- [CI/CD Integration](#cicd-integration)

---

## Overview

This security testing suite provides comprehensive penetration testing capabilities:

- **Automated Scans:** OWASP ZAP baseline and full scans
- **Manual Testing:** Comprehensive security checklist (100+ tests)
- **Vulnerability Assessment:** SQL injection, XSS, auth bypass testing
- **Security Headers:** HTTPS, CORS, CSP validation
- **Authorization Testing:** RBAC and tenant isolation validation

### Architecture

```
tests/security/
├── zap/
│   ├── baseline-scan.sh           # Quick passive scan
│   └── full-scan.sh                # Comprehensive active scan
├── manual/
│   └── SECURITY_TEST_CHECKLIST.md  # Manual testing guide
├── results/                        # Test results (gitignored)
└── README.md                       # This file
```

---

## Prerequisites

### Required Tools

1. **Docker** - For OWASP ZAP
   ```bash
   # Verify Docker installation
   docker --version
   ```

2. **jq** - JSON processor (for result analysis)
   ```bash
   # macOS
   brew install jq

   # Linux
   sudo apt-get install jq
   ```

3. **curl** - For manual API testing
   ```bash
   curl --version
   ```

### Optional Tools

- **Burp Suite Community Edition** - For advanced manual testing
- **Postman** - For API security testing
- **SSLLabs** - For HTTPS/TLS validation

---

## Quick Start

### 1. Baseline Scan (10 minutes)

Quick passive scan to identify obvious security issues:

```bash
cd tests/security/zap

# Set target URL
export API_URL=https://api.yourdomain.com

# Run baseline scan
./baseline-scan.sh
```

**Expected Output:**
- HTML report in `results/zap-baseline-*.html`
- JSON report in `results/zap-baseline-*.json`
- Quick summary of findings

### 2. Review Results

```bash
# Open HTML report
open results/zap-baseline-*.html

# Or view JSON summary
cat results/zap-baseline-*.json | jq '.site[].alerts[] | {name, risk, description}'
```

### 3. Fix Issues & Re-test

After addressing security findings:

```bash
# Re-run baseline scan
./baseline-scan.sh

# Compare results
diff results/zap-baseline-OLD.json results/zap-baseline-NEW.json
```

---

## Test Types

### Automated Testing

#### OWASP ZAP Baseline Scan
- **Duration:** 5-10 minutes
- **Type:** Passive scanning
- **Coverage:** Common vulnerabilities
- **Script:** `zap/baseline-scan.sh`

**Tests:**
- Missing security headers
- Insecure cookies
- Information disclosure
- Basic misconfigurations

**When to Use:**
- Quick validation after deployment
- Pre-production checks
- Continuous security monitoring

#### OWASP ZAP Full Scan
- **Duration:** 30-60 minutes
- **Type:** Active scanning
- **Coverage:** Comprehensive (OWASP Top 10)
- **Script:** `zap/full-scan.sh`

**Tests:**
- SQL Injection
- Cross-Site Scripting (XSS)
- Security Misconfigurations
- Broken Authentication
- Sensitive Data Exposure
- XML External Entities (XXE)
- Broken Access Control
- CSRF
- Using Components with Known Vulnerabilities
- Insufficient Logging & Monitoring

**When to Use:**
- Before major releases
- Monthly security assessments
- After significant code changes
- Pre-production validation

---

### Manual Testing

#### Security Checklist
- **Duration:** 4-8 hours
- **Type:** Manual validation
- **Coverage:** 100+ security tests
- **Guide:** `manual/SECURITY_TEST_CHECKLIST.md`

**Test Categories:**
1. Authentication & Session Management (15 tests)
2. Authorization & Access Control (10 tests)
3. SQL Injection (6 tests)
4. Cross-Site Scripting (4 tests)
5. Security Headers (8 tests)
6. Data Validation (6 tests)
7. File Upload Security (4 tests)
8. API Rate Limiting (4 tests)
9. Information Disclosure (4 tests)
10. HTTPS & TLS (4 tests)
11. Business Logic (4 tests)
12. Tenant Isolation (2 tests)

**When to Use:**
- Final pre-production validation
- Security audits
- Compliance requirements
- After automated scans

---

## Running Tests

### Automated Scans

#### Baseline Scan

```bash
cd tests/security/zap

# Basic usage
./baseline-scan.sh

# With custom URL
API_URL=https://staging.api.com ./baseline-scan.sh
```

#### Full Scan

```bash
cd tests/security/zap

# IMPORTANT: Only run against authorized test environments
./full-scan.sh

# The script will ask for confirmation before proceeding
```

**⚠️ WARNING:** Full scan performs active attacks. Only run against authorized test environments!

### Manual Testing

```bash
cd tests/security/manual

# Open the checklist
open SECURITY_TEST_CHECKLIST.md

# Follow the checklist systematically
# Document all findings
```

**Tips for Manual Testing:**
1. Use a dedicated test environment
2. Create test users for each role (admin, tenant admin, employee)
3. Use tools like Burp Suite or Postman for requests
4. Document all findings with screenshots
5. Re-test after fixes

---

## Understanding Results

### OWASP ZAP Report Structure

```json
{
  "site": [{
    "alerts": [{
      "alert": "Security Header Missing",
      "risk": "Medium",
      "confidence": "High",
      "description": "...",
      "solution": "...",
      "instances": [...]
    }]
  }]
}
```

### Risk Levels

| Risk | Severity | Action Required |
|------|----------|-----------------|
| **High** | Critical | **MUST FIX** before production |
| **Medium** | Important | **SHOULD FIX** soon |
| **Low** | Minor | Fix when convenient |
| **Informational** | Info only | Review and acknowledge |

### Common Findings

#### 1. Missing Security Headers

**Finding:**
```
Alert: X-Frame-Options Header Not Set
Risk: Medium
```

**Solution:**
```csharp
// In Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});
```

#### 2. Weak TLS Configuration

**Finding:**
```
Alert: TLS 1.0 or 1.1 Enabled
Risk: High
```

**Solution:**
```csharp
// Enforce TLS 1.2+
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
    {
        https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});
```

#### 3. SQL Injection Vulnerability

**Finding:**
```
Alert: SQL Injection
Risk: High
Parameter: search
```

**Solution:**
```csharp
// NEVER do this:
var query = $"SELECT * FROM Employees WHERE Name = '{search}'";

// ALWAYS use parameterized queries:
var employees = await _context.Employees
    .Where(e => e.Name.Contains(search))
    .ToListAsync();
```

---

## Security Checklist

The manual security testing checklist covers:

### ✅ Authentication (15 tests)
- Password strength enforcement
- Brute force protection
- JWT token security
- Session management

### ✅ Authorization (10 tests)
- Vertical privilege escalation
- Horizontal privilege escalation
- Direct object references

### ✅ Input Validation (20 tests)
- SQL injection
- XSS (reflected & stored)
- Command injection
- Path traversal

### ✅ Security Configuration (15 tests)
- Security headers
- CORS policy
- TLS configuration
- Error handling

### ✅ Business Logic (10 tests)
- Attendance manipulation
- Leave request abuse
- Tenant isolation

---

## CI/CD Integration

### GitHub Actions

Security scans are integrated into the CI/CD pipeline:

**Automated Triggers:**
- Before production deployments
- Weekly scheduled scans
- After security-related code changes

**Manual Trigger:**
```bash
# From GitHub UI
Actions → Security Tests → Run workflow
```

### Results

Security scan results are:
- Uploaded as artifacts (retained 90 days)
- Summarized in GitHub Actions summary
- Posted to Slack on failures (HIGH risk findings)

### Deployment Gates

Deployment is blocked if:
- **HIGH** risk vulnerabilities found
- Security scan fails to run
- TLS configuration below A rating

---

## Best Practices

### Before Testing

1. **Authorization**
   - Obtain written authorization for penetration testing
   - Test only authorized environments
   - Document test scope and boundaries

2. **Environment**
   - Use isolated test environment
   - Back up database before active scans
   - Notify team of testing schedule

3. **Preparation**
   - Create test user accounts
   - Configure monitoring/logging
   - Have rollback plan ready

### During Testing

1. **Documentation**
   - Record all tests performed
   - Screenshot vulnerabilities found
   - Note reproduction steps

2. **Escalation**
   - Stop testing if critical issue found
   - Immediately report HIGH risk findings
   - Coordinate with development team

### After Testing

1. **Reporting**
   - Complete security checklist
   - Generate comprehensive report
   - Prioritize findings by risk

2. **Remediation**
   - Create tickets for each finding
   - Assign owners and due dates
   - Schedule re-test after fixes

3. **Verification**
   - Re-run scans after fixes
   - Verify all HIGH/MEDIUM issues resolved
   - Update security documentation

---

## Compliance

This testing suite helps validate compliance with:

- **OWASP Top 10** - Industry standard web security risks
- **GDPR** - Data protection and privacy
- **SOC 2** - Security and availability controls
- **ISO 27001** - Information security management
- **PCI DSS** - Payment card data security (if applicable)

---

## Troubleshooting

### ZAP Scan Fails to Start

**Issue:** Docker container won't start

**Solution:**
```bash
# Check Docker is running
docker ps

# Pull latest ZAP image
docker pull owasp/zap2docker-stable

# Check disk space
df -h
```

### No Results Generated

**Issue:** Scan completes but no reports

**Solution:**
```bash
# Check results directory permissions
chmod 755 tests/security/results

# Verify Docker volume mounting
docker run --rm -v "$(pwd)/results:/zap/wrk/" owasp/zap2docker-stable ls /zap/wrk/
```

### High Memory Usage

**Issue:** ZAP consumes excessive memory

**Solution:**
```bash
# Limit Docker memory
docker run --memory="4g" ... owasp/zap2docker-stable ...

# Or split scans into smaller batches
```

---

## Support

- **Documentation:** This README
- **OWASP ZAP Docs:** https://www.zaproxy.org/docs/
- **Security Issues:** Report to security@company.com
- **Questions:** Contact security team

---

## Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP ZAP User Guide](https://www.zaproxy.org/docs/)
- [OWASP Testing Guide](https://owasp.org/www-project-web-security-testing-guide/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [SSL Labs](https://www.ssllabs.com/ssltest/)

---

**Last Updated:** 2025-11-22
**Version:** 1.0.0
**Maintained By:** Security Team
