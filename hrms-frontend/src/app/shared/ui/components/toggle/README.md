# Toggle Component

A production-ready iOS-style toggle switch component built to replace `mat-slide-toggle` with modern, accessible, and performant design.

## Location

```
/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toggle/
```

## Features

- **iOS-style Design**: Modern toggle switch with smooth sliding animation
- **Smooth Animations**: 200ms transition for track color and thumb position
- **Label Support**: Configurable label with before/after positioning
- **Color Variants**: Primary, success, and warning color themes
- **Disabled State**: Visual feedback for disabled toggles
- **Keyboard Accessible**: Full keyboard navigation (Tab + Space)
- **Focus Indicators**: Clear focus ring for accessibility
- **Standalone Component**: Can be imported directly without module dependencies
- **TypeScript**: Fully typed with proper interfaces

## API Reference

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `checked` | `boolean` | `false` | Current checked state of the toggle |
| `disabled` | `boolean` | `false` | Whether the toggle is disabled |
| `label` | `string` | `''` | Label text to display next to toggle |
| `labelPosition` | `'before' \| 'after'` | `'after'` | Position of the label relative to toggle |
| `color` | `'primary' \| 'success' \| 'warning'` | `'primary'` | Color theme for the toggle |

### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `checkedChange` | `EventEmitter<boolean>` | Emits when the toggle state changes |

## Usage Examples

### Basic Usage

```typescript
import { Component } from '@angular/core';
import { Toggle } from '@shared/ui';

@Component({
  selector: 'app-my-component',
  standalone: true,
  imports: [Toggle],
  template: `
    <app-toggle
      [checked]="isEnabled"
      (checkedChange)="isEnabled = $event"
    />
  `
})
export class MyComponent {
  isEnabled = false;
}
```

### With Label

```html
<!-- Label after toggle (default) -->
<app-toggle
  [checked]="notifications"
  (checkedChange)="notifications = $event"
  label="Enable notifications"
  labelPosition="after"
/>

<!-- Label before toggle -->
<app-toggle
  [checked]="darkMode"
  (checkedChange)="darkMode = $event"
  label="Dark mode"
  labelPosition="before"
/>
```

### Color Variants

```html
<!-- Primary (blue) - default -->
<app-toggle
  [checked]="setting1"
  (checkedChange)="setting1 = $event"
  label="Primary toggle"
  color="primary"
/>

<!-- Success (green) -->
<app-toggle
  [checked]="setting2"
  (checkedChange)="setting2 = $event"
  label="Success toggle"
  color="success"
/>

<!-- Warning (orange) -->
<app-toggle
  [checked]="setting3"
  (checkedChange)="setting3 = $event"
  label="Warning toggle"
  color="warning"
/>
```

### Disabled State

```html
<!-- Disabled unchecked -->
<app-toggle
  [checked]="false"
  [disabled]="true"
  label="Disabled toggle"
/>

<!-- Disabled checked -->
<app-toggle
  [checked]="true"
  [disabled]="true"
  label="Disabled checked"
/>
```

### Real-world Example: Settings Form

```typescript
import { Component } from '@angular/core';
import { Toggle } from '@shared/ui';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [Toggle],
  template: `
    <div class="settings-form">
      <div class="setting-item">
        <app-toggle
          [checked]="settings.emailNotifications"
          (checkedChange)="settings.emailNotifications = $event"
          label="Email notifications"
          color="primary"
        />
      </div>

      <div class="setting-item">
        <app-toggle
          [checked]="settings.pushNotifications"
          (checkedChange)="settings.pushNotifications = $event"
          label="Push notifications"
          color="primary"
        />
      </div>

      <div class="setting-item">
        <app-toggle
          [checked]="settings.autoSave"
          (checkedChange)="settings.autoSave = $event"
          label="Auto-save drafts"
          color="success"
        />
      </div>

      <div class="setting-item">
        <app-toggle
          [checked]="settings.analytics"
          (checkedChange)="settings.analytics = $event"
          label="Share analytics"
          [disabled]="settings.privacyMode"
        />
      </div>

      <div class="setting-item">
        <app-toggle
          [checked]="settings.privacyMode"
          (checkedChange)="onPrivacyModeChange($event)"
          label="Privacy mode"
          color="warning"
        />
      </div>
    </div>
  `,
  styles: [`
    .settings-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .setting-item {
      padding: 12px 0;
      border-bottom: 1px solid #e5e7eb;
    }
  `]
})
export class SettingsComponent {
  settings = {
    emailNotifications: true,
    pushNotifications: false,
    autoSave: true,
    analytics: true,
    privacyMode: false
  };

  onPrivacyModeChange(enabled: boolean): void {
    this.settings.privacyMode = enabled;
    if (enabled) {
      this.settings.analytics = false;
    }
  }
}
```

## Migration from mat-slide-toggle

### Before (Material)

```html
<mat-slide-toggle
  [(ngModel)]="isEnabled"
  [disabled]="isDisabled"
  color="primary"
  labelPosition="after"
>
  Enable feature
</mat-slide-toggle>
```

### After (Custom Toggle)

