// ════════════════════════════════════════════════════════════════════════════
// K6 Utility Functions
// Common helpers for load testing
// ════════════════════════════════════════════════════════════════════════════

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter, Rate } from 'k6/metrics';
import { config } from './config.js';

// ────────────────────────────────────────────────────────────────────────────
// Custom Metrics
// ────────────────────────────────────────────────────────────────────────────

export const authDuration = new Trend('auth_duration');
export const dashboardLoadTime = new Trend('dashboard_load_time');
export const employeeCreationTime = new Trend('employee_creation_time');
export const attendanceRecordTime = new Trend('attendance_record_time');
export const cacheHitRate = new Rate('cache_hit_rate');
export const dbQueryDuration = new Trend('db_query_duration');
export const apiErrors = new Counter('api_errors');
export const authErrors = new Counter('auth_errors');

// ────────────────────────────────────────────────────────────────────────────
// Authentication
// ────────────────────────────────────────────────────────────────────────────

/**
 * Authenticate as SuperAdmin
 * @returns {string} JWT token
 */
export function authenticateAdmin() {
  const startTime = Date.now();

  const payload = JSON.stringify({
    email: config.adminCredentials.email,
    password: config.adminCredentials.password,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: { name: 'AdminAuth' },
  };

  const response = http.post(
    `${config.baseUrl}/api/auth/login`,
    payload,
    params
  );

  const success = check(response, {
    'admin auth status is 200': (r) => r.status === 200,
    'admin auth has token': (r) => r.json('token') !== undefined,
  });

  if (!success) {
    authErrors.add(1);
    console.error('Admin authentication failed:', response.status, response.body);
    return null;
  }

  const duration = Date.now() - startTime;
  authDuration.add(duration);

  return response.json('token');
}

/**
 * Authenticate as Tenant Admin
 * @returns {string} JWT token
 */
export function authenticateTenantAdmin() {
  const startTime = Date.now();

  const payload = JSON.stringify({
    email: config.tenantAdminCredentials.email,
    password: config.tenantAdminCredentials.password,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: { name: 'TenantAdminAuth' },
  };

  const response = http.post(
    `${config.baseUrl}/api/auth/tenant/login`,
    payload,
    params
  );

  const success = check(response, {
    'tenant auth status is 200': (r) => r.status === 200,
    'tenant auth has token': (r) => r.json('token') !== undefined,
  });

  if (!success) {
    authErrors.add(1);
    return null;
  }

  const duration = Date.now() - startTime;
  authDuration.add(duration);

  return response.json('token');
}

/**
 * Authenticate as Employee
 * @returns {string} JWT token
 */
export function authenticateEmployee() {
  const startTime = Date.now();

  const payload = JSON.stringify({
    email: config.employeeCredentials.email,
    password: config.employeeCredentials.password,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: { name: 'EmployeeAuth' },
  };

  const response = http.post(
    `${config.baseUrl}/api/auth/employee/login`,
    payload,
    params
  );

  const success = check(response, {
    'employee auth status is 200': (r) => r.status === 200,
    'employee auth has token': (r) => r.json('token') !== undefined,
  });

  if (!success) {
    authErrors.add(1);
    return null;
  }

  const duration = Date.now() - startTime;
  authDuration.add(duration);

  return response.json('token');
}

// ────────────────────────────────────────────────────────────────────────────
// HTTP Helpers
// ────────────────────────────────────────────────────────────────────────────

/**
 * Get authenticated request headers
 * @param {string} token - JWT token
 * @returns {object} Headers object
 */
export function getAuthHeaders(token) {
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };
}

/**
 * Make authenticated GET request
 * @param {string} url - Request URL
 * @param {string} token - JWT token
 * @param {string} tagName - Metric tag name
 * @returns {object} Response object
 */
export function authenticatedGet(url, token, tagName = 'GET') {
  const params = {
    headers: getAuthHeaders(token),
    tags: { name: tagName },
  };

  const response = http.get(url, params);

  check(response, {
    [`${tagName} status is 200`]: (r) => r.status === 200,
  });

  if (response.status !== 200) {
    apiErrors.add(1);
  }

  return response;
}

/**
 * Make authenticated POST request
 * @param {string} url - Request URL
 * @param {object} payload - Request payload
 * @param {string} token - JWT token
 * @param {string} tagName - Metric tag name
 * @returns {object} Response object
 */
export function authenticatedPost(url, payload, token, tagName = 'POST') {
  const params = {
    headers: getAuthHeaders(token),
    tags: { name: tagName },
  };

  const response = http.post(url, JSON.stringify(payload), params);

  check(response, {
    [`${tagName} status is 200 or 201`]: (r) => r.status === 200 || r.status === 201,
  });

  if (response.status !== 200 && response.status !== 201) {
    apiErrors.add(1);
  }

  return response;
}

/**
 * Make authenticated PUT request
 * @param {string} url - Request URL
 * @param {object} payload - Request payload
 * @param {string} token - JWT token
 * @param {string} tagName - Metric tag name
 * @returns {object} Response object
 */
export function authenticatedPut(url, payload, token, tagName = 'PUT') {
  const params = {
    headers: getAuthHeaders(token),
    tags: { name: tagName },
  };

  const response = http.put(url, JSON.stringify(payload), params);

  check(response, {
    [`${tagName} status is 200`]: (r) => r.status === 200,
  });

  if (response.status !== 200) {
    apiErrors.add(1);
  }

  return response;
}

/**
 * Make authenticated DELETE request
 * @param {string} url - Request URL
 * @param {string} token - JWT token
 * @param {string} tagName - Metric tag name
 * @returns {object} Response object
 */
export function authenticatedDelete(url, token, tagName = 'DELETE') {
  const params = {
    headers: getAuthHeaders(token),
    tags: { name: tagName },
  };

  const response = http.del(url, null, params);

  check(response, {
    [`${tagName} status is 200 or 204`]: (r) => r.status === 200 || r.status === 204,
  });

  if (response.status !== 200 && response.status !== 204) {
    apiErrors.add(1);
  }

  return response;
}

// ────────────────────────────────────────────────────────────────────────────
// Test Data Generation
// ────────────────────────────────────────────────────────────────────────────

/**
 * Generate random tenant data
 * @param {number} index - Tenant index
 * @returns {object} Tenant data
 */
export function generateTenantData(index) {
  return {
    name: `LoadTest Tenant ${index}`,
    subdomain: `loadtest${index}`,
    adminEmail: `admin${index}@loadtest.com`,
    adminPassword: 'LoadTest@123!',
    adminFirstName: `Admin${index}`,
    adminLastName: 'User',
    tier: 'Professional',
    maxEmployees: 1000,
    expiryDate: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString(),
  };
}

/**
 * Generate random employee data
 * @param {number} index - Employee index
 * @returns {object} Employee data
 */
export function generateEmployeeData(index) {
  const timestamp = Date.now();
  return {
    firstName: `Employee${index}`,
    lastName: `LoadTest${timestamp}`,
    email: `employee${index}_${timestamp}@loadtest.com`,
    phoneNumber: `+1${Math.floor(Math.random() * 9000000000 + 1000000000)}`,
    dateOfBirth: '1990-01-01',
    hireDate: '2024-01-01',
    jobTitle: 'Software Engineer',
    department: 'Engineering',
    salary: 75000 + (index % 50) * 1000,
    employeeStatus: 'Active',
  };
}

/**
 * Generate random attendance record
 * @param {string} employeeId - Employee ID
 * @returns {object} Attendance data
 */
export function generateAttendanceData(employeeId) {
  const now = new Date();
  const clockIn = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 9, 0, 0);

  return {
    employeeId: employeeId,
    clockIn: clockIn.toISOString(),
    clockOut: new Date(clockIn.getTime() + 8 * 60 * 60 * 1000).toISOString(),
    status: 'Present',
  };
}

/**
 * Generate random leave request
 * @param {string} employeeId - Employee ID
 * @returns {object} Leave request data
 */
export function generateLeaveRequest(employeeId) {
  const startDate = new Date();
  startDate.setDate(startDate.getDate() + 7);

  const endDate = new Date(startDate);
  endDate.setDate(endDate.getDate() + 3);

  return {
    employeeId: employeeId,
    leaveType: 'Vacation',
    startDate: startDate.toISOString().split('T')[0],
    endDate: endDate.toISOString().split('T')[0],
    reason: 'Load testing vacation request',
  };
}

// ────────────────────────────────────────────────────────────────────────────
// Performance Monitoring
// ────────────────────────────────────────────────────────────────────────────

/**
 * Check if response indicates cache hit
 * @param {object} response - HTTP response
 * @returns {boolean} True if cache hit
 */
export function isCacheHit(response) {
  const cacheHeader = response.headers['X-Cache'];
  const isCached = cacheHeader === 'HIT' || cacheHeader === 'hit';
  cacheHitRate.add(isCached ? 1 : 0);
  return isCached;
}

/**
 * Track database query performance from response headers
 * @param {object} response - HTTP response
 */
export function trackDatabasePerformance(response) {
  const dbTimeHeader = response.headers['X-Database-Time'];
  if (dbTimeHeader) {
    const dbTime = parseFloat(dbTimeHeader);
    dbQueryDuration.add(dbTime);
  }
}

/**
 * Random sleep between requests
 * @param {number} minSeconds - Minimum sleep time
 * @param {number} maxSeconds - Maximum sleep time
 */
export function randomSleep(minSeconds = 1, maxSeconds = 3) {
  const sleepTime = minSeconds + Math.random() * (maxSeconds - minSeconds);
  sleep(sleepTime);
}

/**
 * Think time - realistic user behavior
 */
export function thinkTime() {
  randomSleep(1, 5);
}

// ────────────────────────────────────────────────────────────────────────────
// Reporting Helpers
// ────────────────────────────────────────────────────────────────────────────

/**
 * Format bytes to human readable
 * @param {number} bytes - Bytes
 * @returns {string} Formatted string
 */
export function formatBytes(bytes) {
  if (bytes < 1024) return `${bytes}B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)}KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)}MB`;
}

/**
 * Format duration to human readable
 * @param {number} ms - Milliseconds
 * @returns {string} Formatted string
 */
export function formatDuration(ms) {
  if (ms < 1000) return `${ms.toFixed(2)}ms`;
  if (ms < 60000) return `${(ms / 1000).toFixed(2)}s`;
  return `${(ms / 60000).toFixed(2)}min`;
}

// ────────────────────────────────────────────────────────────────────────────
// Validation Helpers
// ────────────────────────────────────────────────────────────────────────────

/**
 * Validate response has expected fields
 * @param {object} response - HTTP response
 * @param {array} fields - Expected fields
 * @returns {boolean} True if valid
 */
export function validateResponseFields(response, fields) {
  const json = response.json();
  return fields.every(field => json.hasOwnProperty(field));
}

/**
 * Validate pagination response
 * @param {object} response - HTTP response
 * @returns {boolean} True if valid pagination
 */
export function validatePagination(response) {
  return validateResponseFields(response, ['items', 'totalCount', 'pageNumber', 'pageSize']);
}

/**
 * Validate error response format
 * @param {object} response - HTTP response
 * @returns {boolean} True if valid error format
 */
export function validateErrorResponse(response) {
  const json = response.json();
  return json.hasOwnProperty('error') || json.hasOwnProperty('message');
}

// Export all utilities
export default {
  // Auth
  authenticateAdmin,
  authenticateTenantAdmin,
  authenticateEmployee,

  // HTTP
  getAuthHeaders,
  authenticatedGet,
  authenticatedPost,
  authenticatedPut,
  authenticatedDelete,

  // Data generation
  generateTenantData,
  generateEmployeeData,
  generateAttendanceData,
  generateLeaveRequest,

  // Performance
  isCacheHit,
  trackDatabasePerformance,
  randomSleep,
  thinkTime,

  // Reporting
  formatBytes,
  formatDuration,

  // Validation
  validateResponseFields,
  validatePagination,
  validateErrorResponse,
};
