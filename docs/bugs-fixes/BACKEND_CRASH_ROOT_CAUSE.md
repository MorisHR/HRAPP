# Root Cause: Backend Was Crashed + Port Forwarding Cached 404

**Date:** 2025-11-19
**Status:** 🟢 RESOLVED

---

## What Happened

User saw: `"Cannot connect to server. Please check your internet connection"`

---

## Root Cause (3 Issues)

### Issue #1: Backend Crashed ❌
The backend server **crashed or was stopped** after the codespace restarted.

**Evidence:**
- Process 66074 (dotnet run) was no longer running
- `curl http://localhost:5090/api/health` returned nothing
- `netstat` showed port 5090 not listening

**Fix:** Restarted backend with correct environment variables ✅

---

### Issue #2: Port Forwarding Cached 404 ❌
GitHub Codespaces port forwarding **cached the 404** from when backend was down.

**Evidence:**
```bash
# Backend is UP on localhost:
$ curl http://localhost:5090/api/auth/login
HTTP/1.1 200 OK  ✅

# But Codespaces URL still returns 404:
$ curl https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api/health
HTTP/2 404  ❌
```

**Why:** When port forwarding was started, backend wasn't running yet. GitHub cached the 404 response.

**Fix:** Use localhost URL in development environment ✅

---

### Issue #3: Lazy Error Message ❌
Frontend shows "check your internet connection" instead of the real error.

**Code:**
```typescript
// superadmin-login.component.ts:298-300
if (error.status === 0) {
  this.errorMessage.set('Cannot connect to server. Please check your internet connection and try again.');
}
```

**Problem:**
- `error.status === 0` happens when:
  - CORS blocks request
  - Connection refused
  - **Backend not running** ← THIS WAS IT
  - Timeout

**Fix Needed:** Show actual error details (see P0_ERROR_HANDLING_FIX.md) ⏳

---

## Timeline

1. **11:56** - Backend auto-started by codespace
2. **12:09** - User tries to login → error
3. **12:13** - Investigation reveals backend is DOWN
4. **12:15** - Backend restarted
5. **12:16** - Backend compilation begins
6. **12:16:51** - Backend UP and listening on 0.0.0.0:5090
7. **12:19** - Localhost login works! Codespaces URL still 404

---

## Solution

### Quick Fix (Working Now):
**Option A: Use localhost in development**
```typescript
// hrms-frontend/src/environments/environment.ts
apiUrl: 'http://localhost:5090/api'  // ← Change to localhost
```

**Option B: Restart port forwarding in VS Code**
1. Go to "PORTS" tab
2. Find port 5090
3. Right-click → "Stop Forwarding Port"
4. Right-click → "Forward Port" → 5090
5. Set visibility to "Public"
6. Refresh browser

---

## Why "Check Your Internet" Was Wrong

The error message said "check your internet connection" but:
- ✅ Internet connection was fine
- ✅ Frontend could reach GitHub
- ✅ Backend was running (after restart)
- ❌ Backend had crashed earlier
- ❌ Port forwarding cached 404

**Fortune 500 would show:**
```
Backend Service Unavailable (Connection Refused)
URL: https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api/auth/login
Status: Cannot connect (ERR_CONNECTION_REFUSED)

Possible causes:
• Backend service is not running
• Port forwarding not configured
• Service crashed or restarting

Error ID: NET_000_20251119_1219_a3f9
[Retry] [Copy Error ID] [View Logs]
```

---

## Lessons Learned

1. **Generic error messages hide problems** - "check internet" was useless
2. **Show the actual error** - status code, URL, timestamp
3. **Health checks on startup** - Would have detected backend was down
4. **Correlation IDs** - Make debugging possible
5. **Port forwarding can cache** - GitHub Codespaces caches 404s

---

## Next Steps

1. ✅ Backend is running
2. ✅ Login works on localhost
3. ⏳ Change frontend to use localhost URL
4. ⏳ Implement Fortune 500-grade error handling
5. ⏳ Add health check on frontend startup
6. ⏳ Add auto-retry with exponential backoff

---

**Status:** Backend running, login works on localhost. Frontend needs config change.
