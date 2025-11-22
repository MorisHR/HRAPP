// ════════════════════════════════════════════════════════════════════════════
// Stress Test - 10,000+ Concurrent Users
// Find system limits and breaking points
// ════════════════════════════════════════════════════════════════════════════

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Counter, Rate, Trend, Gauge } from 'k6/metrics';
import { config } from './config.js';
import {
  authenticateAdmin,
  authenticateTenantAdmin,
  authenticateEmployee,
  authenticatedGet,
  authenticatedPost,
  randomSleep,
  thinkTime,
} from './utils.js';

// ────────────────────────────────────────────────────────────────────────────
// Custom Metrics
// ────────────────────────────────────────────────────────────────────────────

const concurrentUsers = new Gauge('concurrent_users');
const systemErrors = new Counter('system_errors');
const timeouts = new Counter('timeouts');
const successfulRequests = new Counter('successful_requests');
const failedRequests = new Counter('failed_requests');
const successRate = new Rate('success_rate');
const p99Latency = new Trend('p99_latency');
const connectionErrors = new Counter('connection_errors');
const serverErrors = new Counter('server_errors_5xx');

// ────────────────────────────────────────────────────────────────────────────
// Test Configuration - Progressive Load to 10,000+ Users
// ────────────────────────────────────────────────────────────────────────────

export const options = {
  scenarios: {
    // Stress test with ramping to find breaking point
    stress_test: {
      executor: 'ramping-arrival-rate',
      startRate: 10,
      timeUnit: '1s',
      preAllocatedVUs: 1000,
      maxVUs: 15000,
      stages: [
        // Phase 1: Warmup
        { duration: '2m', target: 50 },      // 50 req/s
        { duration: '3m', target: 50 },      // Hold

        // Phase 2: Normal load
        { duration: '2m', target: 100 },     // 100 req/s
        { duration: '3m', target: 100 },     // Hold

        // Phase 3: High load
        { duration: '2m', target: 500 },     // 500 req/s (~3,000 users)
        { duration: '5m', target: 500 },     // Hold

        // Phase 4: Extreme load
        { duration: '3m', target: 1000 },    // 1000 req/s (~6,000 users)
        { duration: '5m', target: 1000 },    // Hold

        // Phase 5: Breaking point
        { duration: '3m', target: 2000 },    // 2000 req/s (~10,000+ users)
        { duration: '5m', target: 2000 },    // Hold

        // Phase 6: Recovery
        { duration: '3m', target: 100 },     // Scale down
        { duration: '2m', target: 0 },       // Complete shutdown
      ],
    },
  },

  thresholds: {
    // Relaxed thresholds for stress testing
    'http_req_duration': ['p(95)<5000', 'p(99)<10000'],      // Allow higher latency
    'http_req_failed': ['rate<0.10'],                         // Allow 10% error rate
    'success_rate': ['rate>0.85'],                            // 85% success minimum
    'server_errors_5xx': ['count<1000'],                      // Limit server errors
    'timeouts': ['count<500'],                                // Limit timeouts
  },

  // Extended timeout for stress test
  httpDebug: 'full',
};

// ────────────────────────────────────────────────────────────────────────────
// Stress Test Scenario
// ────────────────────────────────────────────────────────────────────────────

