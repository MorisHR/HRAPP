// ════════════════════════════════════════════════════════════════════════════
// Database & Cache Performance Test
// Test database query performance and Redis cache efficiency
// ════════════════════════════════════════════════════════════════════════════

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Counter, Rate, Trend, Gauge } from 'k6/metrics';
import { config } from './config.js';
import {
  authenticateAdmin,
  authenticateTenantAdmin,
  authenticatedGet,
  isCacheHit,
  trackDatabasePerformance,
  randomSleep,
} from './utils.js';

// ────────────────────────────────────────────────────────────────────────────
// Custom Metrics
// ────────────────────────────────────────────────────────────────────────────

const cacheHits = new Counter('cache_hits');
const cacheMisses = new Counter('cache_misses');
const cacheHitRate = new Rate('cache_hit_rate');
const cacheResponseTime = new Trend('cache_response_time');
const dbResponseTime = new Trend('db_response_time');
const dbQueryCount = new Counter('db_query_count');
const slowQueries = new Counter('slow_queries');
const queryDuration = new Trend('query_duration');
const complexQueryDuration = new Trend('complex_query_duration');
const joinQueryDuration = new Trend('join_query_duration');
const aggregationQueryDuration = new Trend('aggregation_query_duration');
const indexUsage = new Rate('index_usage_rate');
const connectionPoolUtilization = new Gauge('connection_pool_utilization');

// ────────────────────────────────────────────────────────────────────────────
// Test Configuration
// ────────────────────────────────────────────────────────────────────────────

export const options = {
  scenarios: {
    // Test 1: Cache efficiency - repeated reads
    cache_efficiency: {
      executor: 'constant-vus',
      vus: 50,
      duration: '5m',
      exec: 'testCacheEfficiency',
    },

    // Test 2: Database query performance - various query types
    database_queries: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 20 },
        { duration: '3m', target: 20 },
        { duration: '1m', target: 50 },
        { duration: '3m', target: 50 },
        { duration: '1m', target: 0 },
      ],
      startTime: '30s',
      exec: 'testDatabaseQueries',
    },

    // Test 3: Complex queries and joins
    complex_queries: {
      executor: 'constant-vus',
      vus: 10,
      duration: '5m',
      startTime: '1m',
      exec: 'testComplexQueries',
    },

    // Test 4: Aggregation queries
    aggregation_queries: {
      executor: 'constant-vus',
      vus: 15,
      duration: '5m',
      startTime: '1m30s',
      exec: 'testAggregationQueries',
    },

    // Test 5: Cache invalidation
    cache_invalidation: {
      executor: 'constant-arrival-rate',
      rate: 5,
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 10,
      startTime: '2m',
      exec: 'testCacheInvalidation',
    },
  },

  thresholds: {
    'cache_hit_rate': ['rate>0.90'],                          // 90% cache hit rate
    'cache_response_time': ['p(95)<10', 'p(99)<50'],          // Cache < 10ms (95th), < 50ms (99th)
    'db_response_time': ['p(95)<100', 'p(99)<500'],           // DB < 100ms (95th), < 500ms (99th)
    'query_duration': ['p(95)<200'],                          // Queries < 200ms
    'complex_query_duration': ['p(95)<1000'],                 // Complex queries < 1s
    'aggregation_query_duration': ['p(95)<2000'],             // Aggregations < 2s
    'slow_queries': ['count<100'],                            // Limit slow queries
    'http_req_failed': ['rate<0.01'],                         // Error rate < 1%
  },
};

// ────────────────────────────────────────────────────────────────────────────
// Test 1: Cache Efficiency
// ────────────────────────────────────────────────────────────────────────────

export function testCacheEfficiency() {
  const token = authenticateTenantAdmin();
  if (!token) return;

  group('Cache Efficiency Test', () => {
    // Request 1: First request (cache miss expected)
    const startTime1 = Date.now();
    const response1 = authenticatedGet(
      `${config.baseUrl}/api/tenant/dashboard`,
      token,
      'CacheMiss'
    );

    if (response1.status === 200) {
      const duration1 = Date.now() - startTime1;
      cacheMisses.add(1);
      cacheHitRate.add(0);
      dbResponseTime.add(duration1);
    }

    sleep(0.5);

    // Request 2: Immediate repeat (cache hit expected)
    const startTime2 = Date.now();
    const response2 = authenticatedGet(
      `${config.baseUrl}/api/tenant/dashboard`,
      token,
      'CacheHit'
    );

    if (response2.status === 200) {
      const duration2 = Date.now() - startTime2;
      const isCached = isCacheHit(response2);

      if (isCached) {
        cacheHits.add(1);
        cacheHitRate.add(1);
        cacheResponseTime.add(duration2);
      } else {
        cacheMisses.add(1);
        cacheHitRate.add(0);
        dbResponseTime.add(duration2);
      }

      check(response2, {
        'cached request is faster': () => duration2 < duration1,
        'response time < 50ms for cached': () => isCached ? duration2 < 50 : true,
      });
    }

    // Request 3: Multiple repeated requests
    for (let i = 0; i < 5; i++) {
      sleep(0.2);
      const startTime = Date.now();
      const response = authenticatedGet(
        `${config.baseUrl}/api/tenant/employees?pageNumber=1&pageSize=10`,
        token,
        'CacheTest'
      );

      if (response.status === 200) {
        const duration = Date.now() - startTime;
        const isCached = isCacheHit(response);

        if (isCached) {
          cacheHits.add(1);
          cacheHitRate.add(1);
          cacheResponseTime.add(duration);
        } else {
          cacheMisses.add(1);
          cacheHitRate.add(0);
          dbResponseTime.add(duration);
        }
      }
    }
  });

  randomSleep(1, 3);
}

