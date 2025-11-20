/**
 * FORTUNE 500 SECURITY ANALYTICS MODELS
 * TypeScript interfaces matching backend DTOs for security analytics
 *
 * PATTERNS: AWS GuardDuty, Azure Sentinel, Splunk ES, Cloudflare WAF
 * COMPLIANCE: PCI-DSS, NIST 800-53, ISO 27001, SOC 2, GDPR
 *
 * Created: 2025-11-20
 * Backend: MonitoringController.cs, SecurityAnalytics DTOs
 */

// ============================================
// FAILED LOGIN ANALYTICS
// ============================================

export interface TimeSeriesDataPoint {
  timestamp: Date;
  count: number;
  label: string;
}

export interface IpFailureCount {
  ipAddress: string;
  failureCount: number;
  isBlacklisted: boolean;
  firstSeen: Date;
  lastSeen: Date;
  uniqueUsersTargeted: number;
  country?: string;
  city?: string;
}

export interface UserFailureCount {
  userIdentifier: string;
  failureCount: number;
  uniqueIps: number;
  tenantSubdomain: string;
  lastAttempt: Date;
  accountLocked: boolean;
}

export interface TenantFailureCount {
  tenantSubdomain: string;
  failureCount: number;
  uniqueUsers: number;
  uniqueIps: number;
  healthScore: number;
}

export interface FailedLoginAnalytics {
  totalFailedLogins: number;
  uniqueUsers: number;
  uniqueIpAddresses: number;
  blacklistedIps: number;
  last24Hours: number;
  last7Days: number;
  last30Days: number;
  trendPercentage: number;
  trendDirection: 'up' | 'down' | 'stable';
  timeSeriesData: TimeSeriesDataPoint[];
  topFailureIps: IpFailureCount[];
  topTargetedUsers: UserFailureCount[];
  failuresByTenant: TenantFailureCount[];
  peakHour: number;
  peakHourCount: number;
  geographicDistribution: { [key: string]: number };
}

// ============================================
// BRUTE FORCE ATTACK STATISTICS
// ============================================

export interface ActiveAttack {
  ipAddress: string;
  targetUser: string;
  attemptCount: number;
  startedAt: Date;
  lastAttempt: Date;
  isBlocked: boolean;
  tenantSubdomain: string;
  attackPattern: string;
}

export interface BlockedIp {
  ipAddress: string;
  blockedAt: Date;
  reason: string;
  violationCount: number;
  isPermanent: boolean;
  expiresAt?: Date;
  lastActivity: Date;
}

export interface AttackPattern {
  patternName: string;
  count: number;
  description: string;
  severity: 'Critical' | 'High' | 'Medium' | 'Low';
}

export interface HourlyDistribution {
  hour: number;
  count: number;
  label: string;
}

export interface TargetedEndpoint {
  endpoint: string;
  attackCount: number;
  successRate: number;
  lastAttack: Date;
}

export interface BruteForceStatistics {
  totalAttacksDetected: number;
  activeAttacks: number;
  attacksBlocked: number;
  blockSuccessRate: number;
  blacklistedIpsCount: number;
  lockedAccountsCount: number;
  averageAttackDuration: number;
  peakAttackRate: number;
  currentAttackRate: number;
  activeAttacksList: ActiveAttack[];
  recentlyBlockedIps: BlockedIp[];
  attackPatterns: { [key: string]: number };
  hourlyDistribution: HourlyDistribution[];
  topTargetedEndpoints: TargetedEndpoint[];
  attackTrend: 'increasing' | 'decreasing' | 'stable';
}

// ============================================
// IP BLACKLIST MANAGEMENT
// ============================================

export interface BlacklistedIp {
  ipAddress: string;
  blacklistedAt: Date;
  reason: string;
  addedBy: string;
  violationCount: number;
  isPermanent: boolean;
  expiresAt?: Date;
  isWhitelisted: boolean;
  lastActivity: Date;
  country?: string;
  threatLevel: 'Critical' | 'High' | 'Medium' | 'Low';
}

