# Table Component Usage Guide

## Overview

A production-ready table component to replace Angular Material's `mat-table`. Features include sorting, row selection (single/multi), loading states, empty states, hover effects, and responsive design.

## Basic Usage

```typescript
import { TableComponent, TableColumn } from '@app/shared/ui/ui.module';

@Component({
  selector: 'app-example',
  imports: [TableComponent],
  template: `
    <app-table
      [columns]="columns"
      [data]="data"
      [loading]="loading"
      (rowClick)="onRowClick($event)"
    />
  `
})
export class ExampleComponent {
  columns: TableColumn[] = [
    { key: 'id', label: 'ID', sortable: true, width: '100px' },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'email', label: 'Email', sortable: true },
    { key: 'status', label: 'Status' }
  ];

  data = [
    { id: 1, name: 'John Doe', email: 'john@example.com', status: 'Active' },
    { id: 2, name: 'Jane Smith', email: 'jane@example.com', status: 'Inactive' }
  ];

  loading = false;

  onRowClick(row: any) {
    console.log('Row clicked:', row);
  }
}
```

## Features

### 1. Sortable Columns

```typescript
columns: TableColumn[] = [
  { key: 'name', label: 'Name', sortable: true },
  { key: 'email', label: 'Email', sortable: true }
];

<app-table
  [columns]="columns"
  [data]="data"
  [sortKey]="currentSortKey"
  [sortDirection]="currentSortDirection"
  (sortChange)="onSortChange($event)"
/>

onSortChange(event: SortEvent) {
  this.currentSortKey = event.key;
  this.currentSortDirection = event.direction;
  // Perform sorting logic
}
```

### 2. Row Selection (Multi-Select)

```typescript
<app-table
  [columns]="columns"
  [data]="data"
  [selectable]="true"
  [multiSelect]="true"
  (selectionChange)="onSelectionChange($event)"
/>

onSelectionChange(selectedRows: any[]) {
  console.log('Selected rows:', selectedRows);
}
```

### 3. Row Selection (Single-Select)

```typescript
<app-table
  [columns]="columns"
  [data]="data"
  [selectable]="true"
  [multiSelect]="false"
  (selectionChange)="onSelectionChange($event)"
/>
```

### 4. Loading State

```typescript
<app-table
  [columns]="columns"
  [data]="data"
  [loading]="isLoading"
/>
```

Shows animated skeleton loaders while data is being fetched.

### 5. Striped Rows

```typescript
<app-table
  [columns]="columns"
  [data]="data"
  [striped]="true"
/>
```

### 6. Disable Hover Effects

```typescript
<app-table
  [columns]="columns"
  [data]="data"
  [hoverable]="false"
/>
```

## API Reference

### Inputs

| Input | Type | Default | Description |
|-------|------|---------|-------------|
| `columns` | `TableColumn[]` | `[]` | Array of column definitions |
| `data` | `any[]` | `[]` | Array of data objects to display |
| `loading` | `boolean` | `false` | Shows skeleton loading state |
| `selectable` | `boolean` | `false` | Enables row selection |
| `multiSelect` | `boolean` | `false` | Enables multi-row selection with checkboxes |
| `sortKey` | `string \| null` | `null` | Currently sorted column key |
| `sortDirection` | `'asc' \| 'desc' \| null` | `null` | Current sort direction |
| `hoverable` | `boolean` | `true` | Enables hover effects on rows |
| `striped` | `boolean` | `false` | Alternates row background colors |

### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `rowClick` | `EventEmitter<any>` | Emits when a row is clicked |
| `selectionChange` | `EventEmitter<any[]>` | Emits when row selection changes |
| `sortChange` | `EventEmitter<SortEvent>` | Emits when column sort changes |

### Types

```typescript
interface TableColumn {
  key: string;          // Property key to display from data objects
  label: string;        // Column header label
  sortable?: boolean;   // Enable sorting for this column
  width?: string;       // CSS width value (e.g., '100px', '20%')
}

interface SortEvent {
  key: string;          // Column key that was sorted
  direction: 'asc' | 'desc';  // Sort direction
}
```

## Custom Column Templates

For columns with custom content (chips, buttons, icons), use column templates:

### Method 1: Using Content Projection

```typescript
import { TableComponent, TableColumn, TableColumnDirective } from '@app/shared/ui/ui.module';

@Component({
  selector: 'app-department-list',
  imports: [TableComponent, TableColumnDirective, MatChipModule, MatIconModule],
  template: `
    <app-table
      [columns]="columns"
      [data]="departments"
      [loading]="loading">

      <!-- Custom template for status column -->
      <ng-template appTableColumn="status" let-row let-value="value">
        <mat-chip [class.active]="row.isActive">
          {{ value }}
        </mat-chip>
      </ng-template>

      <!-- Custom template for actions column -->
      <ng-template appTableColumn="actions" let-row>
        <button mat-icon-button (click)="edit(row)">
          <mat-icon>edit</mat-icon>
        </button>
        <button mat-icon-button (click)="delete(row)">
          <mat-icon>delete</mat-icon>
        </button>
      </ng-template>
    </app-table>
  `
})
```

### Method 2: Using Column Formatter

For simple value transformations without HTML:

```typescript
columns: TableColumn[] = [
  {
    key: 'salary',
    label: 'Salary',
    sortable: true,
    formatter: (value, row) => `$${value.toLocaleString()}`
  },
  {
    key: 'status',
    label: 'Status',
    formatter: (value, row) => row.isActive ? 'Active ✓' : 'Inactive ✗'
  }
];
```

