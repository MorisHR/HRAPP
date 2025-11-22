// ════════════════════════════════════════════════════════════════════════════
// K6 Load Test Configuration
// Fortune 500-Grade Performance Testing
// ════════════════════════════════════════════════════════════════════════════

export const config = {
  // Environment configuration
  baseUrl: __ENV.API_URL || 'https://api.yourdomain.com',

  // Test data
  adminCredentials: {
    email: __ENV.ADMIN_EMAIL || 'admin@hrms.com',
    password: __ENV.ADMIN_PASSWORD || 'Admin@123!',
  },

  tenantAdminCredentials: {
    email: __ENV.TENANT_ADMIN_EMAIL || 'tenant@example.com',
    password: __ENV.TENANT_ADMIN_PASSWORD || 'Tenant@123!',
  },

  employeeCredentials: {
    email: __ENV.EMPLOYEE_EMAIL || 'employee@example.com',
    password: __ENV.EMPLOYEE_PASSWORD || 'Employee@123!',
  },

  // Thresholds for performance metrics
  thresholds: {
    // HTTP metrics
    http_req_duration: ['p(95)<500', 'p(99)<1000'], // 95% < 500ms, 99% < 1s
    http_req_failed: ['rate<0.01'],                  // Error rate < 1%
    http_req_waiting: ['p(95)<400'],                 // Server response < 400ms

    // Iteration metrics
    iteration_duration: ['p(95)<2000'],              // Total iteration < 2s

    // Scenario-specific thresholds
    'http_req_duration{scenario:auth}': ['p(95)<200'],           // Auth < 200ms
    'http_req_duration{scenario:dashboard}': ['p(95)<1000'],     // Dashboard < 1s
    'http_req_duration{scenario:employees}': ['p(95)<800'],      // Employees < 800ms
    'http_req_duration{scenario:attendance}': ['p(95)<300'],     // Attendance < 300ms
  },

  // Load test scenarios
  scenarios: {
    // Smoke test - minimal load to verify functionality
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '1m',
    },

    // Load test - normal expected load
    load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 50 },   // Ramp up to 50 users
        { duration: '5m', target: 50 },   // Stay at 50 for 5 minutes
        { duration: '2m', target: 100 },  // Ramp up to 100
        { duration: '5m', target: 100 },  // Stay at 100 for 5 minutes
        { duration: '2m', target: 0 },    // Ramp down
      ],
      gracefulRampDown: '30s',
    },

    // Stress test - push beyond normal capacity
    stress: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 100 },   // Ramp up to 100
        { duration: '5m', target: 100 },   // Stay at 100
        { duration: '2m', target: 200 },   // Ramp to 200
        { duration: '5m', target: 200 },   // Stay at 200
        { duration: '2m', target: 300 },   // Push to 300
        { duration: '5m', target: 300 },   // Stay at 300
        { duration: '5m', target: 0 },     // Recovery
      ],
      gracefulRampDown: '1m',
    },

    // Spike test - sudden traffic spike
    spike: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 50 },    // Normal load
        { duration: '30s', target: 500 },  // Sudden spike
        { duration: '3m', target: 500 },   // Maintain spike
        { duration: '1m', target: 50 },    // Back to normal
        { duration: '2m', target: 0 },     // Ramp down
      ],
    },

    // Soak test - sustained load over time
    soak: {
      executor: 'constant-vus',
      vus: 100,
      duration: '1h',
    },

    // Breaking point test - find system limits
    breakingPoint: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      preAllocatedVUs: 500,
      maxVUs: 10000,
      stages: [
        { duration: '2m', target: 100 },   // 100 req/s
        { duration: '5m', target: 100 },   // Maintain
        { duration: '2m', target: 200 },   // 200 req/s
        { duration: '5m', target: 200 },   // Maintain
        { duration: '2m', target: 500 },   // 500 req/s
        { duration: '5m', target: 500 },   // Maintain
        { duration: '2m', target: 1000 },  // 1000 req/s
        { duration: '5m', target: 1000 },  // Maintain
      ],
    },
  },

  // Database query performance targets
  database: {
    queryTimeout: 1000,              // Max query time in ms
    connectionPoolMin: 10,
    connectionPoolMax: 100,
    expectedQueriesPerRequest: 5,    // Average queries per API call
  },

  // Redis cache performance targets
  redis: {
    hitRateTarget: 0.90,             // 90% cache hit rate
    avgResponseTime: 5,              // Average 5ms response
    maxResponseTime: 50,             // Max 50ms response
  },

  // Test data generation
  testData: {
    tenantsCount: 100,
    employeesPerTenant: 1000,
    attendanceRecordsPerDay: 500,
  },

  // Report configuration
  reporting: {
    outputFormat: 'json',
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
    enableCloudOutput: false,
  },
};

// Load test execution modes
export const testModes = {
  SMOKE: 'smoke',
  LOAD: 'load',
  STRESS: 'stress',
  SPIKE: 'spike',
  SOAK: 'soak',
  BREAKING_POINT: 'breakingPoint',
};

// Helper function to get scenario by mode
export function getScenario(mode) {
  return config.scenarios[mode] || config.scenarios.load;
}

// Helper to format test results
export function formatDuration(ms) {
  if (ms < 1000) return `${ms.toFixed(2)}ms`;
  return `${(ms / 1000).toFixed(2)}s`;
}

// Export for use in test scripts
export default config;
