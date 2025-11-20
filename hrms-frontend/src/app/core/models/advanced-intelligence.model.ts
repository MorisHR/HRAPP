/**
 * Advanced Intelligence Suite Models
 * 8 Production-Grade Intelligence Features
 *
 * Rule-based, NO AI/ML
 * Performance: All operations < 20ms
 * Security: Multi-tenant safe, Fortune 500-grade
 * Compliance: Mauritius labor laws
 */

// ========================================
// 1. OVERTIME COMPLIANCE MONITOR
// ========================================

export interface OvertimeComplianceResult {
  // Compliance status
  isCompliant: boolean;
  overallRisk: 'none' | 'low' | 'medium' | 'high' | 'critical';

  // Current week analysis
  weeklyHours: number;
  weeklyLimit: number; // Mauritius: 45h/week
  weeklyOvertime: number;
  weeklyOvertimePay: number;

  // Fortnight analysis (Mauritius law)
  fortnightHours: number;
  fortnightLimit: number; // Mauritius: 90h/fortnight

  // Violations
  violations: OvertimeViolation[];

  // Rest period compliance
  restPeriodViolations: RestPeriodViolation[];

  // Cost analysis
  totalOvertimeCost: number;
  budgetBurnRate: number; // Percentage of monthly OT budget
  projectedMonthlyCost: number;

  // Recommendations
  recommendations: string[];
  legalRisk: 'none' | 'low' | 'medium' | 'high';
  potentialFine: number; // Mauritius Workers Rights Act penalties
}

export interface OvertimeViolation {
  type: 'weekly_limit' | 'fortnight_limit' | 'consecutive_days' | 'daily_limit';
  severity: 'warning' | 'violation' | 'critical';
  description: string;
  value: number;
  limit: number;
  date: Date;
  legalReference: string; // E.g., "Workers Rights Act 2019 Section 34"
}

export interface RestPeriodViolation {
  type: 'insufficient_rest' | 'no_weekly_rest' | 'consecutive_days';
  severity: 'warning' | 'violation';
  description: string;
  actualRest: number; // Hours
  requiredRest: number; // Hours (Mauritius: 11h between shifts)
  date: Date;
}

// ========================================
// 2. SALARY ANOMALY DETECTOR
// ========================================

export interface SalaryAnomalyResult {
  // Overall analysis
  hasAnomalies: boolean;
  riskLevel: 'none' | 'low' | 'medium' | 'high' | 'critical';

  // Statistical analysis
  departmentAverage: number;
  roleAverage: number;
  companyAverage: number;
  employeeSalary: number;
  deviationFromAverage: number; // Percentage
  standardDeviations: number; // Z-score

  // Anomaly detection
  anomalies: SalaryAnomaly[];

  // Gender pay gap analysis
  genderPayGap?: GenderPayGapAnalysis;

  // Market comparison
  marketRate?: MarketRateComparison;

  // Recommendations
  recommendations: string[];
  complianceIssues: string[];
}

export interface SalaryAnomaly {
  type: 'outlier_high' | 'outlier_low' | 'gender_gap' | 'role_mismatch' | 'tenure_mismatch';
  severity: 'info' | 'warning' | 'critical';
  description: string;
  impact: string;
  recommendedAction: string;
}

export interface GenderPayGapAnalysis {
  maleAverage: number;
  femaleAverage: number;
  gapPercentage: number;
  gapAmount: number;
  isCompliant: boolean; // Mauritius Equal Pay Act threshold: 10%
  affectedEmployees: number;
  legalRisk: 'none' | 'low' | 'medium' | 'high';
}

export interface MarketRateComparison {
  marketRate: number;
  currentSalary: number;
  differencePercentage: number;
  differenceAmount: number;
  position: 'below_market' | 'at_market' | 'above_market';
  competitiveness: 'non_competitive' | 'competitive' | 'highly_competitive';
}

// ========================================
// 3. EMPLOYEE RETENTION RISK SCORER
// ========================================

export interface RetentionRiskScore {
  // Risk score (0-100, higher = higher risk)
  overallScore: number;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
  turnoverProbability: number; // Percentage (0-100)

  // Factor breakdown
  factors: RetentionRiskFactor[];

  // Financial impact
  replacementCost: number; // Estimated cost to replace (150-200% of salary)
  retentionValue: number; // Value of retaining employee

  // Time analysis
  tenure: number; // Months
  timeInRole: number; // Months
  lastPromotion?: Date;
  lastSalaryIncrease?: Date;

