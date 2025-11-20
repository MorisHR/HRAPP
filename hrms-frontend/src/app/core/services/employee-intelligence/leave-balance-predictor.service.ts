import { Injectable } from '@angular/core';
import {
  LeaveBalancePrediction,
  LeaveScheduleSuggestion,
  PublicHolidayInfo,
  getPublicHolidays,
  calculateWorkingDays
} from '../../models/leave-probation-intelligence.model';

/**
 * Leave Balance Predictor Service
 *
 * INTELLIGENCE: Rule-based leave balance prediction and optimization
 * PERFORMANCE: < 5ms (pure calculations, no API calls)
 * MULTI-TENANT: Stateless, no cross-tenant data
 * PRODUCTION: Full error handling, logging
 *
 * Features:
 * - Predict year-end leave balance
 * - Assess underutilization risk (burnout)
 * - Suggest optimal leave distribution
 * - Factor in public holidays
 * - Handle carry-forward rules
 */
@Injectable({
  providedIn: 'root'
})
export class LeaveBalancePredictorService {

  constructor() {}

  /**
   * Predict leave balance at year-end
   *
   * @param joinDate Employee join date
   * @param annualLeaveDays Annual leave entitlement
   * @param usedLeaveDays Leave used so far this year
   * @param carryForwardAllowed Whether carry forward is allowed
   * @param maxCarryForward Maximum days that can be carried forward
   * @returns Leave balance prediction with recommendations
   */
  predictLeaveBalance(
    joinDate: Date,
    annualLeaveDays: number,
    usedLeaveDays: number,
    carryForwardAllowed: boolean = true,
    maxCarryForward: number = 5
  ): LeaveBalancePrediction {
    try {
      const currentDate = new Date();
      const yearStart = new Date(currentDate.getFullYear(), 0, 1);
      const yearEnd = new Date(currentDate.getFullYear(), 11, 31);

      // Calculate accrued leave to date
      const accruedToDate = this.calculateAccruedLeave(joinDate, currentDate, annualLeaveDays);

      // Calculate remaining accrual for the year
      const totalAnnualEntitlement = this.calculateProRatedEntitlement(joinDate, currentDate, annualLeaveDays);
      const remainingAccrual = Math.max(0, totalAnnualEntitlement - accruedToDate);

      // Current balance
      const currentBalance = accruedToDate - usedLeaveDays;

      // Calculate average monthly usage
      const monthsElapsed = this.getMonthsElapsed(yearStart, currentDate);
      const averageMonthlyUsage = monthsElapsed > 0 ? usedLeaveDays / monthsElapsed : 0;

      // Project usage for remaining months
      const monthsRemaining = this.getMonthsRemaining(currentDate, yearEnd);
      const projectedUsage = averageMonthlyUsage * monthsRemaining;

      // Predicted year-end balance
      const predictedYearEndBalance = currentBalance + remainingAccrual - projectedUsage;

      // Utilization rate
      const utilizationRate = totalAnnualEntitlement > 0
        ? ((usedLeaveDays + projectedUsage) / totalAnnualEntitlement) * 100
        : 0;

      // Risk assessment
      const { riskLevel, riskMessage } = this.assessUnderutilizationRisk(
        utilizationRate,
        predictedYearEndBalance,
        carryForwardAllowed,
        maxCarryForward
      );

      // Get public holidays
      const publicHolidays = getPublicHolidays(currentDate.getFullYear());
      const publicHolidaysRemaining = this.getPublicHolidaysRemaining(publicHolidays, currentDate);

      // Generate recommendations
      const recommendations = this.generateRecommendations(
        currentBalance,
        predictedYearEndBalance,
        utilizationRate,
        carryForwardAllowed,
        maxCarryForward,
        publicHolidaysRemaining.length
      );

      // Suggest leave schedule
      const suggestedLeaveSchedule = this.generateLeaveSchedule(
        currentDate,
        yearEnd,
        predictedYearEndBalance,
        publicHolidaysRemaining
      );

      // Calculate expiring leave
      const expiringLeave = this.calculateExpiringLeave(
        predictedYearEndBalance,
        carryForwardAllowed,
        maxCarryForward
      );

      return {
        currentBalance,
        currentDate,
        annualEntitlement: totalAnnualEntitlement,
        accruedToDate,
        remainingAccrual,
        usedThisYear: usedLeaveDays,
        averageMonthlyUsage,
        projectedUsage,
        predictedYearEndBalance,
        utilizationRate,
        riskLevel,
        riskMessage,
        recommendations,
        suggestedLeaveSchedule,
        publicHolidaysRemaining: publicHolidaysRemaining.length,
        publicHolidayDates: publicHolidaysRemaining,
        carryForwardAllowed,
        maxCarryForward: carryForwardAllowed ? maxCarryForward : undefined,
        expiringLeave: expiringLeave > 0 ? expiringLeave : undefined
      };
    } catch (error) {
      console.error('[LeaveBalancePredictor] Error predicting leave balance:', error);
      throw error;
    }
  }

