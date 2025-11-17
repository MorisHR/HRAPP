# Component Library Documentation

> **Fortune 500-Grade Custom UI Component Library**
> Production-ready components replacing Angular Material with enhanced features and accessibility.

## Table of Contents

1. [Overview](#overview)
2. [Component Catalog](#component-catalog)
3. [Installation & Usage](#installation--usage)
4. [Accessibility Guidelines](#accessibility-guidelines)
5. [Best Practices](#best-practices)

---

## Overview

This HRMS application includes **29 custom UI components** that provide a consistent, accessible, and performant user interface. All components follow WCAG 2.1 Level AA accessibility standards and are optimized for enterprise-scale applications.

### Design System Principles

- **Consistency**: Uniform look and feel across all components
- **Accessibility**: WCAG 2.1 Level AA compliant with keyboard navigation
- **Performance**: Lightweight with minimal dependencies
- **Customization**: Flexible theming and configuration options
- **Type Safety**: Full TypeScript support with strict typing

### Component Architecture

All components follow these patterns:
- Angular standalone components (v17+)
- Signal-based reactive state management
- Component-scoped styling with SCSS
- ControlValueAccessor for form integration
- Comprehensive error handling

---

## Component Catalog

### 1. Button Component

**Location**: `/src/app/shared/ui/components/button/`

A versatile button component with multiple variants, sizes, and states.

#### API

```typescript
@Component({
  selector: 'app-button',
  standalone: true
})
export class ButtonComponent {
  @Input() variant: ButtonVariant = 'primary'; // 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'ghost'
  @Input() size: ButtonSize = 'medium';        // 'small' | 'medium' | 'large'
  @Input() disabled: boolean = false;
  @Input() fullWidth: boolean = false;
  @Input() loading: boolean = false;
  @Input() type: 'button' | 'submit' | 'reset' = 'button';

  @Output() clicked = new EventEmitter<MouseEvent>();
}
```

#### Usage Examples

```html
<!-- Basic button -->
<app-button variant="primary" (clicked)="handleClick()">
  Save Changes
</app-button>

<!-- Loading state -->
<app-button
  variant="primary"
  [loading]="isSubmitting"
  [disabled]="isSubmitting">
  Submit
</app-button>

<!-- Full width button -->
<app-button fullWidth="true" variant="success">
  Create Account
</app-button>

<!-- Small ghost button -->
<app-button size="small" variant="ghost" (clicked)="cancel()">
  Cancel
</app-button>
```

#### Accessibility

- Keyboard accessible (Tab, Enter, Space)
- ARIA attributes for loading states
- Disabled state properly communicated
- Focus visible indicator

---

### 2. Input Component

**Location**: `/src/app/shared/ui/components/input/`

Production-ready text input with floating labels, validation, and character counting.

#### API

```typescript
@Component({
  selector: 'app-input',
  standalone: true,
  providers: [NG_VALUE_ACCESSOR]
})
export class InputComponent implements ControlValueAccessor {
  @Input() type: InputType = 'text';          // 'text' | 'email' | 'password' | 'number' | 'tel' | 'url'
  @Input() label: string = '';
  @Input() placeholder: string = '';
  @Input() value: string | number = '';
  @Input() disabled: boolean = false;
  @Input() readonly: boolean = false;
  @Input() required: boolean = false;
  @Input() error: string | null = null;
  @Input() hint: string = '';
  @Input() maxLength: number | undefined;
  @Input() showCharacterCount: boolean = false;
  @Input() clearable: boolean = false;

  @Output() valueChange = new EventEmitter<string | number>();
  @Output() blur = new EventEmitter<FocusEvent>();
  @Output() focus = new EventEmitter<FocusEvent>();
}
```

#### Usage Examples

```html
<!-- Basic input -->
<app-input
  label="Email Address"
  type="email"
  placeholder="Enter your email"
  [(value)]="email">
</app-input>

<!-- Input with validation -->
<app-input
  label="Password"
  type="password"
  [error]="passwordError"
  [required]="true"
  [(value)]="password">
</app-input>

<!-- Input with character count -->
<app-input
  label="Description"
  [maxLength]="500"
  [showCharacterCount]="true"
  hint="Provide a brief description"
  [(value)]="description">
</app-input>

<!-- Clearable input -->
<app-input
  label="Search"
  [clearable]="true"
  [(value)]="searchQuery">
</app-input>

<!-- Reactive Forms integration -->
<app-input
  label="Username"
  [formControl]="usernameControl"
  [error]="usernameControl.errors?.['required'] ? 'Username is required' : null">
</app-input>
```

#### Features

- Floating label animation
- Auto-focus on value input
- Character count display
- Clear button (optional)
- Error and hint messages
- ControlValueAccessor support

---

### 3. Datepicker Component

**Location**: `/src/app/shared/ui/components/datepicker/`

Enhanced datepicker with full calendar view, no external dependencies.

#### API

```typescript
@Component({
  selector: 'app-datepicker',
  standalone: true
})
export class Datepicker {
  value = model<Date | null>(null);           // Two-way binding with signals
  placeholder = input<string>('Select date');
  label = input<string>('');
  required = input<boolean>(false);
  disabled = input<boolean>(false);
  error = input<string | null>(null);
  minDate = input<Date | null>(null);
  maxDate = input<Date | null>(null);
}
```

#### Usage Examples

```html
<!-- Basic datepicker -->
<app-datepicker
  label="Start Date"
  [(value)]="startDate">
</app-datepicker>

<!-- Datepicker with constraints -->
<app-datepicker
  label="End Date"
  [(value)]="endDate"
  [minDate]="startDate"
  [error]="dateError">
</app-datepicker>

<!-- Disabled datepicker -->
<app-datepicker
  label="Created On"
  [value]="createdDate"
  [disabled]="true">
</app-datepicker>
```

#### Features

- Full calendar view with month/year navigation
- Today/Clear shortcuts
- Min/max date validation
- Keyboard navigation (arrow keys)
- Click-outside to close
- Mobile-responsive

---

### 4. Select Component

**Location**: `/src/app/shared/ui/components/select/`

Dropdown select with search, multi-select, and keyboard navigation.

#### API

```typescript
export interface SelectOption {
  value: any;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-select',
  standalone: true
})
export class SelectComponent {
  @Input() label: string = '';
  @Input() placeholder: string = 'Select an option';
  @Input() options: SelectOption[] = [];
  @Input() value: any = null;
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;
  @Input() error: string | null = null;
  @Input() hint: string = '';
  @Input() searchable: boolean = false;
  @Input() clearable: boolean = false;
  @Input() multiple: boolean = false;

  @Output() valueChange = new EventEmitter<any>();
  @Output() blur = new EventEmitter<FocusEvent>();
}
```

#### Usage Examples

```html
<!-- Basic select -->
<app-select
  label="Department"
  [options]="departmentOptions"
  [(value)]="selectedDepartment">
</app-select>

<!-- Searchable select -->
<app-select
  label="Employee"
  [options]="employeeOptions"
  [searchable]="true"
  [(value)]="selectedEmployee">
</app-select>

<!-- Multi-select -->
<app-select
  label="Skills"
  [options]="skillOptions"
  [multiple]="true"
  [(value)]="selectedSkills">
</app-select>

<!-- Clearable select with error -->
<app-select
  label="Location"
  [options]="locationOptions"
  [clearable]="true"
  [error]="locationError"
  [(value)]="selectedLocation">
</app-select>
```

#### TypeScript Example

```typescript
export class EmployeeFormComponent {
  departmentOptions: SelectOption[] = [
    { value: 'eng', label: 'Engineering' },
    { value: 'hr', label: 'Human Resources' },
    { value: 'sales', label: 'Sales', disabled: false }
  ];

  selectedDepartment: string = '';
}
```

---

### 5. Checkbox Component

**Location**: `/src/app/shared/ui/components/checkbox/`

Checkbox with indeterminate state support.

#### API

```typescript
@Component({
  selector: 'app-checkbox',
  standalone: true
})
export class CheckboxComponent {
  @Input() label: string = '';
  @Input() checked: boolean = false;
  @Input() indeterminate: boolean = false;
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;

  @Output() checkedChange = new EventEmitter<boolean>();
}
```

#### Usage Examples

```html
<!-- Basic checkbox -->
<app-checkbox
  label="I agree to terms and conditions"
  [(checked)]="agreedToTerms">
</app-checkbox>

<!-- Indeterminate checkbox (select all) -->
<app-checkbox
  label="Select All"
  [checked]="allSelected"
  [indeterminate]="someSelected"
  (checkedChange)="toggleAll($event)">
</app-checkbox>

<!-- Disabled checkbox -->
<app-checkbox
  label="Read Only"
  [checked]="true"
  [disabled]="true">
</app-checkbox>
```

---

### 6. Table Component

**Location**: `/src/app/shared/ui/components/table/`

Data table with sorting, selection, and customization.

#### API

```typescript
export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
}

export interface SortEvent {
  key: string;
  direction: 'asc' | 'desc';
}

@Component({
  selector: 'app-table',
  standalone: true
})
export class TableComponent {
  @Input() columns: TableColumn[] = [];
  @Input() data: any[] = [];
  @Input() loading: boolean = false;
  @Input() selectable: boolean = false;
  @Input() multiSelect: boolean = false;
  @Input() sortKey: string | null = null;
  @Input() sortDirection: 'asc' | 'desc' | null = null;
  @Input() hoverable: boolean = true;
  @Input() striped: boolean = false;

  @Output() rowClick = new EventEmitter<any>();
  @Output() selectionChange = new EventEmitter<any[]>();
  @Output() sortChange = new EventEmitter<SortEvent>();
}
```

#### Usage Examples

```html
<!-- Basic table -->
<app-table
  [columns]="columns"
  [data]="employees"
  [hoverable]="true"
  (rowClick)="viewEmployee($event)">
</app-table>

<!-- Sortable table -->
<app-table
  [columns]="columns"
  [data]="employees"
  [sortKey]="sortKey"
  [sortDirection]="sortDirection"
  (sortChange)="onSort($event)">
</app-table>

<!-- Selectable table -->
<app-table
  [columns]="columns"
  [data]="employees"
  [selectable]="true"
  [multiSelect]="true"
  (selectionChange)="onSelectionChange($event)">
</app-table>

<!-- Loading state -->
<app-table
  [columns]="columns"
  [data]="employees"
  [loading]="isLoading">
</app-table>
```

#### TypeScript Example

```typescript
export class EmployeeListComponent {
  columns: TableColumn[] = [
    { key: 'id', label: 'ID', sortable: true, width: '80px' },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'department', label: 'Department', sortable: true },
    { key: 'email', label: 'Email', sortable: false },
    { key: 'status', label: 'Status', sortable: true }
  ];

  employees: any[] = [
    { id: 1, name: 'John Doe', department: 'Engineering', email: 'john@example.com', status: 'Active' },
    { id: 2, name: 'Jane Smith', department: 'HR', email: 'jane@example.com', status: 'Active' }
  ];

  sortKey: string = 'name';
  sortDirection: 'asc' | 'desc' = 'asc';

  onSort(event: SortEvent): void {
    this.sortKey = event.key;
    this.sortDirection = event.direction;
    this.loadEmployees();
  }
}
```

---

### 7. Card Component

**Location**: `/src/app/shared/ui/components/card/`

Container component for content grouping.

#### API

```typescript
@Component({
  selector: 'app-card',
  standalone: true
})
export class CardComponent {
  @Input() elevated: boolean = false;
  @Input() bordered: boolean = true;
  @Input() padding: 'none' | 'small' | 'medium' | 'large' = 'medium';
}
```

#### Usage Examples

```html
<!-- Basic card -->
<app-card>
  <h2>Card Title</h2>
  <p>Card content goes here</p>
</app-card>

<!-- Elevated card with large padding -->
<app-card [elevated]="true" padding="large">
  <h2>Dashboard Stats</h2>
  <div class="stats">...</div>
</app-card>

<!-- No padding card -->
<app-card padding="none">
  <img src="banner.jpg" alt="Banner">
  <div style="padding: 16px;">
    <p>Content with custom padding</p>
  </div>
</app-card>
```

---

### 8. Badge Component

**Location**: `/src/app/shared/ui/components/badge/`

Small status indicators and labels.

#### API

```typescript
@Component({
  selector: 'app-badge',
  standalone: true
})
export class BadgeComponent {
  @Input() variant: 'default' | 'primary' | 'success' | 'warning' | 'error' = 'default';
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
}
```

#### Usage Examples

```html
<!-- Status badges -->
<app-badge variant="success">Active</app-badge>
<app-badge variant="warning">Pending</app-badge>
<app-badge variant="error">Inactive</app-badge>

<!-- Small badge -->
<app-badge size="small" variant="primary">New</app-badge>

<!-- In table cells -->
<td>
  <app-badge [variant]="getStatusVariant(employee.status)">
    {{ employee.status }}
  </app-badge>
</td>
```

---

### 9. Chip Component

**Location**: `/src/app/shared/ui/components/chip/`

Compact elements for tags and selections.

#### API

```typescript
@Component({
  selector: 'app-chip',
  standalone: true
})
export class ChipComponent {
  @Input() removable: boolean = false;
  @Input() disabled: boolean = false;
  @Input() variant: 'default' | 'primary' | 'success' = 'default';

  @Output() removed = new EventEmitter<void>();
}
```

#### Usage Examples

```html
<!-- Tags -->
<app-chip>JavaScript</app-chip>
<app-chip>TypeScript</app-chip>

<!-- Removable chips -->
<app-chip
  [removable]="true"
  (removed)="removeSkill(skill)">
  {{ skill }}
</app-chip>

<!-- Multiple selection display -->
<div class="chip-list">
  <app-chip
    *ngFor="let item of selectedItems"
    [removable]="true"
    (removed)="removeItem(item)">
    {{ item.label }}
  </app-chip>
</div>
```

---

### 10. Tabs Component

**Location**: `/src/app/shared/ui/components/tabs/`

Tab navigation for content organization.

#### API

```typescript
export interface Tab {
  id: string;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-tabs',
  standalone: true
})
export class TabsComponent {
  @Input() tabs: Tab[] = [];
  @Input() activeTabId: string = '';
  @Input() variant: 'default' | 'pills' = 'default';

  @Output() tabChange = new EventEmitter<string>();
}
```

#### Usage Examples

```html
<!-- Basic tabs -->
<app-tabs
  [tabs]="tabs"
  [activeTabId]="activeTab"
  (tabChange)="onTabChange($event)">
</app-tabs>

<div [ngSwitch]="activeTab">
  <div *ngSwitchCase="'profile'">Profile content</div>
  <div *ngSwitchCase="'settings'">Settings content</div>
  <div *ngSwitchCase="'activity'">Activity content</div>
</div>
```

#### TypeScript Example

```typescript
export class ProfileComponent {
  tabs: Tab[] = [
    { id: 'profile', label: 'Profile' },
    { id: 'settings', label: 'Settings' },
    { id: 'activity', label: 'Activity' }
  ];

  activeTab: string = 'profile';

  onTabChange(tabId: string): void {
    this.activeTab = tabId;
  }
}
```

---

### 11. Progress Bar Component

**Location**: `/src/app/shared/ui/components/progress-bar/`

Linear progress indicator.

#### API

```typescript
@Component({
  selector: 'app-progress-bar',
  standalone: true
})
export class ProgressBarComponent {
  @Input() value: number = 0;              // 0-100
  @Input() mode: 'determinate' | 'indeterminate' = 'determinate';
  @Input() color: 'primary' | 'success' | 'warning' = 'primary';
}
```

#### Usage Examples

```html
<!-- Determinate progress -->
<app-progress-bar [value]="uploadProgress"></app-progress-bar>

<!-- Indeterminate loading -->
<app-progress-bar mode="indeterminate"></app-progress-bar>

<!-- Success progress -->
<app-progress-bar
  [value]="completionRate"
  color="success">
</app-progress-bar>
```

---

### 12. Progress Spinner Component

**Location**: `/src/app/shared/ui/components/progress-spinner/`

Circular loading indicator.

#### API

```typescript
@Component({
  selector: 'app-progress-spinner',
  standalone: true
})
export class ProgressSpinnerComponent {
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
  @Input() color: 'primary' | 'accent' = 'primary';
}
```

#### Usage Examples

```html
<!-- Center loading spinner -->
<div class="loading-container">
  <app-progress-spinner></app-progress-spinner>
  <p>Loading...</p>
</div>

<!-- Small inline spinner -->
<app-button [disabled]="isLoading">
  <app-progress-spinner
    *ngIf="isLoading"
    size="small">
  </app-progress-spinner>
  {{ isLoading ? 'Saving...' : 'Save' }}
</app-button>
```

---

### 13. Toggle Component

**Location**: `/src/app/shared/ui/components/toggle/`

Switch/toggle for boolean settings.

#### API

```typescript
@Component({
  selector: 'app-toggle',
  standalone: true
})
export class ToggleComponent {
  @Input() checked: boolean = false;
  @Input() disabled: boolean = false;
  @Input() label: string = '';

  @Output() checkedChange = new EventEmitter<boolean>();
}
```

#### Usage Examples

```html
<!-- Basic toggle -->
<app-toggle
  label="Enable notifications"
  [(checked)]="notificationsEnabled">
</app-toggle>

<!-- Disabled toggle -->
<app-toggle
  label="Dark mode"
  [checked]="darkMode"
  [disabled]="true">
</app-toggle>

<!-- Settings list -->
<div class="settings-list">
  <app-toggle
    label="Email notifications"
    [(checked)]="settings.emailNotifications">
  </app-toggle>
  <app-toggle
    label="SMS notifications"
    [(checked)]="settings.smsNotifications">
  </app-toggle>
</div>
```

---

### 14. Radio Group Component

**Location**: `/src/app/shared/ui/components/radio-group/`

Group of radio buttons for single selection.

#### API

```typescript
export interface RadioOption {
  value: any;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-radio-group',
  standalone: true
})
export class RadioGroupComponent {
  @Input() options: RadioOption[] = [];
  @Input() value: any = null;
  @Input() name: string = '';
  @Input() disabled: boolean = false;
  @Input() layout: 'vertical' | 'horizontal' = 'vertical';

  @Output() valueChange = new EventEmitter<any>();
}
```

#### Usage Examples

```html
<!-- Vertical radio group -->
<app-radio-group
  [options]="genderOptions"
  [(value)]="selectedGender"
  name="gender">
</app-radio-group>

<!-- Horizontal radio group -->
<app-radio-group
  [options]="yesNoOptions"
  [(value)]="answer"
  layout="horizontal"
  name="answer">
</app-radio-group>
```

#### TypeScript Example

```typescript
export class FormComponent {
  genderOptions: RadioOption[] = [
    { value: 'M', label: 'Male' },
    { value: 'F', label: 'Female' },
    { value: 'O', label: 'Other' }
  ];

  selectedGender: string = '';
}
```

---

### 15. Autocomplete Component

**Location**: `/src/app/shared/ui/components/autocomplete/`

Text input with autocomplete suggestions.

#### API

```typescript
export interface AutocompleteOption {
  value: any;
  label: string;
}

@Component({
  selector: 'app-autocomplete',
  standalone: true
})
export class AutocompleteComponent {
  @Input() label: string = '';
  @Input() options: AutocompleteOption[] = [];
  @Input() value: string = '';
  @Input() disabled: boolean = false;
  @Input() placeholder: string = '';
  @Input() minLength: number = 1;

  @Output() valueChange = new EventEmitter<string>();
  @Output() optionSelected = new EventEmitter<AutocompleteOption>();
}
```

#### Usage Examples

```html
<!-- Basic autocomplete -->
<app-autocomplete
  label="Search employee"
  [options]="filteredEmployees"
  [(value)]="searchQuery"
  (optionSelected)="onEmployeeSelected($event)">
</app-autocomplete>
```

---

### 16. Stepper Component

**Location**: `/src/app/shared/ui/components/stepper/`

Multi-step workflow navigation.

#### API

```typescript
export interface Step {
  label: string;
  completed?: boolean;
  optional?: boolean;
}

@Component({
  selector: 'app-stepper',
  standalone: true
})
export class StepperComponent {
  @Input() steps: Step[] = [];
  @Input() currentStep: number = 0;
  @Input() linear: boolean = true;

  @Output() stepChange = new EventEmitter<number>();
}
```

#### Usage Examples

```html
<!-- Employee onboarding stepper -->
<app-stepper
  [steps]="onboardingSteps"
  [currentStep]="currentStep"
  [linear]="true"
  (stepChange)="onStepChange($event)">
</app-stepper>

<div [ngSwitch]="currentStep">
  <div *ngSwitchCase="0">Personal Info Form</div>
  <div *ngSwitchCase="1">Employment Details Form</div>
  <div *ngSwitchCase="2">Review & Submit</div>
</div>
```

---

### 17. Menu Component

**Location**: `/src/app/shared/ui/components/menu/`

Dropdown menu for actions.

#### API

```typescript
export interface MenuItem {
  label: string;
  icon?: string;
  action?: () => void;
  divider?: boolean;
  disabled?: boolean;
}

@Component({
  selector: 'app-menu',
  standalone: true
})
export class MenuComponent {
  @Input() items: MenuItem[] = [];
  @Input() triggerText: string = 'Menu';

  @Output() itemClick = new EventEmitter<MenuItem>();
}
```

#### Usage Examples

```html
<!-- Actions menu -->
<app-menu
  triggerText="Actions"
  [items]="actionMenuItems"
  (itemClick)="handleAction($event)">
</app-menu>
```

#### TypeScript Example

```typescript
export class EmployeeRowComponent {
  actionMenuItems: MenuItem[] = [
    { label: 'Edit', icon: 'edit', action: () => this.editEmployee() },
    { label: 'View Profile', icon: 'visibility' },
    { divider: true },
    { label: 'Delete', icon: 'delete', action: () => this.deleteEmployee() }
  ];
}
```

---

### 18. Paginator Component

**Location**: `/src/app/shared/ui/components/paginator/`

Pagination controls for data tables.

#### API

```typescript
@Component({
  selector: 'app-paginator',
  standalone: true
})
export class PaginatorComponent {
  @Input() totalItems: number = 0;
  @Input() pageSize: number = 10;
  @Input() pageSizeOptions: number[] = [5, 10, 25, 50, 100];
  @Input() currentPage: number = 1;

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();
}
```

#### Usage Examples

```html
<!-- Data table pagination -->
<app-table [data]="paginatedData" [columns]="columns"></app-table>

<app-paginator
  [totalItems]="totalEmployees"
  [pageSize]="pageSize"
  [currentPage]="currentPage"
  [pageSizeOptions]="[10, 25, 50, 100]"
  (pageChange)="onPageChange($event)"
  (pageSizeChange)="onPageSizeChange($event)">
</app-paginator>
```

---

### 19. Icon Component

**Location**: `/src/app/shared/ui/components/icon/`

Material icon wrapper.

#### API

```typescript
@Component({
  selector: 'app-icon',
  standalone: true
})
export class IconComponent {
  @Input() name: string = '';
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
  @Input() color: string = '';
}
```

#### Usage Examples

```html
<!-- Basic icon -->
<app-icon name="home"></app-icon>

<!-- Large colored icon -->
<app-icon name="error" size="large" color="#ff0000"></app-icon>

<!-- Icon in button -->
<app-button>
  <app-icon name="add"></app-icon>
  Add Employee
</app-button>
```

---

### 20. Divider Component

**Location**: `/src/app/shared/ui/components/divider/`

Visual separator for content sections.

#### API

```typescript
@Component({
  selector: 'app-divider',
  standalone: true
})
export class DividerComponent {
  @Input() vertical: boolean = false;
  @Input() inset: boolean = false;
}
```

#### Usage Examples

```html
<!-- Horizontal divider -->
<app-divider></app-divider>

<!-- Vertical divider -->
<div style="display: flex;">
  <div>Section 1</div>
  <app-divider [vertical]="true"></app-divider>
  <div>Section 2</div>
</div>
```

---

### 21. Expansion Panel Component

**Location**: `/src/app/shared/ui/components/expansion-panel/`

Collapsible content panel.

#### API

```typescript
@Component({
  selector: 'app-expansion-panel',
  standalone: true
})
export class ExpansionPanelComponent {
  @Input() title: string = '';
  @Input() expanded: boolean = false;
  @Input() disabled: boolean = false;

  @Output() expandedChange = new EventEmitter<boolean>();
}
```

#### Usage Examples

```html
<!-- Collapsible section -->
<app-expansion-panel
  title="Advanced Settings"
  [(expanded)]="advancedExpanded">
  <div class="panel-content">
    <!-- Advanced settings form -->
  </div>
</app-expansion-panel>
```

---

### 22. Dialog Container Component

**Location**: `/src/app/shared/ui/components/dialog-container/`

Modal dialog wrapper service.

#### Usage

See full documentation in `/src/app/shared/ui/components/dialog/README.md`

```typescript
// Inject DialogService
constructor(private dialog: DialogService) {}

// Open dialog
openDialog(): void {
  const dialogRef = this.dialog.open(MyDialogComponent, {
    width: '600px',
    data: { employeeId: 123 }
  });

  dialogRef.afterClosed().subscribe(result => {
    console.log('Dialog closed with result:', result);
  });
}
```

---

### 23. Toast Container Component

**Location**: `/src/app/shared/ui/components/toast-container/`

Notification toast messages.

#### API

```typescript
export interface ToastMessage {
  type: 'success' | 'error' | 'warning' | 'info';
  message: string;
  duration?: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  show(config: ToastMessage): void;
  success(message: string): void;
  error(message: string): void;
  warning(message: string): void;
  info(message: string): void;
}
```

#### Usage Examples

```typescript
// Inject ToastService
constructor(private toast: ToastService) {}

// Show notifications
save(): void {
  this.employeeService.save(employee).subscribe({
    next: () => this.toast.success('Employee saved successfully!'),
    error: () => this.toast.error('Failed to save employee')
  });
}

// Custom duration
this.toast.show({
  type: 'warning',
  message: 'Session expires in 5 minutes',
  duration: 10000 // 10 seconds
});
```

---

### 24. Sidenav Component

**Location**: `/src/app/shared/ui/components/sidenav/`

Side navigation drawer.

#### API

```typescript
@Component({
  selector: 'app-sidenav',
  standalone: true
})
export class SidenavComponent {
  @Input() opened: boolean = false;
  @Input() mode: 'over' | 'push' | 'side' = 'side';
  @Input() position: 'left' | 'right' = 'left';

  @Output() openedChange = new EventEmitter<boolean>();
}
```

#### Usage Examples

```html
<!-- Navigation layout -->
<app-sidenav
  [(opened)]="sidenavOpened"
  mode="side">
  <nav>
    <!-- Navigation menu -->
  </nav>
</app-sidenav>

<div class="content">
  <!-- Main content -->
</div>
```

---

### 25. Toolbar Component

**Location**: `/src/app/shared/ui/components/toolbar/`

Header toolbar for pages.

#### API

```typescript
@Component({
  selector: 'app-toolbar',
  standalone: true
})
export class ToolbarComponent {
  @Input() color: 'primary' | 'accent' | 'default' = 'primary';
  @Input() position: 'fixed' | 'sticky' | 'static' = 'static';
}
```

#### Usage Examples

```html
<!-- Page header -->
<app-toolbar color="primary" position="sticky">
  <button (click)="toggleSidenav()">
    <app-icon name="menu"></app-icon>
  </button>
  <h1>Employee Management</h1>
  <span class="spacer"></span>
  <app-button variant="ghost">Logout</app-button>
</app-toolbar>
```

---

### 26. List Component

**Location**: `/src/app/shared/ui/components/list/`

Structured list for content display.

#### API

```typescript
@Component({
  selector: 'app-list',
  standalone: true
})
export class ListComponent {
  @Input() items: any[] = [];
  @Input() selectable: boolean = false;

  @Output() itemClick = new EventEmitter<any>();
}
```

---

### 27. Pagination Component

**Location**: `/src/app/shared/ui/components/pagination/`

Simple page navigation.

#### API

```typescript
@Component({
  selector: 'app-pagination',
  standalone: true
})
export class PaginationComponent {
  @Input() currentPage: number = 1;
  @Input() totalPages: number = 1;
  @Input() maxVisible: number = 5;

  @Output() pageChange = new EventEmitter<number>();
}
```

---

## Installation & Usage

### Import Components

```typescript
import { ButtonComponent } from '@/shared/ui/components/button/button';
import { InputComponent } from '@/shared/ui/components/input/input';
import { DatepickerComponent } from '@/shared/ui/components/datepicker/datepicker';

@Component({
  selector: 'app-my-component',
  standalone: true,
  imports: [ButtonComponent, InputComponent, DatepickerComponent],
  template: `...`
})
export class MyComponent {
  // Component logic
}
```

### Global Styles

Components use scoped styles, but global theme variables are defined in `/src/styles.scss`:

```scss
:root {
  --primary-color: #1976d2;
  --secondary-color: #424242;
  --success-color: #4caf50;
  --warning-color: #ff9800;
  --error-color: #f44336;

  --border-radius: 4px;
  --spacing-unit: 8px;
}
```

---

## Accessibility Guidelines

### Keyboard Navigation

All components support keyboard navigation:

- **Tab**: Move focus between interactive elements
- **Enter/Space**: Activate buttons, checkboxes, toggles
- **Arrow Keys**: Navigate menus, select options, datepicker calendar
- **Escape**: Close dialogs, dropdowns, datepicker

### Screen Readers

- Proper ARIA labels and roles
- Hidden helper text for screen readers
- Live regions for dynamic content
- Semantic HTML elements

### Focus Management

- Visible focus indicators
- Logical tab order
- Focus trap in dialogs
- Auto-focus on important fields

### Color Contrast

All components meet WCAG 2.1 Level AA standards:
- Text contrast ratio ≥ 4.5:1
- Interactive element contrast ≥ 3:1
- Error states clearly visible

---

## Best Practices

### Performance

1. **Lazy Load Components**: Import only what you need
2. **Use Signals**: Leverage Angular signals for reactive state
3. **Virtual Scrolling**: For large lists, implement virtual scrolling
4. **OnPush Strategy**: Use ChangeDetectionStrategy.OnPush

### Form Integration

```typescript
import { FormControl, FormGroup, Validators } from '@angular/forms';

export class EmployeeFormComponent {
  form = new FormGroup({
    name: new FormControl('', [Validators.required]),
    email: new FormControl('', [Validators.required, Validators.email]),
    department: new FormControl('', [Validators.required])
  });

  get nameError(): string | null {
    const control = this.form.get('name');
    if (control?.hasError('required') && control.touched) {
      return 'Name is required';
    }
    return null;
  }
}
```

```html
<app-input
  label="Name"
  [formControl]="form.controls.name"
  [error]="nameError">
</app-input>
```

### Error Handling

```typescript
// Display validation errors
<app-input
  [error]="fieldControl.hasError('required') ? 'This field is required' : null">
</app-input>

// Display API errors
<app-input
  [error]="apiError">
</app-input>
```

### Component Composition

```html
<!-- Build complex UIs by composing components -->
<app-card>
  <app-toolbar>
    <h2>Employee Details</h2>
  </app-toolbar>

  <app-tabs [tabs]="tabs" [(activeTabId)]="activeTab"></app-tabs>

  <div [ngSwitch]="activeTab">
    <div *ngSwitchCase="'personal'">
      <app-input label="Name" [(value)]="name"></app-input>
      <app-datepicker label="DOB" [(value)]="dob"></app-datepicker>
    </div>
    <div *ngSwitchCase="'employment'">
      <app-select label="Department" [options]="depts"></app-select>
    </div>
  </div>

  <div class="actions">
    <app-button variant="ghost" (clicked)="cancel()">Cancel</app-button>
    <app-button variant="primary" (clicked)="save()">Save</app-button>
  </div>
</app-card>
```

### Testing Components

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ButtonComponent } from './button';

describe('ButtonComponent', () => {
  let component: ButtonComponent;
  let fixture: ComponentFixture<ButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ButtonComponent);
    component = fixture.componentInstance;
  });

  it('should emit clicked event', () => {
    spyOn(component.clicked, 'emit');

    const button = fixture.nativeElement.querySelector('button');
    button.click();

    expect(component.clicked.emit).toHaveBeenCalled();
  });
});
```

---

## Support & Contributing

For questions or issues with components:

1. Check component README files in their directories
2. Review usage examples in `/src/app/shared/ui/components/*/USAGE_EXAMPLE.*`
3. Consult the team style guide

When creating new components:

1. Follow existing component patterns
2. Include TypeScript interfaces for complex inputs
3. Add comprehensive JSDoc comments
4. Create usage examples
5. Ensure WCAG 2.1 Level AA compliance
6. Write unit tests

---

**Version**: 1.0.0
**Last Updated**: November 2025
**Maintained By**: HRMS Development Team