  // Recommendations
  urgency: 'none' | 'low' | 'medium' | 'high' | 'immediate';
  recommendations: RetentionRecommendation[];

  // Predicted timeline
  estimatedDepartureWindow?: string; // E.g., "Within 3 months"
}

export interface RetentionRiskFactor {
  factor: 'salary_below_market' | 'no_promotion' | 'high_workload' | 'no_training' |
          'low_engagement' | 'tenure_risk' | 'market_demand' | 'manager_issues';
  weight: number; // 0-1 (importance of this factor)
  score: number; // 0-100 (risk contribution)
  status: 'positive' | 'neutral' | 'concerning' | 'critical';
  description: string;
  impact: number; // Contribution to overall score
}

export interface RetentionRecommendation {
  priority: 'low' | 'medium' | 'high' | 'immediate';
  action: string;
  expectedImpact: number; // Risk reduction (0-100)
  cost: 'low' | 'medium' | 'high';
  timeframe: string; // E.g., "Within 2 weeks"
}

// ========================================
// 4. PERFORMANCE REVIEW SCHEDULER
// ========================================

export interface PerformanceReviewSchedule {
  // Employee info
  employeeId: string;
  employeeName: string;
  department: string;

  // Review cycles
  nextReview: PerformanceReview;
  upcomingReviews: PerformanceReview[];
  overdueReviews: PerformanceReview[];
  completedReviews: PerformanceReview[];

  // Status
  reviewStatus: 'on_track' | 'due_soon' | 'overdue' | 'missing_reviews';
  urgency: 'none' | 'low' | 'medium' | 'high' | 'critical';

  // Metrics
  reviewCompletionRate: number; // Percentage
  averageDelay: number; // Days
  lastReviewDate?: Date;
  daysSinceLastReview?: number;

  // Recommendations
  recommendations: string[];
  requiredActions: ReviewAction[];
}

export interface PerformanceReview {
  id: string;
  type: 'annual' | 'quarterly' | 'probation' | 'mid_year' | '360_degree';
  scheduledDate: Date;
  dueDate: Date;
  completedDate?: Date;

  status: 'not_started' | 'employee_pending' | 'manager_pending' | 'hr_pending' |
          'completed' | 'overdue';

  daysUntilDue: number;
  daysOverdue: number;
  isOverdue: boolean;

  participants: ReviewParticipant[];
  progress: number; // Percentage (0-100)

  // Requirements
  requiresSelfAssessment: boolean;
  selfAssessmentComplete: boolean;
  requiresManagerAssessment: boolean;
  managerAssessmentComplete: boolean;
  requires360: boolean;
}

export interface ReviewParticipant {
  role: 'employee' | 'manager' | 'peer' | 'subordinate' | 'hr';
  name: string;
  status: 'pending' | 'in_progress' | 'completed';
  dueDate: Date;
}

export interface ReviewAction {
  action: string;
  assignedTo: 'employee' | 'manager' | 'hr';
  priority: 'low' | 'medium' | 'high' | 'urgent';
  dueDate: Date;
  description: string;
}

// ========================================
// 5. TRAINING NEEDS ANALYZER
// ========================================

export interface TrainingNeedsAnalysis {
  // Overall status
  overallComplianceRate: number; // Percentage
  criticalGaps: number;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';

  // Mandatory training
  mandatoryTraining: MandatoryTrainingStatus[];

  // Skill gaps
  skillGaps: SkillGap[];

  // Certification tracking
  expiringCertifications: ExpiringCertification[];

  // Department analysis
  departmentCoverage: number; // Percentage with required training
  lastTrainingDate?: Date;
  daysSinceLastTraining?: number;

  // Budget analysis
  recommendedTrainingBudget: number;
  estimatedCost: number;
  roi: number; // Expected return on investment

  // Recommendations
  priorityTraining: TrainingRecommendation[];
  recommendations: string[];
}

export interface MandatoryTrainingStatus {
  trainingName: string;
  category: 'health_safety' | 'compliance' | 'security' | 'ethics' | 'technical';
  required: boolean;
  completed: boolean;
  completionDate?: Date;
  expiryDate?: Date;
  daysUntilExpiry?: number;
  status: 'compliant' | 'due_soon' | 'overdue' | 'missing';
  legalRequirement: boolean;
  regulatoryReference?: string; // E.g., "Mauritius OSH Act 2005"
}

