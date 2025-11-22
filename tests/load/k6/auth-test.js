// ════════════════════════════════════════════════════════════════════════════
// Authentication Load Test
// Tests login, token refresh, and session management
// ════════════════════════════════════════════════════════════════════════════

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import { config } from './config.js';
import {
  authenticateAdmin,
  authenticateTenantAdmin,
  authenticateEmployee,
  getAuthHeaders,
  randomSleep,
} from './utils.js';

// ────────────────────────────────────────────────────────────────────────────
// Custom Metrics
// ────────────────────────────────────────────────────────────────────────────

const loginAttempts = new Counter('login_attempts');
const loginSuccesses = new Counter('login_successes');
const loginFailures = new Counter('login_failures');
const loginDuration = new Trend('login_duration');
const tokenRefreshDuration = new Trend('token_refresh_duration');
const logoutDuration = new Trend('logout_duration');
const mfaVerifyDuration = new Trend('mfa_verify_duration');
const loginSuccessRate = new Rate('login_success_rate');

// ────────────────────────────────────────────────────────────────────────────
// Test Configuration
// ────────────────────────────────────────────────────────────────────────────

export const options = {
  scenarios: {
    // Admin login scenario
    admin_login: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 10 },   // Ramp up
        { duration: '1m', target: 10 },    // Sustain
        { duration: '30s', target: 20 },   // Ramp up more
        { duration: '2m', target: 20 },    // Sustain
        { duration: '30s', target: 0 },    // Ramp down
      ],
      exec: 'adminLoginScenario',
    },

    // Tenant admin login scenario
    tenant_admin_login: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 20 },   // Ramp up
        { duration: '2m', target: 20 },    // Sustain
        { duration: '30s', target: 50 },   // Ramp up more
        { duration: '2m', target: 50 },    // Sustain
        { duration: '30s', target: 0 },    // Ramp down
      ],
      startTime: '1m',
      exec: 'tenantAdminLoginScenario',
    },

    // Employee login scenario (highest volume)
    employee_login: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 50 },    // Ramp up
        { duration: '3m', target: 50 },    // Sustain
        { duration: '1m', target: 100 },   // Ramp up more
        { duration: '3m', target: 100 },   // Sustain
        { duration: '1m', target: 0 },     // Ramp down
      ],
      startTime: '2m',
      exec: 'employeeLoginScenario',
    },

    // Token refresh scenario
    token_refresh: {
      executor: 'constant-vus',
      vus: 30,
      duration: '5m',
      startTime: '3m',
      exec: 'tokenRefreshScenario',
    },
  },

  thresholds: {
    'login_duration': ['p(95)<200', 'p(99)<500'],            // Login < 200ms (95th), < 500ms (99th)
    'login_success_rate': ['rate>0.99'],                     // 99% success rate
    'token_refresh_duration': ['p(95)<150'],                 // Token refresh < 150ms
    'mfa_verify_duration': ['p(95)<300'],                    // MFA < 300ms
    'http_req_duration{scenario:admin_login}': ['p(95)<200'],
    'http_req_duration{scenario:tenant_admin_login}': ['p(95)<200'],
    'http_req_duration{scenario:employee_login}': ['p(95)<200'],
    'http_req_failed': ['rate<0.01'],                        // Error rate < 1%
  },
};

// ────────────────────────────────────────────────────────────────────────────
// Test Scenarios
// ────────────────────────────────────────────────────────────────────────────

/**
 * SuperAdmin Login Scenario
 */
export function adminLoginScenario() {
  group('Admin Login Flow', () => {
    const startTime = Date.now();
    loginAttempts.add(1);

    // Step 1: Login
    const token = authenticateAdmin();

    if (token) {
      loginSuccesses.add(1);
      loginSuccessRate.add(1);
      loginDuration.add(Date.now() - startTime);

      // Step 2: Verify token by accessing dashboard
      const dashboardResponse = http.get(
        `${config.baseUrl}/api/admin/dashboard/statistics`,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'AdminDashboard' },
        }
      );

      check(dashboardResponse, {
        'dashboard loaded successfully': (r) => r.status === 200,
        'dashboard has data': (r) => {
          const json = r.json();
          return json !== null && typeof json === 'object';
        },
      });

      // Step 3: Logout
      const logoutStart = Date.now();
      const logoutResponse = http.post(
        `${config.baseUrl}/api/auth/logout`,
        null,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'AdminLogout' },
        }
      );

      check(logoutResponse, {
        'logout successful': (r) => r.status === 200,
      });

      logoutDuration.add(Date.now() - logoutStart);
    } else {
      loginFailures.add(1);
      loginSuccessRate.add(0);
    }

    randomSleep(1, 3);
  });
}

