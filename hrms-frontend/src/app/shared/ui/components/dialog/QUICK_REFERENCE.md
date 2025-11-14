# Dialog System - Quick Reference Card

## Import

```typescript
import { DialogService, DialogRef } from '@shared/ui/ui.module';
import type { DialogConfig } from '@shared/ui/ui.module';
```

## Basic Usage

```typescript
// 1. Create dialog component
@Component({
  template: `<h2>{{ data?.title }}</h2>`
})
class MyDialog {
  dialogRef = inject(DialogRef);
  get data() { return this.dialogRef.data; }
  close() { this.dialogRef.close('result'); }
}

// 2. Open dialog
dialogService = inject(DialogService);
const ref = this.dialogService.open(MyDialog, {
  width: '600px',
  data: { title: 'Hello' }
});

// 3. Handle result
ref.afterClosed().subscribe(result => {
  console.log(result);
});
```

## Configuration Options

```typescript
{
  // Data
  data?: any,

  // Size
  width?: string,           // '600px'
  height?: string,          // 'auto'
  maxWidth?: string,        // '90vw'
  maxHeight?: string,       // '90vh'
  minWidth?: string,
  minHeight?: string,

  // Behavior
  disableClose?: boolean,   // false
  hasCloseButton?: boolean, // true
  autoFocus?: boolean,      // true

  // Style
  backdropClass?: string | string[],
  panelClass?: string | string[],

  // Accessibility
  ariaLabel?: string,
  ariaDescribedBy?: string,
  role?: 'dialog' | 'alertdialog'
}
```

## Common Patterns

### Confirmation Dialog
```typescript
const ref = dialogService.open(ConfirmDialog, {
  width: '400px',
  disableClose: true,
  data: { title: 'Confirm?', message: 'Are you sure?' }
});

ref.afterClosed().subscribe(confirmed => {
  if (confirmed) { /* do action */ }
});
```

### Form Dialog
```typescript
const ref = dialogService.open(FormDialog, {
  width: '600px',
  maxHeight: '90vh',
  data: { userId: 123 }
});

ref.afterClosed().subscribe(formData => {
  if (formData) { /* save data */ }
});
```

### Alert Dialog
```typescript
dialogService.open(AlertDialog, {
  width: '400px',
  hasCloseButton: false,
  disableClose: true,
  role: 'alertdialog'
});
```

## Service Methods

```typescript
// Open
open<T, D, R>(component, config?): DialogRef<T, R>

// Close specific
close(dialogRef, result?)

// Close all
closeAll()

// Get open dialogs
getOpenDialogs(): DialogRef[]

// Get by ID
getDialogById(id): DialogRef | undefined
```

## DialogRef API

```typescript
// Close
close(result?)

// Observables
afterClosed(): Observable<R | undefined>
afterOpened(): Observable<void>
beforeClosed(): Observable<R | undefined>

// Properties
data: any
id: string
```

## Keyboard Shortcuts

- **ESC** - Close dialog (unless disableClose)
- **TAB** - Next focusable element
- **SHIFT+TAB** - Previous focusable element

## File Locations

- Service: `src/app/shared/ui/services/dialog.ts`
- DialogRef: `src/app/shared/ui/services/dialog-ref.ts`
- Container: `src/app/shared/ui/components/dialog-container/`
- Examples: `src/app/shared/ui/components/dialog/`

## TypeScript Generics

```typescript
dialogService.open<
  ComponentType,  // Dialog component
  DataType,       // Input data
  ResultType      // Output result
>(component, config)
```

## Custom Styling

```scss
// In global or component styles
.custom-backdrop {
  background: rgba(0, 0, 0, 0.85) !important;
}

.custom-panel {
  border: 2px solid #3b82f6;
  border-radius: 16px;
}
```

```typescript
dialogService.open(MyDialog, {
  backdropClass: 'custom-backdrop',
  panelClass: 'custom-panel'
});
```

## Best Practices

1. Always handle `afterClosed()` subscription
2. Use TypeScript generics for type safety
3. Set `disableClose: true` for critical actions
4. Provide ARIA labels for accessibility
5. Test keyboard navigation
6. Avoid nested dialogs
7. Consider mobile viewport

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Won't close on backdrop | Check `disableClose` config |
| No animations | User has reduced motion enabled |
| Focus not trapped | Ensure focusable elements exist |
| Content not showing | Check browser console for errors |

## Version: 1.0.0
**Status:** Production Ready ✓
