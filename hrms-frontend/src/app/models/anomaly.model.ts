export interface DetectedAnomaly {
  id: string;
  tenantId: string;
  anomalyType: AnomalyType;
  riskLevel: AnomalyRiskLevel;
  status: AnomalyStatus;
  riskScore: number;

  userId?: string;
  userEmail?: string;
  ipAddress?: string;
  location?: string;
  detectedAt: Date;

  description: string;
  evidence: string;
  relatedAuditLogIds?: string;

  detectionRule: string;
  modelVersion?: string;

  investigatedBy?: string;
  investigatedAt?: Date;
  investigationNotes?: string;
  resolution?: string;
  resolvedAt?: Date;

  notificationSent: boolean;
  notificationSentAt?: Date;
  notificationRecipients?: string;

  createdAt: Date;
  updatedAt?: Date;
}

export enum AnomalyType {
  UNUSUAL_LOGIN_LOCATION = 'UNUSUAL_LOGIN_LOCATION',
  IMPOSSIBLE_TRAVEL = 'IMPOSSIBLE_TRAVEL',
  MULTIPLE_FAILED_LOGINS = 'MULTIPLE_FAILED_LOGINS',
  BRUTE_FORCE_ATTEMPT = 'BRUTE_FORCE_ATTEMPT',
  CONCURRENT_SESSIONS = 'CONCURRENT_SESSIONS',
  AFTER_HOURS_ACCESS = 'AFTER_HOURS_ACCESS',
  MASS_DATA_EXPORT = 'MASS_DATA_EXPORT',
  UNUSUAL_DATA_ACCESS = 'UNUSUAL_DATA_ACCESS',
  PRIVILEGE_ESCALATION = 'PRIVILEGE_ESCALATION',
  UNAUTHORIZED_ACCESS_ATTEMPT = 'UNAUTHORIZED_ACCESS_ATTEMPT',
  UNUSUAL_ACTIVITY_PATTERN = 'UNUSUAL_ACTIVITY_PATTERN',
  RAPID_HIGH_RISK_ACTIONS = 'RAPID_HIGH_RISK_ACTIONS',
  ABNORMAL_ACCESS_FREQUENCY = 'ABNORMAL_ACCESS_FREQUENCY',
  LARGE_SALARY_CHANGE = 'LARGE_SALARY_CHANGE',
  UNUSUAL_PAYROLL_MODIFICATION = 'UNUSUAL_PAYROLL_MODIFICATION',
  SECURITY_SETTING_DISABLED = 'SECURITY_SETTING_DISABLED',
  AUDIT_LOG_ACCESS = 'AUDIT_LOG_ACCESS',
  SYSTEM_CONFIGURATION_CHANGE = 'SYSTEM_CONFIGURATION_CHANGE'
}

export enum AnomalyRiskLevel {
  LOW = 'LOW',
  MEDIUM = 'MEDIUM',
  HIGH = 'HIGH',
  CRITICAL = 'CRITICAL',
  EMERGENCY = 'EMERGENCY'
}

export enum AnomalyStatus {
  NEW = 'NEW',
  INVESTIGATING = 'INVESTIGATING',
  CONFIRMED_THREAT = 'CONFIRMED_THREAT',
  FALSE_POSITIVE = 'FALSE_POSITIVE',
  RESOLVED = 'RESOLVED',
  IGNORED = 'IGNORED'
}

export interface AnomalyStatistics {
  totalAnomalies: number;
  newAnomalies: number;
  investigatingAnomalies: number;
  confirmedThreats: number;
  falsePositives: number;
  resolvedAnomalies: number;
  anomaliesByType: { [key: string]: number };
  anomaliesByRiskLevel: { [key: string]: number };
  averageRiskScore: number;
  criticalAnomalies: number;
  highRiskAnomalies: number;
}
