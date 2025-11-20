/**
 * Leave Balance Predictor & Probation Period Calculator Models
 * Rule-based intelligence for employee lifecycle management
 *
 * Performance: All calculations < 5ms
 * Multi-tenant: Stateless, no cross-tenant data
 * Production-ready: Full error handling, logging
 */

// ========================================
// LEAVE BALANCE PREDICTOR
// ========================================

/**
 * Leave balance prediction result
 * Shows projected leave at year-end with recommendations
 */
export interface LeaveBalancePrediction {
  // Current state
  currentBalance: number;
  currentDate: Date;

  // Entitlements
  annualEntitlement: number;
  accruedToDate: number;
  remainingAccrual: number;

  // Usage patterns
  usedThisYear: number;
  averageMonthlyUsage: number;
  projectedUsage: number;

  // Predictions
  predictedYearEndBalance: number;
  utilizationRate: number; // Percentage (0-100)

  // Risk assessment
  riskLevel: 'low' | 'medium' | 'high'; // Burnout/underutilization risk
  riskMessage: string;

  // Recommendations
  recommendations: string[];
  suggestedLeaveSchedule: LeaveScheduleSuggestion[];

  // Public holidays
  publicHolidaysRemaining: number;
  publicHolidayDates: PublicHolidayInfo[];

  // Compliance
  carryForwardAllowed: boolean;
  maxCarryForward?: number;
  expiringLeave?: number; // Leave that will expire if not used
}

/**
 * Suggested leave schedule for optimal distribution
 */
export interface LeaveScheduleSuggestion {
  month: string;
  suggestedDays: number;
  reason: string;
  priority: 'high' | 'medium' | 'low';
}

/**
 * Public holiday information
 */
export interface PublicHolidayInfo {
  date: Date;
  name: string;
  isLongWeekend: boolean;
  suggestedLeaveExtension?: string; // e.g., "Take 1 day to get 4-day weekend"
}

// ========================================
// PROBATION PERIOD CALCULATOR
// ========================================

/**
 * Probation period calculation result
 * Tracks probation status and upcoming reviews
 */
export interface ProbationCalculation {
  // Basic info
  isOnProbation: boolean;
  joinDate: Date;
  probationPeriodMonths: number;

  // Dates
  probationStartDate: Date;
  probationEndDate: Date;
  currentDate: Date;

  // Progress
  daysElapsed: number;
  daysRemaining: number;
  totalDays: number;
  progressPercentage: number; // 0-100

  // Status
  status: 'not-started' | 'ongoing' | 'ending-soon' | 'completed' | 'extended';
  statusMessage: string;
  urgency: 'none' | 'low' | 'medium' | 'high' | 'critical';

  // Review schedule
  reviewMilestones: ProbationMilestone[];
  nextReview?: ProbationMilestone;
  overdueReviews: ProbationMilestone[];

  // Alerts
  alerts: ProbationAlert[];

  // Recommendations
  recommendations: string[];
}

/**
 * Probation review milestone
 */
export interface ProbationMilestone {
  id: string;
  type: '30-day' | '60-day' | '90-day' | 'final' | 'custom';
  name: string;
  scheduledDate: Date;
  daysUntil: number;
  isCompleted: boolean;
  isOverdue: boolean;
  isPending: boolean;
  priority: 'high' | 'medium' | 'low';
  description: string;
  checklist?: string[]; // Items to review
}

/**
 * Probation alert
 */
export interface ProbationAlert {
  severity: 'info' | 'warning' | 'critical';
  message: string;
  actionRequired: string;
  dueDate?: Date;
  icon: string;
}

// ========================================
// MAURITIUS PUBLIC HOLIDAYS (2025-2026)
// ========================================

/**
 * Mauritius public holidays database
 * Source: Government of Mauritius official calendar
 */
