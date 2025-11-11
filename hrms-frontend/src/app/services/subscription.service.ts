import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SubscriptionPayment,
  RecordPaymentRequest,
  RevenueAnalytics,
  UpcomingRenewal,
  TenantSubscriptionHistory,
  SubscriptionOverview,
  PaymentReminderResponse,
  PaymentStatus,
  SubscriptionTier
} from '../models/subscription.model';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
  private http = inject(HttpClient);
  private readonly API_BASE = '/api/subscription-payments';

  /**
   * Get all pending subscription payments
   * GET /api/subscription-payments/pending
   */
  getPendingPayments(): Observable<SubscriptionPayment[]> {
    return this.http.get<SubscriptionPayment[]>(`${this.API_BASE}/pending`);
  }

  /**
   * Get all overdue subscription payments
   * GET /api/subscription-payments/overdue
   */
  getOverduePayments(): Observable<SubscriptionPayment[]> {
    return this.http.get<SubscriptionPayment[]>(`${this.API_BASE}/overdue`);
  }

  /**
   * Get subscription payment history for a specific tenant
   * GET /api/subscription-payments/tenant/{tenantId}
   */
  getTenantPaymentHistory(tenantId: number): Observable<TenantSubscriptionHistory> {
    return this.http.get<TenantSubscriptionHistory>(`${this.API_BASE}/tenant/${tenantId}`);
  }

  /**
   * Record a payment for a subscription
   * POST /api/subscription-payments/{paymentId}/record
   */
  recordPayment(paymentId: number, request: RecordPaymentRequest): Observable<SubscriptionPayment> {
    return this.http.post<SubscriptionPayment>(
      `${this.API_BASE}/${paymentId}/record`,
      request
    );
  }

  /**
   * Send payment reminder to tenant
   * POST /api/subscription-payments/{paymentId}/reminder
   */
  sendPaymentReminder(paymentId: number): Observable<PaymentReminderResponse> {
    return this.http.post<PaymentReminderResponse>(
      `${this.API_BASE}/${paymentId}/reminder`,
      {}
    );
  }

  /**
   * Get revenue analytics and insights
   * GET /api/subscription-payments/revenue-analytics
   */
  getRevenueAnalytics(): Observable<RevenueAnalytics> {
    return this.http.get<RevenueAnalytics>(`${this.API_BASE}/revenue-analytics`);
  }

  /**
   * Get upcoming subscription renewals (next 30 days)
   * GET /api/subscription-payments/upcoming-renewals
   */
  getUpcomingRenewals(days: number = 30): Observable<UpcomingRenewal[]> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<UpcomingRenewal[]>(`${this.API_BASE}/upcoming-renewals`, { params });
  }

  /**
   * Get subscription overview dashboard data
   * GET /api/subscription-payments/overview
   */
  getSubscriptionOverview(): Observable<SubscriptionOverview> {
    return this.http.get<SubscriptionOverview>(`${this.API_BASE}/overview`);
  }

  /**
   * Get all payments by status
   * GET /api/subscription-payments?status={status}
   */
  getPaymentsByStatus(status: PaymentStatus): Observable<SubscriptionPayment[]> {
    const params = new HttpParams().set('status', status);
    return this.http.get<SubscriptionPayment[]>(this.API_BASE, { params });
  }

  /**
   * Get all payments by subscription tier
   * GET /api/subscription-payments?tier={tier}
   */
  getPaymentsByTier(tier: SubscriptionTier): Observable<SubscriptionPayment[]> {
    const params = new HttpParams().set('tier', tier);
    return this.http.get<SubscriptionPayment[]>(this.API_BASE, { params });
  }

  /**
   * Get payment details by ID
   * GET /api/subscription-payments/{id}
   */
  getPaymentById(id: number): Observable<SubscriptionPayment> {
    return this.http.get<SubscriptionPayment>(`${this.API_BASE}/${id}`);
  }

  // Utility methods

  /**
   * Format currency amount
   */
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  }

  /**
   * Get status badge color
   */
  getStatusColor(status: PaymentStatus): string {
    const colors: Record<PaymentStatus, string> = {
      [PaymentStatus.Paid]: 'success',
      [PaymentStatus.Pending]: 'warning',
      [PaymentStatus.Overdue]: 'error',
      [PaymentStatus.Failed]: 'error',
      [PaymentStatus.Cancelled]: 'secondary'
    };
    return colors[status] || 'default';
  }

  /**
   * Get tier badge color
   */
  getTierColor(tier: SubscriptionTier): string {
    const colors: Record<SubscriptionTier, string> = {
      [SubscriptionTier.Free]: 'secondary',
      [SubscriptionTier.Basic]: 'primary',
      [SubscriptionTier.Professional]: 'accent',
      [SubscriptionTier.Enterprise]: 'warn'
    };
    return colors[tier] || 'default';
  }

  /**
   * Calculate days overdue
   */
  getDaysOverdue(dueDate: string): number {
    const due = new Date(dueDate);
    const now = new Date();
    const diffTime = now.getTime() - due.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return Math.max(0, diffDays);
  }

  /**
   * Calculate days until due
   */
  getDaysUntilDue(dueDate: string): number {
    const due = new Date(dueDate);
    const now = new Date();
    const diffTime = due.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  }

  /**
   * Check if payment is overdue
   */
  isOverdue(dueDate: string): boolean {
    const due = new Date(dueDate);
    const now = new Date();
    return now > due;
  }

  /**
   * Format date
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  /**
   * Format relative date (e.g., "2 days ago", "in 5 days")
   */
  formatRelativeDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = date.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Tomorrow';
    if (diffDays === -1) return 'Yesterday';
    if (diffDays > 0) return `in ${diffDays} days`;
    return `${Math.abs(diffDays)} days ago`;
  }
}
