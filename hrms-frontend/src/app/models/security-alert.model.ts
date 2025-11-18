// Security Alert Models and Interfaces

import { AuditActionType, AuditCategory, AuditSeverity } from './audit-log.model';

// Re-export from audit-log.model for convenience
export { AuditSeverity, AuditActionType, AuditCategory } from './audit-log.model';

// ChipColor type for UI components
export type ChipColor = 'primary' | 'success' | 'warning' | 'error' | 'neutral';

/**
 * Security alert types for real-time threat detection
 * Supports Fortune 500 compliance requirements (SOX, GDPR, ISO 27001, PCI-DSS)
 */
export enum SecurityAlertType {
  FAILED_LOGIN_THRESHOLD = 1,
  UNAUTHORIZED_ACCESS = 2,
  MASS_DATA_EXPORT = 3,
  AFTER_HOURS_ACCESS = 4,
  SALARY_CHANGE = 5,
  PRIVILEGE_ESCALATION = 6,
  GEOGRAPHIC_ANOMALY = 7,
  RAPID_HIGH_RISK_ACTIONS = 8,
  ACCOUNT_LOCKOUT = 9,
  IMPOSSIBLE_TRAVEL = 10,
  RATE_LIMIT_EXCEEDED = 11,
  SQL_INJECTION_ATTEMPT = 12,
  XSS_ATTEMPT = 13,
  CSRF_FAILURE = 14,
  SESSION_HIJACK = 15,
  MALICIOUS_FILE_UPLOAD = 16,
  DATA_BREACH = 17,
  INTEGRITY_VIOLATION = 18,
  COMPLIANCE_VIOLATION = 19,
  ML_ANOMALY = 20,
  GENERAL_SECURITY_EVENT = 99
}

/**
 * Security alert status for workflow management
 */
export enum SecurityAlertStatus {
  NEW = 1,
  ACKNOWLEDGED = 2,
  IN_PROGRESS = 3,
  RESOLVED = 4,
  FALSE_POSITIVE = 5,
  ESCALATED = 6,
  PENDING_REVIEW = 7,
  CLOSED = 8
}

/**
 * Security alert filter for list queries
 */
export interface SecurityAlertFilter {
  tenantId?: string;
  status?: SecurityAlertStatus;
  severity?: AuditSeverity;
  alertType?: SecurityAlertType;
  startDate?: Date;
  endDate?: Date;
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDescending?: boolean;
}

/**
 * Security alert main interface
 */
export interface SecurityAlert {
  // Primary Key
  id: string;

  // Alert Classification
  alertType: SecurityAlertType;
  alertTypeName: string;
  severity: AuditSeverity;
  severityName: string;
  category: AuditCategory;
  categoryName: string;
  status: SecurityAlertStatus;
  statusName: string;

  // Alert Details
  title: string;
  description: string;
  recommendedActions?: string;
  riskScore: number;

  // Related Audit Log
  auditLogId?: string;
  auditActionType?: AuditActionType;

  // User/Target Information
  tenantId?: string;
  tenantName?: string;
  userId?: string;
  userEmail?: string;
  userFullName?: string;
  userRole?: string;

  // Location Information
  ipAddress?: string;
  geolocation?: string;
  userAgent?: string;
  deviceInfo?: string;

  // Timestamps
  createdAt: Date;
  detectedAt: Date;
  acknowledgedAt?: Date;
  resolvedAt?: Date;

  // Workflow & Assignment
  acknowledgedBy?: string;
  acknowledgedByEmail?: string;
  resolvedBy?: string;
  resolvedByEmail?: string;
  resolutionNotes?: string;
  assignedTo?: string;
  assignedToEmail?: string;

  // Notification Tracking
  emailSent: boolean;
  emailSentAt?: Date;
  emailRecipients?: string;
  smsSent: boolean;
  smsSentAt?: Date;
  smsRecipients?: string;
  slackSent: boolean;
  slackSentAt?: Date;
  slackChannels?: string;
  siemSent: boolean;
  siemSentAt?: Date;
  siemSystem?: string;

  // Anomaly Detection Metadata
  detectionRule?: string;
  baselineMetrics?: string;
  currentMetrics?: string;
  deviationPercentage?: number;

  // Context & Metadata
  correlationId?: string;
  additionalMetadata?: string;
  tags?: string;

  // Compliance & Audit
  complianceFrameworks?: string;
  requiresEscalation: boolean;
  escalatedTo?: string;
  escalatedAt?: Date;
}

/**
 * Security alert detailed view (includes parsed JSON fields)
 */
export interface SecurityAlertDetail extends SecurityAlert {
  detectionRuleParsed?: { [key: string]: any };
  baselineMetricsParsed?: { [key: string]: any };
  currentMetricsParsed?: { [key: string]: any };
  additionalMetadataParsed?: { [key: string]: any };
}

/**
 * Security alert statistics for dashboard
 */
export interface SecurityAlertStatistics {
  totalAlerts: number;
  resolvedAlerts: number;
  activeAlerts: number;
  falsePositives: number;
  alertsByType: { [key in SecurityAlertType]?: number };
  alertsBySeverity: { [key in AuditSeverity]?: number };
  averageResolutionTimeHours: number;
  averageRiskScore: number;
  highRiskAlerts: number;
  escalatedAlerts: number;
}

