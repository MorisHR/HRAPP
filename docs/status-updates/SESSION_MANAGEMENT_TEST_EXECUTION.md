# Enterprise Session Management - Test Execution Guide

## Overview
This document provides step-by-step instructions to manually test the enterprise session management implementation.

## Prerequisites
- ✅ Backend API running on http://localhost:5090
- ✅ Frontend application running on http://localhost:4200
- ✅ Valid SuperAdmin credentials (email: admin@morishr.com, password: your_password)
- ✅ Valid Tenant credentials (any tenant account)
- ✅ Browser: Chrome, Edge, or Firefox (latest version)
- ✅ Browser DevTools console open to monitor logs

## Test Environment Setup

### 1. Clear Browser State (Run Before Each Test)
```bash
# Open DevTools Console (F12)
# Run these commands:
localStorage.clear();
sessionStorage.clear();
location.reload();
```

### 2. Verify Backend is Running
```bash
curl http://localhost:5090/health
# Should return: HTTP 200 OK
```

### 3. Verify Frontend is Running
```bash
# Navigate to: http://localhost:4200
# Should load the landing page
```

---

## Test Suite 1: Auto-Redirect Feature

### Test 1.1: SuperAdmin Auto-Redirect from Login Page
**Objective:** Verify that authenticated SuperAdmin users cannot view the login page.

**Steps:**
1. Navigate to http://localhost:4200/auth/superadmin
2. Enter valid SuperAdmin credentials
3. Click "Sign In"
4. **Expected:** Redirected to /admin/dashboard
5. Now manually navigate back to http://localhost:4200/auth/superadmin
6. **Expected:**
   - Immediately redirected to /admin/dashboard
   - Console log: `✅ User already authenticated - redirecting to dashboard`
   - URL should never show /auth/superadmin

**Success Criteria:**
- ✅ Cannot access login page while authenticated
- ✅ Redirect happens instantly (no flash of login form)
- ✅ Console shows authentication check log
- ✅ URL bar shows /admin/dashboard

---

### Test 1.2: Tenant User Auto-Redirect from Login Page
**Objective:** Verify that authenticated tenant users cannot view the login page.

**Steps:**
1. Clear browser state (localStorage.clear())
2. Navigate to http://localhost:4200/auth/subdomain
3. Enter subdomain (e.g., "acme")
4. Enter valid tenant credentials
5. Click "Sign In"
6. **Expected:** Redirected to /tenant/dashboard or /employee/dashboard
7. Now manually navigate to http://localhost:4200/auth/login
8. **Expected:**
   - Immediately redirected to dashboard
   - Console log: `✅ User already authenticated - redirecting to dashboard`

**Success Criteria:**
- ✅ Cannot access login page while authenticated
- ✅ Redirect happens instantly
- ✅ Appropriate dashboard based on user role

---

### Test 1.3: Expired Token Handling
**Objective:** Verify that expired tokens are properly cleared and user can log in again.

**Steps:**
1. Login as SuperAdmin
2. Open DevTools Console
3. Manually expire the token by editing localStorage:
   ```javascript
   // Get current token
   const token = localStorage.getItem('access_token');

   // Create an expired token (set exp to past timestamp)
   // This is a simplified test - you'll need to decode, modify, and re-encode the JWT
   // OR wait 15 minutes for natural expiry

   // Alternatively, set token expiry to past date
   localStorage.setItem('access_token', 'expired_token');
   ```
4. Navigate to http://localhost:4200/auth/superadmin
5. **Expected:**
   - Console log: `⚠️ Token expired - clearing auth state`
   - Login form is displayed (not redirected)
   - localStorage is cleared

**Success Criteria:**
- ✅ Expired token is detected
- ✅ Auth state is cleared
- ✅ User can log in again
- ✅ Console shows expiry detection log

---

## Test Suite 2: Inactivity Timeout Feature

### Test 2.1: 15-Minute Inactivity Timeout
**Objective:** Verify that user is automatically logged out after 15 minutes of inactivity.

**IMPORTANT:** For faster testing, temporarily modify the timeout values:

**Temporary Configuration Change:**
```typescript
// File: src/app/core/services/session-management.service.ts
// Lines 28-30

// ORIGINAL (for production):
private readonly INACTIVITY_TIMEOUT = 15 * 60 * 1000; // 15 minutes
private readonly WARNING_TIME = 1 * 60 * 1000; // 1 minute before timeout
private readonly ACTIVITY_DEBOUNCE = 1000; // 1 second

// CHANGE TO (for testing):
private readonly INACTIVITY_TIMEOUT = 2 * 60 * 1000; // 2 minutes (for testing)
private readonly WARNING_TIME = 30 * 1000; // 30 seconds before timeout (for testing)
private readonly ACTIVITY_DEBOUNCE = 1000; // 1 second
```

