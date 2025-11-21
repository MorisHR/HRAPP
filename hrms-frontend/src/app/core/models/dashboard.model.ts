// ═══════════════════════════════════════════════════════════════
// DASHBOARD MODELS - Production Grade
// Complete type definitions for admin dashboard
// ═══════════════════════════════════════════════════════════════

export interface DashboardStats {
  totalTenants: number;
  activeTenants: number;
  totalEmployees: number;
  monthlyRevenue: number;
  trends: DashboardTrends;
}

export interface DashboardTrends {
  tenantGrowth: TrendData;
  activeGrowth: TrendData;
  employeeGrowth: TrendData;
  revenueGrowth: TrendData;
}

export interface TrendData {
  value: number;
  percentChange: number;
  direction: 'up' | 'down' | 'stable';
  period: 'day' | 'week' | 'month' | 'quarter' | 'year';
}

export interface SystemHealth {
  status: 'healthy' | 'degraded' | 'down';
  timestamp: Date;
  services: ServiceHealth[];
  uptime: number;
  lastIncident?: Date;
}

export interface ServiceHealth {
  name: string;
  status: 'healthy' | 'degraded' | 'down';
  responseTime: number;
  uptime: number;
  lastCheck: Date;
  errorRate?: number;
  details?: string;
}

export interface ActivityLog {
  id: string;
  timestamp: Date;
  type: ActivityType;
  severity: 'info' | 'warning' | 'error' | 'success';
  title: string;
  description: string;
  userId?: string;
  userName?: string;
  tenantId?: string;
  tenantName?: string;
  metadata?: Record<string, any>;
}

export enum ActivityType {
  TenantCreated = 'tenant_created',
  TenantSuspended = 'tenant_suspended',
  TenantUpgraded = 'tenant_upgraded',
  TenantDowngraded = 'tenant_downgraded',
  SecurityAlert = 'security_alert',
  PaymentFailed = 'payment_failed',
  PaymentSuccess = 'payment_success',
  UserLogin = 'user_login',
  UserLogout = 'user_logout',
  SystemError = 'system_error',
  BackupCompleted = 'backup_completed',
  MaintenanceScheduled = 'maintenance_scheduled',
  // Infrastructure Events
  QueueOverload = 'queue_overload',
  JobFailed = 'job_failed',
  DatabaseSlowQuery = 'database_slow_query',
  DatabaseConnectionPoolExhausted = 'database_connection_pool_exhausted',
  RateLimitExceeded = 'rate_limit_exceeded',
  CacheMiss = 'cache_miss',
  CDNError = 'cdn_error',
  HighCPUUsage = 'high_cpu_usage',
  HighMemoryUsage = 'high_memory_usage',
  DiskSpaceWarning = 'disk_space_warning'
}

export interface SystemMetrics {
  timestamp: Date;
  apiMetrics: ApiMetrics;
  databaseMetrics: DatabaseMetrics;
  storageMetrics: StorageMetrics;
  performanceMetrics: PerformanceMetrics;
}

export interface ApiMetrics {
  totalRequests: number;
  successRate: number;
  averageResponseTime: number;
  requestsPerSecond: number;
  errorCount: number;
  topEndpoints: EndpointMetric[];
}

export interface EndpointMetric {
  path: string;
  method: string;
  requests: number;
  averageResponseTime: number;
  errorRate: number;
}

export interface DatabaseMetrics {
  connectionPoolSize: number;
  activeConnections: number;
  averageQueryTime: number;
  slowQueries: number;
  diskUsage: number;
  status: 'healthy' | 'degraded' | 'down';
}

export interface StorageMetrics {
  totalCapacityGB: number;
  usedCapacityGB: number;
  availableCapacityGB: number;
  utilizationPercent: number;
  largestTenants: TenantStorageUsage[];
}

export interface TenantStorageUsage {
  tenantId: string;
  tenantName: string;
  storageUsedGB: number;
  percentOfTotal: number;
}

export interface PerformanceMetrics {
  cpuUsage: number;
  memoryUsage: number;
  diskIOPS: number;
  networkThroughputMBps: number;
}

export interface DashboardChartData {
  labels: string[];
  datasets: DashboardChartDataset[];
}

export interface DashboardChartDataset {
  label: string;
  data: number[];
  backgroundColor?: string | string[];
  borderColor?: string | string[];
  borderWidth?: number;
  fill?: boolean;
  tension?: number;
}

export interface TimeSeriesData {
  timestamp: Date;
  value: number;
}

export interface TenantGrowthData {
  period: Date;
  totalTenants: number;
  activeTenants: number;
  newTenants: number;
  churnedTenants: number;
}

export interface RevenueData {
  period: Date;
  totalRevenue: number;
  recurringRevenue: number;
  newRevenue: number;
  churnRevenue: number;
}

export interface CriticalAlert {
  id: string;
  timestamp: Date;
  severity: 'critical' | 'high' | 'medium' | 'low';
  title: string;
  description: string;
  source: string;
  acknowledged: boolean;
  resolvedAt?: Date;
  assignedTo?: string;
  actions: AlertAction[];
}

