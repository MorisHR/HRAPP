// Audit Log Models and Interfaces

export enum AuditActionType {
  // Authentication (1-10)
  LOGIN_SUCCESS = 1,
  LOGIN_FAILED = 2,
  LOGOUT = 3,
  PASSWORD_CHANGED = 4,
  PASSWORD_RESET_REQUESTED = 5,
  PASSWORD_RESET_COMPLETED = 6,
  MFA_SETUP_STARTED = 7,
  MFA_SETUP_COMPLETED = 8,
  MFA_VERIFICATION_SUCCESS = 9,
  MFA_VERIFICATION_FAILED = 10,

  // Tenant Lifecycle (21-30)
  TENANT_CREATED = 21,
  TENANT_UPDATED = 22,
  TENANT_ACTIVATED = 23,
  TENANT_SUSPENDED = 24,
  TENANT_DELETED = 25,

  // Employee (31-40)
  EMPLOYEE_CREATED = 31,
  EMPLOYEE_UPDATED = 32,
  EMPLOYEE_DELETED = 33,
  EMPLOYEE_SALARY_UPDATED = 38,

  // Generic (111-120)
  RECORD_CREATED = 111,
  RECORD_UPDATED = 112,
  RECORD_DELETED = 113
}

export enum AuditCategory {
  AUTHENTICATION = 1,
  AUTHORIZATION = 2,
  DATA_CHANGE = 3,
  TENANT_LIFECYCLE = 4,
  SYSTEM_ADMIN = 5,
  SECURITY_EVENT = 6,
  SYSTEM_EVENT = 7,
  COMPLIANCE = 8
}

export enum AuditSeverity {
  INFO = 1,
  WARNING = 2,
  CRITICAL = 3,
  EMERGENCY = 4
}

export interface AuditLogFilter {
  tenantId?: string;
  startDate?: Date;
  endDate?: Date;
  userEmail?: string;
  actionTypes?: AuditActionType[];
  categories?: AuditCategory[];
  severities?: AuditSeverity[];
  entityType?: string;
  entityId?: string;
  success?: boolean;
  changedFieldsSearch?: string;
  ipAddress?: string;
  correlationId?: string;
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface AuditLog {
  id: string;
  tenantId?: string;
  tenantName?: string;
  userId?: string;
  userEmail?: string;
  userFullName?: string;
  userRole?: string;
  actionType: AuditActionType;
  actionTypeName: string;
  category: AuditCategory;
  categoryName: string;
  severity: AuditSeverity;
  severityName: string;
  entityType?: string;
  entityId?: string;
  success: boolean;
  changedFields?: string;
  ipAddress?: string;
  userAgent?: string;
  performedAt: Date;
  correlationId?: string;
}

export interface AuditLogDetail extends AuditLog {
  oldValues?: { [key: string]: any };
  newValues?: { [key: string]: any };
  additionalMetadata?: { [key: string]: any };
  reason?: string;
  requestPath?: string;
  httpMethod?: string;
  responseCode?: number;
  durationMs?: number;
  errorMessage?: string;
  sessionId?: string;
  deviceInfo?: string;
  geolocation?: string;
  parentActionId?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface AuditLogStatistics {
  totalActions: number;
  actionsToday: number;
  actionsThisWeek: number;
  actionsThisMonth: number;
  failedLogins: number;
  criticalEvents: number;
  warningEvents: number;
  actionsByCategory: { [key: string]: number };
  actionsBySeverity: { [key: string]: number };
  mostActiveUsers: TopUserActivity[];
  mostModifiedEntities: TopEntityActivity[];
}

export interface TopUserActivity {
  userEmail: string;
  userFullName?: string;
  actionCount: number;
  lastActivity: Date;
}

export interface TopEntityActivity {
  entityType: string;
  entityId?: string;
  changeCount: number;
  lastModified: Date;
}

export interface UserActivity {
  userEmail: string;
  userFullName?: string;
  totalActions: number;
  uniqueActionTypes: number;
  firstActivity: Date;
  lastActivity: Date;
  actionBreakdown: { [key: string]: number };
}

// Helper functions
export class AuditLogHelper {
  static getSeverityColor(severity: AuditSeverity): string {
    switch (severity) {
      case AuditSeverity.INFO:
        return 'info';
      case AuditSeverity.WARNING:
        return 'warning';
      case AuditSeverity.CRITICAL:
        return 'critical';
      case AuditSeverity.EMERGENCY:
        return 'emergency';
      default:
        return 'info';
    }
  }