  /**
   * Calculate accrued leave to date (pro-rated based on months worked)
   */
  private calculateAccruedLeave(joinDate: Date, currentDate: Date, annualLeaveDays: number): number {
    const yearStart = new Date(currentDate.getFullYear(), 0, 1);
    const effectiveStart = joinDate > yearStart ? joinDate : yearStart;

    const monthsWorked = this.getMonthsElapsed(effectiveStart, currentDate);
    const accrued = (annualLeaveDays / 12) * monthsWorked;

    return Math.round(accrued * 10) / 10; // Round to 1 decimal
  }

  /**
   * Calculate pro-rated annual entitlement for employees who joined mid-year
   */
  private calculateProRatedEntitlement(joinDate: Date, currentDate: Date, annualLeaveDays: number): number {
    const currentYear = currentDate.getFullYear();
    const yearStart = new Date(currentYear, 0, 1);

    // If joined this year, pro-rate
    if (joinDate.getFullYear() === currentYear) {
      const monthsRemaining = 12 - joinDate.getMonth();
      return Math.round((annualLeaveDays / 12) * monthsRemaining * 10) / 10;
    }

    // If joined in previous years, full entitlement
    return annualLeaveDays;
  }

  /**
   * Get months elapsed between two dates
   */
  private getMonthsElapsed(startDate: Date, endDate: Date): number {
    const yearDiff = endDate.getFullYear() - startDate.getFullYear();
    const monthDiff = endDate.getMonth() - startDate.getMonth();
    const dayRatio = endDate.getDate() / 30; // Approximate

    return Math.max(0, yearDiff * 12 + monthDiff + dayRatio);
  }

  /**
   * Get months remaining in the year
   */
  private getMonthsRemaining(currentDate: Date, yearEnd: Date): number {
    return this.getMonthsElapsed(currentDate, yearEnd);
  }

  /**
   * Assess underutilization risk (burnout indicator)
   */
  private assessUnderutilizationRisk(
    utilizationRate: number,
    predictedBalance: number,
    carryForwardAllowed: boolean,
    maxCarryForward: number
  ): { riskLevel: 'low' | 'medium' | 'high'; riskMessage: string } {
    // High risk: < 40% utilization (severe underutilization - burnout risk)
    if (utilizationRate < 40) {
      return {
        riskLevel: 'high',
        riskMessage: 'Severe underutilization detected. Risk of burnout. Please schedule leave soon.'
      };
    }

    // Medium risk: 40-60% utilization
    if (utilizationRate < 60) {
      return {
        riskLevel: 'medium',
        riskMessage: 'Moderate underutilization. Consider planning leave to maintain work-life balance.'
      };
    }

    // High risk: Will lose leave (exceeds carry forward limit)
    if (!carryForwardAllowed && predictedBalance > 0) {
      return {
        riskLevel: 'high',
        riskMessage: `You will lose ${Math.round(predictedBalance)} days of leave if not used by year-end!`
      };
    }

    if (carryForwardAllowed && predictedBalance > maxCarryForward) {
      const leaveLoss = predictedBalance - maxCarryForward;
      return {
        riskLevel: 'high',
        riskMessage: `You will lose ${Math.round(leaveLoss)} days of leave (exceeds ${maxCarryForward} day carry-forward limit)!`
      };
    }

    // Low risk: Good utilization
    return {
      riskLevel: 'low',
      riskMessage: 'Healthy leave utilization. Keep balancing work and rest.'
    };
  }

