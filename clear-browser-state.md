# Clear Browser State for SuperAdmin Login

## Issue
When navigating to `/auth/superadmin`, the app redirects to `/auth/subdomain`.

## Root Cause
The application may have residual tenant context stored in browser localStorage from previous sessions, which interferes with the SuperAdmin login flow.

## Solution Steps

### 1. Clear Browser LocalStorage
Open the browser console (F12) and run:

```javascript
// Clear all HRMS-related storage
localStorage.clear();
sessionStorage.clear();

// Verify it's cleared
console.log('LocalStorage cleared:', localStorage.length === 0);

// Force reload
location.reload();
```

### 2. Navigate Directly to SuperAdmin Login
After clearing storage, navigate to:
```
https://repulsive-toad-7vjj6xv99745hrvj-4200.app.github.dev/auth/superadmin
```

### 3. Check Browser Console for Logs
Look for these console logs to understand the routing flow:
- `🔍 SUBDOMAIN GUARD: Checking route:` - Should show "Auth route - bypassing"
- `🛡️ AUTH GUARD:` - Shows authentication checks
- `🔍 ENVIRONMENT DETECTION` - Shows detected environment type
- `📍 Tenant Context` - Shows current tenant context

### 4. If Issue Persists
Check for:
1. Any active guards being triggered
2. Environment detection showing Codespaces correctly
3. No tenant context being loaded from localStorage

## Quick Fix Command
Run in browser console:
```javascript
localStorage.removeItem('hrms_tenant_subdomain');
localStorage.removeItem('auth_token');
localStorage.removeItem('refresh_token');
localStorage.removeItem('last_user_role');
location.href = '/auth/superadmin';
```

## Verified Fixes Applied
- ✅ Subdomain guard explicitly skips `/auth/*` routes
- ✅ SuperAdmin login component clears non-SuperAdmin sessions
- ✅ Auth guards properly redirect based on user role
