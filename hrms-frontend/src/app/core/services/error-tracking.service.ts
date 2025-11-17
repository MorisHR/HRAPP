import { Injectable, signal, computed, inject, ErrorHandler } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { AnalyticsService, ComponentLibrary, AnalyticsEventType } from './analytics.service';
import { FeatureModule } from './feature-flag.service';

/**
 * FORTUNE 500 ERROR TRACKING SERVICE
 *
 * Comprehensive error tracking with component context for feature flag rollout.
 * Integrates with AnalyticsService for auto-rollback capabilities.
 *
 * Features:
 * - Track errors with component and module context
 * - Categorize errors (network, runtime, render, etc.)
 * - Calculate error rates per module
 * - Integration with AnalyticsService for auto-rollback
 * - Error recovery suggestions
 * - Structured error logging for debugging
 */

// ============================================
// TYPE DEFINITIONS
// ============================================

/**
 * Error category for classification
 */
export enum ErrorCategory {
  /** Network/HTTP errors */
  Network = 'network',
  /** Runtime JavaScript errors */
  Runtime = 'runtime',
  /** Component render errors */
  Render = 'render',
  /** API/Backend errors */
  API = 'api',
  /** Validation errors */
  Validation = 'validation',
  /** Permission/Authorization errors */
  Permission = 'permission',
  /** Unknown/Uncategorized errors */
  Unknown = 'unknown'
}

/**
 * Error severity level
 */
export enum ErrorSeverity {
  /** Informational - no action needed */
  Info = 'info',
  /** Warning - minor issue */
  Warning = 'warning',
  /** Error - needs attention */
  Error = 'error',
  /** Critical - requires immediate action */
  Critical = 'critical'
}

/**
 * Tracked error with full context
 */
export interface TrackedError {
  /** Unique error ID */
  id: string;
  /** Error message */
  message: string;
  /** Error stack trace */
  stack?: string;
  /** Error category */
  category: ErrorCategory;
  /** Error severity */
  severity: ErrorSeverity;
  /** Feature module where error occurred */
  module?: FeatureModule;
  /** Component name where error occurred */
  componentName?: string;
  /** Component library (custom vs material) */
  library?: ComponentLibrary;
  /** HTTP status code (for network errors) */
  statusCode?: number;
  /** Error timestamp */
  timestamp: number;
  /** User ID */
  userId?: string;
  /** Tenant ID */
  tenantId?: string;
  /** Session ID */
  sessionId?: string;
  /** Additional metadata */
  metadata?: Record<string, any>;
  /** Has been reported to backend */
  reported: boolean;
}

/**
 * Error rate statistics
 */
export interface ErrorRateInfo {
  /** Time window in ms */
  windowMs: number;
  /** Total errors in window */
  errorCount: number;
  /** Error rate (errors per second) */
  errorRate: number;
  /** Is above threshold */
  aboveThreshold: boolean;
}

/**
 * Component error context
 */
export interface ErrorContext {
  /** Feature module */
  module: FeatureModule;
  /** Component name */
  componentName: string;
  /** Component library */
  library: ComponentLibrary;
  /** Additional context data */
  metadata?: Record<string, any>;
}

// ============================================
// ERROR TRACKING SERVICE
// ============================================

@Injectable({
  providedIn: 'root'
})
export class ErrorTrackingService {
  private analytics = inject(AnalyticsService);

  // Configuration
  private readonly ERROR_RATE_WINDOW_MS = 60000; // 1 minute window
  private readonly ERROR_RATE_THRESHOLD = 5; // 5 errors per minute triggers alert
  private readonly MAX_STORED_ERRORS = 100; // Keep last 100 errors in memory

  // ============================================
  // SIGNALS FOR REACTIVE STATE MANAGEMENT
  // ============================================

  /** All tracked errors */
  private errorsSignal = signal<TrackedError[]>([]);

  /** Current error context (set by components) */
  private contextSignal = signal<ErrorContext | null>(null);

  /** User ID for error tracking */
  private userIdSignal = signal<string | null>(null);

  /** Tenant ID for error tracking */
  private tenantIdSignal = signal<string | null>(null);