export interface AlertAction {
  label: string;
  action: string;
  primary: boolean;
}

// ═══════════════════════════════════════════════════════════════
// INFRASTRUCTURE MONITORING MODELS
// ═══════════════════════════════════════════════════════════════

export interface InfrastructureMetrics {
  timestamp: Date;
  queueMetrics: QueueMetrics;
  databasePerformance: DatabasePerformanceMetrics;
  apiRateLimiting: ApiRateLimitingMetrics;
  cacheMetrics: CacheMetrics;
  cdnMetrics: CdnMetrics;
  backgroundJobs: BackgroundJobMetrics;
}

export interface QueueMetrics {
  totalQueues: number;
  activeJobs: number;
  pendingJobs: number;
  failedJobs: number;
  completedJobsToday: number;
  averageProcessingTime: number; // milliseconds
  oldestJobAge: number; // seconds
  queues: QueueDetails[];
}

export interface QueueDetails {
  name: string;
  pending: number;
  active: number;
  failed: number;
  completed: number;
  averageWaitTime: number;
  status: 'healthy' | 'warning' | 'critical';
}

export interface BackgroundJobMetrics {
  totalJobs: number;
  running: number;
  scheduled: number;
  failed: number;
  retrying: number;
  recentJobs: BackgroundJob[];
}

export interface BackgroundJob {
  id: string;
  name: string;
  status: 'running' | 'completed' | 'failed' | 'scheduled' | 'retrying';
  startedAt?: Date;
  completedAt?: Date;
  duration?: number;
  error?: string;
  progress?: number;
  tenantId?: string;
}

export interface DatabasePerformanceMetrics {
  connectionPool: ConnectionPoolMetrics;
  queryPerformance: QueryPerformanceMetrics;
  slowQueries: SlowQuery[];
  tenantSizes: TenantDatabaseSize[];
  overallHealth: 'healthy' | 'degraded' | 'critical';
}

export interface ConnectionPoolMetrics {
  maxConnections: number;
  activeConnections: number;
  idleConnections: number;
  waitingRequests: number;
  utilizationPercent: number;
  averageWaitTime: number;
  connectionErrors: number;
}

export interface QueryPerformanceMetrics {
  totalQueries: number;
  averageQueryTime: number;
  slowQueryCount: number;
  queryErrorRate: number;
  queriesPerSecond: number;
  topQueries: TopQuery[];
}

export interface SlowQuery {
  query: string;
  executionTime: number;
  timestamp: Date;
  database: string;
  tenantId?: string;
  rowsReturned: number;
}

export interface TopQuery {
  queryHash: string;
  queryTemplate: string;
  executionCount: number;
  averageTime: number;
  maxTime: number;
  minTime: number;
}

export interface TenantDatabaseSize {
  tenantId: string;
  tenantName: string;
  sizeGB: number;
  tableCount: number;
  rowCount: number;
  indexSizeGB: number;
  growthRate: number; // GB per month
}

export interface ApiRateLimitingMetrics {
  totalRequests: number;
  rateLimitedRequests: number;
  throttledRequests: number;
  rateLimitViolations: ApiRateViolation[];
  tenantRequestCounts: TenantApiUsage[];
  topEndpoints: EndpointRateLimit[];
}

export interface ApiRateViolation {
  timestamp: Date;
  tenantId: string;
  tenantName: string;
  endpoint: string;
  requestCount: number;
  limit: number;
  timeWindow: string;
  action: 'throttled' | 'blocked' | 'warned';
}

export interface TenantApiUsage {
  tenantId: string;
  tenantName: string;
  requestCount: number;
  rateLimitHits: number;
  averageResponseTime: number;
  errorRate: number;
  tier: string;
  limit: number;
  utilizationPercent: number;
}

export interface EndpointRateLimit {
  endpoint: string;
  method: string;
  requestCount: number;
  limit: number;
  violations: number;
  averageResponseTime: number;
}

export interface CacheMetrics {
  totalKeys: number;
  memoryUsedMB: number;
  memoryMaxMB: number;
  hitRate: number;
  missRate: number;
  evictions: number;
  connections: number;
  opsPerSecond: number;
  averageLatency: number;
  health: 'healthy' | 'degraded' | 'critical';
  cacheBreakdown: CacheBreakdown[];
}

export interface CacheBreakdown {
  category: string;
  keys: number;
  memoryMB: number;
  hitRate: number;
  ttlAverage: number;
}

export interface CdnMetrics {
  totalRequests: number;
  cacheHitRate: number;
  bandwidthGB: number;
  averageLatency: number;
  errorRate: number;
  origins: CdnOriginMetrics[];
  topAssets: CdnAssetMetrics[];
  geographicDistribution: GeoMetrics[];
}

export interface CdnOriginMetrics {
  origin: string;
  requests: number;
  cacheHitRate: number;
  errorRate: number;
  averageLatency: number;
}

export interface CdnAssetMetrics {
  path: string;
  requests: number;
  cacheHitRate: number;
  bandwidthGB: number;
  contentType: string;
}

export interface GeoMetrics {
  region: string;
  requests: number;
  bandwidthGB: number;
  averageLatency: number;
}