**After making this change, restart Angular dev server:**
```bash
# Stop current server (Ctrl+C)
npm start
```

**Test Steps:**
1. Login as SuperAdmin
2. Navigate to /admin/dashboard
3. **Do not touch mouse, keyboard, or interact with the page**
4. Wait 1 minute 30 seconds (90 seconds with test config)
5. **Expected at 90 seconds:**
   - Warning modal appears
   - Title: "Session Expiring Soon"
   - Message: "Your session will expire in 30 seconds due to inactivity"
   - Two buttons: "Stay Logged In" and "Logout Now"
   - Countdown timer shows remaining time
   - Progress bar animates from 100% to 0%
6. Wait another 30 seconds without interaction
7. **Expected at 120 seconds (2 minutes):**
   - Automatic logout
   - Redirected to /auth/superadmin
   - Console log: `🚪 AUTO-LOGOUT: User inactive for 2 minutes`
   - localStorage cleared
   - Warning modal closes

**Success Criteria:**
- ✅ Warning modal appears at 1:30 mark (with test config)
- ✅ Modal shows accurate countdown (30 seconds → 0)
- ✅ Progress bar animates smoothly
- ✅ Automatic logout at 2:00 mark
- ✅ User redirected to correct login page
- ✅ All auth data cleared

**IMPORTANT:** After testing, **restore the original values** to 15 minutes!

---

### Test 2.2: "Stay Logged In" Button
**Objective:** Verify that clicking "Stay Logged In" extends the session.

**Test Steps:**
1. Login as SuperAdmin
2. Wait for warning modal to appear (1:30 with test config)
3. Click "Stay Logged In" button
4. **Expected:**
   - Modal closes immediately
   - Session timer resets to 0
   - Console log: `✅ Session extended by user action`
   - User remains on dashboard
5. Wait again for 1:30
6. Warning modal should appear again
7. **Expected:**
   - Modal appears again after another 1:30 of inactivity

**Success Criteria:**
- ✅ Modal closes on button click
- ✅ Session timer resets to 0
- ✅ User remains authenticated
- ✅ Warning can appear again after another inactivity period

---

### Test 2.3: "Logout Now" Button
**Objective:** Verify that clicking "Logout Now" immediately logs out the user.

**Test Steps:**
1. Login as SuperAdmin
2. Wait for warning modal to appear (1:30 with test config)
3. Click "Logout Now" button
4. **Expected:**
   - Immediate logout
   - Redirected to /auth/superadmin
   - Console log: `🚪 MANUAL LOGOUT: User clicked 'Logout Now' during session warning`
   - Modal closes
   - localStorage cleared

**Success Criteria:**
- ✅ Immediate logout on button click
- ✅ Proper redirect
- ✅ Console shows manual logout log
- ✅ Auth data cleared

---

## Test Suite 3: Activity Detection

### Test 3.1: Mouse Movement Resets Timer
**Objective:** Verify that mouse movement counts as activity and resets the inactivity timer.

**Test Steps:**
1. Login as SuperAdmin
2. Wait 1 minute without activity
3. Move mouse cursor anywhere on the page
4. **Expected:**
   - Inactivity timer resets to 0
   - Console log: `🔄 User activity detected - timer reset`
5. Wait another 1:30 (warning should not appear yet)
6. **Expected:**
   - Warning modal does NOT appear (because timer was reset)

**Success Criteria:**
- ✅ Mouse movement resets timer
- ✅ Console shows activity detection
- ✅ Warning delayed by mouse movement

---

### Test 3.2: Keyboard Input Resets Timer
**Objective:** Verify that keyboard input counts as activity.

**Test Steps:**
1. Login as SuperAdmin
2. Navigate to a page with a text input (e.g., tenant form)
3. Wait 1 minute without activity
4. Type in any text field
5. **Expected:**
   - Inactivity timer resets to 0
   - Console log: `🔄 User activity detected - timer reset`

**Success Criteria:**
- ✅ Keyboard input resets timer
- ✅ Console shows activity detection

---

### Test 3.3: Scrolling Resets Timer
**Objective:** Verify that scrolling counts as activity.

**Test Steps:**
1. Login as SuperAdmin
2. Navigate to a page with scrollable content
3. Wait 1 minute without activity
4. Scroll up or down
5. **Expected:**
   - Inactivity timer resets to 0
   - Console log: `🔄 User activity detected - timer reset`