  /** Session ID for error tracking */
  private sessionIdSignal = signal<string>(this.generateSessionId());

  // ============================================
  // PUBLIC READONLY SIGNALS
  // ============================================

  readonly errors = this.errorsSignal.asReadonly();
  readonly currentContext = this.contextSignal.asReadonly();

  // ============================================
  // COMPUTED SIGNALS
  // ============================================

  /** Total number of errors tracked */
  readonly totalErrors = computed(() => this.errorsSignal().length);

  /** Errors in current time window */
  readonly recentErrors = computed(() => {
    const windowStart = Date.now() - this.ERROR_RATE_WINDOW_MS;
    return this.errorsSignal().filter(e => e.timestamp >= windowStart);
  });

  /** Current error rate (errors per minute) */
  readonly errorRate = computed(() => {
    const recent = this.recentErrors();
    const windowMinutes = this.ERROR_RATE_WINDOW_MS / 60000;
    return recent.length / windowMinutes;
  });

  /** Is error rate above threshold */
  readonly errorRateAboveThreshold = computed(() => {
    return this.errorRate() > this.ERROR_RATE_THRESHOLD;
  });

  /** Errors by category */
  readonly errorsByCategory = computed(() => {
    const errors = this.errorsSignal();
    const grouped = new Map<ErrorCategory, number>();

    errors.forEach(error => {
      const count = grouped.get(error.category) || 0;
      grouped.set(error.category, count + 1);
    });

    return grouped;
  });

  /** Errors by module */
  readonly errorsByModule = computed(() => {
    const errors = this.errorsSignal();
    const grouped = new Map<FeatureModule, number>();

    errors.forEach(error => {
      if (error.module) {
        const count = grouped.get(error.module) || 0;
        grouped.set(error.module, count + 1);
      }
    });

    return grouped;
  });

  // ============================================
  // INITIALIZATION
  // ============================================

  constructor() {
    console.log('🔍 Error tracking service initialized');
  }

  // ============================================
  // PUBLIC METHODS - CONTEXT MANAGEMENT
  // ============================================

  /**
   * Set current error context (should be called by components)
   */
  setContext(context: ErrorContext): void {
    this.contextSignal.set(context);
  }

  /**
   * Clear current error context
   */
  clearContext(): void {
    this.contextSignal.set(null);
  }

  /**
   * Set user ID for error tracking
   */
  setUserId(userId: string): void {
    this.userIdSignal.set(userId);
  }

  /**
   * Set tenant ID for error tracking
   */
  setTenantId(tenantId: string): void {
    this.tenantIdSignal.set(tenantId);
  }

  // ============================================
  // PUBLIC METHODS - ERROR TRACKING
  // ============================================

  /**
   * Track a generic error with automatic categorization
   */
  trackError(
    error: Error | HttpErrorResponse | string,
    context?: Partial<ErrorContext>
  ): TrackedError {
    const trackedError = this.createTrackedError(error, context);
    this.addError(trackedError);

    // Report to analytics if component context is available
    if (trackedError.module && trackedError.componentName && trackedError.library) {
      this.analytics.trackComponentError(
        trackedError.module,
        trackedError.componentName,
        trackedError.library,
        error instanceof Error ? error : new Error(String(error)),
        trackedError.metadata
      );
    }

    return trackedError;
  }

  /**
   * Track a component render error
   */
  trackComponentError(
    error: Error,
    module: FeatureModule,
    componentName: string,
    library: ComponentLibrary,
    metadata?: Record<string, any>
  ): TrackedError {
    const trackedError: TrackedError = {
      id: this.generateErrorId(),
      message: error.message,
      stack: error.stack,
      category: ErrorCategory.Render,
      severity: ErrorSeverity.Error,
      module,
      componentName,
      library,
      timestamp: Date.now(),
      userId: this.userIdSignal() || undefined,
      tenantId: this.tenantIdSignal() || undefined,
      sessionId: this.sessionIdSignal(),
      metadata,
      reported: false
    };

    this.addError(trackedError);

    // Report to analytics
    this.analytics.trackComponentError(
      module,
      componentName,
      library,
      error,
      metadata
    );

    // Log to console
    console.error(
      `❌ Component Error [${module}/${componentName}]:`,
      error.message,
      { library, metadata }
    );

    return trackedError;
  }