### Method 3: Using ViewChild and TemplateRef

For programmatic template assignment:

```typescript
@ViewChild('statusTemplate', { read: TemplateRef }) statusTemplate!: TemplateRef<any>;

ngAfterViewInit() {
  this.columns = [
    { key: 'name', label: 'Name', sortable: true },
    { key: 'status', label: 'Status', cellTemplate: this.statusTemplate }
  ];
}

template: `
  <ng-template #statusTemplate let-row let-value="value">
    <span [class]="'status-' + value.toLowerCase()">{{ value }}</span>
  </ng-template>

  <app-table [columns]="columns" [data]="data"></app-table>
`
```

## Advanced Examples

### Complete Example with All Features

```typescript
import { Component } from '@angular/core';
import { TableComponent, TableColumn, SortEvent } from '@app/shared/ui/ui.module';

@Component({
  selector: 'app-employee-list',
  imports: [TableComponent],
  template: `
    <app-table
      [columns]="columns"
      [data]="filteredData"
      [loading]="loading"
      [selectable]="true"
      [multiSelect]="true"
      [hoverable]="true"
      [striped]="true"
      [sortKey]="sortKey"
      [sortDirection]="sortDirection"
      (rowClick)="onRowClick($event)"
      (selectionChange)="onSelectionChange($event)"
      (sortChange)="onSortChange($event)"
    />
  `
})
export class EmployeeListComponent {
  columns: TableColumn[] = [
    { key: 'id', label: 'ID', sortable: true, width: '80px' },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'department', label: 'Department', sortable: true },
    { key: 'email', label: 'Email', sortable: true },
    { key: 'status', label: 'Status', sortable: true, width: '120px' }
  ];

  data: any[] = [];
  filteredData: any[] = [];
  loading = false;
  sortKey: string | null = null;
  sortDirection: 'asc' | 'desc' | null = null;
  selectedEmployees: any[] = [];

  ngOnInit() {
    this.loadEmployees();
  }

  loadEmployees() {
    this.loading = true;
    // Simulate API call
    setTimeout(() => {
      this.data = [
        { id: 1, name: 'John Doe', department: 'Engineering', email: 'john@example.com', status: 'Active' },
        { id: 2, name: 'Jane Smith', department: 'HR', email: 'jane@example.com', status: 'Active' },
        { id: 3, name: 'Bob Johnson', department: 'Sales', email: 'bob@example.com', status: 'Inactive' }
      ];
      this.filteredData = [...this.data];
      this.loading = false;
    }, 1500);
  }

  onSortChange(event: SortEvent) {
    this.sortKey = event.key;
    this.sortDirection = event.direction;

    this.filteredData = [...this.data].sort((a, b) => {
      const aVal = a[event.key];
      const bVal = b[event.key];

      if (aVal < bVal) return event.direction === 'asc' ? -1 : 1;
      if (aVal > bVal) return event.direction === 'asc' ? 1 : -1;
      return 0;
    });
  }

  onRowClick(employee: any) {
    console.log('Employee clicked:', employee);
    // Navigate to employee detail page
  }

  onSelectionChange(selected: any[]) {
    this.selectedEmployees = selected;
    console.log('Selected employees:', selected);
  }
}
```

### Handling Empty State

The table automatically displays an empty state when no data is available:

```typescript
<app-table
  [columns]="columns"
  [data]="[]"  <!-- Empty array shows "No data available" -->
  [loading]="false"
/>
```

### Custom Column Widths

```typescript
columns: TableColumn[] = [
  { key: 'id', label: 'ID', width: '80px' },
  { key: 'name', label: 'Name', width: '200px' },
  { key: 'email', label: 'Email', width: '250px' },
  { key: 'status', label: 'Status', width: '120px' }
];
```

## Styling

The table component uses the design system tokens from `/src/styles/index.scss`. All colors, spacing, and animations are consistent with the HRMS design system.

### Responsive Behavior

- **Desktop (>768px)**: Full table layout with all features
- **Tablet (640px-768px)**: Slightly reduced padding
- **Mobile (<640px)**: Horizontal scrolling enabled, minimum width enforced

## Best Practices

1. **Always provide unique keys**: Ensure each row has a unique identifier for proper tracking
2. **Use loading state**: Show loading skeleton while fetching data for better UX
3. **Limit columns on mobile**: Consider hiding less important columns on small screens
4. **Handle sort on backend**: For large datasets, perform sorting server-side
5. **Debounce selection changes**: If performing heavy operations on selection, debounce the handler

## Migration from mat-table

```typescript
// Before (mat-table)
<table mat-table [dataSource]="dataSource">
  <ng-container matColumnDef="name">
    <th mat-header-cell *matHeaderCellDef mat-sort-header>Name</th>
    <td mat-cell *matCellDef="let row">{{ row.name }}</td>
  </ng-container>
</table>

// After (app-table)
<app-table
  [columns]="[{ key: 'name', label: 'Name', sortable: true }]"
  [data]="data"
  (sortChange)="onSortChange($event)"
/>
```

## Browser Support

- Chrome/Edge: Latest 2 versions
- Firefox: Latest 2 versions
- Safari: Latest 2 versions
- Mobile browsers: iOS Safari 13+, Chrome Android latest

## Performance

- Optimized for datasets up to 1000 rows
- For larger datasets, implement:
  - Virtual scrolling
  - Server-side pagination
  - Server-side sorting/filtering
