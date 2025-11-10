export interface SoxComplianceReport {
  reportGeneratedAt: Date;
  reportPeriod: string;
  financialAccess: FinancialDataAccessSummary;
  accessChanges: UserAccessChangesSummary;
  itGeneralControls: ITGCSummary;
  violations: SoxViolation[];
}

export interface FinancialDataAccessSummary {
  totalAccesses: number;
  uniqueUsers: number;
  salaryAccesses: number;
  payrollAccesses: number;
  compensationAccesses: number;
}

export interface UserAccessChangesSummary {
  totalChanges: number;
  roleChanges: number;
  privilegeEscalations: number;
  accessRevocations: number;
}

export interface ITGCSummary {
  failedLoginAttempts: number;
  passwordChanges: number;
  configurationChanges: number;
  adminAccesses: number;
}

export interface SoxViolation {
  violationType: string;
  severity: string;
  description: string;
  detectedAt: Date;
  userId?: string;
}

export interface GdprComplianceReport {
  userId: string;
  userEmail?: string;
  reportGeneratedAt: Date;
  personalData: PersonalDataItem[];
  processingActivities: DataProcessingActivity[];
  dataRecipients: DataRecipient[];
  totalAuditLogEntries: number;
}

export interface PersonalDataItem {
  dataType: string;
  dataValue: string;
  source: string;
  collectedAt: Date;
}

export interface DataProcessingActivity {
  activityType: string;
  purpose: string;
  processedAt: Date;
  legalBasis: string;
}

export interface DataRecipient {
  recipientName: string;
  recipientType: string;
  sharedAt: Date;
  purpose: string;
}

export interface ActivityCorrelation {
  userId: string;
  userEmail?: string;
  userFullName?: string;
  startDate: Date;
  endDate: Date;
  totalActions: number;
  actionsByCategory: { [key: string]: number };
  actionsBySeverity: { [key: string]: number };
  timeline: TimelineEvent[];
  riskScore: number;
  anomalies: any[];
}

export interface TimelineEvent {
  timestamp: Date;
  actionType: string;
  category: string;
  severity: string;
  entityType?: string;
  ipAddress?: string;
  success: boolean;
  description: string;
}
