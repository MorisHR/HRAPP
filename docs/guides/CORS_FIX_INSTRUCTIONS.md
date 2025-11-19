# CORS Error Fix - GitHub Codespaces Port Configuration

## Problem
You're seeing CORS errors because GitHub Codespaces ports are set to **require authentication** by default, which blocks cross-origin requests between the frontend and backend.

Error message:
```
Access to fetch at 'https://...app.github.dev/api/...' has been blocked by CORS policy:
Response to preflight request doesn't pass access control check:
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

## Root Cause
- Frontend runs on port 4200: `https://repulsive-toad-7vjj6xv99745hrvj-4200.app.github.dev`
- Backend runs on port 5090: `https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev`
- Both ports are set to **"Private"** visibility, requiring GitHub authentication
- This breaks CORS because the preflight OPTIONS request gets a 401 Unauthorized response

## Solution: Make Ports Public

### Method 1: Using VS Code UI (RECOMMENDED)

1. **Open the PORTS panel** in VS Code:
   - Look at the bottom of your VS Code window
   - Click on the **"PORTS"** tab (next to Terminal, Problems, etc.)

2. **Change port 5090 (Backend) to Public**:
   - Find the row for port **5090** (Backend API)
   - Right-click on it
   - Select **"Port Visibility"** → **"Public"**
   - You should see a 🌐 globe icon next to the port

3. **Change port 4200 (Frontend) to Public**:
   - Find the row for port **4200** (Frontend)
   - Right-click on it
   - Select **"Port Visibility"** → **"Public"**
   - You should see a 🌐 globe icon next to the port

4. **Refresh your browser**:
   - Reload the frontend page
   - CORS errors should be gone!

### Method 2: Using Command Palette

1. Press `Ctrl+Shift+P` (Windows/Linux) or `Cmd+Shift+P` (Mac)
2. Type: **"Ports: Focus on Ports View"**
3. Follow the same steps as Method 1 to change visibility

### Method 3: Using devcontainer.json (For Future)

I've created a `.devcontainer/devcontainer.json` file that automatically sets ports as public when the container starts. However, this only applies to **new containers**, not the current session.

To use it:
1. Rebuild your dev container: `Ctrl+Shift+P` → "Dev Containers: Rebuild Container"
2. Or start a new Codespace

## Verification

After making ports public, test with these commands:

```bash
# Test backend API from external URL
curl -I https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api/device-webhook/ping

# Should return HTTP 200 with CORS headers like:
# access-control-allow-origin: https://repulsive-toad-7vjj6xv99745hrvj-4200.app.github.dev
```

You should see **200 OK** instead of **401 Unauthorized**.

## What Happens After the Fix

Once ports are public:
1. ✅ CORS preflight OPTIONS requests will succeed
2. ✅ Frontend can call backend APIs
3. ✅ Biometric devices page will load properly
4. ✅ API key generation will work
5. ✅ All CRUD operations will function

## Alternative: Use localhost (Development Only)

If you're testing locally in the same Codespace:
1. Frontend can use `http://localhost:5090` instead of the external URL
2. Update `environment.development.ts` to use localhost
3. This bypasses CORS entirely for local development

## Important Security Note

Making ports public means:
- ✅ **Good**: Your frontend and backend can communicate
- ⚠️ **Note**: Anyone with the URL can access your dev environment
- 🔒 **Protected**: Your app still requires JWT authentication
- 🌐 **Temporary**: This is only for your current Codespace session

For production deployment, you'll use a proper domain with CORS configured for your production URLs.

---

**Status**: Waiting for user to make ports public in GitHub Codespaces UI
