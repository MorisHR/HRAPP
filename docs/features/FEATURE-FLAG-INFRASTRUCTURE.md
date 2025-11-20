# Fortune 500 Feature Flag Infrastructure

## Overview

This document describes the complete feature flag infrastructure for the gradual rollout of custom UI components to replace Material Design.

## Architecture

### Core Services

1. **FeatureFlagService** (`/workspaces/HRAPP/hrms-frontend/src/app/core/services/feature-flag.service.ts`)
   - Signal-based reactive state management
   - Backend API integration for centralized control
   - Local storage caching for offline resilience
   - Consistent user hashing for stable A/B testing
   - Module-based toggles (auth, dashboard, employees, leave, payroll, etc.)

2. **AnalyticsService** (`/workspaces/HRAPP/hrms-frontend/src/app/core/services/analytics.service.ts`)
   - Track component renders (custom vs Material)
   - Track errors with component context
   - Calculate error rates per module
   - Auto-rollback logic if error rate > 5%
   - Integration with console logging (upgradeable to GCP later)

3. **ErrorTrackingService** (`/workspaces/HRAPP/hrms-frontend/src/app/core/services/error-tracking.service.ts`)
   - Comprehensive error tracking with component context
   - Error categorization and severity levels
   - Error rate calculation and monitoring
   - Auto-rollback trigger integration
   - Global error handler for Angular

## Configuration

All configuration is centralized in `/workspaces/HRAPP/hrms-frontend/src/environments/environment.ts`:

```typescript
environment.featureFlags: {
  apiEndpoint: string;           // Feature flag API endpoint
  cacheDurationMs: number;       // Cache duration (default: 5 minutes)
  autoRefreshIntervalMs: number; // Auto-refresh interval (default: 10 minutes)
  enableCaching: boolean;        // Enable local storage caching
  enableDebugLogging: boolean;   // Enable debug logging
}

environment.analytics: {
  enabled: boolean;              // Enable analytics tracking
  errorRateThreshold: number;    // Error rate threshold for auto-rollback (0.05 = 5%)
  minSamplesForRollback: number; // Minimum samples before auto-rollback (default: 10)
  enableLogging: boolean;        // Enable console logging
  enableBackendReporting: boolean; // Enable backend reporting
  batchSize: number;            // Batch size for events (default: 50)
  flushIntervalMs: number;      // Flush interval (default: 60 seconds)
  apiEndpoint: string;          // Analytics API endpoint
}

environment.errorTracking: {
  enabled: boolean;             // Enable error tracking
  errorRateWindowMs: number;    // Error rate window (default: 1 minute)
  errorRateThreshold: number;   // Error rate threshold (default: 5 errors/minute)
  maxStoredErrors: number;      // Maximum stored errors (default: 100)
  enableLogging: boolean;       // Enable console logging
  autoReportCritical: true;     // Auto-report critical errors
}
```

## Usage Guide

### 1. Initialize Services in App Component

```typescript
import { Component, inject, OnInit } from '@angular/core';
import { FeatureFlagService } from './core/services/feature-flag.service';
import { AnalyticsService } from './core/services/analytics.service';
import { ErrorTrackingService } from './core/services/error-tracking.service';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  template: `...`
})
export class AppComponent implements OnInit {
  private featureFlags = inject(FeatureFlagService);
  private analytics = inject(AnalyticsService);
  private errorTracking = inject(ErrorTrackingService);
  private auth = inject(AuthService);

  ngOnInit() {
    // Set user and tenant context when authenticated
    const user = this.auth.user();
    const tenantId = 'tenant-123'; // Get from tenant context service

    if (user) {
      this.featureFlags.setUserId(user.id);
      this.featureFlags.setTenantId(tenantId);

      this.analytics.setUserId(user.id);
      this.analytics.setTenantId(tenantId);

      this.errorTracking.setUserId(user.id);
      this.errorTracking.setTenantId(tenantId);

      // Refresh feature flags
      this.featureFlags.refreshFlags().subscribe();
    }
  }
}
```

### 2. Use Feature Flags in Components