```html
<app-toggle
  [checked]="isEnabled"
  (checkedChange)="isEnabled = $event"
  [disabled]="isDisabled"
  color="primary"
  labelPosition="after"
  label="Enable feature"
/>
```

### Key Differences

1. **Data Binding**: Use `[checked]` with `(checkedChange)` instead of `[(ngModel)]`
2. **Label**: Pass as `@Input()` prop instead of content projection
3. **Colors**: Supports `'primary'`, `'success'`, `'warning'` (no `'accent'`)
4. **Standalone**: Can be imported directly without `MatSlideToggleModule`

## Accessibility

The toggle component includes full accessibility support:

- **Keyboard Navigation**: Use `Tab` to navigate, `Space` to toggle
- **ARIA Attributes**: Proper `role="switch"`, `aria-checked`, `aria-disabled`, `aria-label`
- **Focus Indicators**: Clear focus ring with color-matched styling
- **Screen Reader Support**: Hidden checkbox for form integration

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Tab` | Focus next/previous toggle |
| `Space` | Toggle the switch on/off |
| `Enter` | Toggle the switch on/off (via label click) |

## Design Specifications

### Dimensions
- **Track Width**: 40px
- **Track Height**: 20px
- **Thumb Size**: 16px circle
- **Thumb Travel**: 20px (40px track - 20px padding)

### Animation
- **Duration**: 200ms
- **Easing**: ease (cubic-bezier)
- **Properties**: transform, background-color

### Colors

#### Primary (Default)
- **Track (unchecked)**: `#d1d5db` (gray-300)
- **Track (checked)**: `var(--color-primary, #3b82f6)` (blue-500)
- **Focus Ring**: `rgba(59, 130, 246, 0.3)`

#### Success
- **Track (checked)**: `var(--color-success, #22c55e)` (green-500)
- **Focus Ring**: `rgba(34, 197, 94, 0.3)`

#### Warning
- **Track (checked)**: `var(--color-warning, #fb923c)` (orange-400)
- **Focus Ring**: `rgba(251, 146, 60, 0.3)`

#### Disabled
- **Track (unchecked)**: `#e5e7eb` (gray-200)
- **Track (checked)**: `#9ca3af` (gray-400)

### States

1. **Default**: Gray track, white thumb
2. **Checked**: Colored track, thumb slides right
3. **Hover**: Slightly darker track (5% brightness reduction)
4. **Active/Pressed**: Darker track (10%), slightly wider thumb
5. **Focus**: Colored ring around track
6. **Disabled**: Reduced opacity, no pointer events

## File Structure

```
toggle/
├── toggle.ts           # Component logic
├── toggle.html         # Template
├── toggle.scss         # Styles
├── README.md           # Documentation (this file)
└── USAGE_EXAMPLE.ts    # Comprehensive examples
```

## Browser Support

- Chrome/Edge: 90+
- Firefox: 88+
- Safari: 14+
- Mobile browsers: iOS 14+, Android 8+

## Performance

- **No JavaScript Animation**: Uses CSS transforms for GPU acceleration
- **Minimal Bundle Size**: ~2KB (minified + gzipped)
- **Zero Dependencies**: No external libraries required
- **Optimized Rendering**: OnPush change detection compatible

## Customization

### CSS Variables

You can customize colors via CSS variables:

```css
:root {
  --color-primary: #3b82f6;
  --color-success: #22c55e;
  --color-warning: #fb923c;
  --text-primary: #1f2937;
}
```

### Override Styles

```scss
app-toggle {
  ::ng-deep {
    .toggle-track {
      width: 50px;
      height: 25px;
    }

    .toggle-thumb {
      width: 21px;
      height: 21px;
    }
  }
}
```

## Testing

### Unit Test Example

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Toggle } from './toggle';

describe('Toggle', () => {
  let component: Toggle;
  let fixture: ComponentFixture<Toggle>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Toggle]
    }).compileComponents();

    fixture = TestBed.createComponent(Toggle);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should toggle checked state on click', () => {
    expect(component.checked).toBe(false);
    component.onToggle();
    expect(component.checked).toBe(true);
  });

  it('should emit checkedChange event', (done) => {
    component.checkedChange.subscribe(value => {
      expect(value).toBe(true);
      done();
    });
    component.onToggle();
  });

  it('should not toggle when disabled', () => {
    component.disabled = true;
    component.checked = false;
    component.onToggle();
    expect(component.checked).toBe(false);
  });

  it('should toggle on Space key', () => {
    const event = new KeyboardEvent('keydown', { key: ' ' });
    component.checked = false;
    component.onKeyDown(event);
    expect(component.checked).toBe(true);
  });
});
```

## Export from UI Module

The Toggle component is exported from the shared UI module:

```typescript
// In ui.module.ts
export { Toggle } from './components/toggle/toggle';
```

You can import it from the UI module:

```typescript
import { Toggle } from '@shared/ui';
// or
import { UiModule } from '@shared/ui';
```

## Support

For issues or questions, refer to:
- Component source: `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toggle/`
- Usage examples: `USAGE_EXAMPLE.ts`
- UI module: `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/ui.module.ts`
