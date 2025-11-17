import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { ButtonComponent } from '../../../shared/ui/components/button/button';
import { UiModule } from '../../../shared/ui/ui.module';

interface ResendActivationRequest {
  subdomain: string;
  email: string;
}

interface ResendActivationResponse {
  success: boolean;
  message: string;
  expiresIn?: string;
  remainingAttempts?: number;
  retryAfterSeconds?: number;
  currentCount?: number;
  limit?: number;
}

/**
 * FORTUNE 500: Resend Activation Email Component
 * PATTERN: Slack/GitHub self-service activation recovery
 *
 * FEATURES:
 * - Dual-field validation (subdomain + email)
 * - Real-time input validation
 * - Rate limit handling with countdown
 * - Comprehensive error messaging
 * - Accessibility compliant (WCAG 2.1 AA)
 * - Mobile-responsive design
 */
@Component({
  selector: 'app-resend-activation',
  standalone: true,
  imports: [FormsModule, ButtonComponent, UiModule],
  templateUrl: './resend-activation.component.html',
  styleUrls: ['./resend-activation.component.scss']
})
export class ResendActivationComponent {
  // Form state
  subdomain = signal('');
  email = signal('');
  isLoading = signal(false);

  // UI state
  errorMessage = signal('');
  successMessage = signal('');
  showSuccess = signal(false);

  // Rate limiting state
  isRateLimited = signal(false);
  retryAfterMinutes = signal(0);
  remainingAttempts = signal<number | null>(null);

  // Validation patterns
  private readonly subdomainPattern = /^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$/;
  private readonly emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  /**
   * Subdomain input handler with real-time validation
   * FORTUNE 500: Sanitizes input to prevent injection attacks
   */
  onSubdomainInput(value: string | number): void {
    let cleanValue = String(value).toLowerCase().trim();

    // Remove any non-allowed characters (XSS prevention)
    cleanValue = cleanValue.replace(/[^a-z0-9-]/g, '');

    // Ensure it doesn't start or end with hyphen
    if (cleanValue.startsWith('-')) {
      cleanValue = cleanValue.substring(1);
    }
    if (cleanValue.endsWith('-')) {
      cleanValue = cleanValue.substring(0, cleanValue.length - 1);
    }

    this.subdomain.set(cleanValue);
    this.clearMessages();
  }

  /**
   * Email input handler with real-time validation
   */
  onEmailInput(value: string | number): void {
    const cleanValue = String(value).toLowerCase().trim();
    this.email.set(cleanValue);
    this.clearMessages();
  }

  /**
   * Validate subdomain format
   */
  isValidSubdomain(): boolean {
    const value = this.subdomain();
    if (!value || value.length < 3 || value.length > 50) {
      return false;
    }
    return this.subdomainPattern.test(value);
  }

  /**
   * Validate email format
   */
  isValidEmail(): boolean {
    const value = this.email();
    if (!value || value.length > 100) {
      return false;
    }
    return this.emailPattern.test(value);
  }

  /**
   * Check if form is valid
   */
  isFormValid(): boolean {
    return this.isValidSubdomain() && this.isValidEmail() && !this.isLoading() && !this.isRateLimited();
  }

  /**
   * Clear all messages
   */
  private clearMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
    this.showSuccess.set(false);
    this.isRateLimited.set(false);
  }

  /**
   * Resend activation email
   * FORTUNE 500: Comprehensive error handling with user-friendly messages
   */
  async onResend(): Promise<void> {
    // Validation
    if (!this.isValidSubdomain()) {
      this.errorMessage.set('Please enter a valid subdomain (3-50 characters, lowercase letters, numbers, and hyphens only)');
      return;
    }

    if (!this.isValidEmail()) {
      this.errorMessage.set('Please enter a valid email address');
      return;
    }

    this.isLoading.set(true);
    this.clearMessages();

    const requestBody: ResendActivationRequest = {
      subdomain: this.subdomain(),
      email: this.email()
    };

    try {
      const apiUrl = environment.apiUrl || 'http://localhost:5090';

      const response = await this.http.post<ResendActivationResponse>(
        `${apiUrl}/api/tenants/resend-activation`,
        requestBody
      ).toPromise();

      this.isLoading.set(false);

      if (response?.success) {
        // SUCCESS - Show success message with remaining attempts
        this.showSuccess.set(true);
        this.successMessage.set(response.message);

        if (response.remainingAttempts !== undefined) {
          this.remainingAttempts.set(response.remainingAttempts);
        }

        // Clear form
        this.subdomain.set('');
        this.email.set('');
      } else {
        // Unexpected response format
        this.errorMessage.set('Unexpected response from server. Please try again.');
      }
    } catch (error: any) {
      this.isLoading.set(false);
      this.handleError(error);
    }
  }

  /**
   * Handle HTTP errors with user-friendly messages
   * FORTUNE 500: Detailed error handling for different scenarios
   */
  private handleError(error: HttpErrorResponse): void {
    console.error('Resend activation error:', error);

    // Rate limiting (429)
    if (error.status === 429) {
      this.isRateLimited.set(true);

      const errorData = error.error;
      if (errorData?.retryAfterSeconds) {
        this.retryAfterMinutes.set(Math.ceil(errorData.retryAfterSeconds / 60));
      }

      this.errorMessage.set(
        errorData?.message ||
        'Too many resend requests. Please wait before trying again.'
      );
      return;
    }

    // Not found (404) - Tenant doesn't exist
    if (error.status === 404) {
      this.errorMessage.set(
        error.error?.message ||
        'No pending activation found for this company and email. Please check your details.'
      );
      return;
    }

    // Bad request (400) - Validation errors
    if (error.status === 400) {
      this.errorMessage.set(
        error.error?.message ||
        'Invalid request. Please check your subdomain and email address.'
      );
      return;
    }

    // Server error (500+)
    if (error.status >= 500) {
      this.errorMessage.set(
        'Server error. Please try again later or contact support at support@morishr.com'
      );
      return;
    }

    // Network error or unknown
    if (error.status === 0) {
      this.errorMessage.set(
        'Unable to connect to server. Please check your internet connection.'
      );
      return;
    }

    // Generic error
    this.errorMessage.set(
      error.error?.message ||
      'An error occurred. Please try again or contact support.'
    );
  }

  /**
   * Navigate to login page
   */
  goToLogin(): void {
    this.router.navigate(['/auth/subdomain']);
  }

  /**
   * Contact support via email
   */
  contactSupport(): void {
    window.location.href = 'mailto:support@morishr.com?subject=Activation Help Needed';
  }

  /**
   * Format retry time for user display
   */
  getRetryMessage(): string {
    const minutes = this.retryAfterMinutes();
    if (minutes <= 0) return '';

    if (minutes === 1) {
      return 'Please wait 1 minute before trying again.';
    }

    return `Please wait ${minutes} minutes before trying again.`;
  }

  /**
   * Get remaining attempts message
   */
  getRemainingAttemptsMessage(): string {
    const remaining = this.remainingAttempts();
    if (remaining === null || remaining === undefined) return '';

    if (remaining === 0) {
      return 'This was your last attempt for the next hour.';
    }

    if (remaining === 1) {
      return 'You have 1 attempt remaining in the next hour.';
    }

    return `You have ${remaining} attempts remaining in the next hour.`;
  }
}