  static getCategoryColor(category: AuditCategory): string {
    switch (category) {
      case AuditCategory.AUTHENTICATION:
        return 'authentication';
      case AuditCategory.AUTHORIZATION:
        return 'authorization';
      case AuditCategory.DATA_CHANGE:
        return 'data-change';
      case AuditCategory.TENANT_LIFECYCLE:
        return 'tenant-lifecycle';
      case AuditCategory.SYSTEM_ADMIN:
        return 'system-admin';
      case AuditCategory.SECURITY_EVENT:
        return 'security-event';
      case AuditCategory.SYSTEM_EVENT:
        return 'system-event';
      case AuditCategory.COMPLIANCE:
        return 'compliance';
      default:
        return 'data-change';
    }
  }

  static getSeverityIcon(severity: AuditSeverity): string {
    switch (severity) {
      case AuditSeverity.INFO:
        return 'info';
      case AuditSeverity.WARNING:
        return 'warning';
      case AuditSeverity.CRITICAL:
        return 'error';
      case AuditSeverity.EMERGENCY:
        return 'report';
      default:
        return 'info';
    }
  }

  static getActionTypeOptions(): { value: AuditActionType; label: string }[] {
    return [
      { value: AuditActionType.LOGIN_SUCCESS, label: 'Login Success' },
      { value: AuditActionType.LOGIN_FAILED, label: 'Login Failed' },
      { value: AuditActionType.LOGOUT, label: 'Logout' },
      { value: AuditActionType.EMPLOYEE_CREATED, label: 'Employee Created' },
      { value: AuditActionType.EMPLOYEE_UPDATED, label: 'Employee Updated' },
      { value: AuditActionType.EMPLOYEE_DELETED, label: 'Employee Deleted' },
      { value: AuditActionType.EMPLOYEE_SALARY_UPDATED, label: 'Salary Updated' },
      { value: AuditActionType.TENANT_CREATED, label: 'Tenant Created' },
      { value: AuditActionType.TENANT_UPDATED, label: 'Tenant Updated' },
      { value: AuditActionType.RECORD_CREATED, label: 'Record Created' },
      { value: AuditActionType.RECORD_UPDATED, label: 'Record Updated' },
      { value: AuditActionType.RECORD_DELETED, label: 'Record Deleted' },
    ];
  }

  static getCategoryOptions(): { value: AuditCategory; label: string }[] {
    return [
      { value: AuditCategory.AUTHENTICATION, label: 'Authentication' },
      { value: AuditCategory.AUTHORIZATION, label: 'Authorization' },
      { value: AuditCategory.DATA_CHANGE, label: 'Data Change' },
      { value: AuditCategory.TENANT_LIFECYCLE, label: 'Tenant Lifecycle' },
      { value: AuditCategory.SYSTEM_ADMIN, label: 'System Admin' },
      { value: AuditCategory.SECURITY_EVENT, label: 'Security Event' },
      { value: AuditCategory.SYSTEM_EVENT, label: 'System Event' },
      { value: AuditCategory.COMPLIANCE, label: 'Compliance' },
    ];
  }

  static getSeverityOptions(): { value: AuditSeverity; label: string }[] {
    return [
      { value: AuditSeverity.INFO, label: 'Info' },
      { value: AuditSeverity.WARNING, label: 'Warning' },
      { value: AuditSeverity.CRITICAL, label: 'Critical' },
      { value: AuditSeverity.EMERGENCY, label: 'Emergency' },
    ];
  }
}