export default function () {
  // Track concurrent users
  concurrentUsers.add(__VU);

  // Randomly select user type to simulate realistic traffic
  const userType = Math.random();
  let token;

  group('Authentication', () => {
    if (userType < 0.1) {
      // 10% SuperAdmins
      token = authenticateAdmin();
    } else if (userType < 0.3) {
      // 20% Tenant Admins
      token = authenticateTenantAdmin();
    } else {
      // 70% Employees
      token = authenticateEmployee();
    }

    if (!token) {
      failedRequests.add(1);
      successRate.add(0);
      systemErrors.add(1);
      return;
    }

    successfulRequests.add(1);
    successRate.add(1);
  });

  // Short think time during stress test
  sleep(0.5);

  // Simulate realistic user actions
  group('User Actions', () => {
    const actions = [
      // Dashboard access (most common)
      () => {
        const endpoint = userType < 0.1
          ? '/api/admin/dashboard/statistics'
          : userType < 0.3
          ? '/api/tenant/dashboard'
          : '/api/employee/dashboard';

        const response = authenticatedGet(
          `${config.baseUrl}${endpoint}`,
          token,
          'Dashboard'
        );

        trackResponse(response);
      },

      // List operations (common)
      () => {
        const endpoint = userType < 0.3
          ? '/api/tenants?pageNumber=1&pageSize=20'
          : '/api/tenant/employees?pageNumber=1&pageSize=20';

        const response = authenticatedGet(
          `${config.baseUrl}${endpoint}`,
          token,
          'List'
        );

        trackResponse(response);
      },

      // Create operations (less common)
      () => {
        if (userType >= 0.3) {
          // Employee actions
          const response = authenticatedPost(
            `${config.baseUrl}/api/employee/attendance/clock-in`,
            {},
            token,
            'ClockIn'
          );

          trackResponse(response);
        }
      },

      // Report generation (heavy operation)
      () => {
        if (userType < 0.3) {
          const today = new Date().toISOString().split('T')[0];
          const response = authenticatedGet(
            `${config.baseUrl}/api/tenant/reports/attendance?startDate=${today}&endDate=${today}`,
            token,
            'Report'
          );

          trackResponse(response);
        }
      },
    ];

    // Execute random action
    const action = actions[Math.floor(Math.random() * actions.length)];
    action();
  });

  // Minimal sleep to maximize throughput
  sleep(0.1 + Math.random() * 0.5);
}

// ────────────────────────────────────────────────────────────────────────────
// Helper Functions
// ────────────────────────────────────────────────────────────────────────────

function trackResponse(response) {
  const success = check(response, {
    'status is 2xx': (r) => r.status >= 200 && r.status < 300,
  });

  if (success) {
    successfulRequests.add(1);
    successRate.add(1);
    p99Latency.add(response.timings.duration);
  } else {
    failedRequests.add(1);
    successRate.add(0);

    if (response.status === 0) {
      // Connection error
      connectionErrors.add(1);
      timeouts.add(1);
    } else if (response.status >= 500) {
      // Server error
      serverErrors.add(1);
      systemErrors.add(1);
    } else if (response.status === 429) {
      // Rate limiting (expected under stress)
      console.log('Rate limited - system protecting itself');
    }
  }
}

// ────────────────────────────────────────────────────────────────────────────
// Setup & Teardown
// ────────────────────────────────────────────────────────────────────────────

export function setup() {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  STRESS TEST - Finding System Limits');
  console.log('  Base URL:', config.baseUrl);
  console.log('  Target: 10,000+ concurrent users');
  console.log('  Expected behavior: System may degrade gracefully');
  console.log('═══════════════════════════════════════════════════════════');

  // Verify system is accessible
  const healthCheck = http.get(`${config.baseUrl}/health`);
  if (healthCheck.status !== 200) {
    throw new Error('System health check failed - cannot proceed with stress test');
  }

  console.log('✓ Health check passed - starting stress test...\n');
}

export function teardown(data) {
  console.log('\n═══════════════════════════════════════════════════════════');
  console.log('  STRESS TEST COMPLETE');
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  Review metrics to identify:');
  console.log('  - Breaking point (when error rate exceeds 10%)');
  console.log('  - Performance degradation patterns');
  console.log('  - Resource bottlenecks (CPU, memory, DB connections)');
  console.log('  - Recovery time after load reduction');
  console.log('═══════════════════════════════════════════════════════════');
}

// ────────────────────────────────────────────────────────────────────────────
// Expected Outcomes
// ────────────────────────────────────────────────────────────────────────────
//
// NORMAL LOAD (< 500 req/s):
// - P95 latency < 500ms
// - Error rate < 1%
// - All features fully functional
//
// HIGH LOAD (500-1000 req/s):
// - P95 latency < 2s
// - Error rate < 5%
// - May see connection pool warnings
//
// EXTREME LOAD (1000-2000 req/s):
// - P95 latency < 5s
// - Error rate < 10%
// - Rate limiting may activate
// - Database connection pool may max out
//
// BREAKING POINT (> 2000 req/s):
// - System should degrade gracefully
// - Rate limiting should protect backend
// - Error responses should be fast (fail fast)
// - No cascading failures
//
// ════════════════════════════════════════════════════════════════════════════