```typescript
import { Component, inject, computed } from '@angular/core';
import { FeatureFlagService, FeatureModule } from './core/services/feature-flag.service';
import { AnalyticsService, ComponentLibrary } from './core/services/analytics.service';
import { ErrorTrackingService } from './core/services/error-tracking.service';

@Component({
  selector: 'app-login',
  template: `
    @if (useCustomAuth()) {
      <custom-login-form />
    } @else {
      <material-login-form />
    }
  `
})
export class LoginComponent {
  private featureFlags = inject(FeatureFlagService);
  private analytics = inject(AnalyticsService);
  private errorTracking = inject(ErrorTrackingService);

  // Use computed signal for reactive feature flag
  readonly useCustomAuth = computed(() => {
    const enabled = this.featureFlags.authEnabled();

    // Track feature flag evaluation
    this.analytics.trackFeatureFlagEvaluated(
      FeatureModule.Auth,
      enabled
    );

    return enabled;
  });

  ngOnInit() {
    try {
      // Track component render
      this.analytics.trackComponentRender(
        FeatureModule.Auth,
        'LoginComponent',
        this.useCustomAuth() ? ComponentLibrary.Custom : ComponentLibrary.Material
      );

      // Set error context for better error tracking
      this.errorTracking.setContext({
        module: FeatureModule.Auth,
        componentName: 'LoginComponent',
        library: this.useCustomAuth() ? ComponentLibrary.Custom : ComponentLibrary.Material
      });
    } catch (error) {
      // Track component error
      this.errorTracking.trackComponentError(
        error as Error,
        FeatureModule.Auth,
        'LoginComponent',
        this.useCustomAuth() ? ComponentLibrary.Custom : ComponentLibrary.Material
      );
    }
  }

  ngOnDestroy() {
    // Clear error context on destroy
    this.errorTracking.clearContext();
  }
}
```

### 3. Direct Feature Flag Checks

```typescript
import { Component, inject } from '@angular/core';
import { FeatureFlagService, FeatureModule } from './core/services/feature-flag.service';

@Component({
  selector: 'app-dashboard',
  template: `...`
})
export class DashboardComponent {
  private featureFlags = inject(FeatureFlagService);

  ngOnInit() {
    // Check individual feature flags
    if (this.featureFlags.isFeatureEnabled(FeatureModule.Dashboard)) {
      console.log('Using custom dashboard components');
    }

    // Or use computed signals
    const dashboardEnabled = this.featureFlags.dashboardEnabled();
    const employeesEnabled = this.featureFlags.employeesEnabled();
    const leaveEnabled = this.featureFlags.leaveEnabled();
  }
}
```

### 4. Error Handling Best Practices

```typescript
import { Component, inject } from '@angular/core';
import { ErrorTrackingService, ErrorContext } from './core/services/error-tracking.service';
import { FeatureModule } from './core/services/feature-flag.service';
import { ComponentLibrary } from './core/services/analytics.service';

@Component({
  selector: 'app-employee-list',
  template: `...`
})
export class EmployeeListComponent {
  private errorTracking = inject(ErrorTrackingService);

  ngOnInit() {
    // Set error context
    this.errorTracking.setContext({
      module: FeatureModule.Employees,
      componentName: 'EmployeeListComponent',
      library: ComponentLibrary.Custom
    });

    // Your component logic with error handling
    try {
      this.loadEmployees();
    } catch (error) {
      // Error will automatically be tracked with context
      this.errorTracking.trackError(error as Error);
    }
  }

  loadEmployees() {
    this.http.get('/api/employees').subscribe({
      next: (data) => {
        // Success handling
      },
      error: (error) => {
        // Track HTTP error
        this.errorTracking.trackHttpError(error);
      }
    });
  }
}
```

### 5. Monitoring Auto-Rollback

```typescript
import { Component, inject, effect } from '@angular/core';
import { AnalyticsService } from './core/services/analytics.service';
import { FeatureModule } from './core/services/feature-flag.service';

@Component({
  selector: 'app-root',
  template: `...`
})
export class AppComponent {
  private analytics = inject(AnalyticsService);

  constructor() {
    // Monitor for auto-rollback triggers
    effect(() => {
      const rollbacks = this.analytics.rollbackTriggers();

      if (rollbacks.has(FeatureModule.Auth)) {
        console.error('🚨 AUTO-ROLLBACK: Auth module has been rolled back due to high error rate!');
        // Show notification to user or admin
      }
    });
  }
}
```

## Available Feature Modules

```typescript
enum FeatureModule {
  Auth = 'auth',
  Dashboard = 'dashboard',
  Employees = 'employees',
  Leave = 'leave',
  Payroll = 'payroll',
  Attendance = 'attendance',
  Reports = 'reports',
  Settings = 'settings'
}
```

## Rollout Strategies

### 1. Disabled (All Users See Material)
```typescript
{
  module: FeatureModule.Auth,
  enabled: false,
  strategy: RolloutStrategy.Disabled,
  rolloutPercentage: 0
}
```

### 2. Percentage-Based Rollout (Gradual)
```typescript
{
  module: FeatureModule.Dashboard,
  enabled: true,
  strategy: RolloutStrategy.Percentage,
  rolloutPercentage: 25  // 25% of users see custom components
}
```

### 3. Enabled (All Users See Custom)
```typescript
{
  module: FeatureModule.Leave,
  enabled: true,
  strategy: RolloutStrategy.Enabled,
  rolloutPercentage: 100
}
```

### 4. User-Specific Overrides
```typescript
{
  module: FeatureModule.Payroll,
  enabled: true,
  strategy: RolloutStrategy.Percentage,
  rolloutPercentage: 50,
  userOverrides: {
    'user-123': true,   // Always show custom
    'user-456': false   // Always show Material
  }
}
```

