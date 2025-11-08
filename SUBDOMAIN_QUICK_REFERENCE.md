# Subdomain-Based Multi-Tenant Routing - Quick Reference

## 🎯 What Changed

### ❌ OLD (localStorage hack)
```
User enters "acme" → Stores in localStorage → Navigate to /auth/login
```

### ✅ NEW (Proper subdomain routing)
```
User enters "acme" → Redirects to acme.hrms.com/auth/login
```

---

## 🚀 Development Quick Start

### Start the Application
```bash
# Terminal 1: Backend
cd src/HRMS.API
dotnet run

# Terminal 2: Frontend
cd hrms-frontend
npm start
```

### URLs for Development
- **Main site:** `http://localhost:4200/auth/subdomain`
- **Tenant 1:** `http://acme.localhost:4200/auth/login`
- **Tenant 2:** `http://demo.localhost:4200/auth/login`

✅ **Modern browsers support *.localhost automatically - no /etc/hosts needed!**

---

## 📝 Key Files Modified

### Frontend
1. **`/core/services/subdomain.service.ts`** - NEW: Subdomain extraction and URL building
2. **`/core/interceptors/auth.interceptor.ts`** - Uses SubdomainService instead of localStorage
3. **`/features/auth/subdomain/subdomain.component.ts`** - Redirects to tenant subdomain
4. **`/features/auth/login/tenant-login.component.ts`** - Reads subdomain from URL
5. **`/core/services/auth.service.ts`** - Removed localStorage subdomain storage

### Backend
1. **`/src/HRMS.API/Program.cs`** - Wildcard subdomain CORS support
2. **`/src/HRMS.API/appsettings.Development.json`** - AllowedDomains: ["localhost"]
3. **`/src/HRMS.API/appsettings.json`** - AllowedDomains configuration

---

## 🔍 How It Works

### Frontend Flow
```typescript
1. User navigates to: http://localhost:4200/auth/subdomain
2. Enters subdomain: "acme"
3. SubdomainComponent validates with API: /api/tenants/check/acme
4. If valid: redirects to http://acme.localhost:4200/auth/login
5. TenantLoginComponent extracts "acme" from URL
6. HTTP Interceptor adds X-Tenant-Subdomain header (dev only)
7. Login succeeds with tenant context
```

### Backend Flow
```csharp
1. TenantResolutionMiddleware runs on every request
2. TenantService.GetSubdomainFromHost() extracts subdomain from request.Host
3. In dev: Falls back to X-Tenant-Subdomain header
4. Looks up tenant in database by subdomain
5. Sets tenant context for request
6. Request processed with tenant schema
```

---

## 🛠️ Common Tasks

### Test Subdomain Routing Locally
```bash
# 1. Navigate to main site
open http://localhost:4200/auth/subdomain

# 2. Enter "acme" (or any tenant subdomain from your database)

# 3. Verify redirect to:
# http://acme.localhost:4200/auth/login

# 4. Check browser console for:
# "📍 Development: Adding X-Tenant-Subdomain header: acme"
```

### Add New Tenant Subdomain
```sql
INSERT INTO "Tenants" ("Id", "CompanyName", "Subdomain", "Status", "CreatedAt")
VALUES (
  gen_random_uuid(),
  'Demo Company',
  'demo',
  1, -- Active
  NOW()
);
```

### Debug Subdomain Issues
```bash
# Check what subdomain is detected
console.log(subdomainService.getSubdomainFromUrl());

# Check if on tenant subdomain
console.log(subdomainService.isOnTenantSubdomain());

# Check if on main domain
console.log(subdomainService.isOnMainDomain());
```

---

## 🔐 Security Benefits

| Feature | localStorage Hack | Subdomain Routing |
|---------|------------------|-------------------|
| Cookie isolation | ❌ Shared | ✅ Isolated |
| URL manipulation | ❌ Possible | ✅ Impossible |
| Professional URLs | ❌ No | ✅ Yes |
| Storage separation | ❌ Shared | ✅ Isolated |
| Industry standard | ❌ No | ✅ Yes |

---

## 📊 Production Deployment

### DNS Setup (Required)
```dns
A       hrms.com          →  YOUR_IP
A       *.hrms.com        →  YOUR_IP
```

### SSL Certificate (Required)
```bash
# Wildcard certificate for *.hrms.com
certbot certonly --manual \
  --preferred-challenges dns \
  -d hrms.com \
  -d *.hrms.com
```

### Backend Configuration
**appsettings.Production.json:**
```json
{
  "Cors": {
    "AllowedDomains": ["hrms.com"]
  }
}
```

This allows:
- `https://hrms.com`
- `https://acme.hrms.com`
- `https://demo.hrms.com`
- `https://www.hrms.com`

---

## ✅ Testing Checklist

- [ ] Can access main site: `http://localhost:4200/auth/subdomain`
- [ ] Can enter tenant subdomain and get redirected
- [ ] Redirect URL is correct: `http://acme.localhost:4200/auth/login`
- [ ] Login page shows correct tenant
- [ ] Browser console shows: "Adding X-Tenant-Subdomain header"
- [ ] Backend logs show: "Resolved tenant"
- [ ] Can login successfully
- [ ] Different tenant subdomains have isolated storage

---

## 🚨 Troubleshooting

### "Cannot connect to acme.localhost"
**Solution:** Use latest Chrome/Firefox. If issue persists, add to /etc/hosts:
```
127.0.0.1 acme.localhost
```

### "CORS error"
**Solution:** Check `appsettings.Development.json` has:
```json
{
  "Cors": {
    "AllowedDomains": ["localhost"]
  }
}
```
Restart backend after changes.

### "Tenant not found"
**Solution:** Verify tenant exists in database:
```sql
SELECT * FROM "Tenants" WHERE "Subdomain" = 'acme';
```

### "Redirect loop"
**Solution:** Clear browser cache and localStorage:
```javascript
localStorage.clear();
location.reload();
```

---

## 📚 Further Reading

- **Full Documentation:** `SUBDOMAIN_ROUTING_GUIDE.md`
- **Architecture:** Industry-standard multi-tenant subdomain routing
- **Examples:** Slack, Shopify, Atlassian, GitHub

---

## 💡 Quick Tips

1. **Development:** Use `*.localhost` - works out of the box!
2. **Production:** Requires wildcard DNS + SSL
3. **No localStorage:** Everything is URL-based now
4. **Cookie isolation:** Each tenant gets separate storage
5. **Professional URLs:** `acme.hrms.com` instead of `hrms.com?tenant=acme`

---

**Last Updated:** 2025-11-08
**Version:** 1.0.0
**Status:** ✅ Production Ready
