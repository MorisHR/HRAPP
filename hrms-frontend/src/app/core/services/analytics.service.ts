import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { FeatureModule } from './feature-flag.service';

/**
 * FORTUNE 500 ANALYTICS SERVICE
 *
 * Tracks component renders, errors, and performance during feature flag rollout.
 * Provides auto-rollback capabilities if error rates exceed thresholds.
 *
 * Features:
 * - Track component renders (custom vs Material)
 * - Track errors with component context
 * - Calculate error rates per module
 * - Auto-rollback if error rate > 5%
 * - Integration with backend analytics API
 * - Local buffering for offline support
 */

// ============================================
// TYPE DEFINITIONS
// ============================================

/**
 * Component library type
 */
export enum ComponentLibrary {
  /** Material Design components */
  Material = 'material',
  /** Custom HRMS components */
  Custom = 'custom'
}

/**
 * Event type for analytics tracking
 */
export enum AnalyticsEventType {
  /** Component rendered successfully */
  ComponentRender = 'component_render',
  /** Component threw an error */
  ComponentError = 'component_error',
  /** Feature flag evaluated */
  FeatureFlagEvaluated = 'feature_flag_evaluated',
  /** Auto-rollback triggered */
  AutoRollback = 'auto_rollback'
}

/**
 * Analytics event data structure
 */
export interface AnalyticsEvent {
  /** Event type */
  type: AnalyticsEventType;
  /** Feature module */
  module: FeatureModule;
  /** Component library used */
  library: ComponentLibrary;
  /** Component name */
  componentName: string;
  /** Error message (for error events) */
  errorMessage?: string;
  /** Error stack trace (for error events) */
  errorStack?: string;
  /** Additional metadata */
  metadata?: Record<string, any>;
  /** Timestamp */
  timestamp: number;
  /** User ID */
  userId?: string;
  /** Tenant ID */
  tenantId?: string;
  /** Session ID */
  sessionId?: string;
}

/**
 * Error rate statistics per module
 */
export interface ErrorRateStats {
  /** Feature module */
  module: FeatureModule;
  /** Total renders */
  totalRenders: number;
  /** Total errors */
  totalErrors: number;
  /** Error rate (0-1) */
  errorRate: number;
  /** Is error rate above threshold */
  aboveThreshold: boolean;
}

/**
 * Analytics configuration
 */
interface AnalyticsConfig {
  /** Error rate threshold for auto-rollback (0-1) */
  errorRateThreshold: number;
  /** Minimum samples before calculating error rate */
  minSamplesForRollback: number;
  /** Enable console logging */
  enableLogging: boolean;
  /** Enable backend reporting */
  enableBackendReporting: boolean;
  /** Batch size for backend reports */
  batchSize: number;
  /** Flush interval in ms */
  flushInterval: number;
}

