# Dialog Service - Usage Guide

## Overview

The Dialog Service is a production-ready, enterprise-grade modal/dialog system for the HRMS design system. It provides a flexible and accessible way to display dialogs with dynamic content, animations, and comprehensive configuration options.

## Features

- Dynamic component loading
- Configurable sizing and positioning
- Backdrop click and ESC key support
- Smooth enter/exit animations
- Focus trapping and accessibility (ARIA)
- Multiple dialog support
- TypeScript type safety
- Dark mode support
- Responsive design
- Reduced motion support

## Installation

The Dialog system is already integrated into the UiModule. Simply import the UiModule in your feature module:

```typescript
import { UiModule } from '@shared/ui/ui.module';

@NgModule({
  imports: [UiModule],
  // ...
})
export class YourFeatureModule {}
```

## Basic Usage

### 1. Create a Dialog Content Component

```typescript
import { Component, inject } from '@angular/core';
import { DialogRef } from '@shared/ui/ui.module';

@Component({
  selector: 'app-my-dialog',
  template: `
    <div class="dialog-content">
      <h2>{{ data.title }}</h2>
      <p>{{ data.message }}</p>
      <button (click)="close()">Close</button>
    </div>
  `
})
export class MyDialogComponent {
  dialogRef = inject(DialogRef);

  get data() {
    return this.dialogRef.data;
  }

  close() {
    this.dialogRef.close('Dialog closed!');
  }
}
```

### 2. Open the Dialog

```typescript
import { Component, inject } from '@angular/core';
import { DialogService } from '@shared/ui/ui.module';
import { MyDialogComponent } from './my-dialog.component';

@Component({
  selector: 'app-my-page',
  template: `
    <button (click)="openDialog()">Open Dialog</button>
  `
})
export class MyPageComponent {
  dialogService = inject(DialogService);

  openDialog() {
    const dialogRef = this.dialogService.open(MyDialogComponent, {
      data: {
        title: 'Hello!',
        message: 'This is a dialog'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('Dialog closed with:', result);
    });
  }
}
```

## Configuration Options

### DialogConfig Interface

```typescript
interface DialogConfig<D = any> {
  // Data to pass to the dialog component
  data?: D;

  // Sizing
  width?: string;              // Default: '600px'
  height?: string;             // Default: 'auto'
  maxWidth?: string;           // Default: '90vw'
  maxHeight?: string;          // Default: '90vh'
  minWidth?: string;
  minHeight?: string;

  // Behavior
  disableClose?: boolean;      // Default: false
  hasCloseButton?: boolean;    // Default: true
  autoFocus?: boolean;         // Default: true

  // Styling
  backdropClass?: string | string[];
  panelClass?: string | string[];

  // Accessibility
  ariaLabel?: string;
  ariaDescribedBy?: string;
  role?: 'dialog' | 'alertdialog';
}
```

## Advanced Examples

### Example 1: Confirmation Dialog

```typescript
const dialogRef = this.dialogService.open(ConfirmDialogComponent, {
  width: '400px',
  disableClose: true,
  data: {
    title: 'Delete User?',
    message: 'Are you sure you want to delete this user? This action cannot be undone.',
    confirmText: 'Delete',
    cancelText: 'Cancel'
  }
});

dialogRef.afterClosed().subscribe(confirmed => {
  if (confirmed) {
    // Perform delete action
  }
});
```

### Example 2: Large Form Dialog

```typescript
const dialogRef = this.dialogService.open(UserFormComponent, {
  width: '900px',
  maxHeight: '90vh',
  disableClose: true,
  data: {
    userId: 123,
    mode: 'edit'
  }
});

dialogRef.afterClosed().subscribe(result => {
  if (result) {
    console.log('Form submitted:', result);
  }
});
```

### Example 3: Custom Styled Dialog

```typescript
const dialogRef = this.dialogService.open(CustomDialogComponent, {
  width: '600px',
  backdropClass: 'dark-backdrop',
  panelClass: ['custom-panel', 'elevated'],
  hasCloseButton: false,
  role: 'alertdialog'
});
```

### Example 4: Fullscreen Dialog

```typescript
const dialogRef = this.dialogService.open(DetailsComponent, {
  width: '100vw',
  height: '100vh',
  maxWidth: '100vw',
  maxHeight: '100vh',
  panelClass: 'dialog-panel--fullscreen'
});
```

## Dialog Lifecycle

### Opening a Dialog

```typescript
const dialogRef = this.dialogService.open(MyComponent, config);

// Subscribe to opened event
dialogRef.afterOpened().subscribe(() => {
  console.log('Dialog opened and visible');
});
```

### Closing a Dialog

```typescript
// From within the dialog component
this.dialogRef.close(resultData);

// From the parent component
this.dialogService.close(dialogRef, resultData);

// Close all dialogs
this.dialogService.closeAll();
```

### Listening to Close Event

```typescript
dialogRef.afterClosed().subscribe(result => {
  console.log('Dialog result:', result);
});

dialogRef.beforeClosed().subscribe(result => {
  console.log('Dialog is about to close with:', result);
});
```

## Accessibility Features

### Keyboard Navigation

- **ESC**: Closes the dialog (unless `disableClose: true`)
- **TAB**: Moves focus to next focusable element (trapped within dialog)
- **SHIFT+TAB**: Moves focus to previous focusable element

### ARIA Attributes

