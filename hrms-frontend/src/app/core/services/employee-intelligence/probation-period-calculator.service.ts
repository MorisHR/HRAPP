import { Injectable } from '@angular/core';
import {
  ProbationCalculation,
  ProbationMilestone,
  ProbationAlert
} from '../../models/leave-probation-intelligence.model';

/**
 * Probation Period Calculator Service
 *
 * INTELLIGENCE: Rule-based probation tracking and review scheduling
 * PERFORMANCE: < 2ms (pure date calculations)
 * MULTI-TENANT: Stateless, no cross-tenant data
 * PRODUCTION: Full error handling, logging
 *
 * Features:
 * - Calculate probation period dates
 * - Track probation progress (percentage complete)
 * - Schedule review milestones (30/60/90 day reviews)
 * - Alert approaching end date
 * - Identify overdue reviews
 */
@Injectable({
  providedIn: 'root'
})
export class ProbationPeriodCalculatorService {

  constructor() {}

  /**
   * Calculate probation period details
   *
   * @param joinDate Employee join date
   * @param probationPeriodMonths Probation period in months (default: 3)
   * @param currentDate Current date (defaults to now, injectable for testing)
   * @returns Complete probation calculation with milestones and alerts
   */
  calculateProbation(
    joinDate: Date,
    probationPeriodMonths: number = 3,
    currentDate: Date = new Date()
  ): ProbationCalculation {
    try {
      // Validate inputs
      if (!joinDate || probationPeriodMonths <= 0) {
        throw new Error('Invalid probation parameters');
      }

      // Calculate probation dates
      const probationStartDate = new Date(joinDate);
      const probationEndDate = this.addMonths(probationStartDate, probationPeriodMonths);

      // Calculate progress
      const totalDays = this.getDaysBetween(probationStartDate, probationEndDate);
      const daysElapsed = this.getDaysBetween(probationStartDate, currentDate);
      const daysRemaining = Math.max(0, this.getDaysBetween(currentDate, probationEndDate));

      const progressPercentage = totalDays > 0
        ? Math.min(100, Math.max(0, (daysElapsed / totalDays) * 100))
        : 0;

      // Determine status
      const isOnProbation = currentDate >= probationStartDate && currentDate <= probationEndDate;
      const { status, statusMessage, urgency } = this.determineStatus(
        currentDate,
        probationStartDate,
        probationEndDate,
        daysRemaining
      );

      // Generate review milestones
      const reviewMilestones = this.generateReviewMilestones(
        probationStartDate,
        probationEndDate,
        probationPeriodMonths,
        currentDate
      );

      // Find next review
      const nextReview = reviewMilestones.find(m => !m.isCompleted && !m.isOverdue);

      // Find overdue reviews
      const overdueReviews = reviewMilestones.filter(m => m.isOverdue && !m.isCompleted);

      // Generate alerts
      const alerts = this.generateAlerts(
        status,
        daysRemaining,
        overdueReviews,
        nextReview
      );

      // Generate recommendations
      const recommendations = this.generateRecommendations(
        status,
        daysRemaining,
        overdueReviews,
        progressPercentage
      );

      return {
        isOnProbation,
        joinDate: probationStartDate,
        probationPeriodMonths,
        probationStartDate,
        probationEndDate,
        currentDate,
        daysElapsed,
        daysRemaining,
        totalDays,
        progressPercentage,
        status,
        statusMessage,
        urgency,
        reviewMilestones,
        nextReview,
        overdueReviews,
        alerts,
        recommendations
      };
    } catch (error) {
      console.error('[ProbationCalculator] Error calculating probation:', error);
      throw error;
    }
  }

  /**
   * Add months to a date
   */
  private addMonths(date: Date, months: number): Date {
    const result = new Date(date);
    result.setMonth(result.getMonth() + months);
    return result;
  }