export interface SkillGap {
  skill: string;
  currentLevel: 'none' | 'basic' | 'intermediate' | 'advanced' | 'expert';
  requiredLevel: 'basic' | 'intermediate' | 'advanced' | 'expert';
  gapSeverity: 'low' | 'medium' | 'high' | 'critical';
  impact: string;
  recommendedTraining: string;
  estimatedDuration: string; // E.g., "2 weeks"
  estimatedCost: number;
}

export interface ExpiringCertification {
  certificationName: string;
  issuedDate: Date;
  expiryDate: Date;
  daysUntilExpiry: number;
  status: 'valid' | 'expiring_soon' | 'expired';
  renewalRequired: boolean;
  renewalCost: number;
  renewalDuration: string; // E.g., "1 day course"
}

export interface TrainingRecommendation {
  trainingName: string;
  priority: 'low' | 'medium' | 'high' | 'urgent';
  reason: string;
  targetEmployees: number;
  estimatedCost: number;
  estimatedDuration: string;
  expectedBenefit: string;
  deadline?: Date;
}

// ========================================
// 6. CAREER PROGRESSION ANALYZER
// ========================================

export interface CareerProgressionAnalysis {
  // Promotion readiness
  promotionReadinessScore: number; // 0-100
  readinessLevel: 'not_ready' | 'developing' | 'ready' | 'overdue';

  // Current position analysis
  timeInRole: number; // Months
  timeInCompany: number; // Months
  currentLevel: string;
  nextLevel?: string;

  // Qualification factors
  qualifications: PromotionQualification[];

  // Performance history
  performanceRating: 'below' | 'meets' | 'exceeds' | 'exceptional';
  performanceHistory: PerformanceTrend[];

  // Skill readiness
  skillMatch: number; // Percentage match with next role
  missingSkills: string[];
  developmentNeeds: DevelopmentNeed[];

  // Financial analysis
  currentSalary: number;
  expectedSalaryIncrease: number;
  marketRateNextLevel: number;

  // Recommendations
  recommendation: 'not_recommended' | 'monitor' | 'consider' | 'recommend' | 'strongly_recommend';
  reasoning: string[];
  actionPlan: CareerAction[];

  // Risk analysis
  retentionRiskIfNotPromoted: 'low' | 'medium' | 'high' | 'critical';
  timelineToPromotion?: string; // E.g., "3-6 months"
}

export interface PromotionQualification {
  criterion: 'tenure' | 'performance' | 'skills' | 'education' | 'leadership' | 'projects';
  required: boolean;
  met: boolean;
  status: 'met' | 'partially_met' | 'not_met';
  details: string;
  weight: number; // Importance (0-1)
}

export interface PerformanceTrend {
  period: string; // E.g., "2024 Q3"
  rating: 'below' | 'meets' | 'exceeds' | 'exceptional';
  score: number; // 0-100
}

export interface DevelopmentNeed {
  area: string;
  currentLevel: string;
  targetLevel: string;
  developmentAction: string;
  timeline: string;
  cost: number;
}

export interface CareerAction {
  action: string;
  owner: 'employee' | 'manager' | 'hr';
  priority: 'low' | 'medium' | 'high';
  deadline: Date;
  description: string;
}

// ========================================
// 7. VISA/WORK PERMIT RENEWAL FORECASTER
// ========================================

export interface VisaRenewalForecast {
  // Current status
  currentPermitType: string;
  currentPermitNumber: string;
  issueDate: Date;
  expiryDate: Date;
  daysUntilExpiry: number;

  // Urgency
  status: 'valid' | 'renewal_planning' | 'renewal_required' | 'urgent' | 'expired';
  urgency: 'none' | 'low' | 'medium' | 'high' | 'critical';

  // Renewal timeline
  timeline: RenewalMilestone[];
  nextMilestone?: RenewalMilestone;

  // Document status
  requiredDocuments: PermitDocument[];
  documentCompletion: number; // Percentage

  // Cost analysis
  estimatedCost: VisaRenewalCost;

  // Processing information
  processingAuthority: string; // E.g., "Economic Development Board (EDBM)"
  processingTime: string; // E.g., "4-6 weeks"
  successRate: number; // Historical success rate

  // Recommendations
  recommendations: string[];
  risks: RenewalRisk[];

  // Alerts
  alerts: RenewalAlert[];
}

export interface RenewalMilestone {
  id: string;
  name: string;
  description: string;
  dueDate: Date;
  status: 'pending' | 'in_progress' | 'completed' | 'overdue';
  isOverdue: boolean;
  daysUntilDue: number;
  responsible: string; // E.g., "HR Department", "Employee", "Legal Team"
  checklist: string[];
}

