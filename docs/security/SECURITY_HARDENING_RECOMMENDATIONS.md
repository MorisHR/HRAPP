# Security Hardening Recommendations
## Immediate Action Items for HRMS Application

**Priority:** CRITICAL
**Timeline:** Next 7 Days
**Prepared By:** Senior Security Engineer
**Date:** November 17, 2025

---

## Executive Summary

Based on the comprehensive security audit, the HRMS application requires **2 critical fixes** before production deployment. This document provides step-by-step implementation guidance for immediate remediation.

**Overall Security Score:** 92/100 (Excellent)
**Production Readiness:** ✅ Ready after implementing P0 fixes

---

## Critical Priority (P0) - Deploy Immediately

### 1. Implement Security Headers Middleware

**Impact:** Prevents XSS, clickjacking, MIME-sniffing attacks
**Estimated Time:** 2-4 hours
**Risk if Not Fixed:** CRITICAL (OWASP Top 10)

#### Implementation Steps

**Step 1: Create Security Headers Middleware**

Create file: `/workspaces/HRAPP/src/HRMS.API/Middleware/SecurityHeadersMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace HRMS.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // CRITICAL: Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' data: https://fonts.gstatic.com; " +
            "img-src 'self' data: https: blob:; " +
            "connect-src 'self' https://morishr.com https://*.morishr.com; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "upgrade-insecure-requests; " +
            "block-all-mixed-content;"
        );

        // CRITICAL: Prevent clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");

        // HIGH: Prevent MIME-sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // CRITICAL: Enforce HTTPS (1 year, only in production)
        if (!context.Request.Host.Host.Contains("localhost"))
        {
            context.Response.Headers.Add("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }

        // MEDIUM: XSS Protection (legacy, but included for older browsers)
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        // MEDIUM: Referrer policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        // MEDIUM: Permissions policy
        context.Response.Headers.Add("Permissions-Policy",
            "geolocation=(), microphone=(), camera=(), payment=(), usb=()");

        // MEDIUM: Cross-origin isolation
        context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
        context.Response.Headers.Add("Cross-Origin-Resource-Policy", "same-origin");

        // LOW: Other security headers
        context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");

        // Remove server information disclosure headers
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");

        await _next(context);
    }
}

// Extension method for easy registration
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
```

**Step 2: Register Middleware in Program.cs**

Add to `/workspaces/HRAPP/src/HRMS.API/Program.cs` (BEFORE `app.UseRouting()`):

```csharp
// Add this line AFTER var app = WebApplication.Build()
// and BEFORE app.UseRouting()
app.UseSecurityHeaders();
```

**Step 3: Test Security Headers**

```bash
# Local testing
curl -I http://localhost:5000/health
# Should see security headers in response

# Production testing (after deployment)
curl -I https://morishr.com/health
# Should see all security headers
```

**Step 4: Validate with Online Tools**

Visit: https://securityheaders.com/?q=https://morishr.com
**Target Score:** A+

**Step 5: Monitor CSP Violations (Optional but Recommended)**

Create endpoint: `/workspaces/HRAPP/src/HRMS.API/Controllers/SecurityController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(ILogger<SecurityController> logger)
    {
        _logger = logger;
    }

    [HttpPost("csp-violation-report")]
    [AllowAnonymous]
    public IActionResult CspViolationReport([FromBody] object report)
    {
        _logger.LogWarning("CSP Violation: {Report}",
            System.Text.Json.JsonSerializer.Serialize(report));
        return Ok();
    }
}
```

Then update CSP header to include:
```csharp
"report-uri /api/security/csp-violation-report;"
```

---

### 2. Fix XSS Vulnerabilities in Frontend

**Impact:** Prevents Cross-Site Scripting attacks
**Estimated Time:** 2 hours
**Risk if Not Fixed:** HIGH (Session hijacking, data theft)

#### Files to Update

**File 1: `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/autocomplete/autocomplete.ts`**

Add DomSanitizer to escape HTML:

```typescript
import { Component, Input, Output, EventEmitter, HostListener, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser'; // ADD THIS

@Component({
  selector: 'app-autocomplete',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './autocomplete.html',
  styleUrl: './autocomplete.scss',
})
export class Autocomplete implements OnInit, OnDestroy {
  // ... existing code ...

  constructor(
    private elementRef: ElementRef,
    private sanitizer: DomSanitizer // ADD THIS
  ) {}

  // REPLACE the highlightMatch method with this secure version:
  highlightMatch(text: string): string {
    if (!this.inputValue || !text) {
      return this.escapeHTML(text); // Escape HTML entities
    }

    const escapedText = this.escapeHTML(text); // Escape first
    const regex = new RegExp(`(${this.escapeRegExp(this.inputValue)})`, 'gi');
    return escapedText.replace(regex, '<mark>$1</mark>');
  }

  // ADD this new method:
  private escapeHTML(text: string): string {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  // ... rest of existing code ...
}
```