## Auto-Rollback Logic

The system automatically monitors error rates and triggers rollback if:

1. **Error Rate Threshold**: Error rate > 5% (configurable)
2. **Minimum Samples**: At least 10 component renders (configurable)
3. **Time Window**: Within 1 minute window (configurable)

When triggered:
- Feature flag is automatically disabled for that module
- Event is logged to analytics
- Admin notification can be sent
- Users automatically fall back to Material components

## Backend API Integration

### Expected Endpoints

1. **GET /api/feature-flags/current-tenant**
   - Returns feature flags for current tenant
   - Authenticated endpoint (requires valid JWT)
   - Response format:
   ```json
   {
     "success": true,
     "data": [
       {
         "module": "auth",
         "enabled": true,
         "strategy": "percentage",
         "rolloutPercentage": 50,
         "userOverrides": {},
         "tenantId": "tenant-123",
         "updatedAt": "2025-11-15T00:00:00Z"
       }
     ]
   }
   ```

2. **POST /api/analytics/events**
   - Receives batched analytics events
   - Authenticated endpoint
   - Request format:
   ```json
   {
     "events": [
       {
         "type": "component_render",
         "module": "auth",
         "library": "custom",
         "componentName": "LoginComponent",
         "timestamp": 1700000000000,
         "userId": "user-123",
         "tenantId": "tenant-123",
         "sessionId": "session_123"
       }
     ]
   }
   ```

## Testing

### Manual Testing

```typescript
// In browser console or component
const featureFlags = inject(FeatureFlagService);

// Force enable a feature
featureFlags.setUserId('test-user-123');
featureFlags.refreshFlags().subscribe();

// Check feature status
console.log('Auth enabled:', featureFlags.authEnabled());
console.log('Dashboard enabled:', featureFlags.dashboardEnabled());

// Get all flags
console.log('All flags:', featureFlags.getAllFlags());

// Force refresh from backend
featureFlags.forceRefresh().subscribe();
```

### Testing Auto-Rollback

```typescript
const analytics = inject(AnalyticsService);
const errorTracking = inject(ErrorTrackingService);

// Simulate multiple errors to trigger rollback
for (let i = 0; i < 10; i++) {
  errorTracking.trackComponentError(
    new Error('Test error'),
    FeatureModule.Auth,
    'TestComponent',
    ComponentLibrary.Custom
  );
}

// Check if rollback was triggered
console.log('Should rollback:', analytics.shouldRollback(FeatureModule.Auth));
console.log('Error stats:', analytics.getErrorRateForModule(FeatureModule.Auth));
```

## Monitoring & Debugging

### View Analytics

```typescript
const analytics = inject(AnalyticsService);

// View all events
console.log('Total events:', analytics.totalEvents());
console.log('Total errors:', analytics.totalErrors());
console.log('Overall error rate:', analytics.overallErrorRate());

// View error stats per module
console.log('Error stats:', analytics.errorStats());

// View rollback triggers
console.log('Rollbacks:', analytics.rollbackTriggers());
```

### View Error Tracking

```typescript
const errorTracking = inject(ErrorTrackingService);

// View all errors
console.log('All errors:', errorTracking.errors());
console.log('Total errors:', errorTracking.totalErrors());

// View errors by module
console.log('Auth errors:', errorTracking.getErrorsForModule(FeatureModule.Auth));

// View critical errors
console.log('Critical errors:', errorTracking.getCriticalErrors());

// View error rate
console.log('Error rate:', errorTracking.getOverallErrorRate());
```

## Best Practices

1. **Always set context**: Set user ID, tenant ID, and error context when components initialize
2. **Track everything**: Track both renders and errors for accurate analytics
3. **Use computed signals**: Leverage Angular's computed signals for reactive feature flags
4. **Clear context on destroy**: Always clear error context when components are destroyed
5. **Handle rollback gracefully**: Monitor rollback triggers and provide fallback UI
6. **Test thoroughly**: Test both custom and Material components before rollout
7. **Start small**: Begin with low rollout percentages (10-25%) and monitor
8. **Monitor closely**: Watch error rates and analytics during initial rollout
9. **Document issues**: Track any issues found during rollout for future reference
10. **Communicate**: Keep stakeholders informed of rollout progress and issues

## Next Steps

1. **Backend Implementation**: Implement the feature flag and analytics API endpoints
2. **Admin Dashboard**: Create admin interface to manage feature flags
3. **GCP Integration**: Upgrade console logging to Google Cloud Platform
4. **Alerting**: Set up alerts for high error rates and auto-rollbacks
5. **A/B Testing**: Implement more sophisticated A/B testing metrics
6. **Performance Monitoring**: Add performance tracking for component render times

## Support

For questions or issues with the feature flag infrastructure:
- Check console logs for debug information
- Review error tracking service for detailed error context
- Verify feature flag configuration in environment.ts
- Ensure backend API endpoints are working correctly
