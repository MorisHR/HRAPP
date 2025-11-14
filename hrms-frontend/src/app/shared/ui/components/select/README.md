# Select Component

A production-ready, accessible dropdown/select component for the HRMS design system.

## Features

- Single and multiple selection
- Searchable dropdown
- Clearable selection
- Keyboard navigation (Arrow keys, Enter, Escape, Tab)
- Error and hint messages
- Disabled state
- Custom styling with design tokens
- Full accessibility (ARIA attributes)
- Smooth animations
- Responsive design
- Dark mode support

## Basic Usage

```typescript
import { SelectComponent } from './shared/ui/components/select/select';

@Component({
  selector: 'app-example',
  imports: [SelectComponent],
  template: `
    <app-select
      label="Department"
      placeholder="Select a department"
      [options]="departments"
      [value]="selectedDepartment"
      (valueChange)="onDepartmentChange($event)"
    />
  `
})
export class ExampleComponent {
  departments = [
    { value: 'hr', label: 'Human Resources' },
    { value: 'it', label: 'Information Technology' },
    { value: 'sales', label: 'Sales' },
    { value: 'finance', label: 'Finance' }
  ];

  selectedDepartment = null;

  onDepartmentChange(value: any) {
    console.log('Selected:', value);
    this.selectedDepartment = value;
  }
}
```

## API

### Inputs

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `label` | `string` | `''` | Label text displayed above the select |
| `placeholder` | `string` | `'Select an option'` | Placeholder text when no value selected |
| `options` | `SelectOption[]` | `[]` | Array of options to display |
| `value` | `any` | `null` | Currently selected value(s) |
| `disabled` | `boolean` | `false` | Disable the select |
| `required` | `boolean` | `false` | Show required indicator (*) |
| `error` | `string \| null` | `null` | Error message to display |
| `hint` | `string` | `''` | Hint text below the select |
| `searchable` | `boolean` | `false` | Enable search functionality |
| `clearable` | `boolean` | `false` | Show clear button |
| `multiple` | `boolean` | `false` | Enable multiple selection |

### Outputs

| Event | Type | Description |
|-------|------|-------------|
| `valueChange` | `EventEmitter<any>` | Emitted when selection changes |
| `blur` | `EventEmitter<FocusEvent>` | Emitted when select loses focus |

### SelectOption Interface

```typescript
interface SelectOption {
  value: any;           // The actual value
  label: string;        // Display text
  disabled?: boolean;   // Optional: disable this option
}
```

## Examples

### Single Select

```html
<app-select
  label="Country"
  placeholder="Select your country"
  [options]="countries"
  [value]="selectedCountry"
  (valueChange)="selectedCountry = $event"
/>
```

### Multiple Select

```html
<app-select
  label="Skills"
  placeholder="Select your skills"
  [options]="skills"
  [value]="selectedSkills"
  [multiple]="true"
  (valueChange)="selectedSkills = $event"
/>
```

### Searchable Select

```html
<app-select
  label="Employee"
  placeholder="Search for an employee"
  [options]="employees"
  [value]="selectedEmployee"
  [searchable]="true"
  (valueChange)="selectedEmployee = $event"
/>
```

### Clearable Select

```html
<app-select
  label="Manager"
  placeholder="Select a manager"
  [options]="managers"
  [value]="selectedManager"
  [clearable]="true"
  (valueChange)="selectedManager = $event"
/>
```

### With Error

```html
<app-select
  label="Department"
  placeholder="Select a department"
  [options]="departments"
  [value]="selectedDepartment"
  [required]="true"
  [error]="departmentError"
  (valueChange)="onDepartmentChange($event)"
/>
```

```typescript
departmentError: string | null = null;

onDepartmentChange(value: any) {
  this.selectedDepartment = value;
  this.departmentError = value ? null : 'Please select a department';
}
```

### With Hint

```html
<app-select
  label="Role"
  hint="Select the primary role for this user"
  [options]="roles"
  [value]="selectedRole"
  (valueChange)="selectedRole = $event"
/>
```

### Disabled Options

```typescript
options = [
  { value: '1', label: 'Option 1' },
  { value: '2', label: 'Option 2', disabled: true },
  { value: '3', label: 'Option 3' }
];
```

### Disabled Select

```html
<app-select
  label="Fixed Department"
  [options]="departments"
  [value]="fixedDepartment"
  [disabled]="true"
/>
```

## Keyboard Navigation

- **Arrow Down/Up**: Navigate through options
- **Enter/Space**: Select focused option or open dropdown
- **Escape**: Close dropdown
- **Tab**: Close dropdown and move to next field
- **Type to search**: When searchable, type to filter options

## Accessibility

- Full ARIA support (`aria-expanded`, `aria-haspopup`, `role="listbox"`, etc.)
- Keyboard navigation
- Focus management
- Screen reader friendly
- High contrast mode support
- Visible focus indicators

## Styling

The component uses design tokens from the HRMS design system. All colors, spacing, typography, and animations are consistent with the design system.

To customize:

```scss
// Override in your component's styles
::ng-deep .select {
  // Your custom styles
}
```

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers

## Notes

- Options are filtered client-side when searchable
- Dropdown automatically closes on outside click
- Supports both single and multiple selection modes
- Animations respect `prefers-reduced-motion`
- Fully responsive on mobile devices