  /**
   * Generate recommendations based on prediction
   */
  private generateRecommendations(
    currentBalance: number,
    predictedBalance: number,
    utilizationRate: number,
    carryForwardAllowed: boolean,
    maxCarryForward: number,
    publicHolidaysRemaining: number
  ): string[] {
    const recommendations: string[] = [];

    // Underutilization recommendations
    if (utilizationRate < 40) {
      recommendations.push('🚨 Take leave soon to avoid burnout and maintain productivity');
      recommendations.push('💡 Consider taking 2-3 days off next month for mental health');
    } else if (utilizationRate < 60) {
      recommendations.push('💡 Plan regular breaks throughout the remaining year');
    }

    // Leave loss prevention
    if (!carryForwardAllowed && predictedBalance > 0) {
      recommendations.push(`⚠️ Use ${Math.round(predictedBalance)} days before year-end or they will be lost`);
    } else if (carryForwardAllowed && predictedBalance > maxCarryForward) {
      const excess = predictedBalance - maxCarryForward;
      recommendations.push(`⚠️ Use ${Math.round(excess)} days before year-end (exceeds ${maxCarryForward} day carry-forward limit)`);
    }

    // Public holiday optimization
    if (publicHolidaysRemaining > 0) {
      recommendations.push(`🎉 ${publicHolidaysRemaining} public holidays remaining - plan long weekends!`);
    }

    // Current balance alerts
    if (currentBalance < 2) {
      recommendations.push('⚠️ Low leave balance. Request time off carefully until accrual catches up');
    } else if (currentBalance > 15) {
      recommendations.push('✓ Healthy leave balance. Good time to plan a vacation!');
    }

    // Optimal distribution
    recommendations.push('📅 Spread leave evenly throughout the year for best work-life balance');

    return recommendations;
  }

  /**
   * Generate suggested leave schedule
   */
  private generateLeaveSchedule(
    currentDate: Date,
    yearEnd: Date,
    predictedBalance: number,
    publicHolidays: PublicHolidayInfo[]
  ): LeaveScheduleSuggestion[] {
    const schedule: LeaveScheduleSuggestion[] = [];

    // If excess leave, suggest distribution
    if (predictedBalance > 5) {
      const monthsRemaining = Math.ceil(this.getMonthsRemaining(currentDate, yearEnd));
      const daysPerMonth = Math.ceil(predictedBalance / monthsRemaining);

      // Suggest leave around public holidays
      const upcomingHolidays = publicHolidays.filter(h => h.date > currentDate);

      upcomingHolidays.slice(0, 3).forEach(holiday => {
        const month = holiday.date.toLocaleString('default', { month: 'long' });
        schedule.push({
          month,
          suggestedDays: 2,
          reason: `Extend ${holiday.name} for a long weekend`,
          priority: 'high'
        });
      });

      // Fill remaining months
      const currentMonth = currentDate.getMonth();
      for (let i = 0; i < Math.min(monthsRemaining, 3); i++) {
        const targetMonth = new Date(currentDate.getFullYear(), currentMonth + i + 1, 1);
        const monthName = targetMonth.toLocaleString('default', { month: 'long' });

        // Skip if already suggested
        if (!schedule.find(s => s.month === monthName)) {
          schedule.push({
            month: monthName,
            suggestedDays: daysPerMonth,
            reason: 'Maintain regular breaks for work-life balance',
            priority: 'medium'
          });
        }
      }
    }

    return schedule.slice(0, 4); // Return top 4 suggestions
  }

  /**
   * Get public holidays remaining in the year
   */
  private getPublicHolidaysRemaining(holidays: PublicHolidayInfo[], currentDate: Date): PublicHolidayInfo[] {
    return holidays.filter(h => h.date > currentDate);
  }

  /**
   * Calculate leave that will expire (cannot be carried forward)
   */
  private calculateExpiringLeave(
    predictedBalance: number,
    carryForwardAllowed: boolean,
    maxCarryForward: number
  ): number {
    if (!carryForwardAllowed) {
      return Math.max(0, predictedBalance);
    }

    if (predictedBalance > maxCarryForward) {
      return predictedBalance - maxCarryForward;
    }

    return 0;
  }
}
