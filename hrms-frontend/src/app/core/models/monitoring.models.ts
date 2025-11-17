/**
 * FORTUNE 50: Monitoring Dashboard TypeScript Models
 *
 * TypeScript interfaces matching the backend monitoring DTOs.
 * These models support real-time monitoring, performance tracking,
 * and security event analysis for the SuperAdmin dashboard.
 *
 * COMPLIANCE: ISO 27001, SOC 2, SOX monitoring requirements
 */

/**
 * Real-time dashboard metrics aggregated from all monitoring layers.
 * Provides high-level system health overview for SuperAdmin dashboard.
 */
export interface DashboardMetrics {
  /** Overall system health status */
  systemStatus: 'Healthy' | 'Degraded' | 'Critical' | 'Unknown';

  /** Database cache hit rate percentage (target: >95%) */
  cacheHitRate: number;

  /** Current active database connections */
  activeConnections: number;

  /** Connection pool utilization percentage (alert threshold: 80%) */
  connectionPoolUtilization: number;

  /** API P95 response time in milliseconds (SLA target: <200ms) */
  apiResponseTimeP95: number;

  /** API P99 response time in milliseconds (SLA target: <500ms) */
  apiResponseTimeP99: number;

  /** API error rate percentage (target: <0.1%) */
  apiErrorRate: number;

  /** Total active tenant count (tenants with activity in last 24h) */
  activeTenants: number;

  /** Total registered tenants in the system */
  totalTenants: number;

  /** Average schema switch time in milliseconds (target: <10ms) */
  avgSchemaSwitchTime: number;

  /** Count of active critical alerts requiring immediate attention */
  criticalAlerts: number;

  /** Count of active warning alerts for monitoring */
  warningAlerts: number;

  /** Failed authentication attempts in last hour (threshold: 100) */
  failedAuthAttemptsLastHour: number;

  /** IDOR prevention triggers in last hour (should be 0) */
  idorPreventionTriggersLastHour: number;

  /** Timestamp of the most recent metric snapshot */
  lastUpdated: Date;

  /** Timestamp of the next scheduled metric collection */
  nextUpdate: Date;
}

/**
 * Infrastructure layer health metrics.
 * Monitors database performance, connection pooling, and system resources.
 */
export interface InfrastructureHealth {
  /** PostgreSQL database version */
  databaseVersion: string;

  /** Current database uptime in hours */
  uptimeHours: number;

  /** Current cache hit rate percentage (target: >95%) */
  cacheHitRate: number;

  /** Cache hit rate status */
  cacheHitRateStatus: 'Excellent' | 'Good' | 'Warning' | 'Unknown';

  /** Total number of connections (active + idle) */
  totalConnections: number;

  /** Number of active connections executing queries */
  activeConnections: number;

  /** Number of idle connections in the pool */
  idleConnections: number;

  /** Maximum allowed connections (PostgreSQL limit) */
  maxConnections: number;

  /** Connection pool utilization percentage */
  connectionUtilization: number;

  /** Total database size in bytes */
  databaseSizeBytes: number;

  /** Human-readable database size (e.g., "2.5 GB") */
  databaseSizeFormatted: string;

  /** Disk I/O read operations per second */
  diskReadsPerSecond: number;

  /** Disk I/O write operations per second */
  diskWritesPerSecond: number;

  /** Average query execution time in milliseconds */
  avgQueryTimeMs: number;

  /** Number of queries executed in last collection interval */
  queriesExecuted: number;

  /** Number of deadlocks detected (should be 0) */
  deadlocks: number;

  /** List of top 5 slowest queries */
  topSlowQueries: SlowQuery[];

  /** Last health check timestamp */
  lastChecked: Date;

  // System Resource Metrics (Fortune 500 Standard)

  /** Database server CPU usage percentage (0-100) */
  cpuUsagePercent: number;

  /** Database server memory usage percentage (0-100) */
  memoryUsagePercent: number;

  /** Database disk usage percentage (0-100) */
  diskUsagePercent: number;

