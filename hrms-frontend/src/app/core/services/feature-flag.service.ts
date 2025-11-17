import { Injectable, signal, computed, inject, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of, BehaviorSubject, map } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * FORTUNE 500 FEATURE FLAG SYSTEM
 *
 * Enables gradual rollout of custom UI components to replace Material Design.
 * Provides percentage-based rollout, consistent user experience, and offline support.
 *
 * Architecture:
 * - Signal-based reactive state management (Angular 18+)
 * - Backend API integration for centralized control
 * - Local storage caching for offline resilience
 * - Consistent user hashing for stable A/B testing
 * - Module-based toggles (auth, dashboard, employees, leave, payroll)
 * - Auto-refresh on tenant context changes
 */

// ============================================
// TYPE DEFINITIONS
// ============================================

/**
 * Feature modules that can be toggled
 */
export enum FeatureModule {
  Auth = 'auth',
  Dashboard = 'dashboard',
  Employees = 'employees',
  Leave = 'leave',
  Payroll = 'payroll',
  Attendance = 'attendance',
  Reports = 'reports',
  Settings = 'settings'
}

/**
 * Rollout strategy for gradual deployment
 */
export enum RolloutStrategy {
  /** No users get the feature */
  Disabled = 'disabled',
  /** Percentage-based rollout (0-100%) */
  Percentage = 'percentage',
  /** All users get the feature */
  Enabled = 'enabled'
}

/**
 * Feature flag configuration from backend
 */
export interface FeatureFlag {
  /** Feature module identifier */
  module: FeatureModule;
  /** Is feature enabled for this tenant */
  enabled: boolean;
  /** Rollout strategy */
  strategy: RolloutStrategy;
  /** Rollout percentage (0-100) for percentage strategy */
  rolloutPercentage: number;
  /** Override for specific users (by user ID) */
  userOverrides?: Record<string, boolean>;
  /** Tenant ID this flag applies to */
  tenantId?: string;
  /** Last updated timestamp */
  updatedAt?: Date;
}

/**
 * Backend API response format
 */
interface FeatureFlagsResponse {
  success: boolean;
  data: FeatureFlag[];
  message?: string;
}

/**
 * Cached feature flags with expiration
 */
interface CachedFlags {
  flags: FeatureFlag[];
  tenantId: string;
  timestamp: number;
}

// ============================================
// FEATURE FLAG SERVICE
// ============================================

@Injectable({
  providedIn: 'root'
})
export class FeatureFlagService {
  private http = inject(HttpClient);

  // API endpoint from environment
  private apiUrl = environment.apiUrl;

  // Cache configuration
  private readonly CACHE_KEY = 'hrms_feature_flags';
  private readonly CACHE_DURATION_MS = 5 * 60 * 1000; // 5 minutes

  // ============================================
  // SIGNALS FOR REACTIVE STATE MANAGEMENT
  // ============================================

  /** All feature flags for current tenant */
  private flagsSignal = signal<FeatureFlag[]>([]);

  /** Loading state */
  private loadingSignal = signal<boolean>(false);

  /** Error state */
  private errorSignal = signal<string | null>(null);

  /** Current user ID for consistent hashing */
  private userIdSignal = signal<string | null>(null);

  /** Current tenant ID */
  private tenantIdSignal = signal<string | null>(null);

  /** Last refresh timestamp */
  private lastRefreshSignal = signal<number>(0);

  // ============================================
  // PUBLIC READONLY SIGNALS
  // ============================================

  readonly flags = this.flagsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly lastRefresh = this.lastRefreshSignal.asReadonly();

  // ============================================
  // COMPUTED SIGNALS FOR MODULE FLAGS
  // ============================================

  /** Is custom auth components enabled */
  readonly authEnabled = computed(() =>
    this.isFeatureEnabled(FeatureModule.Auth)
  );

  /** Is custom dashboard components enabled */
  readonly dashboardEnabled = computed(() =>
    this.isFeatureEnabled(FeatureModule.Dashboard)
  );

  /** Is custom employee components enabled */
  readonly employeesEnabled = computed(() =>
    this.isFeatureEnabled(FeatureModule.Employees)
  );