/**
 * Tenant Admin Login Scenario
 */
export function tenantAdminLoginScenario() {
  group('Tenant Admin Login Flow', () => {
    const startTime = Date.now();
    loginAttempts.add(1);

    // Step 1: Login
    const token = authenticateTenantAdmin();

    if (token) {
      loginSuccesses.add(1);
      loginSuccessRate.add(1);
      loginDuration.add(Date.now() - startTime);

      // Step 2: Access tenant dashboard
      const dashboardResponse = http.get(
        `${config.baseUrl}/api/tenant/dashboard`,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'TenantDashboard' },
        }
      );

      check(dashboardResponse, {
        'tenant dashboard loaded': (r) => r.status === 200,
      });

      // Step 3: Access employee list
      const employeesResponse = http.get(
        `${config.baseUrl}/api/tenant/employees?pageNumber=1&pageSize=10`,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'TenantEmployeeList' },
        }
      );

      check(employeesResponse, {
        'employee list loaded': (r) => r.status === 200,
        'has pagination': (r) => {
          const json = r.json();
          return json.hasOwnProperty('totalCount') && json.hasOwnProperty('items');
        },
      });

      // Step 4: Logout
      const logoutResponse = http.post(
        `${config.baseUrl}/api/auth/logout`,
        null,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'TenantLogout' },
        }
      );

      check(logoutResponse, {
        'logout successful': (r) => r.status === 200,
      });
    } else {
      loginFailures.add(1);
      loginSuccessRate.add(0);
    }

    randomSleep(2, 5);
  });
}

/**
 * Employee Login Scenario
 */
export function employeeLoginScenario() {
  group('Employee Login Flow', () => {
    const startTime = Date.now();
    loginAttempts.add(1);

    // Step 1: Login
    const token = authenticateEmployee();

    if (token) {
      loginSuccesses.add(1);
      loginSuccessRate.add(1);
      loginDuration.add(Date.now() - startTime);

      // Step 2: Access employee dashboard
      const dashboardResponse = http.get(
        `${config.baseUrl}/api/employee/dashboard`,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'EmployeeDashboard' },
        }
      );

      check(dashboardResponse, {
        'employee dashboard loaded': (r) => r.status === 200,
      });

      // Step 3: Check attendance records
      const attendanceResponse = http.get(
        `${config.baseUrl}/api/employee/attendance?pageNumber=1&pageSize=10`,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'EmployeeAttendance' },
        }
      );

      check(attendanceResponse, {
        'attendance records loaded': (r) => r.status === 200,
      });

      // Step 4: Logout
      http.post(
        `${config.baseUrl}/api/auth/logout`,
        null,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'EmployeeLogout' },
        }
      );
    } else {
      loginFailures.add(1);
      loginSuccessRate.add(0);
    }

    randomSleep(1, 4);
  });
}

/**
 * Token Refresh Scenario
 */
export function tokenRefreshScenario() {
  group('Token Refresh Flow', () => {
    // Login first
    const token = authenticateEmployee();

    if (token) {
      // Simulate working for a while
      sleep(30);

      // Refresh token
      const refreshStart = Date.now();
      const refreshResponse = http.post(
        `${config.baseUrl}/api/auth/refresh-token`,
        null,
        {
          headers: getAuthHeaders(token),
          tags: { name: 'TokenRefresh' },
        }
      );

      check(refreshResponse, {
        'token refresh successful': (r) => r.status === 200,
        'new token received': (r) => r.json('token') !== undefined,
      });

      tokenRefreshDuration.add(Date.now() - refreshStart);

      if (refreshResponse.status === 200) {
        const newToken = refreshResponse.json('token');

        // Verify new token works
        const verifyResponse = http.get(
          `${config.baseUrl}/api/employee/dashboard`,
          {
            headers: getAuthHeaders(newToken),
            tags: { name: 'VerifyNewToken' },
          }
        );

        check(verifyResponse, {
          'new token is valid': (r) => r.status === 200,
        });
      }
    }

    randomSleep(2, 5);
  });
}

// ────────────────────────────────────────────────────────────────────────────
// Setup & Teardown
// ────────────────────────────────────────────────────────────────────────────

export function setup() {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  Authentication Load Test');
  console.log('  Base URL:', config.baseUrl);
  console.log('═══════════════════════════════════════════════════════════');
}

export function teardown(data) {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  Authentication Load Test Complete');
  console.log('═══════════════════════════════════════════════════════════');
}

// ────────────────────────────────────────────────────────────────────────────
// Default Export
// ────────────────────────────────────────────────────────────────────────────

export default function () {
  // This is intentionally empty - scenarios are defined above
  // K6 will execute the scenario-specific functions
}
