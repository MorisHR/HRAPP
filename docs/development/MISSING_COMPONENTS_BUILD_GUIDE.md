# Missing Components - Build Guide
## Phase 0 Prerequisites for Migration

**Priority:** 🔴 CRITICAL BLOCKER
**Estimated Effort:** 11 hours (1.5 days)
**Status:** 🚫 NOT STARTED
**Must Complete Before:** Phase 1 migration can begin

---

## Overview

Before we can proceed with the Fortune 500 migration strategy, we need to build **4 missing components** that are currently blocking 18 feature components from migration.

### Missing Components:

| Component | Priority | Usage | Effort | Blocks |
|-----------|----------|-------|--------|--------|
| Divider | 🔴 CRITICAL | 12 components | 2h | Phase 2-5 |
| ExpansionPanel | 🟡 HIGH | 4 components | 4h | Phase 3-4 |
| List | 🟢 MEDIUM | 2 components | 3h | Phase 2 |
| Table Sort | 🟢 MEDIUM | 2 components | 2h | Phase 1 |

---

## Component 1: Divider (CRITICAL)

### Why It's Needed
Used in 12 components for visual separation:
- audit-logs.component
- tenant-audit-logs.component
- billing-overview.component
- attendance-dashboard.component
- salary-components.component
- payment dialogs
- And 6 more...

### Implementation

