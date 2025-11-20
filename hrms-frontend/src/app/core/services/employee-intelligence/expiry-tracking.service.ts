// ═══════════════════════════════════════════════════════════════
// EXPIRY TRACKING SERVICE - Production Grade
// Document expiry calculation and alert generation
// NO AI - Pure date mathematics
// Multi-tenant safe, high performance
// ═══════════════════════════════════════════════════════════════

import { Injectable } from '@angular/core';
import { ExpiryAlert } from '../../models/employee-intelligence.model';

@Injectable({
  providedIn: 'root'
})
export class ExpiryTrackingService {

  // Alert thresholds (days before expiry)
  private readonly CRITICAL_THRESHOLD = 30;
  private readonly URGENT_THRESHOLD = 60;
  private readonly WARNING_THRESHOLD = 90;
  private readonly INFO_THRESHOLD = 180;

  // Renewal lead times (days needed for processing)
  private readonly RENEWAL_LEAD_TIMES: Record<string, number> = {
    'passport': 90,
    'workPermit': 60,
    'visa': 45,
    'residencePermit': 60,
    'drivingLicense': 30,
    'healthInsurance': 15,
    'employmentContract': 30
  };

  /**
   * Calculate expiry alerts for multiple documents
   * Performance: < 1ms per document
   * Multi-tenant safe: Stateless calculations
   */
  calculateExpiryAlerts(documents: Record<string, Date | null>): ExpiryAlert[] {
    try {
      const alerts: ExpiryAlert[] = [];
      const today = new Date();
      today.setHours(0, 0, 0, 0); // Normalize to start of day

      for (const [docType, expiryDate] of Object.entries(documents)) {
        if (!expiryDate || !(expiryDate instanceof Date)) {
          continue;
        }

        const alert = this.createAlert(docType, expiryDate, today);
        if (alert) {
          alerts.push(alert);
        }
      }

      // Sort by days remaining (most urgent first)
      return alerts.sort((a, b) => a.daysRemaining - b.daysRemaining);

    } catch (error) {
      console.error('[ExpiryTrackingService] Error calculating alerts:', error);
      return [];
    }
  }

  /**
   * Check if document is expiring soon
   */
  isExpiringSoon(expiryDate: Date, thresholdDays: number = 90): boolean {
    try {
      const today = new Date();
      const daysRemaining = this.calculateDaysRemaining(expiryDate, today);
      return daysRemaining >= 0 && daysRemaining <= thresholdDays;
    } catch (error) {
      return false;
    }
  }

  /**
   * Check if document is already expired
   */
  isExpired(expiryDate: Date): boolean {
    try {
      const today = new Date();
      return expiryDate < today;
    } catch (error) {
      return false;
    }
  }

  /**
   * Get renewal deadline (expiry date - lead time)
   */
  getRenewalDeadline(expiryDate: Date, documentType: string): Date {
    try {
      const leadTime = this.RENEWAL_LEAD_TIMES[documentType] || 30;
      const deadline = new Date(expiryDate);
      deadline.setDate(deadline.getDate() - leadTime);
      return deadline;
    } catch (error) {
      return expiryDate;
    }
  }

  /**
   * Format days remaining as human-readable string
   */
  formatDaysRemaining(days: number): string {
    if (days < 0) {
      return `Expired ${Math.abs(days)} days ago`;
    }
    if (days === 0) {
      return 'Expires today';
    }
    if (days === 1) {
      return 'Expires tomorrow';
    }
    if (days < 30) {
      return `${days} days remaining`;
    }
    if (days < 60) {
      const weeks = Math.floor(days / 7);
      return `${weeks} ${weeks === 1 ? 'week' : 'weeks'} remaining`;
    }
    const months = Math.floor(days / 30);
    return `${months} ${months === 1 ? 'month' : 'months'} remaining`;
  }

  /**
   * Create alert for single document
   */
  private createAlert(
    documentType: string,
    expiryDate: Date,
    today: Date
  ): ExpiryAlert | null {
    try {
      const daysRemaining = this.calculateDaysRemaining(expiryDate, today);
      const severity = this.calculateSeverity(daysRemaining);
      const renewalLeadTime = this.RENEWAL_LEAD_TIMES[documentType] || 30;
      const actionDeadline = new Date(expiryDate);
      actionDeadline.setDate(actionDeadline.getDate() - renewalLeadTime);

      const documentNames: Record<string, string> = {
        'passport': 'Passport',
        'passportExpiry': 'Passport',
        'workPermit': 'Work Permit',
        'workPermitExpiry': 'Work Permit',
        'visa': 'Visa',
        'visaExpiry': 'Visa',
        'residencePermit': 'Residence Permit',
        'passportExpiryDate': 'Passport',
        'workPermitExpiryDate': 'Work Permit',
        'visaExpiryDate': 'Visa'
      };

      return {
        documentType,
        documentName: documentNames[documentType] || documentType,
        expiryDate,
        daysRemaining,
        severity,
        actionRequired: this.getActionRequired(severity, daysRemaining),
        actionDeadline,
        renewalLeadTime,
        icon: this.getIcon(severity),
        color: this.getColor(severity)
      };

    } catch (error) {
      return null;
    }
  }

  /**
   * Calculate days between today and expiry
   */
  private calculateDaysRemaining(expiryDate: Date, today: Date): number {
    const expiry = new Date(expiryDate);
    expiry.setHours(0, 0, 0, 0);

    const todayNorm = new Date(today);
    todayNorm.setHours(0, 0, 0, 0);

    const diffMs = expiry.getTime() - todayNorm.getTime();
    return Math.floor(diffMs / (1000 * 60 * 60 * 24));
  }

  /**
   * Determine severity based on days remaining
   */
  private calculateSeverity(daysRemaining: number): 'critical' | 'urgent' | 'warning' | 'info' {
    if (daysRemaining < 0 || daysRemaining <= this.CRITICAL_THRESHOLD) {
      return 'critical';
    }
    if (daysRemaining <= this.URGENT_THRESHOLD) {
      return 'urgent';
    }
    if (daysRemaining <= this.WARNING_THRESHOLD) {
      return 'warning';
    }
    return 'info';
  }

  /**
   * Get action message based on severity
   */
  private getActionRequired(severity: string, daysRemaining: number): string {
    if (daysRemaining < 0) {
      return 'EXPIRED: Immediate renewal required - employee cannot work legally';
    }

    switch (severity) {
      case 'critical':
        return 'URGENT: Start renewal process immediately (< 30 days)';
      case 'urgent':
        return 'Prepare renewal documents now (< 60 days)';
      case 'warning':
        return 'Plan renewal process (< 90 days)';
      default:
        return 'Monitor expiry date';
    }
  }

  /**
   * Get icon for severity level
   */
  private getIcon(severity: string): string {
    const icons: Record<string, string> = {
      'critical': 'error',
      'urgent': 'warning',
      'warning': 'info',
      'info': 'schedule'
    };
    return icons[severity] || 'info';
  }

  /**
   * Get color for severity level
   */
  private getColor(severity: string): string {
    const colors: Record<string, string> = {
      'critical': '#D32F2F', // Red
      'urgent': '#F57C00',   // Orange
      'warning': '#FBC02D',  // Yellow
      'info': '#1976D2'      // Blue
    };
    return colors[severity] || '#1976D2';
  }
}