  /**
   * Track a network/HTTP error
   */
  trackHttpError(
    error: HttpErrorResponse,
    context?: Partial<ErrorContext>
  ): TrackedError {
    const trackedError: TrackedError = {
      id: this.generateErrorId(),
      message: error.message,
      stack: error.error?.stack,
      category: ErrorCategory.Network,
      severity: this.getHttpErrorSeverity(error.status),
      statusCode: error.status,
      timestamp: Date.now(),
      userId: this.userIdSignal() || undefined,
      tenantId: this.tenantIdSignal() || undefined,
      sessionId: this.sessionIdSignal(),
      metadata: {
        url: error.url,
        statusText: error.statusText,
        ...context?.metadata
      },
      reported: false,
      ...this.getContextFields(context)
    };

    this.addError(trackedError);

    // Log to console
    console.error(
      `🌐 HTTP Error [${error.status}]:`,
      error.message,
      { url: error.url, context }
    );

    return trackedError;
  }

  /**
   * Track a validation error
   */
  trackValidationError(
    message: string,
    field: string,
    value: any,
    context?: Partial<ErrorContext>
  ): TrackedError {
    const trackedError: TrackedError = {
      id: this.generateErrorId(),
      message,
      category: ErrorCategory.Validation,
      severity: ErrorSeverity.Warning,
      timestamp: Date.now(),
      userId: this.userIdSignal() || undefined,
      tenantId: this.tenantIdSignal() || undefined,
      sessionId: this.sessionIdSignal(),
      metadata: {
        field,
        value,
        ...context?.metadata
      },
      reported: false,
      ...this.getContextFields(context)
    };

    this.addError(trackedError);

    return trackedError;
  }

  // ============================================
  // PUBLIC METHODS - ERROR RATE
  // ============================================

  /**
   * Get error rate information for a specific module
   */
  getErrorRateForModule(module: FeatureModule): ErrorRateInfo {
    const windowStart = Date.now() - this.ERROR_RATE_WINDOW_MS;
    const moduleErrors = this.errorsSignal().filter(
      e => e.module === module && e.timestamp >= windowStart
    );

    const errorCount = moduleErrors.length;
    const errorRate = errorCount / (this.ERROR_RATE_WINDOW_MS / 1000); // errors per second

    return {
      windowMs: this.ERROR_RATE_WINDOW_MS,
      errorCount,
      errorRate,
      aboveThreshold: errorRate > (this.ERROR_RATE_THRESHOLD / 60) // threshold per second
    };
  }

  /**
   * Get overall error rate information
   */
  getOverallErrorRate(): ErrorRateInfo {
    const recentErrors = this.recentErrors();
    const errorCount = recentErrors.length;
    const errorRate = errorCount / (this.ERROR_RATE_WINDOW_MS / 1000);

    return {
      windowMs: this.ERROR_RATE_WINDOW_MS,
      errorCount,
      errorRate,
      aboveThreshold: this.errorRateAboveThreshold()
    };
  }

  /**
   * Check if a specific module should trigger rollback based on error rate
   */
  shouldTriggerRollback(module: FeatureModule, threshold: number = 0.05): boolean {
    const errorRate = this.getErrorRateForModule(module);

    // Need minimum errors before triggering rollback
    if (errorRate.errorCount < 5) {
      return false;
    }

    return errorRate.aboveThreshold;
  }

  // ============================================
  // PUBLIC METHODS - ERROR RETRIEVAL
  // ============================================

  /**
   * Get all errors for a specific module
   */
  getErrorsForModule(module: FeatureModule): TrackedError[] {
    return this.errorsSignal().filter(e => e.module === module);
  }

  /**
   * Get errors by category
   */
  getErrorsByCategory(category: ErrorCategory): TrackedError[] {
    return this.errorsSignal().filter(e => e.category === category);
  }

  /**
   * Get errors by severity
   */
  getErrorsBySeverity(severity: ErrorSeverity): TrackedError[] {
    return this.errorsSignal().filter(e => e.severity === severity);
  }

