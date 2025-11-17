import { inject } from '@angular/core';
import { FeatureFlagService, FeatureModule } from './feature-flag.service';
import { AnalyticsService, ComponentLibrary } from './analytics.service';
import { ErrorTrackingService, ErrorContext } from './error-tracking.service';

/**
 * FORTUNE 500 FEATURE ROLLOUT HELPER
 *
 * Convenience functions to integrate feature flags, analytics, and error tracking
 * in a single, easy-to-use interface.
 *
 * Usage in components:
 * ```typescript
 * import { useFeatureRollout } from './core/services/feature-rollout.helper';
 *
 * class MyComponent {
 *   private rollout = useFeatureRollout(FeatureModule.Auth, 'LoginComponent');
 *
 *   ngOnInit() {
 *     const useCustom = this.rollout.isEnabled();
 *     this.rollout.trackRender();
 *   }
 * }
 * ```
 */

/**
 * Feature rollout helper interface
 */
export interface FeatureRolloutHelper {
  /** Check if feature is enabled */
  isEnabled(): boolean;

  /** Track component render */
  trackRender(metadata?: Record<string, any>): void;

  /** Track component error */
  trackError(error: Error, metadata?: Record<string, any>): void;

  /** Set error context */
  setContext(metadata?: Record<string, any>): void;

  /** Clear error context */
  clearContext(): void;

  /** Check if module should rollback */
  shouldRollback(): boolean;

  /** Get error rate for module */
  getErrorRate(): number;

  /** Get component library being used */
  getLibrary(): ComponentLibrary;
}

/**
 * Create a feature rollout helper for a component
 *
 * This helper integrates feature flags, analytics, and error tracking
 * into a single, easy-to-use interface.
 *
 * @param module - Feature module to check
 * @param componentName - Name of the component
 * @returns Feature rollout helper with convenience methods
 *
 * @example
 * ```typescript
 * class LoginComponent implements OnInit, OnDestroy {
 *   private rollout = useFeatureRollout(FeatureModule.Auth, 'LoginComponent');
 *
 *   ngOnInit() {
 *     // Check if custom components are enabled
 *     if (this.rollout.isEnabled()) {
 *       console.log('Using custom auth components');
 *     }
 *
 *     // Track component render
 *     this.rollout.trackRender({ version: '1.0' });
 *
 *     // Set error context
 *     this.rollout.setContext();
 *   }
 *
 *   onSubmit() {
 *     try {
 *       // Component logic
 *     } catch (error) {
 *       this.rollout.trackError(error as Error);
 *     }
 *   }
 *
 *   ngOnDestroy() {
 *     this.rollout.clearContext();
 *   }
 * }
 * ```
 */
export function useFeatureRollout(
  module: FeatureModule,
  componentName: string
): FeatureRolloutHelper {
  const featureFlags = inject(FeatureFlagService);
  const analytics = inject(AnalyticsService);
  const errorTracking = inject(ErrorTrackingService);

  return {
    isEnabled(): boolean {
      const enabled = featureFlags.isFeatureEnabled(module);

      // Track feature flag evaluation
      analytics.trackFeatureFlagEvaluated(module, enabled);

      return enabled;
    },

    trackRender(metadata?: Record<string, any>): void {
      const library = this.isEnabled() ? ComponentLibrary.Custom : ComponentLibrary.Material;

      analytics.trackComponentRender(
        module,
        componentName,
        library,
        metadata
      );
    },

    trackError(error: Error, metadata?: Record<string, any>): void {
      const library = this.isEnabled() ? ComponentLibrary.Custom : ComponentLibrary.Material;

      errorTracking.trackComponentError(
        error,
        module,
        componentName,
        library,
        metadata
      );
    },

    setContext(metadata?: Record<string, any>): void {
      const library = this.isEnabled() ? ComponentLibrary.Custom : ComponentLibrary.Material;

      errorTracking.setContext({
        module,
        componentName,
        library,
        metadata
      });
    },

    clearContext(): void {
      errorTracking.clearContext();
    },

    shouldRollback(): boolean {
      return analytics.shouldRollback(module);
    },

    getErrorRate(): number {
      const stats = analytics.getErrorRateForModule(module);
      return stats?.errorRate || 0;
    },

    getLibrary(): ComponentLibrary {
      return this.isEnabled() ? ComponentLibrary.Custom : ComponentLibrary.Material;
    }
  };
}

