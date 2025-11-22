# Security Testing Checklist - HRMS Fortune 500

**Version:** 1.0.0
**Last Updated:** 2025-11-22
**Tester:** ___________________
**Date:** ___________________
**Environment:** ___________________

---

## ✅ PRE-TEST CHECKLIST

- [ ] Test environment is isolated from production
- [ ] Authorization obtained for penetration testing
- [ ] Backup of test database created
- [ ] Test user accounts created (admin, tenant admin, employee)
- [ ] API base URL confirmed: ___________________
- [ ] Testing tools installed (Burp Suite, Postman, curl)
- [ ] Results directory created

---

## 1. AUTHENTICATION & SESSION MANAGEMENT

### 1.1 Password Security
- [ ] **Test:** Try weak passwords (e.g., "password123")
- [ ] **Expected:** Rejected - Password complexity enforced
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Notes:** ___________________

- [ ] **Test:** Try common passwords from password dictionary
- [ ] **Expected:** Rejected
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Password minimum length (should be 8+ characters)
- [ ] **Expected:** Enforced
- [ ] **Result:** ☐ PASS ☐ FAIL

### 1.2 Brute Force Protection
- [ ] **Test:** Attempt 10+ failed logins rapidly
- [ ] **Expected:** Account locked or rate limited
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Lockout Duration:** _____ minutes

- [ ] **Test:** Check for CAPTCHA after failures
- [ ] **Expected:** CAPTCHA required after 3-5 failures
- [ ] **Result:** ☐ PASS ☐ FAIL

### 1.3 JWT Token Security
- [ ] **Test:** Decode JWT token and verify claims
- [ ] **Expected:** No sensitive data in payload
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Try using expired token
- [ ] **Expected:** 401 Unauthorized
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Try using modified token (change user ID)
- [ ] **Expected:** 401 Unauthorized - Signature invalid
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Token expiry time (should be ≤ 1 hour)
- [ ] **Expected:** Tokens expire in reasonable time
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Actual Expiry:** _____ minutes

### 1.4 Session Management
- [ ] **Test:** Check if session persists after logout
- [ ] **Expected:** Token invalidated, subsequent requests fail
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Check for concurrent sessions (login from 2 devices)
- [ ] **Expected:** Both sessions allowed OR old session invalidated
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Behavior:** ___________________

---

## 2. AUTHORIZATION & ACCESS CONTROL

### 2.1 Vertical Privilege Escalation
- [ ] **Test:** Employee tries to access admin endpoints
- [ ] **Example:** `GET /api/admin/dashboard/statistics`
- [ ] **Expected:** 403 Forbidden
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Tenant admin tries to access super admin endpoints
- [ ] **Example:** `GET /api/tenants` (list all tenants)
- [ ] **Expected:** 403 Forbidden
- [ ] **Result:** ☐ PASS ☐ FAIL

### 2.2 Horizontal Privilege Escalation
- [ ] **Test:** User A tries to access User B's data
- [ ] **Example:** Employee A views Employee B's payslip
- [ ] **Expected:** 403 Forbidden or filtered results
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Tenant A admin tries to access Tenant B data
- [ ] **Example:** Modify employee in different tenant
- [ ] **Expected:** 403 Forbidden
- [ ] **Result:** ☐ PASS ☐ FAIL

### 2.3 Direct Object Reference
- [ ] **Test:** Try sequential IDs (e.g., employee/1, employee/2, employee/3)
- [ ] **Expected:** GUIDs used OR authorization enforced
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Try accessing resources by guessing IDs
- [ ] **Expected:** 404 or 403, no data exposure
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 3. SQL INJECTION

### 3.1 Authentication Bypass
- [ ] **Test:** Login with `' OR '1'='1` in email field
- [ ] **Expected:** Login fails, SQL injection blocked
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Login with `admin'--` in email field
- [ ] **Expected:** Login fails
- [ ] **Result:** ☐ PASS ☐ FAIL

### 3.2 Query String Injection
- [ ] **Test:** Search with `'; DROP TABLE employees--`
- [ ] **Expected:** No SQL execution, safe error message
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Filter with `1' OR '1'='1`
- [ ] **Example:** `/api/employees?status=1' OR '1'='1`
- [ ] **Expected:** Invalid parameter error
- [ ] **Result:** ☐ PASS ☐ FAIL

### 3.3 Error-Based Injection
- [ ] **Test:** Inject `'` to trigger SQL error
- [ ] **Expected:** Generic error, no database details exposed
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 4. CROSS-SITE SCRIPTING (XSS)

### 4.1 Reflected XSS
- [ ] **Test:** Search with `<script>alert('XSS')</script>`
- [ ] **Expected:** Script not executed, HTML encoded
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Name field with `<img src=x onerror=alert(1)>`
- [ ] **Expected:** Script tags stripped or encoded
- [ ] **Result:** ☐ PASS ☐ FAIL

### 4.2 Stored XSS
- [ ] **Test:** Create employee with name `<script>alert('XSS')</script>`
- [ ] **Expected:** Data sanitized on save and display
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Leave request reason with malicious script
- [ ] **Expected:** Script not executed when viewed by admin
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 5. SECURITY HEADERS

