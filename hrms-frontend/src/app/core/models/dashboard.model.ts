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
  MaintenanceScheduled = 'maintenance_scheduled'
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