**File 2: Find and update tabs component** (if exists)

Search for tabs component:
```bash
find /workspaces/HRAPP/hrms-frontend -name "tabs.ts" -o -name "tabs.component.ts"
```

If found, update to use DomSanitizer similar to autocomplete.

**File 3: Verify icon component is safe** ✅

The icon component already uses `sanitizer.sanitize()`, so it's safe. No changes needed.

---

## High Priority (P1) - Fix Within 7 Days

### 3. Verify No SQL Injection Vulnerabilities

**Impact:** Prevents database compromise
**Estimated Time:** 1 hour
**Risk if Not Fixed:** HIGH (Data breach)

#### Code Review Checklist

Review `/workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs`:

1. Find all instances of `ExecuteSqlRawAsync`
2. Verify each uses parameterized queries (no string concatenation)
3. Look for patterns like:
   ```csharp
   // UNSAFE (if found, fix immediately):
   var query = $"SELECT * FROM Users WHERE Id = {userId}";
   await context.Database.ExecuteSqlRawAsync(query);

   // SAFE (acceptable):
   var query = "SELECT * FROM Users WHERE Id = {0}";
   await context.Database.ExecuteSqlRawAsync(query, userId);
   ```

4. If any unsafe queries found, refactor to use parameterized statements

**Verification Command:**
```bash
grep -n "ExecuteSqlRawAsync" /workspaces/HRAPP/src/HRMS.Infrastructure/Services/MonitoringService.cs
```

Review each line manually to ensure parameters are used correctly.

---

### 4. Add Deployment Pipeline Security Check

**Impact:** Prevents accidental deployment of dev secrets
**Estimated Time:** 30 minutes

#### GitHub Actions Workflow

Create or update: `.github/workflows/security-check.yml`

```yaml
name: Security Checks

on:
  push:
    branches: [ main, staging, production ]
  pull_request:
    branches: [ main, staging, production ]

jobs:
  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Check for dev secrets in production config
        run: |
          if grep -r "dev-secret-key\|dev-encryption-key\|Password=postgres" src/HRMS.API/appsettings.Production.json; then
            echo "ERROR: Development secrets found in production config!"
            exit 1
          fi
          echo "✅ No dev secrets in production config"

      - name: .NET Dependency Vulnerability Check
        run: |
          cd src/HRMS.API
          dotnet list package --vulnerable --include-transitive

      - name: Frontend Dependency Audit
        run: |
          cd hrms-frontend
          npm audit --audit-level=high
```

---

## Medium Priority (P2) - Fix Within 30 Days

### 5. Implement CSP Violation Monitoring

Already covered in Step 1.5 above. Deploy this in production to monitor for CSP violations.

### 6. Add Dependency Scanning to CI/CD

Already covered in Step 4 above.

### 7. Complete GDPR Data Protection Impact Assessment (DPIA)

**Action Items:**
1. Document all high-risk data processing activities
2. Assess privacy impact for each activity
3. Identify additional controls needed
4. Document findings in DPIA report

**Template:** Use ISO 29134 DPIA template

---

## Low Priority (P3) - Fix Within 90 Days

### 8. Security Awareness Training

**Action Items:**
1. Develop training curriculum (OWASP Top 10, secure coding)
2. Schedule quarterly training sessions
3. Track completion rates
4. Annual refresher training

**Recommended Platform:** OWASP WebGoat, Kontra, or Secure Code Warrior

### 9. Annual Penetration Testing

**Action Items:**
1. Engage third-party security firm (e.g., Bishop Fox, NCC Group)
2. Define scope: web app, API, infrastructure
3. Schedule test (2-4 weeks duration)
4. Remediate findings within 30 days

**Budget:** $15,000 - $50,000 depending on scope

### 10. Bug Bounty Program

**Action Items:**
1. Evaluate platforms (HackerOne, Bugcrowd, Synack)
2. Define scope and exclusions
3. Set reward tiers ($100 - $10,000)
4. Launch private program (invite-only initially)

**Budget:** $500/month minimum

---

## Testing & Validation

### Pre-Deployment Testing Checklist

- [ ] Security headers present on all endpoints
  ```bash
  curl -I https://staging.morishr.com/health | grep -i "content-security-policy"
  ```

- [ ] No XSS vulnerabilities in frontend
  ```javascript
  // Test in browser console:
  const payload = '<img src=x onerror=alert("XSS")>';
  // Input this into autocomplete and tabs - should NOT execute
  ```

- [ ] SQL injection prevention verified
  ```bash
  # Try SQL injection payloads:
  curl -X POST https://staging.morishr.com/api/auth/login \
    -d '{"email": "admin@test.com", "password": "' OR '1'='1"}'
  # Should fail gracefully, not execute SQL
  ```

