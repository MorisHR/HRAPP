# Radio & RadioGroup Components

Production-ready radio button components to replace Angular Material's mat-radio.

## Features

- Custom radio button design with design tokens
- Selected state with primary color
- Disabled state support
- Vertical/horizontal layouts (RadioGroup)
- Full keyboard navigation
- WCAG 2.1 compliant accessibility (ARIA)
- Smooth animations and transitions
- Focus ring indicators
- Dark mode support

## Components

### Radio Component (`app-radio`)

Individual radio button component.

#### Inputs
- `label: string` - Label text for the radio button
- `value: any` - Value associated with this radio button
- `checked: boolean = false` - Whether the radio is checked
- `disabled: boolean = false` - Whether the radio is disabled
- `name: string` - Name attribute for the radio group

#### Outputs
- `change: EventEmitter<any>` - Emits when the radio is selected

#### Example
```html
<app-radio
  label="Option 1"
  [value]="1"
  [checked]="selectedValue === 1"
  (change)="onRadioChange($event)">
</app-radio>
```

### RadioGroup Component (`app-radio-group`)

Group of radio buttons with automatic state management.

#### Inputs
- `options: RadioOption[]` - Array of radio options
- `value: any` - Currently selected value
- `name: string` - Name for the radio group (auto-generated if not provided)
- `label: string` - Label for the entire group
- `required: boolean = false` - Whether selection is required
- `disabled: boolean = false` - Disable all radio buttons
- `layout: 'vertical' | 'horizontal' = 'vertical'` - Layout direction

#### Outputs
- `valueChange: EventEmitter<any>` - Emits when selection changes

#### RadioOption Interface
```typescript
interface RadioOption {
  label: string;
  value: any;
  disabled?: boolean;
}
```

#### Example - Vertical Layout
```typescript
// Component
options: RadioOption[] = [
  { label: 'Option 1', value: 1 },
  { label: 'Option 2', value: 2 },
  { label: 'Option 3', value: 3, disabled: true }
];
selectedValue: any;

onValueChange(value: any): void {
  this.selectedValue = value;
}
```

```html
<!-- Template -->
<app-radio-group
  label="Select an option"
  [options]="options"
  [value]="selectedValue"
  [required]="true"
  layout="vertical"
  (valueChange)="onValueChange($event)">
</app-radio-group>
```

#### Example - Horizontal Layout
```html
<app-radio-group
  label="Choose your plan"
  [options]="planOptions"
  [value]="selectedPlan"
  layout="horizontal"
  (valueChange)="onPlanChange($event)">
</app-radio-group>
```

## Keyboard Navigation

RadioGroup supports full keyboard navigation:

- `Arrow Down` / `Arrow Right` - Select next radio button
- `Arrow Up` / `Arrow Left` - Select previous radio button
- `Enter` / `Space` - Select focused radio button
- `Tab` - Move focus to next focusable element

Navigation automatically wraps around and skips disabled options.

## Accessibility

Both components are fully accessible:

- Proper ARIA roles (`role="radio"`, `role="radiogroup"`)
- ARIA attributes (`aria-checked`, `aria-disabled`, `aria-label`, `aria-required`)
- Keyboard navigation support
- Focus indicators
- Screen reader friendly labels

## Styling

Components use design tokens for consistent theming:

- `--primary-500` - Selected state color
- `--gray-*` - Border, text, and background colors
- `--spacing-*` - Consistent spacing
- `--font-size-*` - Typography
- `--transition-normal` - Smooth animations

### Dark Mode

Both components automatically adapt to dark mode via CSS `prefers-color-scheme`.

## Import

```typescript
import { UiModule } from '@app/shared/ui/ui.module';

// Or direct imports
import { Radio } from '@app/shared/ui/components/radio/radio';
import { RadioGroup, RadioOption } from '@app/shared/ui/components/radio-group/radio-group';
```

## Migration from Material

### Before (Material)
```html
<mat-radio-group [(ngModel)]="selectedValue">
  <mat-radio-button value="1">Option 1</mat-radio-button>
  <mat-radio-button value="2">Option 2</mat-radio-button>
</mat-radio-group>
```

### After (Custom)
```typescript
options = [
  { label: 'Option 1', value: '1' },
  { label: 'Option 2', value: '2' }
];
```

```html
<app-radio-group
  [options]="options"
  [(value)]="selectedValue">
</app-radio-group>
```
