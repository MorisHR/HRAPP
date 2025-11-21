/**
 * Revenue Analytics Models
 * Fortune 500-Grade SaaS Metrics for HRMS Admin Dashboard
 * Matches backend DTOs from RevenueAnalyticsController
 */

// ═══════════════════════════════════════════════════════════════
// MRR (Monthly Recurring Revenue) Breakdown
// ═══════════════════════════════════════════════════════════════

export interface MrrByTierItem {
  tier: string;
  tenantCount: number;
  mrr: number;
  averageRevenuePerTenant: number;
}

export interface MrrBreakdownResponse {
  totalMRR: number;
  totalActiveTenants: number;
  byTier: MrrByTierItem[];
  generatedAt: Date;
}

// ═══════════════════════════════════════════════════════════════
// ARR (Annual Recurring Revenue) Tracking
// ═══════════════════════════════════════════════════════════════

export interface ArrTrendItem {
  month: Date;
  arr: number;
}

export interface ArrTrackingResponse {
  currentARR: number;
  growthRate: number;
  trend: ArrTrendItem[];
  generatedAt: Date;
}

// ═══════════════════════════════════════════════════════════════
// Revenue Cohort Analysis
// ═══════════════════════════════════════════════════════════════

export interface CohortAnalysisItem {
  cohortMonth: Date;
  initialTenants: number;
  currentActiveTenants: number;
  retentionRate: number;
  monthlyRevenue: number;
  averageRevenuePerTenant: number;
}

// ═══════════════════════════════════════════════════════════════
// Expansion & Contraction Revenue
// ═══════════════════════════════════════════════════════════════

export interface ExpansionContractionItem {
  month: Date;
  expansionRevenue: number;
  contractionRevenue: number;
  netExpansion: number;
}

export interface ExpansionContractionResponse {
  trend: ExpansionContractionItem[];
  totalExpansion: number;
  totalContraction: number;
  netExpansion: number;
  generatedAt: Date;
}

// ═══════════════════════════════════════════════════════════════
// Churn Rate Analysis
// ═══════════════════════════════════════════════════════════════

export interface ChurnTrendItem {
  month: Date;
  churnedTenants: number;
  totalTenantsAtStart: number;
  churnRate: number;
}

export interface ChurnRateResponse {
  currentMonthChurnRate: number;
  averageChurnRate: number;
  trend: ChurnTrendItem[];
  generatedAt: Date;
}

// ═══════════════════════════════════════════════════════════════
// Key SaaS Metrics (LTV, CAC, ARPU, Payback Period)
// ═══════════════════════════════════════════════════════════════

export interface KeyMetricsResponse {
  // ARPU (Average Revenue Per User)
  arpu: number;

  // LTV (Customer Lifetime Value)
  ltv: number;
  averageCustomerLifetimeMonths: number;

  // CAC (Customer Acquisition Cost)
  cac: number;
  ltvToCACRatio: number;

  // Payback Period
  paybackPeriodMonths: number;

  // Context
  activeTenants: number;
  totalMRR: number;

  generatedAt: Date;
}

// ═══════════════════════════════════════════════════════════════
// Unified Dashboard Response
// ═══════════════════════════════════════════════════════════════

export interface RevenueAnalyticsDashboard {
  mrr: MrrBreakdownResponse;
  arr: ArrTrackingResponse;
  churnRate: ChurnRateResponse;
  keyMetrics: KeyMetricsResponse;
  generatedAt: Date;
}