```typescript
this.dialogService.open(MyComponent, {
  role: 'alertdialog',
  ariaLabel: 'User deletion confirmation',
  ariaDescribedBy: 'dialog-description'
});
```

### Focus Management

- Auto-focuses first focusable element on open (configurable)
- Traps focus within dialog
- Restores focus to triggering element on close

## Service Methods

### DialogService API

```typescript
// Open a dialog
open<T, D, R>(component: Type<T>, config?: DialogConfig<D>): DialogRef<T, R>

// Close a specific dialog
close<R>(dialogRef: DialogRef<any, R>, result?: R): void

// Close all open dialogs
closeAll(): void

// Get all open dialogs
getOpenDialogs(): DialogRef[]

// Get dialog by ID
getDialogById(id: string): DialogRef | undefined
```

### DialogRef API

```typescript
// Close this dialog
close(result?: R): void

// Observable that emits when dialog closes
afterClosed(): Observable<R | undefined>

// Observable that emits when dialog opens
afterOpened(): Observable<void>

// Observable that emits before dialog closes
beforeClosed(): Observable<R | undefined>

// Access dialog data
data: any

// Dialog unique ID
id: string
```

## Styling

### Custom Panel Classes

```scss
// In your component styles or global styles
.dialog-panel--custom {
  border: 2px solid #3b82f6;
  border-radius: 16px;
}

.dialog-panel--no-padding {
  .dialog-content {
    padding: 0;
  }
}
```

### Custom Backdrop

```scss
.dark-backdrop {
  background-color: rgba(0, 0, 0, 0.85) !important;
}

.light-backdrop {
  background-color: rgba(255, 255, 255, 0.95) !important;
}
```

## Best Practices

1. **Always handle the close result**: Subscribe to `afterClosed()` to handle user actions
2. **Use TypeScript generics**: Specify data and result types for type safety
3. **Provide ARIA labels**: For better accessibility, especially for alert dialogs
4. **Test keyboard navigation**: Ensure TAB and ESC keys work as expected
5. **Consider mobile**: Test dialogs on small screens
6. **Avoid nested dialogs**: Only open one dialog at a time for better UX
7. **Set disableClose for critical actions**: Prevent accidental closure for important operations

## Complete Example

```typescript
// dialog-data.ts
export interface UserFormData {
  userId?: number;
  mode: 'create' | 'edit';
}

export interface UserFormResult {
  success: boolean;
  user: User;
}

// user-form-dialog.component.ts
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { DialogRef } from '@shared/ui/ui.module';

@Component({
  selector: 'app-user-form-dialog',
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="user-form-dialog">
      <h2>{{ isEditMode ? 'Edit User' : 'Create User' }}</h2>

      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <input formControlName="name" placeholder="Name" />
        <input formControlName="email" placeholder="Email" />

        <div class="actions">
          <button type="button" (click)="onCancel()">Cancel</button>
          <button type="submit">Save</button>
        </div>
      </form>
    </div>
  `
})
export class UserFormDialogComponent {
  dialogRef = inject(DialogRef<UserFormDialogComponent, UserFormResult>);
  fb = inject(FormBuilder);

  form = this.fb.group({
    name: [''],
    email: ['']
  });

  get isEditMode() {
    return this.dialogRef.data?.mode === 'edit';
  }

  onCancel() {
    this.dialogRef.close();
  }

  onSubmit() {
    const result: UserFormResult = {
      success: true,
      user: this.form.value as User
    };
    this.dialogRef.close(result);
  }
}

// parent.component.ts
import { Component, inject } from '@angular/core';
import { DialogService } from '@shared/ui/ui.module';

@Component({
  selector: 'app-parent',
  template: `<button (click)="openUserDialog()">Add User</button>`
})
export class ParentComponent {
  dialogService = inject(DialogService);

  openUserDialog() {
    const dialogRef = this.dialogService.open<
      UserFormDialogComponent,
      UserFormData,
      UserFormResult
    >(UserFormDialogComponent, {
      width: '600px',
      disableClose: true,
      data: {
        mode: 'create'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.success) {
        console.log('User created:', result.user);
        // Refresh data, show success message, etc.
      }
    });
  }
}
```

## File Structure

```
src/app/shared/ui/
├── services/
│   ├── dialog.ts           // DialogService and DialogConfig
│   └── dialog-ref.ts       // DialogRef class
├── components/
│   └── dialog-container/
│       ├── dialog-container.ts
│       ├── dialog-container.html
│       └── dialog-container.scss
└── ui.module.ts            // Exports DialogService and DialogRef
```

## TypeScript Types

```typescript
// Import types
import {
  DialogService,
  DialogConfig,
  DialogRef
} from '@shared/ui/ui.module';

// Use with generics
const dialogRef: DialogRef<MyComponent, MyResult> =
  this.dialogService.open<MyComponent, MyData, MyResult>(
    MyComponent,
    { data: myData }
  );
```

## Troubleshooting

### Dialog doesn't close on backdrop click
- Check if `disableClose: true` is set in config
- Verify the backdrop click handler is not being prevented

### Focus not trapped
- Ensure dialog content has focusable elements
- Check if `autoFocus: false` is set

### Animations not working
- Check if user has `prefers-reduced-motion` enabled
- Verify Angular animations are properly configured

### Dialog content not displaying
- Ensure the content component is properly created
- Check browser console for errors
- Verify the component is not using route-specific dependencies

## Support

For issues or questions, please refer to the HRMS design system documentation or contact the development team.