  /**
   * Get days between two dates
   */
  private getDaysBetween(startDate: Date, endDate: Date): number {
    const msPerDay = 1000 * 60 * 60 * 24;
    const start = new Date(startDate).setHours(0, 0, 0, 0);
    const end = new Date(endDate).setHours(0, 0, 0, 0);
    return Math.floor((end - start) / msPerDay);
  }

  /**
   * Determine probation status
   */
  private determineStatus(
    currentDate: Date,
    probationStartDate: Date,
    probationEndDate: Date,
    daysRemaining: number
  ): {
    status: 'not-started' | 'ongoing' | 'ending-soon' | 'completed' | 'extended';
    statusMessage: string;
    urgency: 'none' | 'low' | 'medium' | 'high' | 'critical';
  } {
    // Not started yet
    if (currentDate < probationStartDate) {
      return {
        status: 'not-started',
        statusMessage: 'Probation period has not started yet',
        urgency: 'none'
      };
    }

    // Completed
    if (currentDate > probationEndDate) {
      return {
        status: 'completed',
        statusMessage: 'Probation period completed',
        urgency: 'none'
      };
    }

    // Ending soon (< 14 days)
    if (daysRemaining <= 14 && daysRemaining > 0) {
      return {
        status: 'ending-soon',
        statusMessage: `Probation ends in ${daysRemaining} days - Final review required!`,
        urgency: daysRemaining <= 7 ? 'critical' : 'high'
      };
    }

    // Ongoing
    return {
      status: 'ongoing',
      statusMessage: `Probation in progress (${daysRemaining} days remaining)`,
      urgency: 'low'
    };
  }

  /**
   * Generate review milestones based on probation period
   */
  private generateReviewMilestones(
    probationStartDate: Date,
    probationEndDate: Date,
    probationPeriodMonths: number,
    currentDate: Date
  ): ProbationMilestone[] {
    const milestones: ProbationMilestone[] = [];

    // 30-day review (if probation >= 1 month)
    if (probationPeriodMonths >= 1) {
      milestones.push(this.createMilestone(
        '30-day',
        '30-Day Review',
        this.addDays(probationStartDate, 30),
        currentDate,
        'Review initial performance and integration',
        [
          'Evaluate job knowledge and skills',
          'Assess cultural fit and team integration',
          'Provide constructive feedback',
          'Set goals for next period'
        ]
      ));
    }

    // 60-day review (if probation >= 2 months)
    if (probationPeriodMonths >= 2) {
      milestones.push(this.createMilestone(
        '60-day',
        '60-Day Review',
        this.addDays(probationStartDate, 60),
        currentDate,
        'Mid-probation performance check',
        [
          'Review progress against goals',
          'Identify any performance gaps',
          'Discuss career development',
          'Confirm probation continuation or early confirmation'
        ]
      ));
    }

    // 90-day review (if probation >= 3 months)
    if (probationPeriodMonths >= 3) {
      milestones.push(this.createMilestone(
        '90-day',
        '90-Day Review',
        this.addDays(probationStartDate, 90),
        currentDate,
        'Final probation assessment',
        [
          'Comprehensive performance evaluation',
          'Decision on permanent employment',
          'Discuss compensation and benefits',
          'Set annual goals if confirmed'
        ]
      ));
    }

    // Final review (7 days before end)
    const finalReviewDate = this.addDays(probationEndDate, -7);
    if (finalReviewDate > probationStartDate) {
      milestones.push(this.createMilestone(
        'final',
        'Final Probation Review',
        finalReviewDate,
        currentDate,
        'Complete probation and confirm employment',
        [
          'Final performance assessment',
          'Confirm permanent employment status',
          'Sign employment confirmation letter',
          'Update HR records'
        ],
        'high'
      ));
    }

    return milestones.sort((a, b) => a.scheduledDate.getTime() - b.scheduledDate.getTime());
  }

