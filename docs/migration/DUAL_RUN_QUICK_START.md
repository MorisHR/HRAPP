# Dual-Run Pattern - Quick Start Guide

## What Was Done

Migrated **Employee List Component** to dual-run pattern (Material UI ↔ Custom UI)

**File**: `/workspaces/HRAPP/hrms-frontend/src/app/features/tenant/employees/employee-list.component.ts`

---

## How to Test

### Method 1: Toggle Button (Easiest)

1. Start the dev server:
   ```bash
   cd /workspaces/HRAPP/hrms-frontend
   npm start
   ```

2. Navigate to: `http://localhost:4200/tenant/employees`

3. Look for the **purple/green toggle button** in the top-right corner

4. Click to switch between:
   - ✅ **Green** = Custom UI Components (NEW)
   - ❌ **Purple** = Material UI Components (OLD)

5. Open browser console (F12) to see analytics logs

### Method 2: Code Toggle

Edit line 181 in `employee-list.component.ts`:

```typescript
// Line 181: Change this value
private mockFeatureFlags = signal({
  employees: true   // true = Custom UI, false = Material UI
});
```

---

## What to Look For

### Custom UI Mode (Feature Flag ON)
- Custom card with subtle shadow
- Custom table with hover effects and alternating row colors
- Custom buttons with gradient hover states
- Custom icons from icon registry
- Custom progress spinner

### Material UI Mode (Feature Flag OFF)
- Standard Material Design card
- Material table (mat-table)
- Material buttons with ripple
- Material icons
- Material spinner

---

## Console Output Examples

**On Page Load**:
```
📋 Employee List Component initialized
📊 Analytics: employee-list rendered with CUSTOM components
{
  timestamp: "2025-11-15T05:55:00.000Z",
  featureFlag: { employees: true }
}
```

**On Toggle Click**:
```
🔄 Feature flag toggled: { employees: false }
📊 Analytics: employee-list rendered with MATERIAL components
{
  timestamp: "2025-11-15T05:56:00.000Z",
  featureFlag: { employees: false }
}
```

---

## Feature Comparison

| Feature | Material UI | Custom UI | Status |
|---------|-------------|-----------|--------|
| Employee list display | ✅ | ✅ | Identical |
| Loading spinner | ✅ | ✅ | Identical |
| Empty state | ✅ | ✅ | Identical |
| Add employee button | ✅ | ✅ | Identical |
| Edit employee | ✅ | ✅ | Identical |
| Delete employee | ✅ | ✅ | Identical |
| Table sorting | ✅ | ✅ | Identical |
| Responsive design | ✅ | ✅ | Identical |

---

## Troubleshooting

### Issue: Toggle button not visible
**Solution**: Check if you're on the `/tenant/employees` route

### Issue: Console errors about icons
**Solution**: Icons use the IconRegistryService - ensure it's properly configured

### Issue: Table doesn't show data
**Solution**: This is expected if no employees exist - use "Add Employee" button

### Issue: Can't click toggle button
**Solution**: Check z-index (should be 1000) and ensure it's not behind other elements

---

## Code Locations

**Component File**:
```
/workspaces/HRAPP/hrms-frontend/src/app/features/tenant/employees/employee-list.component.ts
```

**Feature Flag Mock** (lines 180-182):
```typescript
private mockFeatureFlags = signal({
  employees: true  // Change this to toggle
});
```

**Analytics Logs** (lines 199-210):
```typescript
private logComponentRender(componentName: string, useCustom: boolean)
```

**Toggle Button** (lines 42-50 in template):
```html
<div class="dev-controls">
  <button class="toggle-btn" (click)="toggleFeatureFlag()">
```

---

## Next Actions

### Replace Mock Services (Once DevOps creates them)

**Before** (Current):
```typescript
private mockFeatureFlags = signal({ employees: true });
useCustomComponents = computed(() => this.mockFeatureFlags().employees);
```

**After** (Production):
```typescript
private featureFlags = inject(FeatureFlagService);
useCustomComponents = computed(() => this.featureFlags.isEnabled('employees'));
```

### Remove Dev Toggle (Before Production)

Delete lines 42-50 (the dev-controls div) from the template

---

## Migration Template for Other Components

Copy this pattern to migrate other components:

```typescript
// 1. Import custom components
import { CardComponent } from '../../../shared/ui/components/card/card';
import { ButtonComponent } from '../../../shared/ui/components/button/button';

// 2. Add to imports array
imports: [
  MatCardModule,  // Keep Material
  CardComponent,  // Add Custom
  // ... other imports
]

// 3. Add feature flag
useCustomComponents = computed(() => this.featureFlags.isEnabled('MODULE_NAME'));

// 4. Split template
@if (useCustomComponents()) {
  <!-- Custom components -->
} @else {
  <!-- Material components (existing) -->
}

// 5. Add analytics
this.analytics.logComponentRender('component-name', this.useCustomComponents());
```

---

## Performance Notes

- **Bundle Size**: Both Material and Custom components are loaded (acceptable for transition)
- **Runtime**: Only ONE path executes (no double rendering)
- **Memory**: Signals ensure efficient reactivity
- **Re-renders**: Minimal - only on feature flag change

---

## Support

**Questions?** Check the full report: `/workspaces/HRAPP/DUAL_RUN_MIGRATION_REPORT.md`

**Issues?** Contact the Frontend Migration team