**Success Criteria:**
- ✅ Scrolling resets timer
- ✅ Console shows activity detection

---

### Test 3.4: API Calls Reset Timer
**Objective:** Verify that API requests count as activity.

**Test Steps:**
1. Login as SuperAdmin
2. Wait 1 minute without any interaction
3. Trigger an API call by:
   - Navigating to a different page (loads data)
   - Clicking "Refresh" on any list view
   - Performing any CRUD operation
4. **Expected:**
   - Inactivity timer resets to 0
   - Console logs:
     - `🔵 API Request: GET /api/...`
     - `🔄 User activity detected - API call`

**Success Criteria:**
- ✅ API calls reset timer
- ✅ Console shows API activity
- ✅ Both successful and failed API calls count as activity

---

### Test 3.5: Navigation Resets Timer
**Objective:** Verify that navigating between pages resets the timer.

**Test Steps:**
1. Login as SuperAdmin
2. Stay on /admin/dashboard for 1 minute
3. Navigate to /admin/tenants
4. **Expected:**
   - Inactivity timer resets to 0
   - Console log: `🔄 User activity detected - navigation`

**Success Criteria:**
- ✅ Navigation resets timer
- ✅ Console shows navigation activity

---

## Test Suite 4: Multi-Tab Synchronization

### Test 4.1: Activity in One Tab Extends All Tabs
**Objective:** Verify that user activity in any tab extends session in all tabs.

**Test Steps:**
1. Login as SuperAdmin in **Tab 1**
2. Open **Tab 2** and navigate to http://localhost:4200/admin/dashboard
3. **Tab 2** will auto-login (same browser session)
4. In **Tab 1**, wait 1 minute without activity
5. In **Tab 2**, move the mouse or type something
6. **Expected in Tab 1:**
   - Inactivity timer resets to 0
   - Console log: `🔄 Multi-tab activity sync: Activity detected in another tab`
7. **Expected in Tab 2:**
   - Inactivity timer resets to 0
   - Console log: `🔄 User activity detected - timer reset`

**Success Criteria:**
- ✅ Activity in Tab 2 extends session in Tab 1
- ✅ Both tabs show activity logs
- ✅ Warning does not appear in either tab

---

### Test 4.2: Warning Appears in All Tabs
**Objective:** Verify that the warning modal appears in all tabs simultaneously.

**Test Steps:**
1. Login as SuperAdmin
2. Open 2-3 tabs with the application
3. Wait 1:30 without any activity in any tab
4. **Expected in ALL tabs:**
   - Warning modal appears simultaneously
   - Countdown shows same remaining time
   - Progress bars all animate together

**Success Criteria:**
- ✅ Modal appears in all tabs
- ✅ Countdown synchronized across tabs
- ✅ Console logs show sync messages

---

### Test 4.3: "Stay Logged In" in One Tab Extends All Tabs
**Objective:** Verify that clicking "Stay Logged In" in one tab closes warning in all tabs.

**Test Steps:**
1. Login as SuperAdmin
2. Open **Tab 1** and **Tab 2**
3. Wait 1:30 for warning to appear in both tabs
4. In **Tab 1**, click "Stay Logged In"
5. **Expected in Tab 1:**
   - Modal closes
   - Session extended
   - Console log: `✅ Session extended by user action`
6. **Expected in Tab 2:**
   - Modal closes automatically
   - Session extended
   - Console log: `🔄 Multi-tab sync: Session extended in another tab`

**Success Criteria:**
- ✅ Modal closes in all tabs
- ✅ Session extended in all tabs
- ✅ Console shows sync logs

---

### Test 4.4: Logout in One Tab Logs Out All Tabs
**Objective:** Verify that logging out in one tab logs out all tabs.

**Test Steps:**
1. Login as SuperAdmin
2. Open **Tab 1** and **Tab 2**
3. In **Tab 1**, click logout (or click "Logout Now" during warning)
4. **Expected in Tab 1:**
   - Redirected to /auth/superadmin
   - Console log: `🚪 LOGOUT: Starting logout process`
5. **Expected in Tab 2:**
   - Automatically redirected to /auth/superadmin
   - Console log: `🔄 Multi-tab sync: Logout detected in another tab`
   - Auth data cleared

**Success Criteria:**
- ✅ Logout in Tab 1 triggers logout in Tab 2
- ✅ Both tabs redirect to login page
- ✅ Console shows sync logs
- ✅ localStorage cleared in all tabs

---

## Test Suite 5: Token Expiry Auto-Logout

