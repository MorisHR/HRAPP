import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

/**
 * Error response structure from backend
 */
export interface ApiErrorResponse {
  statusCode: number;
  errorCode: string;
  message: string;
  suggestedAction?: string;
  correlationId: string;
  timestamp: string;
  supportContact?: string;
}

/**
 * User-friendly error message with guidance
 */
export interface UserError {
  title: string;
  message: string;
  action: string;
  errorCode: string;
  correlationId: string;
  showSupport: boolean;
}

/**
 * Fortune 500 Grade Error Handling Service
 * Transforms technical errors into user-friendly messages
 * Provides actionable guidance and support information
 */
@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  private readonly supportEmail = 'support@morishr.com';

  /**
   * Transform any error into a user-friendly message
   */
  handleError(error: any, context?: string): UserError {
    // Network/Connection errors
    if (error.status === 0) {
      return {
        title: 'Connection Lost',
        message: 'Unable to connect to the server. Please check your internet connection.',
        action: 'Check your network connection and try again.',
        errorCode: 'NET_001',
        correlationId: this.generateCorrelationId(),
        showSupport: false
      };
    }

    // HTTP Error Response
    if (error instanceof HttpErrorResponse) {
      return this.handleHttpError(error, context);
    }

    // Generic error
    return {
      title: 'Unexpected Error',
      message: 'An unexpected error occurred.',
      action: 'Please try again. If this continues, contact support.',
      errorCode: 'FE_UNKNOWN',
      correlationId: this.generateCorrelationId(),
      showSupport: true
    };
  }

  /**
   * Handle HTTP errors with proper status code mapping
   */
  private handleHttpError(error: HttpErrorResponse, context?: string): UserError {
    // Check if backend returned structured error
    if (this.isApiErrorResponse(error.error)) {
      return this.transformApiError(error.error);
    }

    // Fallback to status code based messages
    return this.getStatusCodeError(error.status, context);
  }

  /**
   * Check if error response matches our API error structure
   */
  private isApiErrorResponse(error: any): error is ApiErrorResponse {
    return error &&
           typeof error.errorCode === 'string' &&
           typeof error.message === 'string' &&
           typeof error.correlationId === 'string';
  }

  /**
   * Transform backend API error to user-friendly format
   */
  private transformApiError(apiError: ApiErrorResponse): UserError {
    return {
      title: this.getTitleForErrorCode(apiError.errorCode),
      message: apiError.message,
      action: apiError.suggestedAction || 'Please try again or contact support.',
      errorCode: apiError.errorCode,
      correlationId: apiError.correlationId,
      showSupport: apiError.statusCode >= 500
    };
  }

  /**
   * Get user-friendly error messages based on HTTP status code
   */
  private getStatusCodeError(status: number, context?: string): UserError {
    const correlationId = this.generateCorrelationId();
    const contextStr = context ? ` ${context}` : '';

    switch (status) {
      case 400:
        return {
          title: 'Invalid Request',
          message: `The${contextStr} information provided is invalid.`,
          action: 'Please review your input and try again.',
          errorCode: 'HTTP_400',
          correlationId,
          showSupport: false
        };

      case 401:
        return {
          title: 'Session Expired',
          message: 'Your session has expired for security.',
          action: 'Please sign in again to continue.',
          errorCode: 'HTTP_401',
          correlationId,
          showSupport: false
        };

      case 403:
        return {
          title: 'Access Denied',
          message: "You don't have permission to access this resource.",
          action: 'Contact your administrator if you need access.',
          errorCode: 'HTTP_403',
          correlationId,
          showSupport: false
        };

      case 404:
        return {
          title: 'Not Found',
          message: `The requested${contextStr} could not be found.`,
          action: 'It may have been removed or you may not have access.',
          errorCode: 'HTTP_404',
          correlationId,
          showSupport: false
        };

      case 409:
        return {
          title: 'Conflict',
          message: `This${contextStr} conflicts with existing information.`,
          action: 'Please review and try again with different information.',
          errorCode: 'HTTP_409',
          correlationId,
          showSupport: false
        };

      case 422:
        return {
          title: 'Validation Failed',
          message: 'Some of your information doesn\'t meet the requirements.',
          action: 'Please review the highlighted fields and correct any errors.',
          errorCode: 'HTTP_422',
          correlationId,
          showSupport: false
        };

      case 429:
        return {
          title: 'Too Many Requests',
          message: 'You\'ve made too many requests. Please slow down.',
          action: 'Wait a moment and try again.',
          errorCode: 'HTTP_429',
          correlationId,
          showSupport: false
        };

      case 500:
      case 502:
      case 503:
      case 504:
        return {
          title: 'System Error',
          message: 'We\'re experiencing technical difficulties. Our team has been notified.',
          action: `Please try again in a few moments. If this continues, contact ${this.supportEmail} with error ID: ${correlationId}`,
          errorCode: `HTTP_${status}`,
          correlationId,
          showSupport: true
        };

      default:
        return {
          title: 'Request Failed',
          message: `Unable to complete your request${contextStr}.`,
          action: 'Please try again or contact support if this continues.',
          errorCode: `HTTP_${status}`,
          correlationId,
          showSupport: true
        };
    }
  }

  /**
   * Get friendly title based on error code
   */
  private getTitleForErrorCode(errorCode: string): string {
    if (errorCode.startsWith('AUTH_')) return 'Authentication Error';
    if (errorCode.startsWith('EMP_')) return 'Employee Error';
    if (errorCode.startsWith('ATT_')) return 'Attendance Error';
    if (errorCode.startsWith('PAY_')) return 'Payroll Error';
    if (errorCode.startsWith('LEV_')) return 'Leave Error';
    if (errorCode.startsWith('LOC_')) return 'Location Error';
    if (errorCode.startsWith('DEV_')) return 'Device Error';
    if (errorCode.startsWith('TEN_')) return 'Tenant Error';
    if (errorCode.startsWith('SEC_')) return 'Security Error';
    if (errorCode.startsWith('VAL_')) return 'Validation Error';
    if (errorCode.startsWith('SYS_')) return 'System Error';
    return 'Error';
  }

  /**
   * Generate correlation ID for error tracking
   */
  private generateCorrelationId(): string {
    const timestamp = Date.now().toString(36);
    const random = Math.random().toString(36).substring(2, 7);
    return `ERR-${timestamp}-${random}`.toUpperCase();
  }

  /**
   * Format error for logging (with more details)
   */
  logError(error: any, context?: string): void {
    const userError = this.handleError(error, context);
    console.error('[ErrorHandler]', {
      correlationId: userError.correlationId,
      errorCode: userError.errorCode,
      context,
      originalError: error
    });
  }
}
