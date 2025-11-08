# Subdomain-Based Multi-Tenant Routing Guide

## Overview

This HRMS application now uses **proper subdomain-based routing** for multi-tenancy, following industry best practices (Slack, Shopify, Atlassian model).

### Before (❌ localStorage hack):
```
1. User enters "acme" → localhost/auth/subdomain
2. Stores in localStorage: hrms_subdomain = "acme"
3. Navigates to localhost/auth/login
4. Reads from localStorage
```

**Problems:**
- ❌ Not true multi-tenancy
- ❌ Cookie/localStorage sharing between tenants
- ❌ Unprofessional URLs
- ❌ localStorage can be manipulated
- ❌ Breaks if user clears storage

### After (✅ Proper subdomain routing):
```
1. User enters "acme" → hrms.com/auth/subdomain
2. Validates tenant exists
3. Redirects to acme.hrms.com/auth/login
4. Backend extracts "acme" from hostname
5. Tenant-specific cookies and storage
```

**Benefits:**
- ✅ True multi-tenancy
- ✅ Cookie isolation between tenants
- ✅ Professional URLs
- ✅ Secure (can't manipulate subdomain)
- ✅ Industry-standard architecture

---

## Architecture Changes

### Frontend Changes

#### 1. New SubdomainService (`/hrms-frontend/src/app/core/services/subdomain.service.ts`)
- Extracts subdomain from window.location.hostname
- Provides methods for subdomain detection and redirection
- Builds tenant-specific URLs

**Key Methods:**
```typescript
getSubdomainFromUrl(): string | null
isOnTenantSubdomain(): boolean
redirectToTenant(subdomain: string, path: string)
redirectToMainDomain(path: string)
```

#### 2. Updated HTTP Interceptor (`/hrms-frontend/src/app/core/interceptors/auth.interceptor.ts`)
- **Development:** Adds `X-Tenant-Subdomain` header (extracted from URL)
- **Production:** No header needed (backend extracts from request host)

#### 3. Subdomain Component (`/hrms-frontend/src/app/features/auth/subdomain/subdomain.component.ts`)
- **Before:** Stored subdomain in localStorage and navigated to /auth/login
- **After:** Redirects to `{subdomain}.domain.com/auth/login`

#### 4. Tenant Login Component (`/hrms-frontend/src/app/features/auth/login/tenant-login.component.ts`)
- **Before:** Read subdomain from localStorage
- **After:** Extracts subdomain from URL using SubdomainService

#### 5. Auth Service (`/hrms-frontend/src/app/core/services/auth.service.ts`)
- Removed all localStorage subdomain storage
- Subdomain now comes from URL

### Backend Changes

#### 1. CORS Configuration (`/src/HRMS.API/Program.cs`)
**New wildcard subdomain support:**
```json
{
  "Cors": {
    "AllowedDomains": ["hrms.com"]
  }
}
```

This allows:
- `https://acme.hrms.com`
- `https://demo.hrms.com`
- `https://www.hrms.com`
- `https://hrms.com`

**Development:** Automatically allows `*.localhost`

#### 2. Tenant Resolution (`/src/HRMS.Infrastructure/Services/TenantService.cs`)
Already implemented! The `GetSubdomainFromHost()` method:
- Extracts subdomain from request hostname
- Falls back to `X-Tenant-Subdomain` header (dev/staging only)

---

## Development Setup

### Option 1: Using *.localhost (Recommended - No configuration needed)

Modern browsers support wildcard localhost subdomains out of the box:

**URLs:**
- Main domain: `http://localhost:4200`
- Tenant 1: `http://acme.localhost:4200`
- Tenant 2: `http://demo.localhost:4200`

**Steps:**
1. Start backend: `cd src/HRMS.API && dotnet run`
2. Start frontend: `cd hrms-frontend && npm start`
3. Navigate to `http://localhost:4200/auth/subdomain`
4. Enter tenant subdomain (e.g., "acme")
5. You'll be redirected to `http://acme.localhost:4200/auth/login`

✅ **No /etc/hosts configuration needed!**

### Option 2: Using /etc/hosts (Alternative)

If you prefer custom local domain names:

**Edit /etc/hosts:**
```
127.0.0.1 hrms.local
127.0.0.1 acme.hrms.local
127.0.0.1 demo.hrms.local
127.0.0.1 test.hrms.local
```

**URLs:**
- Main domain: `http://hrms.local:4200`
- Tenant 1: `http://acme.hrms.local:4200`

---

## Production Deployment

### 1. DNS Configuration

**Required DNS Records:**

```dns
# Root domain
A       hrms.com          →  YOUR_SERVER_IP

# Wildcard subdomain (for all tenants)
A       *.hrms.com        →  YOUR_SERVER_IP

# Optional: www subdomain
CNAME   www.hrms.com      →  hrms.com
```

### 2. SSL Certificate

**Option A: Wildcard Certificate (Recommended)**
```bash
# Let's Encrypt with Certbot
certbot certonly --manual \
  --preferred-challenges dns \
  -d hrms.com \
  -d *.hrms.com
```

**Option B: Cloud Provider Wildcard SSL**
- AWS Certificate Manager: Request `*.hrms.com`
- Google Cloud Load Balancer: Configure wildcard SSL
- Azure Application Gateway: Add wildcard certificate

### 3. Backend Configuration

**appsettings.Production.json:**
```json
{
  "Cors": {
    "AllowedDomains": [
      "hrms.com"
    ]
  },
  "JwtSettings": {
    "Secret": "USE_GOOGLE_SECRET_MANAGER",
    "Issuer": "HRMS.API",
    "Audience": "HRMS.Client"
  },
  "Security": {
    "RequireHttpsMetadata": true
  }
}
```

**Google Secret Manager (Production):**
```bash
# Store secrets
gcloud secrets create DB_CONNECTION_STRING --data-file=./connection-string.txt
gcloud secrets create JWT_SECRET --data-file=./jwt-secret.txt

# Grant access to service account
gcloud secrets add-iam-policy-binding DB_CONNECTION_STRING \
  --member="serviceAccount:hrms@PROJECT_ID.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"
```

### 4. Frontend Configuration

**environment.prod.ts:**
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.hrms.com/api', // Dedicated API subdomain
  // OR
  apiUrl: 'https://hrms.com/api',     // Same domain
  appName: 'HRMS',
  version: '1.0.0'
};
```

### 5. Web Server Configuration

#### Nginx
```nginx
server {
    listen 443 ssl;
    server_name hrms.com *.hrms.com;

    ssl_certificate /etc/letsencrypt/live/hrms.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/hrms.com/privkey.pem;

    # Frontend (Angular)
    location / {
        root /var/www/hrms-frontend;
        try_files $uri $uri/ /index.html;
    }

    # Backend API
    location /api {
        proxy_pass http://localhost:5090;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

#### Apache
```apache
<VirtualHost *:443>
    ServerName hrms.com
    ServerAlias *.hrms.com

    SSLEngine on
    SSLCertificateFile /etc/letsencrypt/live/hrms.com/fullchain.pem
    SSLCertificateKeyFile /etc/letsencrypt/live/hrms.com/privkey.pem

    DocumentRoot /var/www/hrms-frontend

    # Angular fallback routing
    <Directory /var/www/hrms-frontend>
        RewriteEngine On
        RewriteBase /
        RewriteRule ^index\.html$ - [L]
        RewriteCond %{REQUEST_FILENAME} !-f
        RewriteCond %{REQUEST_FILENAME} !-d
        RewriteRule . /index.html [L]
    </Directory>

    # API proxy
    ProxyPass /api http://localhost:5090/api
    ProxyPassReverse /api http://localhost:5090/api
</VirtualHost>
```

### 6. Cloud Deployment Examples

#### Google Cloud Platform (Cloud Run + Load Balancer)

**Frontend (Cloud Run):**
```bash
# Build and deploy
gcloud run deploy hrms-frontend \
  --source ./hrms-frontend \
  --region us-central1 \
  --allow-unauthenticated
```

**Backend (Cloud Run):**
```bash
# Build and deploy
gcloud run deploy hrms-api \
  --source ./src/HRMS.API \
  --region us-central1 \
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production"
```

**Load Balancer:**
```bash
# Create load balancer with wildcard SSL
gcloud compute ssl-certificates create hrms-wildcard-cert \
  --domains=hrms.com,*.hrms.com

# Map to backend services
# Frontend: *.hrms.com → Cloud Run Frontend
# Backend: api.hrms.com → Cloud Run Backend
```

#### AWS (Elastic Beanstalk + CloudFront)

**ACM Certificate:**
```bash
# Request wildcard certificate
aws acm request-certificate \
  --domain-name hrms.com \
  --subject-alternative-names *.hrms.com \
  --validation-method DNS
```

**CloudFront Distribution:**
- Origin: Elastic Beanstalk
- Alternate Domain Names: `hrms.com`, `*.hrms.com`
- SSL Certificate: ACM Wildcard Certificate

#### Azure (App Service + Application Gateway)

**App Service:**
- Deploy frontend and backend
- Configure custom domains: `*.hrms.com`

**Application Gateway:**
- Configure wildcard SSL certificate
- Routing rules for subdomains

---

## Security Considerations

### 1. Cookie Isolation ✅
With subdomain routing:
- `acme.hrms.com` cookies are isolated from `demo.hrms.com`
- No cookie sharing between tenants
- Enhanced security and privacy

### 2. CORS Security ✅
- Production: Only allows configured domains
- Wildcard subdomain support is explicit
- No `AllowAnyOrigin` used

### 3. Subdomain Validation ✅
- Backend validates subdomain against database
- Active tenants only
- Returns 404 for invalid subdomains

### 4. HTTPS Enforcement ✅
- `RequireHttpsMetadata: true` in production
- Secure cookie transmission
- TLS 1.2+ required

---

## Testing the Implementation

### Development Testing

**1. Test Main Domain (Subdomain Entry)**
```bash
# Navigate to
http://localhost:4200/auth/subdomain

# Enter subdomain: "acme"
# Should redirect to: http://acme.localhost:4200/auth/login
```

**2. Test Tenant Subdomain (Login)**
```bash
# Navigate directly to
http://acme.localhost:4200/auth/login

# Should:
- Extract subdomain "acme" from URL
- Show login form for tenant "acme"
- Send X-Tenant-Subdomain header to API (dev only)
```

**3. Test Backend Subdomain Resolution**
```bash
# Check backend logs
# Should see:
[INFO] Development: Adding X-Tenant-Subdomain header: acme
[INFO] Resolved tenant: {TenantId}, Schema: {SchemaName}
```

**4. Test Cookie Isolation**
```bash
# Login to acme.localhost:4200
# Check localStorage: should have access_token

# Navigate to demo.localhost:4200
# Check localStorage: should be empty (different origin)
```

### Production Testing

**1. DNS Verification**
```bash
# Check DNS resolution
dig hrms.com
dig acme.hrms.com
dig demo.hrms.com

# All should resolve to same IP
```

**2. SSL Verification**
```bash
# Check wildcard certificate
openssl s_client -connect acme.hrms.com:443 -servername acme.hrms.com

# Verify certificate is valid for *.hrms.com
```

**3. CORS Verification**
```bash
# Test CORS headers
curl -H "Origin: https://acme.hrms.com" \
     -H "Access-Control-Request-Method: POST" \
     -X OPTIONS \
     https://api.hrms.com/api/auth/tenant/login

# Should return:
# Access-Control-Allow-Origin: https://acme.hrms.com
# Access-Control-Allow-Credentials: true
```

---

## Troubleshooting

### Issue: "Cannot connect to acme.localhost"

**Solution:**
Most modern browsers support `*.localhost` automatically. If not:
1. Try Firefox or Chrome (latest versions)
2. Use `/etc/hosts` configuration (Option 2)
3. Use local DNS server (dnsmasq)

### Issue: "CORS error in development"

**Solution:**
Check `appsettings.Development.json`:
```json
{
  "Cors": {
    "AllowedDomains": ["localhost"]
  }
}
```

Restart backend after configuration change.

### Issue: "Tenant not found" in production

**Solution:**
1. Verify tenant exists in database: `SELECT * FROM "Tenants" WHERE "Subdomain" = 'acme'`
2. Check tenant status: `Status` should be `Active`
3. Verify backend is extracting subdomain: Check logs for "Resolved tenant"

### Issue: "Redirect loop" on tenant login page

**Solution:**
This happens if the subdomain component redirects but the login page doesn't detect subdomain properly.

Check:
1. SubdomainService is injected in login component
2. `getSubdomainFromUrl()` returns correct subdomain
3. Browser console for errors

### Issue: "SSL certificate not valid for subdomain"

**Solution:**
Ensure you're using a **wildcard certificate** (`*.hrms.com`), not individual certificates.

---

## Migration Guide

If you have existing users with localStorage data:

**Option 1: Clear localStorage (Clean Migration)**
```typescript
// Add to SubdomainComponent or App initialization
if (localStorage.getItem('hrms_subdomain')) {
  console.warn('Cleaning up old localStorage subdomain data');
  localStorage.removeItem('hrms_subdomain');
  localStorage.removeItem('hrms_company_name');
  localStorage.removeItem('tenant_subdomain');
}
```

**Option 2: Redirect Based on Old Data (Gradual Migration)**
```typescript
// In App initialization
const oldSubdomain = localStorage.getItem('hrms_subdomain');
if (oldSubdomain && !this.subdomainService.isOnTenantSubdomain()) {
  console.log('Migrating from old localStorage to subdomain routing');
  this.subdomainService.redirectToTenant(oldSubdomain, window.location.pathname);
}
```

---

## Performance Considerations

### 1. DNS Caching
- Wildcard DNS lookups are cached by browsers
- No performance impact after first lookup

### 2. SSL Handshake
- Wildcard certificates work efficiently
- Same certificate used for all subdomains
- No additional SSL overhead

### 3. CORS Preflight
- Same as before
- CORS headers cached by browser
- Minimal overhead

---

## Summary

✅ **What Changed:**
1. SubdomainComponent now redirects instead of localStorage
2. TenantLoginComponent extracts subdomain from URL
3. HTTP Interceptor adds header in dev, extracts from URL
4. Backend CORS supports wildcard subdomains
5. No localStorage dependency

✅ **What Stayed the Same:**
1. Backend tenant resolution logic (already supported subdomain extraction)
2. Database schema
3. Authentication flow
4. JWT tokens

✅ **Benefits:**
1. True multi-tenancy with cookie/storage isolation
2. Professional URLs (acme.hrms.com vs hrms.com?tenant=acme)
3. Security (can't manipulate subdomain)
4. Industry-standard architecture
5. Better for SEO and branding

---

## Next Steps

1. **Development:**
   - Test authentication flow with `*.localhost`
   - Verify subdomain extraction works
   - Check CORS headers in browser DevTools

2. **Staging:**
   - Configure wildcard SSL certificate
   - Test with real domain (e.g., `staging.hrms.com`)
   - Verify DNS and CORS

3. **Production:**
   - Configure wildcard DNS (`*.hrms.com`)
   - Install wildcard SSL certificate
   - Update CORS configuration
   - Deploy and monitor

---

## Support

For issues or questions:
- Check logs: Backend logs show tenant resolution
- Browser console: Frontend logs show subdomain detection
- Network tab: Check CORS headers and API requests

---

**Last Updated:** 2025-11-08
**Version:** 1.0.0
**Architecture:** Subdomain-Based Multi-Tenant Routing