### Test 5.1: Token Expiry Detected During Page Navigation
**Objective:** Verify that expired tokens are detected when navigating.

**Steps:**
1. Login as SuperAdmin
2. Manually expire the token in localStorage:
   ```javascript
   // Option 1: Replace with invalid token
   localStorage.setItem('access_token', 'invalid_token');

   // Option 2: Wait 15 minutes for real expiry
   ```
3. Navigate to a different page (e.g., /admin/tenants)
4. **Expected:**
   - Automatic logout
   - Redirected to /auth/superadmin
   - Console log: `⚠️ Token expired - auto logout`

**Success Criteria:**
- ✅ Expired token detected
- ✅ Automatic logout triggered
- ✅ Redirect to login page
- ✅ Auth data cleared

---

### Test 5.2: Token Expiry Detected by Periodic Check
**Objective:** Verify that the 1-minute periodic token check works.

**Steps:**
1. Login as SuperAdmin
2. Wait on the dashboard without any activity
3. After 15 minutes (or when token actually expires)
4. **Expected:**
   - Within 1 minute of token expiry, automatic logout
   - Console log: `⚠️ Token expiry check: Token has expired`
   - Redirected to /auth/superadmin

**Success Criteria:**
- ✅ Periodic check detects expiry
- ✅ Automatic logout within 1 minute of actual expiry
- ✅ Proper redirect and cleanup

---

## Test Suite 6: Production Readiness

### Test 6.1: Performance Impact
**Objective:** Verify that session management has minimal performance impact.

**Steps:**
1. Login as SuperAdmin
2. Open Chrome DevTools → Performance tab
3. Record performance for 30 seconds while:
   - Moving mouse
   - Typing
   - Scrolling
   - Navigating
4. Stop recording
5. **Expected:**
   - CPU usage < 5% for session management
   - No memory leaks
   - No jank or stuttering
   - Activity listeners debounced properly

**Success Criteria:**
- ✅ Minimal CPU usage
- ✅ No memory leaks
- ✅ Smooth user experience
- ✅ Debouncing works (max 1 activity log per second)

---

### Test 6.2: Mobile Device Testing
**Objective:** Verify session management works on mobile devices.

**Steps:**
1. Open Chrome DevTools → Toggle Device Toolbar (Ctrl+Shift+M)
2. Select mobile device (e.g., iPhone 12)
3. Login as SuperAdmin
4. Test touch events:
   - Touch screen (should reset timer)
   - Swipe to scroll (should reset timer)
   - Wait for warning modal
5. **Expected:**
   - Touch events count as activity
   - Warning modal displays properly on small screen
   - Buttons are accessible and clickable

**Success Criteria:**
- ✅ Touch events reset timer
- ✅ Modal responsive on mobile
- ✅ Buttons easy to tap
- ✅ Text readable on small screens

---

### Test 6.3: Console Log Verification
**Objective:** Verify all console logs are present and helpful for debugging.

**Steps:**
1. Open DevTools Console
2. Filter by "session" or "activity"
3. Perform various actions:
   - Login
   - Activity detection
   - Warning appearance
   - Session extension
   - Logout
4. **Expected Console Logs:**

```
✅ Session management started after login
🔄 User activity detected - timer reset
⏰ Session warning triggered (1 minute remaining)
✅ Session extended by user action
🔄 Multi-tab activity sync: Activity detected in another tab
⚠️ Token expiry check: Token has expired
🚪 AUTO-LOGOUT: User inactive for 15 minutes
🚪 MANUAL LOGOUT: User clicked 'Logout Now' during session warning
⏹️ Session management stopped
```

**Success Criteria:**
- ✅ All expected logs present
- ✅ Logs include useful context
- ✅ Timestamps accurate
- ✅ No error logs (except expected errors)

---

## Test Suite 7: Edge Cases

### Test 7.1: Rapid Tab Opening/Closing
**Objective:** Verify system handles rapid tab changes gracefully.

**Steps:**
1. Login as SuperAdmin
2. Rapidly open and close 5-10 tabs
3. **Expected:**
   - No errors in console
   - BroadcastChannel doesn't break
   - Active tab maintains session correctly

**Success Criteria:**
- ✅ No errors or crashes
- ✅ Session state remains consistent
- ✅ Memory cleaned up properly

---

### Test 7.2: Network Disconnection During Session
**Objective:** Verify behavior when network is disconnected.