```typescript
// File: hrms-frontend/src/app/shared/ui/components/divider/divider.ts

import { Component, input, computed } from '@angular/core';

/**
 * Divider Component
 *
 * A simple horizontal or vertical divider line for visual separation.
 * Replaces Angular Material's MatDividerModule.
 *
 * @example
 * <app-divider />
 * <app-divider [orientation]="'vertical'" />
 * <app-divider [inset]="true" />
 */
@Component({
  selector: 'app-divider',
  standalone: true,
  template: `
    <hr
      [class]="classes()"
      [attr.role]="'separator'"
      [attr.aria-orientation]="orientation()"
    />
  `,
  styles: [`
    :host {
      display: block;
    }

    :host(.vertical) {
      display: inline-block;
      height: 100%;
      width: 1px;
    }

    hr {
      margin: 16px 0;
      border: 0;
      border-top: 1px solid var(--border-color, #e0e0e0);
      background-color: transparent;
    }

    hr.vertical {
      writing-mode: vertical-lr;
      height: 100%;
      width: 1px;
      margin: 0 16px;
      border-top: 0;
      border-left: 1px solid var(--border-color, #e0e0e0);
    }

    hr.inset {
      margin-left: 72px;
    }

    hr.dense {
      margin: 8px 0;
    }
  `],
  host: {
    '[class.vertical]': 'orientation() === "vertical"'
  }
})
export class Divider {
  /**
   * Orientation of the divider
   * @default 'horizontal'
   */
  orientation = input<'horizontal' | 'vertical'>('horizontal');

  /**
   * Whether the divider is inset (indented)
   * @default false
   */
  inset = input<boolean>(false);

  /**
   * Whether to use dense spacing
   * @default false
   */
  dense = input<boolean>(false);

  /**
   * Computed CSS classes
   */
  protected classes = computed(() => {
    const orientation = this.orientation();
    const inset = this.inset();
    const dense = this.dense();

    return [
      orientation,
      inset ? 'inset' : '',
      dense ? 'dense' : ''
    ].filter(Boolean).join(' ');
  });
}
```

### CSS Variables (add to global styles)

```scss
// File: hrms-frontend/src/styles.scss

:root {
  --border-color: #e0e0e0;
  --border-color-dark: #424242;
}

[data-theme="dark"] {
  --border-color: var(--border-color-dark);
}
```

### Usage Examples

```html
<!-- Horizontal divider (default) -->
<app-divider />

<!-- Vertical divider -->
<div style="display: flex; align-items: center;">
  <span>Item 1</span>
  <app-divider [orientation]="'vertical'" />
  <span>Item 2</span>
</div>

<!-- Inset divider (indented) -->
<app-divider [inset]="true" />

<!-- Dense spacing -->
<app-divider [dense]="true" />
```

### Unit Tests

```typescript
// File: hrms-frontend/src/app/shared/ui/components/divider/divider.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Divider } from './divider';

describe('Divider', () => {
  let component: Divider;
  let fixture: ComponentFixture<Divider>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Divider]
    }).compileComponents();

    fixture = TestBed.createComponent(Divider);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render horizontal divider by default', () => {
    const hr = fixture.nativeElement.querySelector('hr');
    expect(hr).toBeTruthy();
    expect(hr.className).not.toContain('vertical');
  });

  it('should render vertical divider when orientation is vertical', () => {
    fixture.componentRef.setInput('orientation', 'vertical');
    fixture.detectChanges();

    const hr = fixture.nativeElement.querySelector('hr');
    expect(hr.className).toContain('vertical');
  });

  it('should add inset class when inset is true', () => {
    fixture.componentRef.setInput('inset', true);
    fixture.detectChanges();

    const hr = fixture.nativeElement.querySelector('hr');
    expect(hr.className).toContain('inset');
  });

  it('should have proper ARIA attributes', () => {
    const hr = fixture.nativeElement.querySelector('hr');
    expect(hr.getAttribute('role')).toBe('separator');
    expect(hr.getAttribute('aria-orientation')).toBe('horizontal');
  });
});
```

---

## Component 2: ExpansionPanel (HIGH PRIORITY)

### Why It's Needed
Used in 4 components for collapsible content sections:
- comprehensive-employee-form.component (multi-step form)
- biometric-device-form.component
- audit-logs.component (filter sections)
- tenant-audit-logs.component

### Implementation

```typescript
// File: hrms-frontend/src/app/shared/ui/components/expansion-panel/expansion-panel.ts

import { Component, input, model, output, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { trigger, state, style, transition, animate } from '@angular/animations';
import { IconComponent } from '../icon/icon';

/**
 * ExpansionPanel Component (Accordion)
 *
 * Collapsible panel for organizing content into expandable sections.
 * Replaces Angular Material's MatExpansionModule.
 *
 * @example
 * <app-expansion-panel [expanded]="true">
 *   <div panel-title>Section Title</div>
 *   <p>Content goes here</p>
 * </app-expansion-panel>
 */
@Component({
  selector: 'app-expansion-panel',
  standalone: true,
  imports: [CommonModule, IconComponent],
  template: `
    <div
      class="expansion-panel"
      [class.expanded]="expanded()"
      [class.disabled]="disabled()"
    >
      <div
        class="panel-header"
        [attr.role]="'button'"
        [attr.aria-expanded]="expanded()"
        [attr.tabindex]="disabled() ? -1 : 0"
        (click)="toggle()"
        (keydown.enter)="toggle()"
        (keydown.space)="$event.preventDefault(); toggle()"
      >
        <div class="panel-title">
          <ng-content select="[panel-title]" />
        </div>
        <app-icon
          [name]="expanded() ? 'expand_less' : 'expand_more'"
          [size]="24"
        />
      </div>

      @if (expanded()) {
        <div
          class="panel-content"
          [@expandCollapse]
          role="region"
        >
          <ng-content />
        </div>
      }
    </div>
  `,
  styles: [`
    .expansion-panel {
      border: 1px solid var(--border-color, #e0e0e0);
      border-radius: 4px;
      margin-bottom: 8px;
      overflow: hidden;
      transition: box-shadow 200ms ease;
    }

    .expansion-panel.expanded {
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .expansion-panel.disabled {
      opacity: 0.6;
      pointer-events: none;
    }

    .panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px;
      cursor: pointer;
      user-select: none;
      background-color: var(--surface-color, #fff);
      transition: background-color 200ms ease;
    }

    .panel-header:hover:not(.disabled .panel-header) {
      background-color: var(--hover-color, #f5f5f5);
    }

    .panel-header:focus {
      outline: 2px solid var(--primary-color, #1976d2);
      outline-offset: -2px;
    }

    .panel-title {
      flex: 1;
      font-size: 14px;
      font-weight: 500;
    }

    .panel-content {
      padding: 0 16px 16px 16px;
      background-color: var(--surface-color, #fff);
    }
  `],
  animations: [
    trigger('expandCollapse', [
      transition(':enter', [
        style({ height: 0, opacity: 0 }),
        animate('200ms ease-out', style({ height: '*', opacity: 1 }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ height: 0, opacity: 0 }))
      ])
    ])
  ]
})
export class ExpansionPanel {
  /**
   * Whether the panel is expanded
   * @default false
   */
  expanded = model<boolean>(false);

  /**
   * Whether the panel is disabled
   * @default false
   */
  disabled = input<boolean>(false);

  /**
   * Event emitted when panel is toggled
   */
  toggledChange = output<boolean>();

  /**
   * Toggle the panel expansion state
   */
  toggle(): void {
    if (this.disabled()) return;

    const newState = !this.expanded();
    this.expanded.set(newState);
    this.toggledChange.emit(newState);
  }
}

/**
 * ExpansionPanelGroup Component
 *
 * Container for multiple expansion panels with accordion behavior.
 * Only one panel can be open at a time.
 */
@Component({
  selector: 'app-expansion-panel-group',
  standalone: true,
  template: `
    <div class="expansion-panel-group" role="region">
      <ng-content />
    </div>
  `,
  styles: [`
    .expansion-panel-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
  `]
})
export class ExpansionPanelGroup {
  /**
   * Whether to allow multiple panels to be open
   * @default false (accordion mode)
   */
  multi = input<boolean>(false);
}
```

### Usage Examples

```html
<!-- Single expansion panel -->
<app-expansion-panel [expanded]="true">
  <div panel-title>Personal Information</div>
  <p>Form fields go here...</p>
</app-expansion-panel>

<!-- Accordion (only one open at a time) -->
<app-expansion-panel-group>
  <app-expansion-panel>
    <div panel-title>Section 1</div>
    <p>Content 1</p>
  </app-expansion-panel>

  <app-expansion-panel [expanded]="true">
    <div panel-title>Section 2</div>
    <p>Content 2</p>
  </app-expansion-panel>
</app-expansion-panel-group>

<!-- Programmatic control -->
<app-expansion-panel [(expanded)]="isPanelOpen">
  <div panel-title>Controlled Panel</div>
  <p>Content</p>
</app-expansion-panel>
```

---

## Component 3: List (MEDIUM PRIORITY)

### Why It's Needed
Used in 2 components for simple list rendering:
- tenant-detail.component
- billing-overview.component

### Implementation

```typescript
// File: hrms-frontend/src/app/shared/ui/components/list/list.ts

import { Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * List Component
 *
 * Simple list container for displaying items.
 * Replaces Angular Material's MatListModule.
 *
 * @example
 * <app-list>
 *   <app-list-item>Item 1</app-list-item>
 *   <app-list-item>Item 2</app-list-item>
 * </app-list>
 */
@Component({
  selector: 'app-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <ul
      [class]="classes()"
      role="list"
    >
      <ng-content />
    </ul>
  `,
  styles: [`
    ul {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    ul.dense {
      /* Tighter spacing */
    }

    ul.bordered {
      border: 1px solid var(--border-color, #e0e0e0);
      border-radius: 4px;
    }
  `]
})
export class List {
  /**
   * Whether to use dense spacing
   * @default false
   */
  dense = input<boolean>(false);

  /**
   * Whether to add border around list
   * @default false
   */
  bordered = input<boolean>(false);

  protected classes = computed(() => {
    const classes: string[] = ['list'];
    if (this.dense()) classes.push('dense');
    if (this.bordered()) classes.push('bordered');
    return classes.join(' ');
  });
}

/**
 * ListItem Component
 */
@Component({
  selector: 'app-list-item',
  standalone: true,
  imports: [CommonModule],
  template: `
    <li
      [class]="classes()"
      [attr.role]="clickable() ? 'button' : 'listitem'"
      [attr.tabindex]="clickable() ? 0 : undefined"
    >
      <ng-content />
    </li>
  `,
  styles: [`
    li {
      padding: 12px 16px;
      border-bottom: 1px solid var(--border-color, #e0e0e0);
      transition: background-color 150ms ease;
    }

    li:last-child {
      border-bottom: none;
    }

    li.clickable {
      cursor: pointer;
    }

    li.clickable:hover {
      background-color: var(--hover-color, #f5f5f5);
    }

    li.clickable:focus {
      outline: 2px solid var(--primary-color, #1976d2);
      outline-offset: -2px;
    }
  `]
})
export class ListItem {
  /**
   * Whether the list item is clickable
   * @default false
   */
  clickable = input<boolean>(false);

  protected classes = computed(() => {
    const classes: string[] = ['list-item'];
    if (this.clickable()) classes.push('clickable');
    return classes.join(' ');
  });
}
```

### Usage Examples

```html
<!-- Simple list -->
<app-list>
  <app-list-item>Item 1</app-list-item>
  <app-list-item>Item 2</app-list-item>
  <app-list-item>Item 3</app-list-item>
</app-list>

<!-- Dense list with border -->
<app-list [dense]="true" [bordered]="true">
  <app-list-item>Dense Item 1</app-list-item>
  <app-list-item>Dense Item 2</app-list-item>
</app-list>

<!-- Clickable list items -->
<app-list>
  <app-list-item [clickable]="true" (click)="onItemClick(1)">
    Clickable Item 1
  </app-list-item>
  <app-list-item [clickable]="true" (click)="onItemClick(2)">
    Clickable Item 2
  </app-list-item>
</app-list>
```

---

## Component 4: Table Sort Enhancement (MEDIUM PRIORITY)

### Why It's Needed
2 components use MatSortModule for table sorting

### Implementation

This is not a new component but an **enhancement to the existing Table component**.

```typescript
// File: hrms-frontend/src/app/shared/ui/components/table/table.ts
// Add these enhancements to the EXISTING TableComponent

import { Component, input, output, model, computed } from '@angular/core';

export interface SortEvent {
  column: string;
  direction: 'asc' | 'desc' | '';
}

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean; // NEW PROPERTY
  width?: string;
}

@Component({
  selector: 'app-table',
  // ... existing component code ...
  template: `
    <table>
      <thead>
        <tr>
          @for (column of columns(); track column.key) {
            <th
              [style.width]="column.width"
              [class.sortable]="column.sortable"
              (click)="column.sortable ? handleSort(column.key) : null"
            >
              <div class="header-content">
                {{ column.label }}
                @if (column.sortable && sortColumn() === column.key) {
                  <app-icon
                    [name]="sortDirection() === 'asc' ? 'arrow_upward' : 'arrow_downward'"
                    [size]="16"
                  />
                }
              </div>
            </th>
          }
        </tr>
      </thead>
      <!-- ... rest of table ... -->
    </table>
  `,
  styles: [`
    th.sortable {
      cursor: pointer;
      user-select: none;
    }

    th.sortable:hover {
      background-color: var(--hover-color, #f5f5f5);
    }

    .header-content {
      display: flex;
      align-items: center;
      gap: 4px;
    }
  `]
})
export class TableComponent {
  // Existing inputs...
  columns = input.required<TableColumn[]>();
  data = input.required<any[]>();

  // NEW: Sorting inputs/outputs
  sortColumn = model<string>('');
  sortDirection = model<'asc' | 'desc' | ''>('');
  sort = output<SortEvent>();

  /**
   * Handle column sort click
   */
  protected handleSort(columnKey: string): void {
    const currentColumn = this.sortColumn();
    const currentDirection = this.sortDirection();

    if (currentColumn === columnKey) {
      // Toggle direction: asc -> desc -> '' -> asc
      const newDirection =
        currentDirection === 'asc' ? 'desc' :
        currentDirection === 'desc' ? '' : 'asc';

      this.sortDirection.set(newDirection);
      if (newDirection === '') {
        this.sortColumn.set('');
      }
    } else {
      // New column, start with asc
      this.sortColumn.set(columnKey);
      this.sortDirection.set('asc');
    }

    this.sort.emit({
      column: this.sortColumn(),
      direction: this.sortDirection()
    });
  }
}
```

### Usage Examples

```typescript
// Component TypeScript
columns: TableColumn[] = [
  { key: 'name', label: 'Name', sortable: true },
  { key: 'email', label: 'Email', sortable: true },
  { key: 'role', label: 'Role', sortable: false },
  { key: 'status', label: 'Status', sortable: true }
];

onSort(event: SortEvent) {
  console.log('Sort by', event.column, event.direction);
  // Sort data locally or make API call
}
```

```html
<!-- Component Template -->
<app-table
  [columns]="columns"
  [data]="employees"
  (sort)="onSort($event)"
/>
```

---

## Integration Checklist

### 1. Create Component Files
- [ ] Create `hrms-frontend/src/app/shared/ui/components/divider/divider.ts`
- [ ] Create `hrms-frontend/src/app/shared/ui/components/divider/divider.spec.ts`
- [ ] Create `hrms-frontend/src/app/shared/ui/components/expansion-panel/expansion-panel.ts`
- [ ] Create `hrms-frontend/src/app/shared/ui/components/expansion-panel/expansion-panel.spec.ts`
- [ ] Create `hrms-frontend/src/app/shared/ui/components/list/list.ts`
- [ ] Create `hrms-frontend/src/app/shared/ui/components/list/list.spec.ts`
- [ ] Enhance `hrms-frontend/src/app/shared/ui/components/table/table.ts`
- [ ] Update `hrms-frontend/src/app/shared/ui/components/table/table.spec.ts`

### 2. Update UiModule
- [ ] Import new components in `ui.module.ts`
- [ ] Export new components from `ui.module.ts`
- [ ] Add TypeScript type exports

```typescript
// File: hrms-frontend/src/app/shared/ui/ui.module.ts

// Add imports
import { Divider } from './components/divider/divider';
import { ExpansionPanel, ExpansionPanelGroup } from './components/expansion-panel/expansion-panel';
import { List, ListItem } from './components/list/list';

@NgModule({
  imports: [
    // ... existing imports ...
    Divider,
    ExpansionPanel,
    ExpansionPanelGroup,
    List,
    ListItem
  ],
  exports: [
    // ... existing exports ...
    Divider,
    ExpansionPanel,
    ExpansionPanelGroup,
    List,
    ListItem
  ]
})
export class UiModule {}

// Add type exports at bottom
export { Divider } from './components/divider/divider';
export { ExpansionPanel, ExpansionPanelGroup } from './components/expansion-panel/expansion-panel';
export { List, ListItem } from './components/list/list';
export type { SortEvent } from './components/table/table'; // Updated
```

### 3. Add CSS Variables
- [ ] Add CSS variables to `hrms-frontend/src/styles.scss`

### 4. Run Tests
- [ ] Run unit tests: `npm run test`
- [ ] Verify >80% coverage for new components
- [ ] Fix any failing tests

### 5. Build & Verify
- [ ] Run TypeScript compilation: `npx tsc --noEmit`
- [ ] Build project: `npm run build`
- [ ] Verify no build errors

### 6. Documentation
- [ ] Add JSDoc comments to all public APIs
- [ ] Create usage examples in comments
- [ ] Update component README (optional)

---

## Acceptance Criteria

### ✅ Divider Component
- [ ] Supports horizontal and vertical orientation
- [ ] Supports inset mode
- [ ] Proper ARIA attributes
- [ ] Unit tests pass with >80% coverage
- [ ] TypeScript strict mode compliant

### ✅ ExpansionPanel Component
- [ ] Expand/collapse animation works
- [ ] Keyboard navigation (Enter/Space to toggle)
- [ ] Disabled state works
- [ ] Two-way binding with `[(expanded)]`
- [ ] ExpansionPanelGroup supports accordion mode
- [ ] Unit tests pass with >80% coverage

### ✅ List Component
- [ ] Dense and bordered modes work
- [ ] ListItem clickable mode works
- [ ] Proper ARIA roles
- [ ] Unit tests pass with >80% coverage

### ✅ Table Sort Enhancement
- [ ] Sort icons appear on sortable columns
- [ ] Three-state sort (asc → desc → none)
- [ ] Sort event emitted with correct data
- [ ] Existing table tests still pass
- [ ] New sort tests added

---

## Timeline

**Day 1 (4 hours):**
- Build Divider component (2h)
- Build List component (2h)

**Day 2 (7 hours):**
- Build ExpansionPanel component (4h)
- Enhance Table component with sort (2h)
- Integration testing (1h)

**Total:** 11 hours (1.5 days)

---

## Next Steps After Completion

1. ✅ Merge Phase 0 components to main branch
2. 🚀 Kickoff Phase 1: Quick Wins (Week 1-2)
3. 📋 Update migration tracker with Phase 0 completion
4. 🎯 Begin migrating employee-list.component (finalize dual-run)

---

**Document Version:** 1.0.0
**Created:** November 17, 2025
**Owner:** Frontend Team Lead
**Status:** 🚫 AWAITING IMPLEMENTATION
