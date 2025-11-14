# Toast/Snackbar Service - Production Ready

## Overview

A production-ready Toast/Snackbar notification system that replaces Angular Material's `mat-snackbar` with a Fortune 500-grade implementation featuring:

- **4 notification types** (success, error, warning, info) with distinct styling
- **Auto-dismiss** with configurable duration
- **Manual dismiss** on click
- **Action buttons** with callbacks
- **Toast stacking** (multiple toasts displayed simultaneously)
- **Smooth animations** (slide-in/slide-out with cubic-bezier easing)
- **Progress bar** showing auto-dismiss countdown
- **Pause on hover** (pauses auto-dismiss when user hovers over toast)
- **4 position options** (top-right, top-center, bottom-right, bottom-center)
- **Accessibility** (ARIA attributes, keyboard support)
- **Responsive design** (mobile-optimized)
- **Dark mode support**
- **Design token integration**

---

## Files Created

### Service Layer
- `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/services/toast.ts` - Main service
- `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/services/toast-ref.ts` - Toast reference class
- `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/services/toast-usage-examples.ts` - Usage examples

### Component Layer
- `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toast-container/toast-container.ts` - Container component
- `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toast-container/toast-container.html` - Template
- `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toast-container/toast-container.scss` - Styles

### Module Integration
- Updated `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/ui.module.ts` to export `ToastService`

---

## Usage

### Basic Usage

```typescript
import { Component, inject } from '@angular/core';
import { ToastService } from '@app/shared/ui';

@Component({
  selector: 'app-my-component',
  template: `<button (click)="showToast()">Show Toast</button>`
})
export class MyComponent {
  private toastService = inject(ToastService);

  showToast() {
    // Quick methods for common types
    this.toastService.success('Operation completed!');
    this.toastService.error('Something went wrong');
    this.toastService.warning('Be careful!');
    this.toastService.info('New updates available');
  }
}
```

### Custom Duration

```typescript
// Short toast (1 second)
this.toastService.success('Quick message', 1000);

// Long toast (5 seconds)
this.toastService.error('Important error', 5000);

// Persistent toast (won't auto-dismiss)
this.toastService.warning('Manual dismiss only', 0);
```

### With Action Button

```typescript
this.toastService.show({
  message: 'Item deleted',
  type: 'success',
  action: {
    label: 'Undo',
    callback: () => {
      // Restore the item
      console.log('Undo clicked');
    }
  }
});
```

### Different Positions

```typescript
this.toastService.show({
  message: 'Top right notification',
  type: 'info',
  position: 'top-right'  // Default
});

this.toastService.show({
  message: 'Centered notification',
  type: 'info',
  position: 'top-center'
});

this.toastService.show({
  message: 'Bottom notification',
  type: 'info',
  position: 'bottom-right'
});
```

### Advanced Configuration

```typescript
const toastRef = this.toastService.show({
  message: 'Complex notification with all features',
  type: 'warning',
  duration: 5000,
  position: 'top-right',
  customClass: 'my-custom-toast',
  dismissible: true,
  showProgress: true,
  action: {
    label: 'View Details',
    callback: () => {
      // Navigate to details page
    }
  }
});

// Subscribe to lifecycle events
toastRef.afterDismissed().subscribe(() => {
  console.log('Toast dismissed');
});

toastRef.onAction().subscribe(() => {
  console.log('Action button clicked');
});

// Manually dismiss
toastRef.dismiss();
```

---

## API Reference

### ToastService

#### Methods

##### `success(message: string, duration?: number): ToastRef`
Displays a success toast (green theme).

##### `error(message: string, duration?: number): ToastRef`
Displays an error toast (red theme).

##### `warning(message: string, duration?: number): ToastRef`
Displays a warning toast (orange theme).

##### `info(message: string, duration?: number): ToastRef`
Displays an info toast (blue theme).

##### `show(config: ToastConfig): ToastRef`
Displays a toast with custom configuration.

##### `dismiss(toastRef: ToastRef): void`
Dismisses a specific toast.

##### `dismissAll(): void`
Dismisses all active toasts.

##### `getActiveToasts(): ToastRef[]`
Returns an array of all currently active toasts.

### ToastConfig Interface

```typescript
interface ToastConfig {
  message: string;               // Required: The message to display
  type: ToastType;               // Required: 'success' | 'error' | 'warning' | 'info'
  duration?: number;             // Optional: Duration in ms (default: 3000, 0 = no auto-dismiss)
  action?: {                     // Optional: Action button configuration
    label: string;               // Button text
    callback?: () => void;       // Click handler
  };
  position?: ToastPosition;      // Optional: 'top-right' | 'top-center' | 'bottom-right' | 'bottom-center'
  customClass?: string;          // Optional: Custom CSS class
  dismissible?: boolean;         // Optional: Allow click to dismiss (default: true)
  showProgress?: boolean;        // Optional: Show progress bar (default: true)
}
```

### ToastRef Class

#### Methods

##### `dismiss(): void`
Dismisses the toast immediately.

##### `triggerAction(): void`
Triggers the action and dismisses the toast.

##### `pauseAutoDismiss(): void`
Pauses the auto-dismiss timer (used internally on hover).

##### `resumeAutoDismiss(callback: () => void, remainingTime: number): void`
Resumes the auto-dismiss timer (used internally when hover ends).

#### Observables

##### `afterDismissed(): Observable<void>`
Emits when the toast has been dismissed.

##### `onAction(): Observable<void>`
Emits when the action button is clicked.

---

## Features in Detail

