# Feature Flag Infrastructure - Quick Start Guide

## 5-Minute Integration Guide

### Step 1: Initialize in AppComponent

```typescript
// app.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from './core/services/auth.service';
import { initializeFeatureRollout } from './core/services/feature-rollout.helper';

@Component({
  selector: 'app-root',
  template: `<router-outlet />`
})
export class AppComponent implements OnInit {
  private auth = inject(AuthService);

  ngOnInit() {
    // Get authenticated user
    const user = this.auth.user();

    if (user) {
      // Initialize feature rollout infrastructure
      const tenantId = 'YOUR_TENANT_ID'; // Get from TenantContextService
      initializeFeatureRollout(user.id, tenantId);
    }
  }
}
```

### Step 2: Use in Components

```typescript
// login.component.ts
import { Component, inject, computed } from '@angular/core';
import { useFeatureRollout } from './core/services/feature-rollout.helper';
import { FeatureModule } from './core/services/feature-flag.service';

@Component({
  selector: 'app-login',
  template: `
    @if (useCustomAuth()) {
      <custom-login-form />
    } @else {
      <mat-card>
        <!-- Material Design login form -->
      </mat-card>
    }
  `
})
export class LoginComponent implements OnInit, OnDestroy {
  // Initialize rollout helper
  private rollout = useFeatureRollout(FeatureModule.Auth, 'LoginComponent');

  // Reactive computed signal
  readonly useCustomAuth = computed(() => this.rollout.isEnabled());

  ngOnInit() {
    // Track component render
    this.rollout.trackRender();

    // Set error context for better tracking
    this.rollout.setContext();
  }

  onSubmit(credentials: LoginRequest) {
    try {
      // Your login logic
      this.authService.login(credentials).subscribe({
        next: () => {
          console.log('Login successful');
        },
        error: (error) => {
          // Track error with context
          this.rollout.trackError(error);
        }
      });
    } catch (error) {
      // Track unexpected errors
      this.rollout.trackError(error as Error);
    }
  }

  ngOnDestroy() {
    // Clean up error context
    this.rollout.clearContext();
  }
}
```

### Step 3: Handle Rollback (Optional)

```typescript
// app.component.ts
import { Component, inject, effect } from '@angular/core';
import { getActiveRollbacks } from './core/services/feature-rollout.helper';
import { FeatureModule } from './core/services/feature-flag.service';

@Component({
  selector: 'app-root',
  template: `...`
})
export class AppComponent {
  constructor() {
    // Monitor for auto-rollback events
    effect(() => {
      const rollbacks = getActiveRollbacks();

      if (rollbacks.size > 0) {
        console.warn('Auto-rollback triggered for:', Array.from(rollbacks));

        // Show notification to admin
        if (rollbacks.has(FeatureModule.Auth)) {
          this.showAdminAlert('Auth module rolled back due to high error rate');
        }
      }
    });
  }

  showAdminAlert(message: string) {
    // Your notification logic
  }
}
```

## That's It!

You're now ready to:
- ✅ Gradually roll out custom components
- ✅ Track component renders and errors
- ✅ Get automatic rollback protection
- ✅ Monitor error rates in real-time

## Next Steps

1. Read [FEATURE-FLAG-INFRASTRUCTURE.md](./FEATURE-FLAG-INFRASTRUCTURE.md) for complete documentation
2. Review [IMPLEMENTATION-SUMMARY.md](./IMPLEMENTATION-SUMMARY.md) for technical details
3. Start migrating components one module at a time
4. Monitor analytics and error rates

## Common Patterns

### Pattern 1: Simple Toggle
```typescript
class MyComponent {
  private rollout = useFeatureRollout(FeatureModule.Dashboard, 'MyComponent');

  readonly useCustomUI = computed(() => this.rollout.isEnabled());
}
```

### Pattern 2: With Error Handling
```typescript
class MyComponent {
  private rollout = useFeatureRollout(FeatureModule.Employees, 'MyComponent');

  ngOnInit() {
    this.rollout.trackRender();
    this.rollout.setContext();

    try {
      this.loadData();
    } catch (error) {
      this.rollout.trackError(error as Error);
    }
  }

  ngOnDestroy() {
    this.rollout.clearContext();
  }
}
```

### Pattern 3: Conditional Logic
```typescript
class MyComponent {
  private rollout = useFeatureRollout(FeatureModule.Leave, 'MyComponent');

  submit() {
    if (this.rollout.isEnabled()) {
      this.submitWithCustomValidation();
    } else {
      this.submitWithMaterialValidation();
    }
  }
}
```

## Available Feature Modules

```typescript
FeatureModule.Auth        // Login, signup, password reset
FeatureModule.Dashboard   // Dashboard widgets and layout
FeatureModule.Employees   // Employee management
FeatureModule.Leave       // Leave management
FeatureModule.Payroll     // Payroll processing
FeatureModule.Attendance  // Attendance tracking
FeatureModule.Reports     // Report generation
FeatureModule.Settings    // Settings pages
```

## Troubleshooting

### Issue: Feature flags not loading
```typescript
// Check console for errors
// Force refresh manually
const featureFlags = inject(FeatureFlagService);
featureFlags.forceRefresh().subscribe();
```

### Issue: Error tracking not working
```typescript
// Verify context is set
const errorTracking = inject(ErrorTrackingService);
console.log('Current context:', errorTracking.currentContext());
```

### Issue: Rollback not triggering
```typescript
// Check error rate and threshold
const rollout = useFeatureRollout(FeatureModule.Auth, 'Test');
console.log('Error rate:', rollout.getErrorRate());
console.log('Should rollback:', rollout.shouldRollback());
```

## Questions?

Refer to:
- [FEATURE-FLAG-INFRASTRUCTURE.md](./FEATURE-FLAG-INFRASTRUCTURE.md) - Complete guide
- [IMPLEMENTATION-SUMMARY.md](./IMPLEMENTATION-SUMMARY.md) - Technical details
- Console logs (enabled by default in development)