export interface PermitDocument {
  documentName: string;
  required: boolean;
  status: 'missing' | 'expired' | 'expiring_soon' | 'valid';
  expiryDate?: Date;
  daysUntilExpiry?: number;
  notes?: string;
}

export interface VisaRenewalCost {
  applicationFee: number;
  medicalExamination: number;
  legalFees: number;
  documentationCosts: number;
  miscellaneous: number;
  totalEstimated: number;
}

export interface RenewalRisk {
  type: 'document_missing' | 'timeline_tight' | 'compliance_issue' | 'cost_high';
  severity: 'low' | 'medium' | 'high' | 'critical';
  description: string;
  mitigation: string;
}

export interface RenewalAlert {
  severity: 'info' | 'warning' | 'critical';
  message: string;
  actionRequired: string;
  deadline?: Date;
}

// ========================================
// 8. WORKFORCE ANALYTICS DASHBOARD
// ========================================

export interface WorkforceAnalytics {
  // Snapshot date
  snapshotDate: Date;

  // Headcount
  totalHeadcount: number;
  headcountTrend: TrendData[];
  growthRate: number; // Percentage

  // Turnover analysis
  turnoverAnalysis: TurnoverAnalysis;

  // Diversity metrics
  diversityMetrics: DiversityMetrics;

  // Compensation analysis
  compensationAnalysis: CompensationAnalysis;

  // Department breakdown
  departmentBreakdown: DepartmentMetrics[];

  // Age demographics
  ageDemographics: AgeBracket[];

  // Tenure analysis
  tenureAnalysis: TenureBracket[];

  // Cost analysis
  totalPayrollCost: number;
  averageCostPerEmployee: number;
  payrollTrend: TrendData[];

  // Key insights
  insights: WorkforceInsight[];

  // Compliance status
  complianceMetrics: ComplianceMetric[];
}

export interface TrendData {
  period: string; // E.g., "2025 Q3"
  value: number;
  change: number; // Percentage change from previous
}

export interface TurnoverAnalysis {
  voluntaryTurnover: number; // Percentage
  involuntaryTurnover: number;
  totalTurnover: number;
  turnoverRate: number; // Annual rate
  averageTenure: number; // Months
  atRiskEmployees: number;
  topReasons: string[];
}

export interface DiversityMetrics {
  genderDistribution: { male: number; female: number; other: number };
  nationalityDistribution: { [nationality: string]: number };
  ageDistribution: { [bracket: string]: number };
  genderBalanceScore: number; // 0-100 (100 = perfect 50/50)
  diversityIndex: number; // 0-100 (Simpson's Diversity Index)
  complianceStatus: 'compliant' | 'at_risk' | 'non_compliant';
}

export interface CompensationAnalysis {
  averageSalary: number;
  medianSalary: number;
  salaryRange: { min: number; max: number };
  salaryDistribution: SalaryBracket[];
  genderPayGap: number; // Percentage
  payEquityScore: number; // 0-100
}

export interface DepartmentMetrics {
  departmentName: string;
  headcount: number;
  averageSalary: number;
  turnoverRate: number;
  vacancyRate: number;
  averageAge: number;
  genderRatio: { male: number; female: number };
  performanceScore: number; // Average team performance
}

export interface AgeBracket {
  bracket: string; // E.g., "25-30"
  count: number;
  percentage: number;
}

export interface TenureBracket {
  bracket: string; // E.g., "1-2 years"
  count: number;
  percentage: number;
  averageAge: number;
}

export interface SalaryBracket {
  bracket: string; // E.g., "MUR 25K-35K"
  count: number;
  percentage: number;
}

export interface WorkforceInsight {
  category: 'positive' | 'neutral' | 'concern' | 'critical';
  title: string;
  description: string;
  impact: string;
  recommendation: string;
}

export interface ComplianceMetric {
  regulation: string; // E.g., "Equal Opportunities Act"
  status: 'compliant' | 'at_risk' | 'non_compliant';
  score: number; // 0-100
  details: string;
  deadline?: Date;
}

// ========================================
// HELPER TYPES
// ========================================

export type RiskLevel = 'none' | 'low' | 'medium' | 'high' | 'critical';
export type ComplianceStatus = 'compliant' | 'at_risk' | 'non_compliant';
export type Urgency = 'none' | 'low' | 'medium' | 'high' | 'critical' | 'immediate';