// ============================================
// ANALYTICS SERVICE
// ============================================

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private http = inject(HttpClient);

  // API endpoint
  private apiUrl = environment.apiUrl;

  // Configuration with defaults
  private config: AnalyticsConfig = {
    errorRateThreshold: 0.05, // 5% error rate triggers rollback
    minSamplesForRollback: 10, // Need at least 10 renders before rollback
    enableLogging: !environment.production,
    enableBackendReporting: true,
    batchSize: 50,
    flushInterval: 60000 // 1 minute
  };

  // ============================================
  // SIGNALS FOR REACTIVE STATE MANAGEMENT
  // ============================================

  /** All analytics events (in-memory buffer) */
  private eventsSignal = signal<AnalyticsEvent[]>([]);

  /** Error rate statistics per module */
  private errorStatsSignal = signal<Map<FeatureModule, ErrorRateStats>>(new Map());

  /** Current user ID */
  private userIdSignal = signal<string | null>(null);

  /** Current tenant ID */
  private tenantIdSignal = signal<string | null>(null);

  /** Current session ID */
  private sessionIdSignal = signal<string>(this.generateSessionId());

  /** Auto-rollback triggers */
  private rollbackTriggersSignal = signal<Set<FeatureModule>>(new Set());

  // ============================================
  // PUBLIC READONLY SIGNALS
  // ============================================

  readonly events = this.eventsSignal.asReadonly();
  readonly errorStats = this.errorStatsSignal.asReadonly();
  readonly rollbackTriggers = this.rollbackTriggersSignal.asReadonly();

  // ============================================
  // COMPUTED SIGNALS
  // ============================================

  /** Total number of events tracked */
  readonly totalEvents = computed(() => this.eventsSignal().length);

  /** Total number of errors */
  readonly totalErrors = computed(() =>
    this.eventsSignal().filter(e => e.type === AnalyticsEventType.ComponentError).length
  );

  /** Overall error rate across all modules */
  readonly overallErrorRate = computed(() => {
    const renders = this.eventsSignal().filter(
      e => e.type === AnalyticsEventType.ComponentRender
    ).length;

    const errors = this.totalErrors();

    if (renders === 0) return 0;
    return errors / (renders + errors);
  });

  // ============================================
  // INITIALIZATION
  // ============================================

  constructor() {
    // Set up periodic flush to backend
    if (this.config.enableBackendReporting) {
      setInterval(() => this.flushEvents(), this.config.flushInterval);
    }

    this.log('📊 Analytics service initialized', {
      sessionId: this.sessionIdSignal(),
      config: this.config
    });
  }

  // ============================================
  // PUBLIC METHODS - CONTEXT
  // ============================================

  /**
   * Set current user ID for analytics tracking
   */
  setUserId(userId: string): void {
    this.userIdSignal.set(userId);
    this.log('👤 Analytics user set:', userId);
  }

  /**
   * Set current tenant ID for analytics tracking
   */
  setTenantId(tenantId: string): void {
    this.tenantIdSignal.set(tenantId);
    this.log('🏢 Analytics tenant set:', tenantId);
  }

  /**
   * Update analytics configuration
   */
  updateConfig(config: Partial<AnalyticsConfig>): void {
    this.config = { ...this.config, ...config };
    this.log('⚙️ Analytics config updated:', this.config);
  }

  // ============================================
  // PUBLIC METHODS - EVENT TRACKING
  // ============================================

  /**
   * Track component render event
   */
  trackComponentRender(
    module: FeatureModule,
    componentName: string,
    library: ComponentLibrary,
    metadata?: Record<string, any>
  ): void {
    const event: AnalyticsEvent = {
      type: AnalyticsEventType.ComponentRender,
      module,
      library,
      componentName,
      metadata,
      timestamp: Date.now(),
      userId: this.userIdSignal() || undefined,
      tenantId: this.tenantIdSignal() || undefined,
      sessionId: this.sessionIdSignal()
    };

    this.addEvent(event);
    this.updateErrorStats(module);

    this.log(`✅ Component render: ${componentName} (${library})`, { module, metadata });
  }

  /**
   * Track component error event
   * Automatically checks for auto-rollback condition
   */
  trackComponentError(
    module: FeatureModule,
    componentName: string,
    library: ComponentLibrary,
    error: Error,
    metadata?: Record<string, any>
  ): void {
    const event: AnalyticsEvent = {
      type: AnalyticsEventType.ComponentError,
      module,
      library,
      componentName,
      errorMessage: error.message,
      errorStack: error.stack,
      metadata,
      timestamp: Date.now(),
      userId: this.userIdSignal() || undefined,
      tenantId: this.tenantIdSignal() || undefined,
      sessionId: this.sessionIdSignal()
    };

    this.addEvent(event);
    this.updateErrorStats(module);

    console.error(`❌ Component error: ${componentName} (${library})`, {
      module,
      error: error.message,
      stack: error.stack,
      metadata
    });

    // Check if auto-rollback should be triggered
    this.checkAutoRollback(module);
  }

  /**
   * Track feature flag evaluation
   */
  trackFeatureFlagEvaluated(
    module: FeatureModule,
    enabled: boolean,
    metadata?: Record<string, any>
  ): void {
    const event: AnalyticsEvent = {
      type: AnalyticsEventType.FeatureFlagEvaluated,
      module,
      library: enabled ? ComponentLibrary.Custom : ComponentLibrary.Material,
      componentName: 'feature-flag',
      metadata: { ...metadata, enabled },
      timestamp: Date.now(),
      userId: this.userIdSignal() || undefined,
      tenantId: this.tenantIdSignal() || undefined,
      sessionId: this.sessionIdSignal()
    };

    this.addEvent(event);
    this.log(`🚩 Feature flag evaluated: ${module} = ${enabled}`, metadata);
  }

  // ============================================
  // PUBLIC METHODS - ERROR RATE & ROLLBACK
  // ============================================

  /**
   * Get error rate statistics for a specific module
   */
  getErrorRateForModule(module: FeatureModule): ErrorRateStats | null {
    return this.errorStatsSignal().get(module) || null;
  }

  /**
   * Check if a module should be rolled back due to high error rate
   */
  shouldRollback(module: FeatureModule): boolean {
    return this.rollbackTriggersSignal().has(module);
  }

  /**
   * Manually trigger rollback for a module
   */
  triggerRollback(module: FeatureModule, reason: string): void {
    this.log(`🔄 Manual rollback triggered for ${module}: ${reason}`);

    this.rollbackTriggersSignal.update(triggers => {
      triggers.add(module);
      return new Set(triggers);
    });

    // Track rollback event
    const event: AnalyticsEvent = {
      type: AnalyticsEventType.AutoRollback,
      module,
      library: ComponentLibrary.Custom,
      componentName: 'rollback',
      metadata: { reason, manual: true },
      timestamp: Date.now(),
      userId: this.userIdSignal() || undefined,
      tenantId: this.tenantIdSignal() || undefined,
      sessionId: this.sessionIdSignal()
    };

    this.addEvent(event);
  }

  /**
   * Clear rollback trigger for a module
   */
  clearRollback(module: FeatureModule): void {
    this.rollbackTriggersSignal.update(triggers => {
      triggers.delete(module);
      return new Set(triggers);
    });

    this.log(`✅ Rollback cleared for ${module}`);
  }

  /**
   * Reset all analytics data
   */
  reset(): void {
    this.eventsSignal.set([]);
    this.errorStatsSignal.set(new Map());
    this.rollbackTriggersSignal.set(new Set());
    this.sessionIdSignal.set(this.generateSessionId());
    this.log('🔄 Analytics reset');
  }

  // ============================================
  // PRIVATE METHODS
  // ============================================

  /**
   * Add event to buffer and check if flush is needed
   */
  private addEvent(event: AnalyticsEvent): void {
    this.eventsSignal.update(events => {
      const updated = [...events, event];

      // Auto-flush if batch size reached
      if (updated.length >= this.config.batchSize) {
        this.flushEvents();
      }

      return updated;
    });
  }

  /**
   * Update error rate statistics for a module
   */
  private updateErrorStats(module: FeatureModule): void {
    const moduleEvents = this.eventsSignal().filter(e => e.module === module);

    const renders = moduleEvents.filter(
      e => e.type === AnalyticsEventType.ComponentRender
    ).length;

    const errors = moduleEvents.filter(
      e => e.type === AnalyticsEventType.ComponentError
    ).length;

    const totalEvents = renders + errors;
    const errorRate = totalEvents > 0 ? errors / totalEvents : 0;
    const aboveThreshold = errorRate > this.config.errorRateThreshold;

    const stats: ErrorRateStats = {
      module,
      totalRenders: renders,
      totalErrors: errors,
      errorRate,
      aboveThreshold
    };

    this.errorStatsSignal.update(statsMap => {
      const updated = new Map(statsMap);
      updated.set(module, stats);
      return updated;
    });
  }

  /**
   * Check if auto-rollback should be triggered for a module
   */
  private checkAutoRollback(module: FeatureModule): void {
    const stats = this.errorStatsSignal().get(module);

    if (!stats) return;

    // Need minimum samples before triggering rollback
    const totalEvents = stats.totalRenders + stats.totalErrors;
    if (totalEvents < this.config.minSamplesForRollback) {
      return;
    }

    // Check if error rate exceeds threshold
    if (stats.aboveThreshold && !this.rollbackTriggersSignal().has(module)) {
      console.warn(
        `⚠️ AUTO-ROLLBACK TRIGGERED for ${module}:`,
        `Error rate ${(stats.errorRate * 100).toFixed(2)}% exceeds threshold ${(this.config.errorRateThreshold * 100).toFixed(2)}%`
      );

      this.rollbackTriggersSignal.update(triggers => {
        triggers.add(module);
        return new Set(triggers);
      });

      // Track auto-rollback event
      const event: AnalyticsEvent = {
        type: AnalyticsEventType.AutoRollback,
        module,
        library: ComponentLibrary.Custom,
        componentName: 'rollback',
        metadata: {
          reason: 'Error rate threshold exceeded',
          errorRate: stats.errorRate,
          threshold: this.config.errorRateThreshold,
          totalErrors: stats.totalErrors,
          totalRenders: stats.totalRenders
        },
        timestamp: Date.now(),
        userId: this.userIdSignal() || undefined,
        tenantId: this.tenantIdSignal() || undefined,
        sessionId: this.sessionIdSignal()
      };

      this.addEvent(event);
    }
  }

  /**
   * Flush events to backend API
   */
  private flushEvents(): Observable<void> {
    const events = this.eventsSignal();

    if (events.length === 0) {
      return of(undefined);
    }

    if (!this.config.enableBackendReporting) {
      this.log('📊 Backend reporting disabled, clearing buffer locally');
      this.eventsSignal.set([]);
      return of(undefined);
    }

    this.log(`📤 Flushing ${events.length} analytics events to backend`);

    return this.http.post<void>(
      `${this.apiUrl}/analytics/events`,
      { events },
      { withCredentials: true }
    ).pipe(
      catchError(error => {
        console.error('❌ Failed to flush analytics events:', error);
        // Keep events in buffer if flush failed
        return of(undefined);
      })
    );
  }

  /**
   * Generate unique session ID
   */
  private generateSessionId(): string {
    return `session_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
  }

  /**
   * Log message if logging is enabled
   */
  private log(message: string, data?: any): void {
    if (this.config.enableLogging) {
      console.log(message, data || '');
    }
  }
}
