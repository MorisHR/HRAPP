# Security Guidelines for HRMS Application

## Table of Contents
1. [Overview](#overview)
2. [Critical Security Notice](#critical-security-notice)
3. [Secret Management](#secret-management)
4. [Developer Setup](#developer-setup)
5. [Production Deployment](#production-deployment)
6. [Security Best Practices](#security-best-practices)
7. [Incident Response](#incident-response)
8. [Security Audit History](#security-audit-history)

---

## Overview

This document outlines security practices for the HRMS multi-tenant application. All developers, DevOps engineers, and security personnel must read and follow these guidelines.

**Security Classification:** CONFIDENTIAL
**Last Updated:** 2025-11-12
**Owned By:** DevSecOps Team

---

## Critical Security Notice

### 🔴 NEVER Commit These Files to Git:

```
❌ appsettings.Production.json (already gitignored)
❌ appsettings.Staging.json (already gitignored)
❌ environment.ts (gitignored as of 2025-11-12)
❌ environment.prod.ts (gitignored as of 2025-11-12)
❌ .env files (already gitignored)
❌ secrets.json (already gitignored)
❌ Any file containing passwords, API keys, or tokens
```

### ✅ Safe to Commit (Templates Only):

```
✓ appsettings.json.template
✓ appsettings.Development.json.template
✓ environment.ts.template
✓ environment.prod.ts.template
✓ .env.example
```

---

## Secret Management

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    SECRET MANAGEMENT LAYERS                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Development (Local)                                         │
│  ├─ .NET User Secrets (~/.microsoft/usersecrets/)           │
│  ├─ Environment Variables                                    │
│  └─ Local configuration files (gitignored)                   │
│                                                              │
│  Staging/Production (Cloud)                                  │
│  ├─ Google Secret Manager (Primary)                         │
│  ├─ Environment Variables (Cloud Run/GKE)                    │
│  └─ Kubernetes Secrets (if applicable)                       │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Secret Categories

| Category | Examples | Storage Location (Dev) | Storage Location (Prod) |
|----------|----------|------------------------|-------------------------|
| **Database** | Connection strings, passwords | User Secrets | Google Secret Manager |
| **Authentication** | JWT secrets, OAuth keys | User Secrets | Google Secret Manager |
| **Email** | SMTP credentials | User Secrets | Google Secret Manager |
| **API Keys** | Third-party service keys | User Secrets | Google Secret Manager |
| **Certificates** | SSL/TLS certificates | Local keystore | Cloud KMS |
| **Encryption** | Data encryption keys | User Secrets | Google Secret Manager |

### Secrets Rotation Schedule

| Secret Type | Rotation Frequency | Automated? | Owner |
|-------------|-------------------|------------|-------|
| JWT Secret | Every 90 days | No | DevSecOps |
| Database Passwords | Every 90 days | No | Database Admin |
| SMTP Passwords | Every 180 days | No | DevOps |
| SuperAdmin Secret Path | Every 90 days | No | Security Team |
| API Keys (3rd party) | Per vendor policy | No | Development Team |
| SSL Certificates | Before expiry | Yes (Let's Encrypt) | DevOps |

---

## Developer Setup

### Initial Setup (First Time)

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/HRAPP.git
   cd HRAPP
   ```

2. **Run the automated setup script:**
   ```bash
   chmod +x scripts/setup-dev-secrets.sh
   ./scripts/setup-dev-secrets.sh
   ```

   This script will:
   - Initialize .NET User Secrets
   - Generate a secure JWT secret
   - Prompt for database credentials
   - Optionally configure SMTP settings
   - Create frontend environment files

3. **Verify the setup:**
   ```bash
   # List all configured secrets
   dotnet user-secrets list --project src/HRMS.API/HRMS.API.csproj

   # Check frontend environment
   cat hrms-frontend/src/environments/environment.ts
   ```

### Manual Secret Configuration

If you prefer manual setup or the script fails:

#### Backend (.NET API)

```bash
# Navigate to the API project
cd src/HRMS.API

# Initialize User Secrets
dotnet user-secrets init

# Set JWT Secret
dotnet user-secrets set "JwtSettings:Secret" "YOUR-SECURE-SECRET-MIN-32-CHARS"

# Set Database Password
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=hrms_master;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Prefer"

# Set SMTP Credentials (if not using MailHog)
dotnet user-secrets set "EmailSettings:SmtpUsername" "your-username"
dotnet user-secrets set "EmailSettings:SmtpPassword" "your-password"
```

#### Frontend (Angular)

```bash
cd hrms-frontend/src/environments

# Copy template files
cp environment.ts.template environment.ts
cp environment.prod.ts.template environment.prod.ts

# Generate a unique SuperAdmin secret path
python3 -c "import uuid; print('system-' + str(uuid.uuid4()))"

# Edit environment.ts and replace the placeholder with your generated UUID
nano environment.ts
```

### Environment Variables (Alternative Method)

You can also use environment variables instead of User Secrets:

```bash
# .NET Configuration
export ConnectionStrings__DefaultConnection="Host=localhost;Database=hrms_master;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Prefer"
export JwtSettings__Secret="YOUR-SECURE-SECRET-MIN-32-CHARS"
export EmailSettings__SmtpPassword="YOUR-SMTP-PASSWORD"

# Run the API
cd src/HRMS.API
dotnet run
```

---

## Production Deployment

### Pre-Deployment Checklist

- [ ] All secrets are stored in Google Secret Manager
- [ ] No hardcoded secrets in configuration files
- [ ] SSL/TLS is enforced on all connections
- [ ] Database connection uses encrypted connection (SSL Mode=Require)
- [ ] MFA is enabled for all admin accounts
- [ ] SuperAdmin secret path has been rotated from development value
- [ ] Rate limiting is enabled
- [ ] Security headers are configured
- [ ] Logging and monitoring are active
- [ ] Backup and disaster recovery plans are tested

### Google Secret Manager Setup

1. **Create secrets in Google Cloud Console:**

   ```bash
   # Set project ID
   export PROJECT_ID="your-gcp-project-id"

   # Create secrets
   echo -n "YOUR-JWT-SECRET" | gcloud secrets create jwt-secret --data-file=- --project=$PROJECT_ID
   echo -n "YOUR-DB-CONNECTION-STRING" | gcloud secrets create db-connection-string --data-file=- --project=$PROJECT_ID
   echo -n "YOUR-SMTP-PASSWORD" | gcloud secrets create smtp-password --data-file=- --project=$PROJECT_ID
   ```

2. **Grant access to service account:**

   ```bash
   gcloud secrets add-iam-policy-binding jwt-secret \
     --member="serviceAccount:your-service-account@your-project.iam.gserviceaccount.com" \
     --role="roles/secretmanager.secretAccessor" \
     --project=$PROJECT_ID
   ```

3. **Configure the application:**

   Update `appsettings.Production.json`:
   ```json
   {
     "GoogleCloud": {
       "ProjectId": "your-gcp-project-id",
       "SecretManagerEnabled": true
     }
   }
   ```

### CI/CD Pipeline Secrets

#### GitHub Actions

Store secrets in GitHub Repository Settings → Secrets and variables → Actions:

```yaml
# .github/workflows/deploy.yml
env:
  JWT_SECRET: ${{ secrets.JWT_SECRET }}
  DB_CONNECTION_STRING: ${{ secrets.DB_CONNECTION_STRING }}
  SMTP_PASSWORD: ${{ secrets.SMTP_PASSWORD }}
  SUPERADMIN_SECRET_PATH: ${{ secrets.SUPERADMIN_SECRET_PATH }}
```

#### Building Frontend with Secrets

```yaml
- name: Create production environment file
  run: |
    cat > src/environments/environment.prod.ts << EOF
    export const environment = {
      production: true,
      apiUrl: '${{ secrets.API_URL }}',
      appName: 'HRMS',
      version: '1.0.0',
      superAdminSecretPath: '${{ secrets.SUPERADMIN_SECRET_PATH }}'
    };
    EOF
```

---

## Security Best Practices

### Code Review Requirements

Before merging any PR:

- [ ] No secrets in code, comments, or commit messages
- [ ] All new configuration uses environment variables or Secret Manager
- [ ] Sensitive data is encrypted at rest and in transit
- [ ] Input validation is present for all user inputs
- [ ] SQL injection protection is verified (use parameterized queries)
- [ ] XSS protection is implemented
- [ ] CSRF tokens are used for state-changing operations
- [ ] Authentication and authorization are properly implemented

### Common Security Pitfalls to Avoid

| ❌ Don't Do This | ✅ Do This Instead |
|------------------|-------------------|
| `Password = "postgres"` in appsettings.json | Use User Secrets or Secret Manager |
| `app.UseCors(options => options.AllowAnyOrigin())` | Specify exact allowed origins |
| `[AllowAnonymous]` on sensitive endpoints | Use `[Authorize]` with appropriate roles |
| Logging sensitive data | Sanitize logs, use `[SensitiveData]` attributes |
| Storing passwords in plain text | Hash with bcrypt/Argon2 |
| Using HTTP in production | Enforce HTTPS everywhere |
| Trusting client-side validation | Always validate on server |

### Database Security

```sql
-- Create read-only user for reporting
CREATE USER hrms_readonly WITH PASSWORD 'secure-password';
GRANT CONNECT ON DATABASE hrms_db TO hrms_readonly;
GRANT USAGE ON SCHEMA public TO hrms_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO hrms_readonly;

-- Create application user with limited privileges
CREATE USER hrms_app WITH PASSWORD 'secure-password';
GRANT CONNECT ON DATABASE hrms_db TO hrms_app;
GRANT USAGE, CREATE ON SCHEMA public TO hrms_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO hrms_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hrms_app;

-- Enable SSL/TLS
ALTER SYSTEM SET ssl = on;
```

### SSL/TLS Configuration

**Development:**
```
SSL Mode=Prefer
```

**Production:**
```
SSL Mode=Require;Trust Server Certificate=false
```

---

## Incident Response

### Security Incident Categories

| Severity | Examples | Response Time | Escalation |
|----------|----------|---------------|------------|
| **CRITICAL** | Data breach, exposed secrets in public repo | < 1 hour | CTO, CEO, Legal |
| **HIGH** | Unauthorized admin access, SQL injection | < 4 hours | Security Team, CTO |
| **MEDIUM** | Failed login attempts spike, DDoS | < 8 hours | DevOps, Security Team |
| **LOW** | Outdated dependencies, misconfigurations | < 48 hours | Development Team |

### If Secrets Are Exposed

**Immediate Actions (Within 1 Hour):**

1. **Revoke compromised credentials immediately**
   ```bash
   # Disable the compromised user/service account
   # Rotate all secrets immediately
   # Force logout all active sessions
   ```

2. **Remove secrets from Git history**
   ```bash
   # Use BFG Repo-Cleaner or git-filter-repo
   git filter-repo --path appsettings.json --invert-paths
   git push --force --all
   ```

3. **Notify stakeholders**
   - Send security incident report
   - Notify affected users if applicable
   - File incident in security log

4. **Rotate all related secrets**
   - Database passwords
   - JWT secrets
   - API keys
   - SuperAdmin secret paths
   - SMTP credentials

5. **Conduct post-mortem**
   - Document what happened
   - Identify root cause
   - Implement preventive measures
   - Update this security documentation

### Emergency Contacts

| Role | Contact | When to Contact |
|------|---------|-----------------|
| Security Team Lead | security@yourdomain.com | Any security incident |
| DevOps On-Call | devops-oncall@yourdomain.com | Production issues |
| CTO | cto@yourdomain.com | Critical incidents |
| Legal/Compliance | legal@yourdomain.com | Data breaches, GDPR issues |

---

## Security Audit History

### 2025-11-12: Critical Security Remediation

**Vulnerabilities Fixed:**

1. ✅ **Removed exposed SMTP password** from `appsettings.json`
   - Before: `SmtpPassword: "Cv0Tnh8du1suVuT9"`
   - After: `SmtpPassword: ""` (must use Secret Manager)
   - **Action Required:** Rotate SMTP password immediately

2. ✅ **Removed exposed JWT secret** from `appsettings.Development.json`
   - Before: Hardcoded 64-character secret
   - After: Empty string (must use User Secrets)
   - **Action Required:** All developers must run `./scripts/setup-dev-secrets.sh`

3. ✅ **Removed database passwords** from configuration files
   - Before: `Password=postgres` in connection strings
   - After: `Password=` (empty, use User Secrets/Secret Manager)
   - Added SSL Mode=Prefer for encrypted connections
   - Removed `Include Error Detail=true` (information disclosure risk)

4. ✅ **Rotated SuperAdmin secret paths**
   - Development: Changed to `system-732c44d0-d59b-494c-9fc0-bf1d65add4e5`
   - Production: Changed to `system-d27b11e7-fae7-4f19-8a56-6d3455b97105`
   - **Action Required:** Update backend routes (marked as Obsolete)

5. ✅ **Enhanced .gitignore** to prevent future exposure
   - Added patterns to block all environment files
   - Added patterns to block all appsettings except templates
   - Added .NET User Secrets exclusions

6. ✅ **Created secure configuration templates**
   - `appsettings.json.template`
   - `appsettings.Development.json.template`
   - `environment.ts.template`
   - `environment.prod.ts.template`

7. ✅ **Created developer setup automation**
   - `scripts/setup-dev-secrets.sh`
   - Automates User Secrets initialization
   - Generates secure random secrets
   - Creates frontend environment files

**Risk Assessment:**

| Risk | Before | After | Mitigation |
|------|--------|-------|------------|
| Secret Exposure | CRITICAL | LOW | Secrets removed, templates created |
| Database Compromise | HIGH | MEDIUM | SSL enforced, passwords externalized |
| JWT Token Forgery | HIGH | LOW | Secret externalized, rotation enabled |
| Man-in-the-Middle | MEDIUM | LOW | SSL/TLS enforced |
| Information Disclosure | MEDIUM | LOW | Error details disabled in production |

**Immediate Actions Required by Team:**

1. **All Developers:**
   - Pull latest changes
   - Run `./scripts/setup-dev-secrets.sh`
   - Verify local development environment works

2. **DevOps Team:**
   - Rotate SMTP password in SMTP2GO
   - Update Google Secret Manager with new secrets
   - Update production deployment scripts

3. **Security Team:**
   - Verify old SuperAdmin paths are disabled
   - Monitor for unauthorized access attempts
   - Schedule next security audit (90 days)

---

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Google Secret Manager Documentation](https://cloud.google.com/secret-manager/docs)
- [ASP.NET Core User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)

---

## Questions or Concerns?

Contact the Security Team:
- Email: security@yourdomain.com
- Slack: #security-team
- Emergency Hotline: [Your emergency contact]

---

**Document Version:** 2.0
**Last Security Audit:** 2025-11-12
**Next Scheduled Audit:** 2026-02-12
**Document Owner:** DevSecOps Team