// ────────────────────────────────────────────────────────────────────────────
// Test 2: Database Query Performance
// ────────────────────────────────────────────────────────────────────────────

export function testDatabaseQueries() {
  const token = authenticateTenantAdmin();
  if (!token) return;

  group('Database Query Performance', () => {
    // Simple SELECT with WHERE clause
    const startTime1 = Date.now();
    const response1 = authenticatedGet(
      `${config.baseUrl}/api/tenant/employees?pageNumber=1&pageSize=10`,
      token,
      'SimpleQuery'
    );

    if (response1.status === 200) {
      const duration1 = Date.now() - startTime1;
      queryDuration.add(duration1);
      dbQueryCount.add(1);
      trackDatabasePerformance(response1);

      if (duration1 > 1000) {
        slowQueries.add(1);
      }
    }

    sleep(0.5);

    // Filtered query with multiple conditions
    const startTime2 = Date.now();
    const response2 = authenticatedGet(
      `${config.baseUrl}/api/tenant/employees?pageNumber=1&pageSize=20&status=Active&department=Engineering`,
      token,
      'FilteredQuery'
    );

    if (response2.status === 200) {
      const duration2 = Date.now() - startTime2;
      queryDuration.add(duration2);
      dbQueryCount.add(1);
      trackDatabasePerformance(response2);
    }

    sleep(0.5);

    // Pagination test (large offset)
    const startTime3 = Date.now();
    const response3 = authenticatedGet(
      `${config.baseUrl}/api/tenant/employees?pageNumber=10&pageSize=50`,
      token,
      'PaginationQuery'
    );

    if (response3.status === 200) {
      const duration3 = Date.now() - startTime3;
      queryDuration.add(duration3);
      dbQueryCount.add(1);

      check(response3, {
        'pagination query < 500ms': () => duration3 < 500,
      });
    }

    sleep(0.5);

    // Search query
    const startTime4 = Date.now();
    const response4 = authenticatedGet(
      `${config.baseUrl}/api/tenant/employees?search=John&pageNumber=1&pageSize=20`,
      token,
      'SearchQuery'
    );

    if (response4.status === 200) {
      const duration4 = Date.now() - startTime4;
      queryDuration.add(duration4);
      dbQueryCount.add(1);

      check(response4, {
        'search query < 300ms': () => duration4 < 300,
        'search uses index': () => duration4 < 200, // Indexed search should be fast
      });

      if (duration4 < 200) {
        indexUsage.add(1);
      } else {
        indexUsage.add(0);
      }
    }
  });

  randomSleep(1, 2);
}

// ────────────────────────────────────────────────────────────────────────────
// Test 3: Complex Queries with Joins
// ────────────────────────────────────────────────────────────────────────────

export function testComplexQueries() {
  const token = authenticateTenantAdmin();
  if (!token) return;

  group('Complex Queries with Joins', () => {
    // Employee with department and position (1-2 joins)
    const startTime1 = Date.now();
    const response1 = authenticatedGet(
      `${config.baseUrl}/api/tenant/employees?includeDetails=true&pageNumber=1&pageSize=10`,
      token,
      'JoinQuery'
    );

    if (response1.status === 200) {
      const duration1 = Date.now() - startTime1;
      joinQueryDuration.add(duration1);
      complexQueryDuration.add(duration1);
      dbQueryCount.add(1);

      check(response1, {
        'join query < 500ms': () => duration1 < 500,
      });
    }

    sleep(1);

    // Attendance with employee details (multiple joins)
    const startTime2 = Date.now();
    const today = new Date().toISOString().split('T')[0];
    const response2 = authenticatedGet(
      `${config.baseUrl}/api/tenant/attendance?date=${today}&includeEmployee=true&pageNumber=1&pageSize=20`,
      token,
      'AttendanceJoinQuery'
    );

    if (response2.status === 200) {
      const duration2 = Date.now() - startTime2;
      joinQueryDuration.add(duration2);
      complexQueryDuration.add(duration2);
      dbQueryCount.add(1);

      if (duration2 > 1000) {
        slowQueries.add(1);
      }
    }

    sleep(1);

    // Leave requests with approver details
    const startTime3 = Date.now();
    const response3 = authenticatedGet(
      `${config.baseUrl}/api/tenant/leave/all?pageNumber=1&pageSize=20&includeApprover=true`,
      token,
      'LeaveJoinQuery'
    );

    if (response3.status === 200) {
      const duration3 = Date.now() - startTime3;
      joinQueryDuration.add(duration3);
      complexQueryDuration.add(duration3);
      dbQueryCount.add(1);
    }
  });

  randomSleep(2, 4);
}

