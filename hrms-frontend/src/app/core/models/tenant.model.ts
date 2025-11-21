export interface Tenant {
  id: string;
  companyName: string;
  subdomain: string;
  schemaName: string;
  contactEmail: string;
  contactPhone: string;
  status: TenantStatus;
  statusDisplay: string;
  employeeTier: string;
  employeeTierDisplay: string;
  monthlyPrice: number;
  maxUsers: number;
  currentUserCount: number;
  maxStorageGB: number;
  currentStorageGB: number;
  apiCallsPerMonth: number;
  employeeCount: number; // derived from currentUserCount
  createdAt: string;
  subscriptionStartDate: string;
  subscriptionEndDate?: string;
  trialEndDate?: string;
  suspensionReason?: string;
  suspensionDate?: string;
  softDeleteDate?: string;
  deletionReason?: string;
  daysUntilHardDelete?: number;
  adminUserName: string;
  adminEmail: string;
  // FORTUNE 500 PATTERN: Industry sector
  sectorId?: number;
  sectorCode?: string;
  sectorName?: string;
  sectorSelectedAt?: string;
}

export enum IndustrySector {
  Technology = 'Technology',
  Manufacturing = 'Manufacturing',
  Healthcare = 'Healthcare',
  Finance = 'Finance',
  Retail = 'Retail',
  Education = 'Education',
  Hospitality = 'Hospitality',
  Construction = 'Construction',
  RealEstate = 'RealEstate',
  Transportation = 'Transportation',
  Energy = 'Energy',
  Telecommunications = 'Telecommunications',
  Agriculture = 'Agriculture',
  Media = 'Media',
  Consulting = 'Consulting',
  Legal = 'Legal',
  Automotive = 'Automotive',
  Aerospace = 'Aerospace',
  Pharmaceuticals = 'Pharmaceuticals',
  Insurance = 'Insurance',
  FoodBeverage = 'FoodBeverage',
  Fashion = 'Fashion',
  Sports = 'Sports',
  Entertainment = 'Entertainment',
  GovernmentPublicSector = 'GovernmentPublicSector',
  NonProfitCharity = 'NonProfitCharity',
  Logistics = 'Logistics',
  Mining = 'Mining',
  Utilities = 'Utilities',
  Other = 'Other'
}

export enum TenantStatus {
  Active = 'Active',
  Suspended = 'Suspended',
  Trial = 'Trial',
  Expired = 'Expired'
}

export interface TenantSettings {
  timezone: string;
  dateFormat: string;
  currency: string;
  workingDays: number[];
  workingHoursStart: string;
  workingHoursEnd: string;
}

export interface CreateTenantRequest {
  companyName: string;
  subdomain: string;
  contactEmail: string;
  contactPhone: string;
  employeeTier: string;
  maxUsers: number;
  maxStorageGB: number;
  apiCallsPerMonth: number;
  monthlyPrice: number;
  adminUserName: string;
  adminEmail: string;
  adminPassword: string;
}

// ═══════════════════════════════════════════════════════════════
// FORTUNE 500: Bulk Operations
// ═══════════════════════════════════════════════════════════════

export interface BulkOperationResult {
  total: number;
  success: number;
  failed: number;
  inProgress: number;
  errors: string[];
}

export interface BulkOperationProgress {
  totalTenants: number;
  processedTenants: number;
  successfulTenants: number;
  failedTenants: number;
  currentBatch: number;
  totalBatches: number;
  percentComplete: number;
  estimatedTimeRemaining?: number;
}

// ═══════════════════════════════════════════════════════════════
// FORTUNE 500: Tenant Health Scoring
// ═══════════════════════════════════════════════════════════════

export interface TenantHealthScore {
  tenantId: string;
  tenantName: string;
  overallScore: number; // 0-100
  scoreGrade: 'A' | 'B' | 'C' | 'D' | 'F';
  lastCalculated: Date;
  metrics: TenantHealthMetrics;
  alerts: HealthAlert[];
  trend: 'improving' | 'stable' | 'declining';
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
}

export interface TenantHealthMetrics {
  usageScore: number;        // 0-100: API calls, storage, active users
  engagementScore: number;   // 0-100: Login frequency, feature adoption
  paymentScore: number;      // 0-100: Payment history, on-time rate
  supportScore: number;      // 0-100: Ticket volume, satisfaction
  riskScore: number;         // 0-100: Churn indicators, complaints
}

export interface HealthAlert {
  id: string;
  severity: 'info' | 'warning' | 'critical';
  category: 'usage' | 'payment' | 'support' | 'engagement' | 'risk';
  title: string;
  message: string;
  actionRequired: string;
  createdAt: Date;
  acknowledged: boolean;
}

// ═══════════════════════════════════════════════════════════════
// FORTUNE 500: Provisioning Queue Status
// ═══════════════════════════════════════════════════════════════

export interface TenantProvisioningStatus {
  tenantId: string;
  status: 'queued' | 'provisioning' | 'completed' | 'failed';
  startedAt?: Date;
  completedAt?: Date;
  estimatedCompletionTime?: Date;
  currentStep: string;
  totalSteps: number;
  completedSteps: number;
  progressPercent: number;
  steps: ProvisioningStep[];
  error?: string;
}

export interface ProvisioningStep {
  name: string;
  description: string;
  status: 'pending' | 'in_progress' | 'completed' | 'failed';
  startedAt?: Date;
  completedAt?: Date;
  duration?: number;
  details?: string;
  error?: string;
}

// ═══════════════════════════════════════════════════════════════
// FORTUNE 500: Tenant Lifecycle Actions
// ═══════════════════════════════════════════════════════════════

export interface SuspendTenantRequest {
  reason: string;
  notifyTenant?: boolean;
  suspensionDuration?: number; // days, null = indefinite
}

export interface DeleteTenantRequest {
  reason: string;
  confirmationText: string;
  dataRetentionDays?: number; // Default 30 days
}

export interface HardDeleteTenantRequest {
  confirmationName: string; // Must type exact company name
  confirmationText: string; // Must type "PERMANENTLY DELETE"
  acknowledgeIrreversible: boolean;
}
