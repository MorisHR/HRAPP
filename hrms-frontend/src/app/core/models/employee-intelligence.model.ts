// ═══════════════════════════════════════════════════════════════
// EMPLOYEE INTELLIGENCE MODELS - Production Grade
// Type-safe interfaces for rule-based intelligence features
// NO AI - Pure business logic
// ═══════════════════════════════════════════════════════════════

/**
 * Passport pattern detection result
 */
export interface PassportDetectionResult {
  detectedNationality: string | null;
  confidence: 'high' | 'medium' | 'low';
  pattern: string;
  suggestions: string[];
}

/**
 * Passport patterns database (Mauritius immigration authority)
 */
export interface PassportPattern {
  nationality: string;
  pattern: RegExp;
  description: string;
  example: string;
}

/**
 * Work permit recommendation
 */
export interface WorkPermitRecommendation {
  permitType: string;
  eligible: boolean;
  reasons: string[];
  requiredDocuments: string[];
  processingTime: string;
  applicationFee: number;
  validityPeriod: string;
  restrictions: string[];
  warnings: string[];
}

/**
 * Work permit types in Mauritius
 */
export type MauritiusWorkPermitType =
  | 'Occupation Permit (Professional)'
  | 'Occupation Permit (Investor)'
  | 'Occupation Permit (Self-Employed)'
  | 'Residence Permit'
  | 'Work Permit (EPZ)'
  | 'Work Permit (General)';

/**
 * Document expiry alert
 */
export interface ExpiryAlert {
  documentType: string;
  documentName: string;
  expiryDate: Date;
  daysRemaining: number;
  severity: 'critical' | 'urgent' | 'warning' | 'info';
  actionRequired: string;
  actionDeadline: Date;
  renewalLeadTime: number;
  icon: string;
  color: string;
}

/**
 * Tax treaty information
 */
export interface TaxTreaty {
  country: string;
  treatyYear: number;
  mauritiusRate: number;
  homeCountryRate: number;
  avoidDoubleXTaxation: boolean;
  benefits: string[];
  requirements: string[];
  withholdingTaxRates: {
    dividends: number;
    interest: number;
    royalties: number;
  };
}

/**
 * Tax calculation result
 */
export interface TaxCalculationResult {
  mauritiusTax: number;
  homeCountryTax: number;
  treatyBenefit: number;
  totalTax: number;
  effectiveRate: number;
  savingsVsNoTreaty: number;
  isResident: boolean;
  residencyDaysRequired: number;
  recommendations: string[];
}

/**
 * Sector-specific rules
 */
export interface SectorRules {
  sectorName: string;
  sectorCode: string;
  corporateTaxRate: number;
  expatQuotaPercent: number | null; // null = unlimited
  minimumSalary: number | null; // null = no minimum
  specialBenefits: string[];
  restrictions: string[];
  requiredLicenses: string[];
  complianceChecks: string[];
  regulatoryBodies: string[];
}

/**
 * Sector compliance validation result
 */
export interface SectorComplianceResult {
  valid: boolean;
  score: number; // 0-100
  warnings: ComplianceWarning[];
  requirements: ComplianceRequirement[];
  benefits: string[];
  estimatedTaxSavings: number;
}

export interface ComplianceWarning {
  severity: 'error' | 'warning' | 'info';
  message: string;
  field?: string;
  resolution?: string;
}

export interface ComplianceRequirement {
  name: string;
  description: string;
  mandatory: boolean;
  deadline?: Date;
  status: 'pending' | 'in_progress' | 'completed';
}

/**
 * Mauritius statutory compliance fields
 */
export interface MauritiusStatutoryData {
  csgNumber?: string; // Contribution Sociale Générale
  nsfNumber?: string; // National Savings Fund
  taxIdNumber?: string; // Tax Account Number (TAN)
  prNumber?: string; // Portable Retirement Gratuity Fund
}

/**
 * NIC (National Identity Card) validation result
 */
export interface NICValidationResult {
  valid: boolean;
  format: 'valid' | 'invalid';
  message: string;
}

/**
 * Phone number validation result (Mauritius format)
 */
export interface PhoneValidationResult {
  valid: boolean;
  formatted: string;
  type: 'mobile' | 'landline' | 'invalid';
  carrier?: string;
}

/**
 * Onboarding checklist item
 */
export interface OnboardingChecklistItem {
  id: string;
  day: number;
  category: 'legal' | 'banking' | 'housing' | 'integration';
  task: string;
  description: string;
  priority: 'critical' | 'high' | 'medium' | 'low';
  completed: boolean;
  assignedTo?: string;
  dueDate?: Date;
  dependencies: string[];
}

/**
 * Onboarding plan
 */
export interface OnboardingPlan {
  employeeId: string;
  nationality: string;
  permitType: string;
  startDate: Date;
  checklist: OnboardingChecklistItem[];
  estimatedCompletionDate: Date;
  progressPercent: number;
}

/**
 * Forex rate (currency conversion)
 */
export interface ForexRate {
  baseCurrency: string;
  targetCurrency: string;
  rate: number;
  inverseRate: number;
  timestamp: Date;
  source: string;
  expiryTime: Date; // Cache expiry
}

/**
 * Currency split recommendation
 */
export interface CurrencySplitRecommendation {
  totalSalaryMUR: number;
  murPortion: number;
  foreignPortion: number;
  foreignCurrency: string;
  exchangeRate: number;
  reasoning: string[];
  bankingRequirements: string[];
}

/**
 * Form intelligence context (per tenant)
 */
export interface FormIntelligenceContext {
  tenantId: string;
  formId: string;
  nationality: string | null;
  sector: string | null;
  salary: number | null;
  isExpatriate: boolean;
  detectedPermitType: MauritiusWorkPermitType | null;
  activeTreaty: TaxTreaty | null;
  expiryAlerts: ExpiryAlert[];
  complianceStatus: SectorComplianceResult | null;
}

/**
 * Cache entry for expensive operations
 */
export interface CacheEntry<T> {
  data: T;
  timestamp: Date;
  ttl: number; // Time to live in milliseconds
}

/**
 * Service configuration
 */
export interface IntelligenceServiceConfig {
  enableCaching: boolean;
  cacheTTL: number;
  debounceTime: number;
  maxConcurrentRequests: number;
  enableLogging: boolean;
}
