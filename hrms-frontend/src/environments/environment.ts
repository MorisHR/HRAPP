export const environment = {
  production: false,
  apiUrl: 'https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api',
  appName: 'HRMS',
  version: '1.0.0',
  // SECURITY: Secret path must match backend appsettings.json Auth:SuperAdminSecretPath
  // Full endpoint: /api/auth/system-{secretPath}
  superAdminSecretPath: '732c44d0-d59b-494c-9fc0-bf1d65add4e5',

  // ============================================
  // FORTUNE 500 FEATURE FLAG CONFIGURATION
  // ============================================
  featureFlags: {
    // Feature flag API endpoint
    apiEndpoint: 'https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api/feature-flags',

    // Cache configuration
    cacheDurationMs: 5 * 60 * 1000, // 5 minutes

    // Refresh interval (auto-refresh feature flags)
    autoRefreshIntervalMs: 10 * 60 * 1000, // 10 minutes

    // Enable local storage caching for offline support
    enableCaching: true,

    // Enable debug logging
    enableDebugLogging: true
  },

  // ============================================
  // ANALYTICS CONFIGURATION
  // ============================================
  analytics: {
    // Enable analytics tracking
    enabled: true,

    // Error rate threshold for auto-rollback (0-1)
    errorRateThreshold: 0.05, // 5% error rate triggers rollback

    // Minimum samples before auto-rollback
    minSamplesForRollback: 10,

    // Enable console logging for analytics events
    enableLogging: true,

    // Enable backend reporting
    enableBackendReporting: true,

    // Batch size for analytics events
    batchSize: 50,

    // Flush interval for analytics events
    flushIntervalMs: 60000, // 1 minute

    // Analytics API endpoint
    apiEndpoint: 'https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api/analytics'
  },

  // ============================================
  // ERROR TRACKING CONFIGURATION
  // ============================================
  errorTracking: {
    // Enable error tracking
    enabled: true,

    // Error rate window in milliseconds
    errorRateWindowMs: 60000, // 1 minute

    // Error rate threshold (errors per minute)
    errorRateThreshold: 5,

    // Maximum stored errors in memory
    maxStoredErrors: 100,

    // Enable console error logging
    enableLogging: true,

    // Auto-report critical errors to backend
    autoReportCritical: true
  }
};