export interface WhitelistedIp {
  ipAddress: string;
  whitelistedAt: Date;
  reason: string;
  addedBy: string;
  expiresAt?: Date;
}

export interface RecentBlockActivity {
  timestamp: Date;
  ipAddress: string;
  action: 'Blocked' | 'Unblocked' | 'Whitelisted' | 'Removed';
  reason: string;
  performedBy: string;
}

export interface IpBlacklist {
  totalBlacklisted: number;
  autoBlockedCount: number;
  manuallyBlockedCount: number;
  permanentBlocksCount: number;
  temporaryBlocksCount: number;
  whitelistedCount: number;
  blacklistedIps: BlacklistedIp[];
  whitelistedIps: WhitelistedIp[];
  recentActivity: RecentBlockActivity[];
  topViolatingIps: BlacklistedIp[];
  blocksByCountry: { [key: string]: number };
}

export interface AddIpToBlacklistRequest {
  ipAddress: string;
  reason: string;
  isPermanent: boolean;
  expiresAt?: Date;
}

export interface AddIpToWhitelistRequest {
  ipAddress: string;
  reason: string;
  expiresAt?: Date;
}

// ============================================
// SESSION MANAGEMENT
// ============================================

export interface ActiveSession {
  sessionId: string;
  userId: string;
  userEmail: string;
  tenantSubdomain: string;
  ipAddress: string;
  userAgent: string;
  startedAt: Date;
  lastActivity: Date;
  expiresAt: Date;
  isSuspicious: boolean;
  suspiciousReasons: string[];
  deviceType: string;
  browser: string;
  operatingSystem: string;
  location?: string;
  concurrentSessionCount: number;
}

export interface SuspiciousSession {
  sessionId: string;
  userId: string;
  userEmail: string;
  suspiciousReasons: string[];
  riskScore: number;
  detectedAt: Date;
}

export interface SessionsByTenant {
  tenantSubdomain: string;
  activeSessionsCount: number;
  uniqueUsersCount: number;
  suspiciousSessionsCount: number;
}

export interface SessionManagement {
  totalActiveSessions: number;
  uniqueActiveUsers: number;
  suspiciousSessionsCount: number;
  concurrentSessionsDetected: number;
  averageSessionDuration: number;
  sessionsLast24Hours: number;
  sessionsLast7Days: number;
  sessionsByTenant: SessionsByTenant[];
  suspiciousSessions: SuspiciousSession[];
  sessionsByHour: HourlyDistribution[];
  sessionsByDevice: { [key: string]: number };
  sessionsByCountry: { [key: string]: number };
}

// ============================================
// MFA COMPLIANCE
// ============================================

export interface NonCompliantUser {
  userId: string;
  userEmail: string;
  tenantSubdomain: string;
  roleName: string;
  lastLogin: Date;
  riskLevel: 'Critical' | 'High' | 'Medium' | 'Low';
  daysSinceLastLogin: number;
}

export interface MfaEnrollment {
  userId: string;
  userEmail: string;
  enrolledAt: Date;
  mfaMethod: string;
  tenantSubdomain: string;
}

export interface ComplianceByTenant {
  tenantSubdomain: string;
  totalUsers: number;
  mfaEnabledCount: number;
  adoptionRate: number;
  complianceStatus: 'Compliant' | 'At Risk' | 'Non-Compliant';
}

export interface ComplianceByRole {
  roleName: string;
  totalUsers: number;
  mfaEnabledCount: number;
  adoptionRate: number;
  isCompliant: boolean;
}

export interface MfaCompliance {
  totalUsers: number;
  mfaEnabledCount: number;
  mfaDisabledCount: number;
  adoptionRate: number;
  complianceRate: number;
  complianceStatus: 'Compliant' | 'At Risk' | 'Non-Compliant';
  requiredAdoptionRate: number;
  nonCompliantUsers: NonCompliantUser[];
  recentEnrollments: MfaEnrollment[];
  complianceByTenant: ComplianceByTenant[];
  complianceByRole: ComplianceByRole[];
  trendPercentage: number;
  trendDirection: 'improving' | 'declining' | 'stable';
}