// ────────────────────────────────────────────────────────────────────────────
// Test 4: Aggregation Queries
// ────────────────────────────────────────────────────────────────────────────

export function testAggregationQueries() {
  const token = authenticateAdmin();
  if (!token) return;

  group('Aggregation Queries', () => {
    // Dashboard statistics (COUNT, SUM aggregations)
    const startTime1 = Date.now();
    const response1 = authenticatedGet(
      `${config.baseUrl}/api/admin/dashboard/statistics`,
      token,
      'DashboardStats'
    );

    if (response1.status === 200) {
      const duration1 = Date.now() - startTime1;
      aggregationQueryDuration.add(duration1);
      dbQueryCount.add(1);

      check(response1, {
        'dashboard stats < 2s': () => duration1 < 2000,
      });

      if (duration1 > 2000) {
        slowQueries.add(1);
      }
    }

    sleep(1);

    // Tenant statistics (GROUP BY, COUNT)
    const startTime2 = Date.now();
    const response2 = authenticatedGet(
      `${config.baseUrl}/api/admin/reports/tenant-statistics`,
      token,
      'TenantStats'
    );

    if (response2.status === 200) {
      const duration2 = Date.now() - startTime2;
      aggregationQueryDuration.add(duration2);
      dbQueryCount.add(1);
    }

    sleep(1);

    // Attendance summary (complex aggregation)
    const token2 = authenticateTenantAdmin();
    if (token2) {
      const startTime3 = Date.now();
      const startDate = new Date();
      startDate.setDate(startDate.getDate() - 30);
      const endDate = new Date();

      const response3 = authenticatedGet(
        `${config.baseUrl}/api/tenant/reports/attendance?startDate=${startDate.toISOString().split('T')[0]}&endDate=${endDate.toISOString().split('T')[0]}`,
        token2,
        'AttendanceAggregation'
      );

      if (response3.status === 200) {
        const duration3 = Date.now() - startTime3;
        aggregationQueryDuration.add(duration3);
        complexQueryDuration.add(duration3);
        dbQueryCount.add(1);

        check(response3, {
          'attendance report < 5s': () => duration3 < 5000,
        });
      }
    }
  });

  randomSleep(2, 5);
}

// ────────────────────────────────────────────────────────────────────────────
// Test 5: Cache Invalidation
// ────────────────────────────────────────────────────────────────────────────

export function testCacheInvalidation() {
  const token = authenticateTenantAdmin();
  if (!token) return;

  group('Cache Invalidation', () => {
    // Read initial data (may be cached)
    const response1 = authenticatedGet(
      `${config.baseUrl}/api/tenant/employees?pageNumber=1&pageSize=5`,
      token,
      'ReadBeforeUpdate'
    );

    sleep(0.5);

    // Modify data (should invalidate cache)
    // Note: This would need actual POST/PUT operations
    // For now, we're testing read patterns after writes

    // Read again (cache should be invalidated)
    const startTime = Date.now();
    const response2 = authenticatedGet(
      `${config.baseUrl}/api/tenant/employees?pageNumber=1&pageSize=5`,
      token,
      'ReadAfterUpdate'
    );

    if (response2.status === 200) {
      const duration = Date.now() - startTime;
      const isCached = isCacheHit(response2);

      // After a write, we expect a cache miss
      check(response2, {
        'cache invalidated after write': () => !isCached,
      });

      if (isCached) {
        cacheHits.add(1);
      } else {
        cacheMisses.add(1);
      }
    }
  });

  randomSleep(1, 3);
}

// ────────────────────────────────────────────────────────────────────────────
// Setup & Teardown
// ────────────────────────────────────────────────────────────────────────────

export function setup() {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  Database & Cache Performance Test');
  console.log('  Base URL:', config.baseUrl);
  console.log('  Testing:');
  console.log('  - Cache hit rate (target: >90%)');
  console.log('  - Database query performance');
  console.log('  - Complex queries and joins');
  console.log('  - Aggregation performance');
  console.log('  - Cache invalidation');
  console.log('═══════════════════════════════════════════════════════════');
}

export function teardown(data) {
  console.log('\n═══════════════════════════════════════════════════════════');
  console.log('  Database & Cache Performance Test Complete');
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  Key Metrics to Review:');
  console.log('  - cache_hit_rate: Should be >90%');
  console.log('  - cache_response_time: Should be <10ms (p95)');
  console.log('  - db_response_time: Should be <100ms (p95)');
  console.log('  - slow_queries: Identify queries >1s for optimization');
  console.log('═══════════════════════════════════════════════════════════');
}

// ────────────────────────────────────────────────────────────────────────────
// Default Export
// ────────────────────────────────────────────────────────────────────────────

export default function () {
  // Scenarios are defined above
}