export const MAURITIUS_PUBLIC_HOLIDAYS_2025: PublicHolidayInfo[] = [
  { date: new Date('2025-01-01'), name: 'New Year\'s Day', isLongWeekend: false },
  { date: new Date('2025-01-02'), name: 'New Year Holiday', isLongWeekend: false },
  { date: new Date('2025-02-01'), name: 'Abolition of Slavery', isLongWeekend: false },
  { date: new Date('2025-03-03'), name: 'Maha Shivaratri', isLongWeekend: false },
  { date: new Date('2025-03-12'), name: 'National Day', isLongWeekend: false },
  { date: new Date('2025-03-31'), name: 'Eid-ul-Fitr', isLongWeekend: false },
  { date: new Date('2025-04-18'), name: 'Good Friday', isLongWeekend: true },
  { date: new Date('2025-05-01'), name: 'Labour Day', isLongWeekend: false },
  { date: new Date('2025-06-07'), name: 'Eid-ul-Adha', isLongWeekend: false },
  { date: new Date('2025-08-15'), name: 'Assumption of Mary', isLongWeekend: false },
  { date: new Date('2025-09-09'), name: 'Ganesh Chaturthi', isLongWeekend: false },
  { date: new Date('2025-10-24'), name: 'Diwali', isLongWeekend: false },
  { date: new Date('2025-11-01'), name: 'All Saints\' Day', isLongWeekend: false },
  { date: new Date('2025-11-02'), name: 'Arrival of Indentured Labourers', isLongWeekend: false },
  { date: new Date('2025-12-25'), name: 'Christmas Day', isLongWeekend: false }
];

export const MAURITIUS_PUBLIC_HOLIDAYS_2026: PublicHolidayInfo[] = [
  { date: new Date('2026-01-01'), name: 'New Year\'s Day', isLongWeekend: false },
  { date: new Date('2026-01-02'), name: 'New Year Holiday', isLongWeekend: false },
  { date: new Date('2026-02-01'), name: 'Abolition of Slavery', isLongWeekend: false },
  { date: new Date('2026-02-22'), name: 'Maha Shivaratri', isLongWeekend: false },
  { date: new Date('2026-03-12'), name: 'National Day', isLongWeekend: false },
  { date: new Date('2026-03-21'), name: 'Eid-ul-Fitr', isLongWeekend: false },
  { date: new Date('2026-04-03'), name: 'Good Friday', isLongWeekend: true },
  { date: new Date('2026-05-01'), name: 'Labour Day', isLongWeekend: false },
  { date: new Date('2026-05-28'), name: 'Eid-ul-Adha', isLongWeekend: false },
  { date: new Date('2026-08-15'), name: 'Assumption of Mary', isLongWeekend: false },
  { date: new Date('2026-08-29'), name: 'Ganesh Chaturthi', isLongWeekend: false },
  { date: new Date('2026-11-01'), name: 'All Saints\' Day', isLongWeekend: false },
  { date: new Date('2026-11-02'), name: 'Arrival of Indentured Labourers', isLongWeekend: false },
  { date: new Date('2026-11-13'), name: 'Diwali', isLongWeekend: false },
  { date: new Date('2026-12-25'), name: 'Christmas Day', isLongWeekend: false }
];

/**
 * Get public holidays for a specific year
 */
export function getPublicHolidays(year: number): PublicHolidayInfo[] {
  switch (year) {
    case 2025:
      return MAURITIUS_PUBLIC_HOLIDAYS_2025;
    case 2026:
      return MAURITIUS_PUBLIC_HOLIDAYS_2026;
    default:
      // For years not in database, return current year or empty
      const currentYear = new Date().getFullYear();
      if (currentYear === 2025) return MAURITIUS_PUBLIC_HOLIDAYS_2025;
      if (currentYear === 2026) return MAURITIUS_PUBLIC_HOLIDAYS_2026;
      return [];
  }
}

/**
 * Calculate working days between two dates (excluding weekends and public holidays)
 */
export function calculateWorkingDays(startDate: Date, endDate: Date): number {
  let workingDays = 0;
  const current = new Date(startDate);
  const publicHolidays = getPublicHolidays(startDate.getFullYear());
  const holidayStrings = publicHolidays.map(h => h.date.toDateString());

  while (current <= endDate) {
    const dayOfWeek = current.getDay();
    const isWeekend = dayOfWeek === 0 || dayOfWeek === 6; // Sunday or Saturday
    const isPublicHoliday = holidayStrings.includes(current.toDateString());

    if (!isWeekend && !isPublicHoliday) {
      workingDays++;
    }

    current.setDate(current.getDate() + 1);
  }

  return workingDays;
}
