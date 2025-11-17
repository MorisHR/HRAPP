/**
 * FORTUNE 50-GRADE MONITORING MODELS
 * TypeScript interfaces for monitoring API responses
 *
 * COMPLIANCE: ISO 27001, SOC 2, NIST 800-53
 * SLA TARGETS: P95 <200ms, P99 <500ms, Cache Hit Rate >95%, Error Rate <0.1%
 */

// ============================================
// DASHBOARD OVERVIEW METRICS
// ============================================

export interface DashboardMetrics {
  totalTenants: number;
  activeTenants: number;
  totalUsers: number;
  activeUsers24h: number;
  avgResponseTimeMs: number;
  errorRate: number;
  cacheHitRate: number;
  databaseConnectionPoolUsage: number;
  criticalAlertsCount: number;
  slaViolationsCount: number;
  timestamp: string;
  cacheAge?: string;
}

// ============================================
// INFRASTRUCTURE HEALTH MONITORING
// ============================================

export interface InfrastructureHealth {
  database: DatabaseHealth;
  cache: CacheHealth;
  connectionPool: ConnectionPoolHealth;
  apiGateway: ApiGatewayHealth;
  timestamp: string;
}

export interface DatabaseHealth {
  status: 'Healthy' | 'Degraded' | 'Critical';
  masterDbConnectionCount: number;
  tenantDbConnectionCount: number;
  slowQueryCount: number;
  avgQueryExecutionTimeMs: number;
  p95QueryExecutionTimeMs: number;
  p99QueryExecutionTimeMs: number;
  deadlocksLast24h: number;
  tableLockWaitsLast24h: number;
}

export interface CacheHealth {
  status: 'Healthy' | 'Degraded' | 'Critical';
  hitRate: number;
  missRate: number;
  evictionRate: number;
  totalKeys: number;
  memoryUsageMb: number;
  avgGetLatencyMs: number;
}

export interface ConnectionPoolHealth {
  status: 'Healthy' | 'Degraded' | 'Critical';
  totalConnections: number;
  activeConnections: number;
  idleConnections: number;
  poolUtilizationPercent: number;
  connectionWaitTimeMs: number;
  connectionTimeoutsLast24h: number;
}

export interface ApiGatewayHealth {
  status: 'Healthy' | 'Degraded' | 'Critical';
  requestsPerSecond: number;
  avgResponseTimeMs: number;
  p95ResponseTimeMs: number;
  p99ResponseTimeMs: number;
  errorRate: number;
  activeRequests: number;
}

export interface SlowQuery {
  id: number;
  queryTemplate: string;
  tenantSubdomain?: string;
  avgExecutionTimeMs: number;
  maxExecutionTimeMs: number;
  executionCount: number;
  lastExecutedAt: string;
  endpoint?: string;
  httpMethod?: string;
}

// ============================================
// API PERFORMANCE MONITORING
// ============================================

export interface ApiPerformance {
  endpoint: string;
  httpMethod: string;
  tenantSubdomain?: string;
  requestCount: number;
  avgResponseTimeMs: number;
  p50ResponseTimeMs: number;
  p95ResponseTimeMs: number;
  p99ResponseTimeMs: number;
  errorCount: number;
  errorRate: number;
  successRate: number;
  throughputPerSecond: number;
  periodStart: string;
  periodEnd: string;
  slaViolation: boolean;
}

// ============================================
// MULTI-TENANT ACTIVITY MONITORING
// ============================================

export interface TenantActivity {
  tenantId: number;
  tenantSubdomain: string;
  tenantName: string;
  status: 'Active' | 'Suspended' | 'Trial' | 'Churned';
  activeUsers: number;
  totalUsers: number;
  requestVolume: number;
  errorCount: number;
  errorRate: number;
  avgResponseTimeMs: number;
  storageUsageMb: number;
  databaseSizeMb: number;
  healthScore: number;
  lastActivityAt: string;
  periodStart: string;
  periodEnd: string;
}

// ============================================
// SECURITY EVENT MONITORING
// ============================================

export interface SecurityEvent {
  id: number;
  eventType: SecurityEventType;
  severity: SecuritySeverity;
  tenantSubdomain?: string;
  userId?: number;
  userEmail?: string;
  ipAddress?: string;
  userAgent?: string;
  endpoint?: string;
  httpMethod?: string;
  description: string;
  metadata?: Record<string, any>;
  isBlocked: boolean;
  isReviewed: boolean;
  reviewedBy?: string;
  reviewedAt?: string;
  reviewNotes?: string;
  occurredAt: string;
}

export type SecurityEventType =
  | 'FailedLogin'
  | 'IdorAttempt'
  | 'RateLimitViolation'
  | 'UnauthorizedAccess'
  | 'SuspiciousActivity'
  | 'SqlInjectionAttempt'
  | 'XssAttempt'
  | 'MalformedRequest'
  | 'PrivilegeEscalation';

export type SecuritySeverity = 'Critical' | 'High' | 'Medium' | 'Low' | 'Info';

export interface SecurityEventReviewRequest {
  reviewNotes?: string;
}

// ============================================
// ALERT MANAGEMENT
// ============================================

export interface Alert {
  id: number;
  alertType: AlertType;
  severity: AlertSeverity;
  status: AlertStatus;
  title: string;
  description: string;
  tenantSubdomain?: string;
  affectedEndpoint?: string;
  metricValue?: number;
  thresholdValue?: number;
  metadata?: Record<string, any>;
  triggeredAt: string;
  acknowledgedAt?: string;
  acknowledgedBy?: string;
  resolvedAt?: string;
  resolvedBy?: string;
  resolutionNotes?: string;
}

export type AlertType =
  | 'Performance'
  | 'Security'
  | 'Availability'
  | 'Capacity'
  | 'Compliance';

export type AlertSeverity = 'Critical' | 'High' | 'Medium' | 'Low' | 'Info';

export type AlertStatus = 'Active' | 'Acknowledged' | 'Resolved' | 'Suppressed';

export interface AlertResolutionRequest {
  resolutionNotes?: string;
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