  /** Average network latency to database in milliseconds */
  networkLatencyMs: number;

  /** Number of active database connections (currently executing queries) */
  dbConnections: number;
}

/**
 * API endpoint performance metrics.
 * Tracks response times, throughput, and error rates per endpoint.
 * SLA TARGETS: P95 <200ms, P99 <500ms, Error Rate <0.1%
 */
export interface ApiPerformance {
  /** API endpoint path (e.g., "/api/employees") */
  endpoint: string;

  /** HTTP method (GET, POST, PUT, DELETE, PATCH) */
  httpMethod: string;

  /** Tenant subdomain (for multi-tenant analysis), null for non-tenant endpoints */
  tenantSubdomain: string | null;

  /** Total number of requests in the measurement period */
  totalRequests: number;

  /** Number of successful requests (2xx status codes) */
  successfulRequests: number;

  /** Number of failed requests (4xx + 5xx status codes) */
  failedRequests: number;

  /** Error rate percentage (failed / total * 100) */
  errorRate: number;

  /** Average response time in milliseconds */
  avgResponseTimeMs: number;

  /** Median (P50) response time in milliseconds */
  p50ResponseTimeMs: number;

  /** P95 response time in milliseconds (SLA target: <200ms) */
  p95ResponseTimeMs: number;

  /** P99 response time in milliseconds (SLA target: <500ms) */
  p99ResponseTimeMs: number;

  /** Minimum response time observed */
  minResponseTimeMs: number;

  /** Maximum response time observed */
  maxResponseTimeMs: number;

  /** Requests per second (throughput) */
  requestsPerSecond: number;

  /** Average request payload size in bytes */
  avgRequestSizeBytes: number | null;

  /** Average response payload size in bytes */
  avgResponseSizeBytes: number | null;

  /** Performance status based on P95 response time vs SLA */
  performanceStatus: 'Excellent' | 'Good' | 'Warning' | 'Critical' | 'Unknown';

  /** Start of measurement period */
  periodStart: Date;

  /** End of measurement period */
  periodEnd: Date;
}

/**
 * Per-tenant activity and resource utilization metrics.
 * Multi-tenant isolation monitoring and usage analytics.
 */
export interface TenantActivity {
  /** Tenant unique identifier */
  tenantId: string;

  /** Tenant subdomain (URL identifier) */
  subdomain: string;

  /** Tenant company name */
  companyName: string;

  /** Tenant subscription tier */
  tier: 'Starter' | 'Professional' | 'Enterprise';

  /** Total number of employees in this tenant */
  totalEmployees: number;

  /** Number of active users (logged in last 24h) */
  activeUsersLast24h: number;

  /** Total API requests from this tenant in the measurement period */
  totalRequests: number;

  /** Requests per second from this tenant */
  requestsPerSecond: number;

  /** Average response time for this tenant's requests (ms) */
  avgResponseTimeMs: number;

  /** Error rate for this tenant's requests (%) */
  errorRate: number;

  /** Database schema size in bytes */
  schemaSizeBytes: number;

  /** Human-readable schema size (e.g., "125 MB") */
  schemaSizeFormatted: string;

  /** Number of database queries executed by this tenant */
  databaseQueries: number;

  /** Average database query time for this tenant (ms) */
  avgQueryTimeMs: number;

  /** Storage utilization percentage (against tier limit) */
  storageUtilization: number;

  /** Last login timestamp for any user in this tenant */
  lastActivityAt: Date | null;

  /** Tenant creation date */
  createdAt: Date;

  /** Tenant status */
  status: 'Active' | 'Suspended' | 'Trial' | 'Churned';

  /** Health score (0-100) based on activity and performance */
  healthScore: number;

  /** Measurement period start */
  periodStart: Date;

  /** Measurement period end */
  periodEnd: Date;
}

/**
 * Security event and threat detection metrics.
 * Monitors authentication failures, IDOR attempts, and suspicious activity.
 * COMPLIANCE: SOC 2, ISO 27001, PCI-DSS security logging requirements
 */
