import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EmailConfigStatusDto {
  smtpServer: string;
  smtpPort: number;
  fromEmail: string;
  fromName?: string;
  enableSsl: boolean;
  hasUsername: boolean;
  hasPassword: boolean;
  isConfigured: boolean;
  recommendation: string;
}

export interface TestEmailRequest {
  toEmail: string;
}

export interface SendTestEmailResponse {
  success: boolean;
  message: string;
  sentAt?: string;
  configuration?: {
    smtpServer: string;
    smtpPort: number;
    fromEmail: string;
  };
  error?: string;
  troubleshooting?: string[];
}

export interface SendSubscriptionTemplatesResponse {
  success: boolean;
  message: string;
  results?: Array<{ template: string; status: string }>;
  error?: string;
  sentTemplates?: Array<{ template: string; status: string }>;
}

export interface ValidationResult {
  field: string;
  status: 'ok' | 'warning' | 'error';
  message?: string;
  value?: any;
}

export interface ValidateConfigurationResponse {
  success: boolean;
  isValid: boolean;
  validationResults: ValidationResult[];
  recommendations: string[];
}

@Injectable({
  providedIn: 'root'
})
export class EmailTestService {
  private apiUrl = `${environment.apiUrl}/admin/EmailTest`;

  constructor(private http: HttpClient) {}

  /**
   * Get current email configuration status (without exposing credentials)
   * GET /api/admin/emailtest/config-status
   *
   * Requires: SuperAdmin role
   */
  getConfigurationStatus(): Observable<{ success: boolean; data: EmailConfigStatusDto }> {
    return this.http.get<{ success: boolean; data: EmailConfigStatusDto }>(`${this.apiUrl}/config-status`);
  }

  /**
   * Send a test email to verify email configuration
   * POST /api/admin/emailtest/send-test
   *
   * Requires: SuperAdmin role
   *
   * @param toEmail Email address to send test to
   */
  sendTestEmail(toEmail: string): Observable<SendTestEmailResponse> {
    return this.http.post<SendTestEmailResponse>(`${this.apiUrl}/send-test`, { toEmail });
  }

  /**
   * Send test emails for all subscription notification types
   * POST /api/admin/emailtest/send-subscription-templates
   *
   * Requires: SuperAdmin role
   *
   * Sends test emails for:
   * - 30-day renewal reminder
   * - 7-day expiring warning
   * - 1-day final warning
   *
   * @param toEmail Email address to send tests to
   */
  sendSubscriptionTemplates(toEmail: string): Observable<SendSubscriptionTemplatesResponse> {
    return this.http.post<SendSubscriptionTemplatesResponse>(`${this.apiUrl}/send-subscription-templates`, { toEmail });
  }

  /**
   * Validate email configuration without sending emails
   * GET /api/admin/emailtest/validate
   *
   * Requires: SuperAdmin role
   *
   * Performs validation checks on:
   * - SMTP server configuration
   * - SMTP port
   * - From email address
   * - Credentials
   * - SSL/TLS settings
   */
  validateConfiguration(): Observable<ValidateConfigurationResponse> {
    return this.http.get<ValidateConfigurationResponse>(`${this.apiUrl}/validate`);
  }
}