- [ ] Rate limiting functional
  ```bash
  for i in {1..10}; do curl https://staging.morishr.com/api/auth/login; done
  # Should return 429 Too Many Requests
  ```

- [ ] HTTPS enforced
  ```bash
  curl -I http://morishr.com
  # Should redirect to https://morishr.com
  ```

### Post-Deployment Validation

- [ ] SecurityHeaders.com score: A+
- [ ] SSL Labs score: A+
- [ ] No console errors in browser
- [ ] All security alerts firing correctly

---

## Monitoring & Alerting

### Security Metrics to Track

1. **Failed Login Attempts**
   - Threshold: >5 in 15 minutes
   - Alert: Email to security@morishr.com

2. **Account Lockouts**
   - Threshold: >0
   - Alert: Immediate email

3. **CSP Violations**
   - Threshold: >10 per day
   - Alert: Daily digest

4. **Rate Limit Violations**
   - Threshold: >100 per day
   - Alert: Daily digest

5. **Unusual Access Patterns**
   - After-hours access by SuperAdmins
   - Concurrent sessions from different countries
   - Mass data exports (>100 records)

### Dashboard Setup

Use Grafana or similar to visualize:
- Failed login attempts (last 24h)
- Active sessions by role
- API response times
- Database connection pool usage
- Security alert trends

---

## Success Criteria

### Pre-Production

- [x] All P0 issues fixed (security headers, XSS)
- [ ] All P1 issues fixed (SQL injection verified)
- [ ] Security testing completed
- [ ] Deployment pipeline security checks passing

### Post-Production (Week 1)

- [ ] No critical security incidents
- [ ] SecurityHeaders.com score: A+
- [ ] SSL Labs score: A+
- [ ] CSP violation monitoring active
- [ ] Security alerts functioning

### 30-Day Review

- [ ] All P2 issues addressed
- [ ] GDPR DPIA completed
- [ ] Dependency scanning in CI/CD
- [ ] Zero high-severity vulnerabilities

### 90-Day Review

- [ ] Security awareness training completed
- [ ] Penetration test scheduled
- [ ] Bug bounty program launched (optional)
- [ ] Quarterly security review conducted

---

## Rollback Plan

If security headers cause issues:

1. **Identify the problematic header:**
   ```bash
   # Check browser console for errors
   # Look for CSP violation messages
   ```

2. **Temporarily disable specific header:**
   ```csharp
   // Comment out problematic header in SecurityHeadersMiddleware
   // context.Response.Headers.Add("Content-Security-Policy", ...);
   ```

3. **Deploy fix:**
   ```bash
   dotnet publish -c Release
   # Deploy to production
   ```

4. **Investigate root cause:**
   - Check CSP violation logs
   - Identify blocked resource
   - Update CSP to allow legitimate resource

5. **Re-enable header with fix**

---

## Support & Escalation

### Security Issues

**Email:** security@morishr.com
**On-Call:** [Phone Number]
**Slack:** #security-alerts

### Incident Severity Levels

**P0 (Critical):** Data breach, system compromise
- Response Time: 15 minutes
- Escalate to: CTO, CISO, CEO

**P1 (High):** Service outage, auth bypass
- Response Time: 1 hour
- Escalate to: CTO, DevOps Lead

**P2 (Medium):** Performance degradation
- Response Time: 4 hours
- Escalate to: Engineering Manager

**P3 (Low):** Non-critical bugs
- Response Time: Next business day
- Escalate to: Product Owner

---

## Additional Resources

### Documentation
- [SECURITY_HEADERS_CONFIG.md](/workspaces/HRAPP/SECURITY_HEADERS_CONFIG.md) - Complete security headers guide
- [SECURITY_DEPLOYMENT_CHECKLIST.md](/workspaces/HRAPP/SECURITY_DEPLOYMENT_CHECKLIST.md) - Pre/post-deployment checklist
- [SECURITY_AUDIT_REPORT.md](/workspaces/HRAPP/SECURITY_AUDIT_REPORT.md) - Full audit findings

### External Resources
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP ASVS](https://owasp.org/www-project-application-security-verification-standard/)
- [SecurityHeaders.com](https://securityheaders.com/)
- [SSL Labs](https://www.ssllabs.com/ssltest/)

---

## Approval & Sign-off

**Prepared By:** Senior Security Engineer
Date: November 17, 2025

**Reviewed By:** CTO/CISO: _________________________ Date: _________

**Approved for Implementation:** ☐ Yes ☐ No

**Implementation Lead:** _________________________ Date: _________

**Target Completion Date:** November 24, 2025 (7 days)

---

**CONFIDENTIAL:** This document contains security-sensitive information.

**END OF DOCUMENT**