/**
 * Alert severity counts for dashboard widgets
 */
export interface AlertSeverityCounts {
  [AuditSeverity.INFO]: number;
  [AuditSeverity.WARNING]: number;
  [AuditSeverity.CRITICAL]: number;
  [AuditSeverity.EMERGENCY]: number;
}

/**
 * Request DTOs for alert actions
 */
export interface AssignAlertRequest {
  assignedTo?: string;  // TODO: Backend expects Guid - requires user lookup endpoint to convert email to Guid
  assignedToEmail: string;
}

export interface ResolveAlertRequest {
  resolutionNotes: string;
}

export interface FalsePositiveRequest {
  reason: string;
}

export interface EscalateAlertRequest {
  escalatedTo: string;
}

/**
 * API Response wrappers
 */
export interface SecurityAlertResponse {
  success: boolean;
  data: SecurityAlert;
  message?: string;
  error?: string;
}

export interface SecurityAlertListResponse {
  success: boolean;
  data: SecurityAlert[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
  error?: string;
}

export interface SecurityAlertStatisticsResponse {
  success: boolean;
  data: SecurityAlertStatistics;
  error?: string;
}

export interface AlertSeverityCountsResponse {
  success: boolean;
  data: AlertSeverityCounts;
  error?: string;
}

/**
 * Helper functions for display and formatting
 */
export class SecurityAlertHelpers {
  static getAlertTypeName(type: SecurityAlertType): string {
    return SecurityAlertType[type].replace(/_/g, ' ');
  }

  static getAlertStatusName(status: SecurityAlertStatus): string {
    return SecurityAlertStatus[status].replace(/_/g, ' ');
  }

  static getSeverityColor(severity: AuditSeverity): string {
    switch (severity) {
      case AuditSeverity.INFO: return 'blue';
      case AuditSeverity.WARNING: return 'yellow';
      case AuditSeverity.CRITICAL: return 'orange';
      case AuditSeverity.EMERGENCY: return 'red';
      default: return 'gray';
    }
  }

  static getSeverityChipColor(severity: AuditSeverity): ChipColor {
    switch (severity) {
      case AuditSeverity.INFO: return 'primary';
      case AuditSeverity.WARNING: return 'warning';
      case AuditSeverity.CRITICAL: return 'error';
      case AuditSeverity.EMERGENCY: return 'error';
      default: return 'neutral';
    }
  }

  static getSeverityIcon(severity: AuditSeverity): string {
    switch (severity) {
      case AuditSeverity.INFO: return 'info-circle';
      case AuditSeverity.WARNING: return 'exclamation-triangle';
      case AuditSeverity.CRITICAL: return 'exclamation-circle';
      case AuditSeverity.EMERGENCY: return 'fire';
      default: return 'question-circle';
    }
  }

  static getStatusColor(status: SecurityAlertStatus): string {
    switch (status) {
      case SecurityAlertStatus.NEW: return 'red';
      case SecurityAlertStatus.ACKNOWLEDGED: return 'yellow';
      case SecurityAlertStatus.IN_PROGRESS: return 'blue';
      case SecurityAlertStatus.RESOLVED: return 'green';
      case SecurityAlertStatus.FALSE_POSITIVE: return 'gray';
      case SecurityAlertStatus.ESCALATED: return 'orange';
      case SecurityAlertStatus.PENDING_REVIEW: return 'purple';
      case SecurityAlertStatus.CLOSED: return 'gray';
      default: return 'gray';
    }
  }

  static getStatusChipColor(status: SecurityAlertStatus): ChipColor {
    switch (status) {
      case SecurityAlertStatus.NEW: return 'error';
      case SecurityAlertStatus.ACKNOWLEDGED: return 'warning';
      case SecurityAlertStatus.IN_PROGRESS: return 'primary';
      case SecurityAlertStatus.RESOLVED: return 'success';
      case SecurityAlertStatus.FALSE_POSITIVE: return 'neutral';
      case SecurityAlertStatus.ESCALATED: return 'error';
      case SecurityAlertStatus.PENDING_REVIEW: return 'warning';
      case SecurityAlertStatus.CLOSED: return 'neutral';
      default: return 'neutral';
    }
  }

  static getRiskScoreColor(score: number): string {
    if (score >= 90) return 'red';
    if (score >= 70) return 'orange';
    if (score >= 50) return 'yellow';
    if (score >= 30) return 'blue';
    return 'gray';
  }

  static getRiskScoreChipColor(score: number): ChipColor {
    if (score >= 90) return 'error';
    if (score >= 70) return 'error';
    if (score >= 50) return 'warning';
    if (score >= 30) return 'primary';
    return 'neutral';
  }

  static getRiskScoreLabel(score: number): string {
    if (score >= 90) return 'Critical';
    if (score >= 70) return 'High';
    if (score >= 50) return 'Medium';
    if (score >= 30) return 'Low';
    return 'Minimal';
  }

  static formatTimeAgo(date: Date): string {
    const now = new Date().getTime();
    const alertTime = new Date(date).getTime();
    const diffMs = now - alertTime;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return new Date(date).toLocaleDateString();
  }
}