/**
 * Initialize feature rollout infrastructure with user and tenant context
 *
 * Should be called once during app initialization after authentication
 *
 * @param userId - Current user ID
 * @param tenantId - Current tenant ID
 *
 * @example
 * ```typescript
 * class AppComponent implements OnInit {
 *   private auth = inject(AuthService);
 *
 *   ngOnInit() {
 *     const user = this.auth.user();
 *     const tenantId = 'tenant-123';
 *
 *     if (user) {
 *       initializeFeatureRollout(user.id, tenantId);
 *     }
 *   }
 * }
 * ```
 */
export function initializeFeatureRollout(userId: string, tenantId: string): void {
  const featureFlags = inject(FeatureFlagService);
  const analytics = inject(AnalyticsService);
  const errorTracking = inject(ErrorTrackingService);

  // Set user and tenant context
  featureFlags.setUserId(userId);
  featureFlags.setTenantId(tenantId);

  analytics.setUserId(userId);
  analytics.setTenantId(tenantId);

  errorTracking.setUserId(userId);
  errorTracking.setTenantId(tenantId);

  // Refresh feature flags from backend
  featureFlags.refreshFlags().subscribe({
    next: () => {
      console.log('✅ Feature rollout infrastructure initialized');
      console.log('   User ID:', userId);
      console.log('   Tenant ID:', tenantId);
    },
    error: (error) => {
      console.error('❌ Failed to initialize feature rollout:', error);
    }
  });
}

/**
 * Clear feature rollout infrastructure on logout
 *
 * Should be called when user logs out
 *
 * @example
 * ```typescript
 * class AppComponent {
 *   private auth = inject(AuthService);
 *
 *   logout() {
 *     clearFeatureRollout();
 *     this.auth.logout();
 *   }
 * }
 * ```
 */
export function clearFeatureRollout(): void {
  const featureFlags = inject(FeatureFlagService);
  const analytics = inject(AnalyticsService);
  const errorTracking = inject(ErrorTrackingService);

  featureFlags.clearFlags();
  analytics.reset();
  errorTracking.clearErrors();

  console.log('🧹 Feature rollout infrastructure cleared');
}

/**
 * Check if any feature module has triggered auto-rollback
 *
 * Can be used in guards or interceptors to show warnings
 *
 * @returns Set of modules that have triggered rollback
 *
 * @example
 * ```typescript
 * const rollbacks = getActiveRollbacks();
 * if (rollbacks.size > 0) {
 *   console.warn('Active rollbacks:', Array.from(rollbacks));
 * }
 * ```
 */
export function getActiveRollbacks(): Set<FeatureModule> {
  const analytics = inject(AnalyticsService);
  return analytics.rollbackTriggers();
}

/**
 * Get comprehensive rollout status for all modules
 *
 * Useful for admin dashboards and debugging
 *
 * @returns Map of modules to their status
 *
 * @example
 * ```typescript
 * const status = getRolloutStatus();
 * console.table(Array.from(status.entries()));
 * ```
 */
export function getRolloutStatus(): Map<FeatureModule, {
  enabled: boolean;
  errorRate: number;
  shouldRollback: boolean;
}> {
  const featureFlags = inject(FeatureFlagService);
  const analytics = inject(AnalyticsService);

  const status = new Map<FeatureModule, any>();

  // Check all available modules
  Object.values(FeatureModule).forEach(module => {
    const enabled = featureFlags.isFeatureEnabled(module as FeatureModule);
    const stats = analytics.getErrorRateForModule(module as FeatureModule);
    const shouldRollback = analytics.shouldRollback(module as FeatureModule);

    status.set(module as FeatureModule, {
      enabled,
      errorRate: stats?.errorRate || 0,
      shouldRollback
    });
  });

  return status;
}