  /** Is custom leave components enabled */
  readonly leaveEnabled = computed(() =>
    this.isFeatureEnabled(FeatureModule.Leave)
  );

  /** Is custom payroll components enabled */
  readonly payrollEnabled = computed(() =>
    this.isFeatureEnabled(FeatureModule.Payroll)
  );

  /** Is custom attendance components enabled */
  readonly attendanceEnabled = computed(() =>
    this.isFeatureEnabled(FeatureModule.Attendance)
  );

  /** Is custom reports components enabled */
  readonly reportsEnabled = computed(() =>
    this.isFeatureEnabled(FeatureModule.Reports)
  );

  /** Is custom settings components enabled */
  readonly settingsEnabled = computed(() =>
    this.isFeatureEnabled(FeatureModule.Settings)
  );

  // ============================================
  // INITIALIZATION
  // ============================================

  constructor() {
    // Load cached flags on startup
    this.loadFromCache();

    // Auto-refresh when tenant changes
    effect(() => {
      const tenantId = this.tenantIdSignal();
      if (tenantId) {
        console.log('🚩 Feature flags: Tenant changed, refreshing flags for:', tenantId);
        this.refreshFlags().subscribe();
      }
    });
  }

  // ============================================
  // PUBLIC METHODS
  // ============================================

  /**
   * Set current user ID for consistent feature evaluation
   * Should be called after successful authentication
   */
  setUserId(userId: string): void {
    console.log('👤 Feature flags: User ID set:', userId);
    this.userIdSignal.set(userId);
  }

  /**
   * Set current tenant ID for feature flag retrieval
   * Should be called when tenant context is established
   */
  setTenantId(tenantId: string): void {
    console.log('🏢 Feature flags: Tenant ID set:', tenantId);
    this.tenantIdSignal.set(tenantId);
  }

  /**
   * Check if a feature module is enabled for current user
   * Uses consistent hashing for percentage-based rollout
   */
  isFeatureEnabled(module: FeatureModule): boolean {
    const flag = this.getFlagForModule(module);

    if (!flag) {
      // Default to disabled if flag not found
      console.warn(`⚠️ Feature flag not found for module: ${module}, defaulting to disabled`);
      return false;
    }

    // Check if feature is globally disabled
    if (!flag.enabled || flag.strategy === RolloutStrategy.Disabled) {
      return false;
    }

    // Check if feature is globally enabled
    if (flag.strategy === RolloutStrategy.Enabled) {
      return true;
    }

    // Check user-specific override
    const userId = this.userIdSignal();
    if (userId && flag.userOverrides && userId in flag.userOverrides) {
      const override = flag.userOverrides[userId];
      console.log(`🎯 User override for ${module}: ${override}`);
      return override;
    }

    // Percentage-based rollout with consistent hashing
    if (flag.strategy === RolloutStrategy.Percentage) {
      return this.isUserInRollout(module, flag.rolloutPercentage);
    }

    // Default to disabled
    return false;
  }

  /**
   * Refresh feature flags from backend API
   * Automatically called when tenant changes
   */
  refreshFlags(): Observable<FeatureFlag[]> {
    const tenantId = this.tenantIdSignal();

    if (!tenantId) {
      console.warn('⚠️ Cannot refresh feature flags: No tenant ID set');
      return of([]);
    }

    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    console.log('🔄 Refreshing feature flags from API for tenant:', tenantId);

    return this.http.get<FeatureFlagsResponse>(
      `${this.apiUrl}/feature-flags/current-tenant`,
      { withCredentials: true }
    ).pipe(
      map(response => {
        if (response.success && response.data) {
          console.log('✅ Feature flags loaded:', response.data.length, 'flags');
          this.flagsSignal.set(response.data);
          this.lastRefreshSignal.set(Date.now());
          this.saveToCache(response.data, tenantId);
          this.loadingSignal.set(false);
          return response.data;
        } else {
          throw new Error(response.message || 'Failed to load feature flags');
        }
      }),
      catchError(error => {
        console.error('❌ Failed to load feature flags:', error);
        this.errorSignal.set(error.message || 'Failed to load feature flags');
        this.loadingSignal.set(false);

        // Fall back to cached flags
        const cached = this.loadFromCache();
        if (cached) {
          console.log('📦 Using cached feature flags as fallback');
        }

        return of(this.flagsSignal());
      })
    );
  }

