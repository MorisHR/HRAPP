import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

/**
 * PRODUCTION-GRADE: Tenant billing and subscription service
 * Integrates with backend TenantBillingController
 * Handles subscription details and payment history
 */

export interface TenantSubscriptionResponse {
  success: boolean;
  data: TenantSubscriptionData;
  message?: string;
}

export interface TenantSubscriptionData {
  subscription: SubscriptionDetails;
  payments: PaymentHistory[];
}

export interface SubscriptionDetails {
  id: string;
  tenantId: string;
  tier: string;
  monthlyPrice: number;
  status: string;
  currentPeriodStart: string | null;
  currentPeriodEnd: string | null;
  autoRenew: boolean;
  gracePeriodEndDate: string | null;
  daysUntilExpiry: number | null;
  isExpired: boolean;
}

export interface PaymentHistory {
  id: string;
  subscriptionId: string;
  amount: number;
  dueDate: string;
  paidDate: string | null;
  status: string;
  paymentMethod: string;
  invoiceNumber: string | null;
  periodStartDate: string;
  periodEndDate: string;
  isOverdue: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class BillingService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/tenant/subscription`;

  /**
   * Get current tenant's subscription and payment history
   * Backend: GET /api/tenant/subscription
   * Security: Requires authentication with Admin or HR role
   */
  getSubscription(): Observable<TenantSubscriptionData> {
    return this.http.get<TenantSubscriptionResponse>(this.apiUrl).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to load subscription data');
        }
        return response.data;
      })
    );
  }

  /**
   * Download invoice for a payment
   * TODO: Backend endpoint to be implemented
   */
  downloadInvoice(paymentId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/invoices/${paymentId}/download`, {
      responseType: 'blob'
    });
  }
}