// ============================================
// PASSWORD COMPLIANCE
// ============================================

export interface PasswordStrengthDistribution {
  strength: 'Weak' | 'Medium' | 'Strong' | 'Very Strong';
  count: number;
  percentage: number;
}

export interface WeakPasswordUser {
  userId: string;
  userEmail: string;
  tenantSubdomain: string;
  passwordStrength: string;
  passwordAge: number;
  lastChanged: Date;
  riskLevel: 'Critical' | 'High' | 'Medium' | 'Low';
}

export interface ExpiringPassword {
  userId: string;
  userEmail: string;
  tenantSubdomain: string;
  daysUntilExpiration: number;
  expiresAt: Date;
  hasBeenNotified: boolean;
}

export interface PasswordCompliance {
  totalUsers: number;
  strongPasswordCount: number;
  weakPasswordCount: number;
  complianceRate: number;
  complianceStatus: 'Compliant' | 'At Risk' | 'Non-Compliant';
  strengthDistribution: PasswordStrengthDistribution[];
  weakPasswordUsers: WeakPasswordUser[];
  expiringPasswords: ExpiringPassword[];
  averagePasswordAge: number;
  passwordsExpiredCount: number;
  compromisedPasswordsDetected: number;
  complianceByTenant: ComplianceByTenant[];
  trendPercentage: number;
  trendDirection: 'improving' | 'declining' | 'stable';
}

// ============================================
// SECURITY DASHBOARD (COMPREHENSIVE)
// ============================================

export interface FailedLoginSummary {
  totalLast24Hours: number;
  totalLast7Days: number;
  trendPercentage: number;
  trendDirection: 'up' | 'down' | 'stable';
  uniqueIps: number;
  blacklistedIps: number;
  topAttackingIp: string;
  topAttackingIpCount: number;
}

export interface BruteForceSummary {
  activeAttacks: number;
  attacksBlockedLast24Hours: number;
  blockSuccessRate: number;
  currentThreatLevel: 'Critical' | 'High' | 'Medium' | 'Low' | 'None';
}

export interface IpBlacklistSummary {
  totalBlacklisted: number;
  newLast24Hours: number;
  permanentBlocks: number;
  topViolatingCountry: string;
}

export interface SessionManagementSummary {
  activeSessions: number;
  suspiciousSessions: number;
  forcedLogoutsLast24Hours: number;
  concurrentSessionsDetected: number;
}

export interface MfaComplianceSummary {
  adoptionRate: number;
  complianceRate: number;
  nonCompliantUsers: number;
  complianceStatus: 'Compliant' | 'At Risk' | 'Non-Compliant';
}

export interface PasswordComplianceSummary {
  complianceRate: number;
  weakPasswords: number;
  expiringNext7Days: number;
  complianceStatus: 'Compliant' | 'At Risk' | 'Non-Compliant';
}

export interface SecurityActivityEntry {
  timestamp: Date;
  severity: 'Critical' | 'High' | 'Medium' | 'Low';
  eventType: string;
  description: string;
  ipAddress?: string;
  userId?: string;
  tenantSubdomain?: string;
}

export interface AtRiskTenant {
  tenantSubdomain: string;
  riskScore: number;
  issues: string[];
  failedLogins: number;
  suspiciousSessions: number;
}

export interface SecurityDashboardAnalytics {
  overallSecurityScore: number;
  securityTrend: 'improving' | 'stable' | 'declining';
  criticalIssuesCount: number;
  highPriorityIssuesCount: number;
  failedLogins: FailedLoginSummary;
  bruteForce: BruteForceSummary;
  ipBlacklist: IpBlacklistSummary;
  sessions: SessionManagementSummary;
  mfaCompliance: MfaComplianceSummary;
  passwordCompliance: PasswordComplianceSummary;
  recentCriticalActivity: SecurityActivityEntry[];
  atRiskTenants: AtRiskTenant[];
  lastRefreshedAt: Date;
  dataFreshnessSeconds: number;
}

// ============================================
// API RESPONSE WRAPPERS
// ============================================

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
}