**Steps:**
1. Login as SuperAdmin
2. Open DevTools → Network tab → Offline
3. Wait for warning modal
4. Click "Stay Logged In"
5. **Expected:**
   - Session extended locally
   - No errors (refresh token call will fail but that's OK)
   - When network reconnects, next API call will verify token

**Success Criteria:**
- ✅ Graceful handling of network errors
- ✅ Session management continues locally
- ✅ No crashes or freezes

---

### Test 7.3: Browser Refresh During Warning
**Objective:** Verify behavior when user refreshes browser while warning is shown.

**Steps:**
1. Login as SuperAdmin
2. Wait for warning modal
3. Press F5 to refresh browser
4. **Expected:**
   - Page refreshes
   - User still authenticated (localStorage persists)
   - Session restarts from 0
   - No warning modal on page load

**Success Criteria:**
- ✅ Refresh doesn't break authentication
- ✅ Session timer resets
- ✅ User can continue working

---

## Troubleshooting

### Issue: Warning modal doesn't appear
**Solution:**
1. Check browser console for errors
2. Verify session-management.service.ts is imported
3. Check that AuthService.startSession() is called after login
4. Verify MatDialog is properly configured in app

### Issue: Multi-tab sync doesn't work
**Solution:**
1. Ensure all tabs are on same domain (localhost:4200)
2. Check BroadcastChannel support in browser
3. Verify localStorage fallback is working
4. Check console for sync logs

### Issue: Activity detection doesn't reset timer
**Solution:**
1. Check that debouncing is configured correctly (1 second)
2. Verify event listeners are attached
3. Check console for activity logs
4. Ensure NgZone is properly configured

---

## Test Results Summary

| Test Suite | Test Case | Status | Notes |
|------------|-----------|--------|-------|
| Suite 1: Auto-Redirect | 1.1: SuperAdmin auto-redirect | ⏳ Pending | |
| | 1.2: Tenant auto-redirect | ⏳ Pending | |
| | 1.3: Expired token handling | ⏳ Pending | |
| Suite 2: Inactivity Timeout | 2.1: 15-min timeout | ⏳ Pending | |
| | 2.2: "Stay Logged In" | ⏳ Pending | |
| | 2.3: "Logout Now" | ⏳ Pending | |
| Suite 3: Activity Detection | 3.1: Mouse movement | ⏳ Pending | |
| | 3.2: Keyboard input | ⏳ Pending | |
| | 3.3: Scrolling | ⏳ Pending | |
| | 3.4: API calls | ⏳ Pending | |
| | 3.5: Navigation | ⏳ Pending | |
| Suite 4: Multi-Tab Sync | 4.1: Activity sync | ⏳ Pending | |
| | 4.2: Warning sync | ⏳ Pending | |
| | 4.3: Extension sync | ⏳ Pending | |
| | 4.4: Logout sync | ⏳ Pending | |
| Suite 5: Token Expiry | 5.1: Navigation detection | ⏳ Pending | |
| | 5.2: Periodic check | ⏳ Pending | |
| Suite 6: Production | 6.1: Performance | ⏳ Pending | |
| | 6.2: Mobile support | ⏳ Pending | |
| | 6.3: Console logs | ⏳ Pending | |
| Suite 7: Edge Cases | 7.1: Rapid tabs | ⏳ Pending | |
| | 7.2: Network disconnect | ⏳ Pending | |
| | 7.3: Browser refresh | ⏳ Pending | |

---

## Sign-Off

**Tester Name:** _____________________

**Date:** _____________________

**Overall Status:** ⏳ Pending / ✅ Pass / ❌ Fail

**Critical Issues Found:** _____________________

**Recommendations:** _____________________

---

## Post-Testing Actions

### 1. Restore Production Configuration
After testing with reduced timeout values, **restore original values**:

```typescript
// File: src/app/core/services/session-management.service.ts
private readonly INACTIVITY_TIMEOUT = 15 * 60 * 1000; // 15 minutes
private readonly WARNING_TIME = 1 * 60 * 1000; // 1 minute before timeout
```

### 2. Git Commit
```bash
git add .
git commit -m "feat: Enterprise session management implementation

- Auto-redirect authenticated users from login pages
- 15-minute inactivity timeout with 1-minute warning
- Multi-tab synchronization via BroadcastChannel
- Activity detection (mouse, keyboard, API, navigation)
- Token expiry validation
- Production-grade error handling
- Comprehensive test coverage"
```

### 3. Deploy to Staging
```bash
# Build for production
npm run build

# Deploy to staging environment
# (Your deployment process)
```

### 4. Monitor Production
- Watch for session-related errors in logs
- Monitor user experience feedback
- Track logout patterns (voluntary vs. timeout)
- Measure performance impact

---

**Generated:** 2025-11-08
**Version:** 1.0.0
**Status:** Ready for Execution