  /**
   * Manually trigger a feature flag refresh
   * Useful for testing or admin interfaces
   */
  forceRefresh(): Observable<FeatureFlag[]> {
    console.log('🔄 Force refreshing feature flags');
    return this.refreshFlags();
  }

  /**
   * Get feature flag for a specific module
   */
  getFlagForModule(module: FeatureModule): FeatureFlag | undefined {
    return this.flagsSignal().find(flag => flag.module === module);
  }

  /**
   * Get all feature flags
   */
  getAllFlags(): FeatureFlag[] {
    return this.flagsSignal();
  }

  /**
   * Clear cached flags and reset state
   * Useful for logout scenarios
   */
  clearFlags(): void {
    console.log('🧹 Clearing feature flags');
    this.flagsSignal.set([]);
    this.userIdSignal.set(null);
    this.tenantIdSignal.set(null);
    this.errorSignal.set(null);
    this.lastRefreshSignal.set(0);
    localStorage.removeItem(this.CACHE_KEY);
  }

  // ============================================
  // PRIVATE HELPER METHODS
  // ============================================

  /**
   * Consistent hashing algorithm for percentage-based rollout
   * Ensures same user always gets same experience
   *
   * Uses simple hash of userId + module name to generate 0-99 number
   * If hash < rolloutPercentage, user is in rollout
   */
  private isUserInRollout(module: FeatureModule, percentage: number): boolean {
    const userId = this.userIdSignal();

    if (!userId) {
      // No user ID - default to disabled for consistency
      return false;
    }

    // Generate hash from userId + module
    const hashInput = `${userId}-${module}`;
    const hash = this.simpleHash(hashInput);

    // Convert hash to 0-99 range
    const userBucket = hash % 100;

    // User is in rollout if their bucket is < percentage
    const inRollout = userBucket < percentage;

    console.log(`🎲 Rollout check for ${module}: user bucket=${userBucket}, threshold=${percentage}, enabled=${inRollout}`);

    return inRollout;
  }

  /**
   * Simple hash function for consistent user bucketing
   * Not cryptographically secure, but good enough for A/B testing
   */
  private simpleHash(str: string): number {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return Math.abs(hash);
  }

  /**
   * Save feature flags to local storage cache
   */
  private saveToCache(flags: FeatureFlag[], tenantId: string): void {
    try {
      const cached: CachedFlags = {
        flags,
        tenantId,
        timestamp: Date.now()
      };
      localStorage.setItem(this.CACHE_KEY, JSON.stringify(cached));
      console.log('💾 Feature flags cached for tenant:', tenantId);
    } catch (error) {
      console.warn('⚠️ Failed to cache feature flags:', error);
    }
  }

  /**
   * Load feature flags from local storage cache
   * Returns true if cache was loaded successfully
   */
  private loadFromCache(): boolean {
    try {
      const cachedJson = localStorage.getItem(this.CACHE_KEY);
      if (!cachedJson) {
        return false;
      }

      const cached: CachedFlags = JSON.parse(cachedJson);
      const tenantId = this.tenantIdSignal();

      // Validate cache
      if (!tenantId || cached.tenantId !== tenantId) {
        console.log('📦 Cache invalid: Different tenant');
        return false;
      }

      const cacheAge = Date.now() - cached.timestamp;
      if (cacheAge > this.CACHE_DURATION_MS) {
        console.log('📦 Cache expired:', cacheAge, 'ms old');
        return false;
      }

      console.log('📦 Loading feature flags from cache');
      this.flagsSignal.set(cached.flags);
      this.lastRefreshSignal.set(cached.timestamp);
      return true;

    } catch (error) {
      console.warn('⚠️ Failed to load cached feature flags:', error);
      return false;
    }
  }
}