### 5.1 HTTP Security Headers
- [ ] **Test:** Check for `X-Frame-Options` header
- [ ] **Expected:** `DENY` or `SAMEORIGIN`
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Value:** ___________________

- [ ] **Test:** Check for `X-Content-Type-Options` header
- [ ] **Expected:** `nosniff`
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Check for `Strict-Transport-Security` (HSTS)
- [ ] **Expected:** `max-age=31536000; includeSubDomains`
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Check for `Content-Security-Policy` (CSP)
- [ ] **Expected:** Defined with restrictive policy
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Policy:** ___________________

- [ ] **Test:** Check for `X-XSS-Protection`
- [ ] **Expected:** `1; mode=block`
- [ ] **Result:** ☐ PASS ☐ FAIL

### 5.2 CORS Configuration
- [ ] **Test:** Send request from unauthorized origin
- [ ] **Expected:** CORS error, request blocked
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Check `Access-Control-Allow-Origin` header
- [ ] **Expected:** Specific domains, not `*`
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 6. DATA VALIDATION

### 6.1 Input Validation
- [ ] **Test:** Submit oversized data (e.g., 10,000 character name)
- [ ] **Expected:** 400 Bad Request, length validation
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Submit wrong data type (string in number field)
- [ ] **Expected:** 400 Bad Request
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Submit negative numbers for salary
- [ ] **Expected:** Validation error
- [ ] **Result:** ☐ PASS ☐ FAIL

### 6.2 Email Validation
- [ ] **Test:** Submit invalid email formats
- [ ] **Expected:** Rejected with proper message
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 7. FILE UPLOAD SECURITY

### 7.1 File Type Validation
- [ ] **Test:** Upload executable file (.exe, .sh)
- [ ] **Expected:** Rejected
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Upload file with double extension (.jpg.php)
- [ ] **Expected:** Rejected or extension properly validated
- [ ] **Result:** ☐ PASS ☐ FAIL

### 7.2 File Size Limits
- [ ] **Test:** Upload extremely large file (>100MB)
- [ ] **Expected:** Rejected with size limit error
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 8. API RATE LIMITING

### 8.1 Rate Limits
- [ ] **Test:** Send 100 requests rapidly to login endpoint
- [ ] **Expected:** 429 Too Many Requests after threshold
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Limit:** _____ requests per _____

- [ ] **Test:** Rapid requests to API endpoints (CRUD)
- [ ] **Expected:** Rate limiting enforced
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 9. INFORMATION DISCLOSURE

### 9.1 Error Messages
- [ ] **Test:** Trigger 404 error
- [ ] **Expected:** Generic message, no stack trace
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Trigger 500 error
- [ ] **Expected:** No database schema or file paths exposed
- [ ] **Result:** ☐ PASS ☐ FAIL

### 9.2 Version Disclosure
- [ ] **Test:** Check HTTP headers for server version
- [ ] **Expected:** No `Server` or `X-Powered-By` headers
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 10. HTTPS & TLS

### 10.1 TLS Configuration
- [ ] **Test:** Check TLS version
- [ ] **Expected:** TLS 1.2 or 1.3 only
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Version:** ___________________

- [ ] **Test:** Test with SSL Labs (https://www.ssllabs.com/ssltest/)
- [ ] **Expected:** A or A+ rating
- [ ] **Result:** ☐ PASS ☐ FAIL
- [ ] **Rating:** ___________________

### 10.2 Certificate Validation
- [ ] **Test:** Check certificate validity
- [ ] **Expected:** Valid, not expired, correct domain
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 11. BUSINESS LOGIC

### 11.1 Attendance Manipulation
- [ ] **Test:** Clock in twice without clocking out
- [ ] **Expected:** Validation error
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Clock out before clocking in
- [ ] **Expected:** Validation error
- [ ] **Result:** ☐ PASS ☐ FAIL

### 11.2 Leave Request Abuse
- [ ] **Test:** Request leave for dates in the past
- [ ] **Expected:** Validation error
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Request more leave days than available balance
- [ ] **Expected:** Rejected
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## 12. TENANT ISOLATION

### 12.1 Data Isolation
- [ ] **Test:** Verify tenant schema isolation
- [ ] **Expected:** Tenant A cannot see Tenant B data
- [ ] **Result:** ☐ PASS ☐ FAIL

- [ ] **Test:** Try modifying tenant context in requests
- [ ] **Expected:** Rejected or ignored
- [ ] **Result:** ☐ PASS ☐ FAIL

---

## SUMMARY

**Total Tests:** _____
**Passed:** _____
**Failed:** _____
**Pass Rate:** _____%

**Critical Issues Found:** _____
**High Severity:** _____
**Medium Severity:** _____
**Low Severity:** _____

**Recommendation:** ☐ APPROVE FOR PRODUCTION ☐ FIX REQUIRED

**Tester Signature:** ___________________
**Date:** ___________________

---

## NOTES AND FINDINGS

Use this space for detailed notes on failures or interesting findings:

```
_______________________________________________________________________________
_______________________________________________________________________________
_______________________________________________________________________________
_______________________________________________________________________________
_______________________________________________________________________________
```