  /**
   * Get recent critical errors
   */
  getCriticalErrors(): TrackedError[] {
    return this.errorsSignal().filter(e => e.severity === ErrorSeverity.Critical);
  }

  // ============================================
  // PUBLIC METHODS - ERROR MANAGEMENT
  // ============================================

  /**
   * Clear all tracked errors
   */
  clearErrors(): void {
    this.errorsSignal.set([]);
    console.log('🧹 All errors cleared');
  }

  /**
   * Clear errors for a specific module
   */
  clearErrorsForModule(module: FeatureModule): void {
    this.errorsSignal.update(errors =>
      errors.filter(e => e.module !== module)
    );
    console.log(`🧹 Errors cleared for module: ${module}`);
  }

  /**
   * Mark error as reported
   */
  markAsReported(errorId: string): void {
    this.errorsSignal.update(errors =>
      errors.map(e => e.id === errorId ? { ...e, reported: true } : e)
    );
  }

  // ============================================
  // PRIVATE METHODS
  // ============================================

  /**
   * Create a tracked error from various error types
   */
  private createTrackedError(
    error: Error | HttpErrorResponse | string,
    context?: Partial<ErrorContext>
  ): TrackedError {
    if (error instanceof HttpErrorResponse) {
      return this.trackHttpError(error, context);
    }

    const errorObj = error instanceof Error ? error : new Error(String(error));
    const currentContext = this.contextSignal();

    return {
      id: this.generateErrorId(),
      message: errorObj.message,
      stack: errorObj.stack,
      category: this.categorizeError(errorObj),
      severity: ErrorSeverity.Error,
      timestamp: Date.now(),
      userId: this.userIdSignal() || undefined,
      tenantId: this.tenantIdSignal() || undefined,
      sessionId: this.sessionIdSignal(),
      reported: false,
      ...this.getContextFields(context || currentContext)
    };
  }

  /**
   * Add error to tracked errors list with size limit
   */
  private addError(error: TrackedError): void {
    this.errorsSignal.update(errors => {
      const updated = [...errors, error];

      // Keep only the most recent errors
      if (updated.length > this.MAX_STORED_ERRORS) {
        return updated.slice(-this.MAX_STORED_ERRORS);
      }

      return updated;
    });
  }

  /**
   * Categorize error based on type and message
   */
  private categorizeError(error: Error): ErrorCategory {
    const message = error.message.toLowerCase();

    if (message.includes('http') || message.includes('network')) {
      return ErrorCategory.Network;
    }

    if (message.includes('permission') || message.includes('unauthorized')) {
      return ErrorCategory.Permission;
    }

    if (message.includes('validation') || message.includes('invalid')) {
      return ErrorCategory.Validation;
    }

    if (error.name === 'TypeError' || error.name === 'ReferenceError') {
      return ErrorCategory.Runtime;
    }

    return ErrorCategory.Unknown;
  }

  /**
   * Get HTTP error severity based on status code
   */
  private getHttpErrorSeverity(status: number): ErrorSeverity {
    if (status >= 500) {
      return ErrorSeverity.Critical;
    }
    if (status >= 400) {
      return ErrorSeverity.Error;
    }
    if (status >= 300) {
      return ErrorSeverity.Warning;
    }
    return ErrorSeverity.Info;
  }

  /**
   * Extract context fields from partial context
   */
  private getContextFields(context?: Partial<ErrorContext> | null): Partial<TrackedError> {
    if (!context) {
      return {};
    }

    return {
      module: context.module,
      componentName: context.componentName,
      library: context.library,
      metadata: context.metadata
    };
  }

  /**
   * Generate unique error ID
   */
  private generateErrorId(): string {
    return `error_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
  }

  /**
   * Generate unique session ID
   */
  private generateSessionId(): string {
    return `session_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
  }
}

/**
 * Global Error Handler for Angular
 * Integrates with ErrorTrackingService
 */
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private errorTracking = inject(ErrorTrackingService);

  handleError(error: any): void {
    // Track the error
    this.errorTracking.trackError(error);

    // Log to console (for development)
    console.error('Global error handler:', error);
  }
}