export interface SecurityEvent {
  /** Unique event identifier */
  eventId: number;

  /** Event type (e.g., FailedLogin, IdorAttempt, RateLimitExceeded, UnauthorizedAccess) */
  eventType: string;

  /** Severity level */
  severity: 'Critical' | 'High' | 'Medium' | 'Low' | 'Info';

  /** User ID involved in the event (if applicable) */
  userId: string | null;

  /** User email (for display purposes) */
  userEmail: string | null;

  /** Source IP address */
  ipAddress: string | null;

  /** Tenant subdomain (for multi-tenant events) */
  tenantSubdomain: string | null;

  /** Resource ID attempted to access (for IDOR detection) */
  resourceId: string | null;

  /** API endpoint involved in the event */
  endpoint: string | null;

  /** Whether the event was blocked by security controls */
  isBlocked: boolean;

  /** Event description for human readability */
  description: string;

  /** Additional event details as JSON object */
  details: any | null;

  /** Timestamp when the event occurred */
  occurredAt: Date;

  /** Whether this event has been reviewed by security team */
  isReviewed: boolean;

  /** Notes from security team review */
  reviewNotes: string | null;

  /** Who reviewed this event */
  reviewedBy: string | null;

  /** When the event was reviewed */
  reviewedAt: Date | null;
}

/**
 * System alert tracking and management.
 * Monitors critical conditions, SLA violations, and security incidents.
 * COMPLIANCE: ISO 27001 (A.16 - Information security incident management)
 */
export interface Alert {
  /** Unique alert identifier */
  alertId: number;

  /**
   * Alert severity level
   * CRITICAL: Immediate action required (SLA breach, security incident)
   * HIGH: Investigation required within 1 hour
   * MEDIUM: Review within 4 hours
   * LOW: Review within 24 hours
   */
  severity: 'Critical' | 'High' | 'Medium' | 'Low' | 'Info';

  /** Alert type/category */
  alertType: 'Performance' | 'Security' | 'Availability' | 'Capacity' | 'Compliance';

  /** Alert title/summary for quick identification */
  title: string;

  /** Detailed alert message with context and metrics */
  message: string;

  /** Affected component or service (e.g., "API", "Database", "Tenant: acme") */
  source: string;

  /** Tenant subdomain (if alert is tenant-specific), null for system-wide alerts */
  tenantSubdomain: string | null;

  /** Metric that triggered the alert (e.g., "P95_RESPONSE_TIME", "ERROR_RATE") */
  triggerMetric: string | null;

  /** Threshold value that was exceeded */
  threshold: number | null;

  /** Actual metric value that triggered the alert */
  actualValue: number | null;

  /** Alert status */
  status: 'Active' | 'Acknowledged' | 'Resolved' | 'Suppressed';

  /** When the alert was first triggered */
  triggeredAt: Date;

  /** When the alert was acknowledged by an admin */
  acknowledgedAt: Date | null;

  /** Who acknowledged the alert */
  acknowledgedBy: string | null;

  /** When the alert was resolved (condition no longer exists) */
  resolvedAt: Date | null;

  /** Who resolved the alert */
  resolvedBy: string | null;

  /** Resolution notes and actions taken */
  resolutionNotes: string | null;

  /** Alert duration in seconds (ResolvedAt - TriggeredAt) */
  durationSeconds: number | null;

  /** Alert notification channels used (Email, SMS, Slack, PagerDuty) */
  notificationChannels: string | null;

  /** Whether alert was successfully delivered via configured channels */
  isNotified: boolean;

  /** Number of times this alert has been triggered (for recurring alerts) */
  occurrenceCount: number;

  /** Link to relevant documentation or runbook for this alert type */
  runbookUrl: string | null;
}

/**
 * Slow query analysis for database performance optimization.
 * Identifies problematic queries requiring optimization or indexing.
 * PERFORMANCE TARGET: Query execution time should be <50ms for 95% of queries
 */
