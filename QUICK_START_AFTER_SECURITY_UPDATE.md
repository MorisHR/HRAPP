# Quick Start Guide - After Security Update

**⚠️ IMPORTANT: Configuration files have been updated for security**

If you're seeing errors when running the application, follow this guide.

---

## For Developers (Local Development)

### Option 1: Automated Setup (Recommended)

Run this single command:

```bash
./scripts/setup-dev-secrets.sh
```

The script will:
- ✅ Set up .NET User Secrets
- ✅ Generate JWT secret
- ✅ Configure database password
- ✅ Set up SMTP (optional)
- ✅ Create frontend environment files

### Option 2: Manual Setup

#### Backend (.NET API)

```bash
cd src/HRMS.API

# Initialize User Secrets
dotnet user-secrets init

# Generate and set JWT secret
# On Linux/macOS:
dotnet user-secrets set "JwtSettings:Secret" "$(openssl rand -base64 32)"

# On Windows (PowerShell):
$secret = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
dotnet user-secrets set "JwtSettings:Secret" "$secret"

# Set database password
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=hrms_master;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Prefer"

# Start the API
dotnet run
```

#### Frontend (Angular)

```bash
cd hrms-frontend/src/environments

# Copy template files
cp environment.ts.template environment.ts
cp environment.prod.ts.template environment.prod.ts

# Generate SuperAdmin secret path
python3 -c "import uuid; print('system-' + str(uuid.uuid4()))"

# Edit environment.ts and paste the generated UUID
nano environment.ts  # or use any text editor

# Install and start
cd ../..
npm install
npm start
```

---

## For DevOps (Production/Staging)

### Google Secret Manager Setup

```bash
# Set your GCP project
export PROJECT_ID="your-gcp-project-id"

# Create secrets
echo -n "YOUR-SECURE-JWT-SECRET" | gcloud secrets create jwt-secret --data-file=- --project=$PROJECT_ID
echo -n "Host=YOUR_DB_HOST;Database=hrms_db;Username=postgres;Password=YOUR_DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=false" | gcloud secrets create db-connection-string --data-file=- --project=$PROJECT_ID
echo -n "YOUR-SMTP-PASSWORD" | gcloud secrets create smtp-password --data-file=- --project=$PROJECT_ID

# Grant access to service account
gcloud secrets add-iam-policy-binding jwt-secret \
  --member="serviceAccount:your-sa@your-project.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor" \
  --project=$PROJECT_ID

# Repeat for other secrets...
```

### Environment Variables (Alternative)

```bash
export ConnectionStrings__DefaultConnection="Host=...;Password=YOUR_PASSWORD"
export JwtSettings__Secret="YOUR-JWT-SECRET"
export EmailSettings__SmtpPassword="YOUR-SMTP-PASSWORD"
```

---

## Quick Verification

### Check Backend Configuration

```bash
cd src/HRMS.API

# List all User Secrets
dotnet user-secrets list

# You should see:
# JwtSettings:Secret = [your-secret]
# ConnectionStrings:DefaultConnection = [connection-string]
```

### Check Frontend Configuration

```bash
cd hrms-frontend

# Verify environment file exists
cat src/environments/environment.ts

# Should NOT contain "GENERATE-YOUR-OWN-UUID"
# Should contain a valid UUID in superAdminSecretPath
```

---

## Common Errors & Solutions

### Error: "JwtSettings:Secret is null or empty"

**Solution:**
```bash
cd src/HRMS.API
dotnet user-secrets set "JwtSettings:Secret" "$(openssl rand -base64 32)"
```

### Error: "Database connection failed"

**Solution:**
```bash
# Make sure PostgreSQL is running
docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:15

# Set the connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=hrms_master;Username=postgres;Password=postgres;SSL Mode=Prefer"
```

### Error: "Could not find environment.ts"

**Solution:**
```bash
cd hrms-frontend/src/environments
cp environment.ts.template environment.ts

# Generate UUID and update the file
python3 -c "import uuid; print('system-' + str(uuid.uuid4()))"
# Then edit environment.ts with the generated UUID
```

---

## What Changed?

| What | Before | After |
|------|--------|-------|
| **SMTP Password** | ❌ In appsettings.json | ✅ User Secrets / Secret Manager |
| **JWT Secret** | ❌ In appsettings.Development.json | ✅ User Secrets / Secret Manager |
| **DB Password** | ❌ In connection string | ✅ User Secrets / Secret Manager |
| **Frontend Env** | ❌ Committed to Git | ✅ .gitignored (use templates) |

---

## Need Help?

1. **Read Full Documentation:** See `SECURITY.md`
2. **Detailed Report:** See `SECURITY_REMEDIATION_REPORT.md`
3. **Contact Security Team:** security@yourdomain.com
4. **Slack Channel:** #security-team

---

## TL;DR (Too Long; Didn't Read)

**Just want to start coding?**

```bash
# Run this one command:
./scripts/setup-dev-secrets.sh

# Then start your services:
cd src/HRMS.API && dotnet run
cd hrms-frontend && npm start
```

**Done!** 🚀

---

**Last Updated:** 2025-11-12
**Priority:** HIGH - Must complete before next development session