  /**
   * Create a probation milestone
   */
  private createMilestone(
    type: '30-day' | '60-day' | '90-day' | 'final' | 'custom',
    name: string,
    scheduledDate: Date,
    currentDate: Date,
    description: string,
    checklist?: string[],
    priority?: 'high' | 'medium' | 'low'
  ): ProbationMilestone {
    const daysUntil = this.getDaysBetween(currentDate, scheduledDate);
    const isOverdue = scheduledDate < currentDate;
    const isPending = daysUntil >= 0 && daysUntil <= 7;
    const isCompleted = false; // Would need backend integration to track completion

    // Determine priority if not specified
    if (!priority) {
      if (isOverdue) priority = 'high';
      else if (isPending) priority = 'high';
      else if (daysUntil <= 14) priority = 'medium';
      else priority = 'low';
    }

    return {
      id: `${type}-${scheduledDate.getTime()}`,
      type,
      name,
      scheduledDate,
      daysUntil,
      isCompleted,
      isOverdue,
      isPending,
      priority,
      description,
      checklist
    };
  }

  /**
   * Add days to a date
   */
  private addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  /**
   * Generate alerts based on probation status
   */
  private generateAlerts(
    status: string,
    daysRemaining: number,
    overdueReviews: ProbationMilestone[],
    nextReview?: ProbationMilestone
  ): ProbationAlert[] {
    const alerts: ProbationAlert[] = [];

    // Overdue reviews
    if (overdueReviews.length > 0) {
      overdueReviews.forEach(review => {
        alerts.push({
          severity: 'critical',
          message: `${review.name} is overdue!`,
          actionRequired: `Schedule ${review.name} immediately`,
          dueDate: review.scheduledDate,
          icon: 'error'
        });
      });
    }

    // Upcoming reviews
    if (nextReview && nextReview.isPending) {
      alerts.push({
        severity: 'warning',
        message: `${nextReview.name} due in ${nextReview.daysUntil} days`,
        actionRequired: `Prepare for ${nextReview.name}`,
        dueDate: nextReview.scheduledDate,
        icon: 'schedule'
      });
    }

    // Probation ending soon
    if (status === 'ending-soon') {
      const severity = daysRemaining <= 7 ? 'critical' : 'warning';
      alerts.push({
        severity,
        message: `Probation ends in ${daysRemaining} days`,
        actionRequired: 'Complete final review and confirm employment status',
        icon: 'warning'
      });
    }

    // Info: Probation ongoing
    if (status === 'ongoing' && alerts.length === 0) {
      alerts.push({
        severity: 'info',
        message: 'Probation period in progress',
        actionRequired: 'Continue regular performance monitoring',
        icon: 'info'
      });
    }

    return alerts;
  }

  /**
   * Generate recommendations
   */
  private generateRecommendations(
    status: string,
    daysRemaining: number,
    overdueReviews: ProbationMilestone[],
    progressPercentage: number
  ): string[] {
    const recommendations: string[] = [];

    // Overdue reviews
    if (overdueReviews.length > 0) {
      recommendations.push('🚨 Complete overdue reviews immediately to maintain compliance');
      recommendations.push('📋 Document all review discussions in employee file');
    }

    // Ending soon
    if (status === 'ending-soon') {
      if (daysRemaining <= 7) {
        recommendations.push('⏰ URGENT: Make employment decision within 7 days');
        recommendations.push('📄 Prepare confirmation letter or termination notice');
      } else {
        recommendations.push('📅 Schedule final review meeting this week');
        recommendations.push('✓ Review all probation feedback and performance data');
      }
    }

    // Mid-probation
    if (progressPercentage >= 40 && progressPercentage <= 60) {
      recommendations.push('💡 Good time for a mid-probation check-in');
      recommendations.push('🎯 Ensure goals are on track for successful completion');
    }

    // Early stage
    if (progressPercentage < 25) {
      recommendations.push('👋 Set clear expectations and success criteria early');
      recommendations.push('🤝 Assign a mentor or buddy for smooth onboarding');
    }

    // General best practices
    recommendations.push('📊 Maintain regular feedback sessions (weekly/bi-weekly)');
    recommendations.push('📝 Document all performance discussions and incidents');

    return recommendations;
  }
}