export interface SlowQuery {
  /** Unique query identifier (hash of normalized query text) */
  queryId: string;

  /**
   * Normalized SQL query text (placeholders for parameters)
   * Example: "SELECT * FROM employees WHERE tenant_id = $1 AND department = $2"
   */
  queryText: string;

  /** Tenant subdomain (if query is tenant-scoped), null for cross-tenant queries */
  tenantSubdomain: string | null;

  /** Database schema where query executes (tenant schema or master) */
  schemaName: string | null;

  /** Total number of times this query has been executed */
  executionCount: number;

  /** Average execution time in milliseconds */
  avgExecutionTimeMs: number;

  /** Minimum execution time observed (ms) */
  minExecutionTimeMs: number;

  /** Maximum execution time observed (ms) */
  maxExecutionTimeMs: number;

  /** P95 execution time in milliseconds */
  p95ExecutionTimeMs: number;

  /** Total cumulative execution time for all executions (ms) */
  totalExecutionTimeMs: number;

  /** Average number of rows returned by this query */
  avgRowsReturned: number | null;

  /** Average number of rows scanned (indicates missing indexes if >> rows returned) */
  avgRowsScanned: number | null;

  /** Cache hit percentage for this query (0-100) */
  cacheHitRate: number | null;

  /** Whether this query performs sequential scans (indicates missing indexes) */
  hasSequentialScan: boolean;

  /** Number of tables joined in this query */
  joinCount: number | null;

  /** Query plan explanation (EXPLAIN output for optimization) */
  executionPlan: string | null;

  /** Optimization recommendations */
  optimizationSuggestion: string | null;

  /** Severity: Critical (>1000ms), High (500-1000ms), Medium (200-500ms), Low (<200ms) */
  severity: 'Critical' | 'High' | 'Medium' | 'Low';

  /** When this query was first detected as slow */
  firstDetected: Date;

  /** When this query was last executed */
  lastExecuted: Date;

  /** Whether this slow query has been reviewed by DBA/DevOps */
  isReviewed: boolean;

  /** Review notes and optimization actions taken */
  reviewNotes: string | null;
}

// ============================================
// API RESPONSE WRAPPERS
// ============================================

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  count?: number;
}

// ============================================
// QUERY PARAMETERS
// ============================================

export interface SlowQueriesParams {
  minExecutionTimeMs?: number;
  limit?: number;
}

export interface SlowQueriesByTenantParams extends SlowQueriesParams {
  tenantSubdomain: string;
}

export interface ApiPerformanceParams {
  periodStart?: string;
  periodEnd?: string;
  tenantSubdomain?: string;
  limit?: number;
}

export interface EndpointPerformanceParams {
  endpoint: string;
  httpMethod?: string;
  periodStart?: string;
  periodEnd?: string;
}

export interface SlaViolationsParams {
  periodStart?: string;
  periodEnd?: string;
}

export interface TenantActivityParams {
  periodStart?: string;
  periodEnd?: string;
  minActiveUsers?: number;
  status?: string;
  sortBy?: string;
  limit?: number;
}

export interface TenantActivityBySubdomainParams {
  tenantSubdomain: string;
  periodStart?: string;
  periodEnd?: string;
}

export interface AtRiskTenantsParams {
  maxHealthScore?: number;
  limit?: number;
}

export interface SecurityEventsParams {
  periodStart?: string;
  periodEnd?: string;
  eventType?: string;
  severity?: string;
  tenantSubdomain?: string;
  isBlocked?: boolean;
  isReviewed?: boolean;
  limit?: number;
}

export interface CriticalSecurityEventsParams {
  periodStart?: string;
  periodEnd?: string;
}

export interface AlertsParams {
  status?: string;
  severity?: string;
  alertType?: string;
  tenantSubdomain?: string;
  periodStart?: string;
  periodEnd?: string;
  limit?: number;
}

export interface SecurityEventReviewRequest {
  reviewNotes?: string;
}

export interface AlertResolutionRequest {
  resolutionNotes?: string;
}
