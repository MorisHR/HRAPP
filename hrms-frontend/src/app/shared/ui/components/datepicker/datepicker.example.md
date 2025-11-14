# Datepicker Component Usage

## Overview
Production-ready datepicker component to replace Material datepicker with full keyboard navigation, accessibility, and responsive design.

## Basic Usage

```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-example',
  template: `
    <app-datepicker
      label="Birth Date"
      [value]="birthDate"
      (valueChange)="onDateChange($event)"
    />
  `
})
export class ExampleComponent {
  birthDate: Date | null = null;

  onDateChange(date: Date): void {
    this.birthDate = date;
    console.log('Selected date:', date);
  }
}
```

## With Validation

```typescript
import { Component } from '@angular/core';

@Component({
  template: `
    <app-datepicker
      label="Start Date"
      placeholder="MM/DD/YYYY"
      [value]="startDate"
      [minDate]="minDate"
      [maxDate]="maxDate"
      [required]="true"
      [error]="error"
      [disabled]="isLoading"
      (valueChange)="onStartDateChange($event)"
    />
  `
})
export class ExampleComponent {
  startDate: Date | null = null;
  minDate: Date = new Date();
  maxDate: Date = new Date(2030, 11, 31);
  error: string | null = null;
  isLoading: boolean = false;

  onStartDateChange(date: Date): void {
    this.startDate = date;
    this.validateDate(date);
  }

  validateDate(date: Date): void {
    if (date < this.minDate) {
      this.error = 'Start date cannot be in the past';
    } else if (date > this.maxDate) {
      this.error = 'Start date is too far in the future';
    } else {
      this.error = null;
    }
  }
}
```

## Date Range Picker

```typescript
import { Component } from '@angular/core';

@Component({
  template: `
    <div class="date-range">
      <app-datepicker
        label="Start Date"
        [value]="startDate"
        [maxDate]="endDate"
        (valueChange)="onStartDateChange($event)"
      />

      <app-datepicker
        label="End Date"
        [value]="endDate"
        [minDate]="startDate"
        (valueChange)="onEndDateChange($event)"
      />
    </div>
  `
})
export class DateRangeComponent {
  startDate: Date | null = null;
  endDate: Date | null = null;

  onStartDateChange(date: Date): void {
    this.startDate = date;
    // Clear end date if it's before start date
    if (this.endDate && this.endDate < date) {
      this.endDate = null;
    }
  }

  onEndDateChange(date: Date): void {
    this.endDate = date;
  }
}
```

## Input Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `label` | `string` | `''` | Label text displayed above the input |
| `value` | `Date \| null` | `null` | Selected date value |
| `minDate` | `Date \| null` | `null` | Minimum selectable date |
| `maxDate` | `Date \| null` | `null` | Maximum selectable date |
| `disabled` | `boolean` | `false` | Disables the datepicker |
| `required` | `boolean` | `false` | Marks field as required (shows asterisk) |
| `placeholder` | `string` | `'MM/DD/YYYY'` | Placeholder text when no date selected |
| `error` | `string \| null` | `null` | Error message to display |

## Output Events

| Event | Type | Description |
|-------|------|-------------|
| `valueChange` | `EventEmitter<Date>` | Emits when date is selected |

## Features

### Calendar Features
- ✅ Month/year navigation with prev/next buttons
- ✅ Month/year dropdown selectors
- ✅ Today button for quick selection
- ✅ Clear button to reset selection
- ✅ Highlights today's date
- ✅ Highlights selected date
- ✅ Shows weekend days differently
- ✅ Disables dates outside min/max range
- ✅ 6-week calendar grid (consistent layout)

### Keyboard Navigation
- **Arrow Keys**: Navigate between dates
  - Left/Right: Previous/next day
  - Up/Down: Previous/next week
- **Home**: First day of month
- **End**: Last day of month
- **PageUp**: Previous month
- **PageDown**: Next month
- **Enter**: Select focused date
- **Escape**: Close calendar

### Accessibility
- ✅ Full ARIA support
- ✅ Screen reader friendly
- ✅ Keyboard navigation
- ✅ Focus management
- ✅ High contrast mode support
- ✅ Reduced motion support

### Responsive Design
- ✅ Mobile-optimized layout
- ✅ Touch-friendly tap targets
- ✅ Adaptive sizing
- ✅ Click outside to close

## Styling Customization

The component uses design tokens from the HRMS design system:

```scss
// Override in your global styles if needed
:root {
  --datepicker-primary-color: #1976d2;
  --datepicker-border-radius: 8px;
  --datepicker-elevation: 0 4px 6px rgba(0, 0, 0, 0.1);
}
```

## Migration from mat-datepicker

### Before (Material)
```html
<mat-form-field>
  <mat-label>Birth Date</mat-label>
  <input matInput [matDatepicker]="picker" [(ngModel)]="birthDate">
  <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
  <mat-datepicker #picker></mat-datepicker>
  <mat-error *ngIf="error">{{ error }}</mat-error>
</mat-form-field>
```

### After (Custom Datepicker)
```html
<app-datepicker
  label="Birth Date"
  [value]="birthDate"
  [error]="error"
  (valueChange)="birthDate = $event"
/>
```

## Advanced Examples

### With Reactive Forms

```typescript
import { Component } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';

@Component({
  template: `
    <app-datepicker
      label="Date of Birth"
      [value]="dateControl.value"
      [error]="getErrorMessage()"
      [required]="true"
      (valueChange)="dateControl.setValue($event)"
    />
  `
})
export class FormExampleComponent {
  dateControl = new FormControl<Date | null>(null, [Validators.required]);

  getErrorMessage(): string | null {
    if (this.dateControl.hasError('required')) {
      return 'Date is required';
    }
    return null;
  }
}
```

### Disable Weekends

```typescript
@Component({
  template: `
    <app-datepicker
      label="Business Day"
      [value]="selectedDate"
      [minDate]="minDate"
      (valueChange)="onDateSelect($event)"
    />
  `
})
export class BusinessDayComponent {
  selectedDate: Date | null = null;
  minDate: Date = new Date();

  onDateSelect(date: Date): void {
    const day = date.getDay();
    if (day === 0 || day === 6) {
      alert('Please select a weekday');
      return;
    }
    this.selectedDate = date;
  }
}
```

## Browser Support
- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Edge 90+
- ✅ Mobile Safari 14+
- ✅ Chrome Mobile 90+