### 1. Auto-Dismiss with Progress Bar
- Default 3-second duration
- Visual progress bar showing time remaining
- Customizable duration (any number in milliseconds)
- Set duration to 0 for persistent toasts

### 2. Pause on Hover
- Auto-dismiss pauses when user hovers over toast
- Timer resumes when mouse leaves
- Gives users time to read without pressure

### 3. Toast Stacking
- Multiple toasts can be displayed simultaneously
- Toasts stack vertically within their container
- Each position (top-right, top-center, etc.) has its own container
- Smooth animations when toasts are added/removed

### 4. Action Buttons
- Optional action button on each toast
- Execute custom callbacks
- Automatically dismisses toast after action
- Can subscribe to action observable

### 5. Accessibility
- ARIA live regions (`assertive` for errors, `polite` for others)
- Keyboard accessible
- Screen reader friendly
- High contrast mode support

### 6. Responsive Design
- Desktop: Fixed width (320-420px)
- Mobile: Full width with padding
- Touch-friendly targets
- Reduced motion support

### 7. Design Tokens
- Uses CSS custom properties for theming
- Consistent with design system
- Easy to customize
- Dark mode support built-in

---

## Real-World Examples

### Form Submission

```typescript
async submitForm(formData: any) {
  try {
    const loading = this.toastService.info('Submitting...', 0);
    await this.api.submit(formData);
    loading.dismiss();
    this.toastService.success('Form submitted successfully!');
  } catch (error) {
    this.toastService.error('Failed to submit form');
  }
}
```

### Undo Action

```typescript
deleteItem(item: any) {
  let undone = false;

  const toast = this.toastService.show({
    message: `${item.name} deleted`,
    type: 'success',
    duration: 5000,
    action: {
      label: 'Undo',
      callback: () => {
        undone = true;
        this.restoreItem(item);
      }
    }
  });

  toast.afterDismissed().subscribe(() => {
    if (!undone) {
      this.permanentlyDelete(item);
    }
  });
}
```

### File Upload

```typescript
async uploadFile(file: File) {
  const toast = this.toastService.show({
    message: 'Uploading file...',
    type: 'info',
    duration: 0,
    dismissible: false
  });

  try {
    await this.uploadService.upload(file);
    toast.dismiss();
    this.toastService.success('File uploaded successfully!');
  } catch (error) {
    toast.dismiss();
    this.toastService.error('Upload failed');
  }
}
```

### Multiple Operations

```typescript
performBatchOperation() {
  // Show multiple toasts for different events
  this.toastService.info('Starting batch operation...');

  setTimeout(() => {
    this.toastService.success('Phase 1 complete');
  }, 2000);

  setTimeout(() => {
    this.toastService.success('Phase 2 complete');
  }, 4000);

  setTimeout(() => {
    this.toastService.success('All operations completed!', 5000);
  }, 6000);
}
```

---

## Migration from mat-snackbar

### Before (Angular Material)

```typescript
constructor(private snackBar: MatSnackBar) {}

showNotification() {
  this.snackBar.open('Message', 'Action', {
    duration: 3000,
    horizontalPosition: 'end',
    verticalPosition: 'top'
  });
}
```

### After (ToastService)

```typescript
constructor(private toastService: ToastService) {}

showNotification() {
  this.toastService.show({
    message: 'Message',
    type: 'info',
    duration: 3000,
    position: 'top-right',
    action: {
      label: 'Action',
      callback: () => { /* action handler */ }
    }
  });
}

// Or use shorthand
showNotification() {
  this.toastService.info('Message');
}
```

---

## Styling Customization

### Using Custom Classes

```typescript
this.toastService.show({
  message: 'Custom styled toast',
  type: 'info',
  customClass: 'my-custom-toast'
});
```

```scss
// In your component styles
.my-custom-toast {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-left-color: #667eea;

  .toast__message {
    color: white;
  }
}
```

### Overriding Design Tokens

```scss
// In your global styles
:root {
  --color-success: #00c853;
  --color-error: #d32f2f;
  --color-warning: #ff6f00;
  --color-info: #0288d1;
}
```

---

## Performance Considerations

1. **Lazy Container Creation**: Containers are only created when first toast is shown
2. **Container Cleanup**: Empty containers are automatically destroyed
3. **Animation Optimization**: Uses CSS transforms for smooth 60fps animations
4. **Memory Management**: Proper cleanup of subscriptions and timers
5. **Change Detection**: Efficient use of Angular's change detection

---

## Browser Support

- Chrome 90+ ✓
- Firefox 88+ ✓
- Safari 14+ ✓
- Edge 90+ ✓
- Mobile browsers ✓

---

## Testing

```typescript
import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  it('should create success toast', () => {
    const ref = service.success('Test message');
    expect(ref).toBeDefined();
    expect(service.getActiveToasts().length).toBe(1);
  });

  it('should dismiss toast', (done) => {
    const ref = service.success('Test');
    ref.afterDismissed().subscribe(() => {
      expect(service.getActiveToasts().length).toBe(0);
      done();
    });
    ref.dismiss();
  });
});
```

---

## Confirmation

✅ **Service Generated**: `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/services/toast.ts`
✅ **ToastRef Generated**: `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/services/toast-ref.ts`
✅ **Component Generated**: `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toast-container/`
✅ **UI Module Updated**: Service exported from `ui.module.ts`
✅ **Full API Implemented**: All requested methods and features
✅ **Production Ready**: Enterprise-grade implementation with all features

The Toast service is now ready to replace mat-snackbar throughout your application!
