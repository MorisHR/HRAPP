// ═══════════════════════════════════════════════════════════
// PREMIUM TABLE COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Production-ready table component to replace mat-table
// ═══════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, TemplateRef, ContentChildren, QueryList, Directive, AfterContentInit } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Directive to define custom column templates
 * Usage: <ng-template appTableColumn="columnKey" let-row let-value="value">...</ng-template>
 */
@Directive({
  selector: '[appTableColumn]',
  standalone: true
})
export class TableColumnDirective {
  @Input('appTableColumn') columnKey!: string;

  constructor(public template: TemplateRef<any>) {}
}

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  /** Optional custom cell template reference */
  cellTemplate?: TemplateRef<any>;
  /** Optional custom formatter function */
  formatter?: (value: any, row: any) => string;
}

export interface SortEvent {
  key: string;
  direction: 'asc' | 'desc';
}

@Component({
  selector: 'app-table',
  imports: [CommonModule],
  templateUrl: './table.html',
  styleUrl: './table.scss',
})
export class TableComponent implements OnChanges, AfterContentInit {
  @ContentChildren(TableColumnDirective) columnTemplates!: QueryList<TableColumnDirective>;
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

  selectedRows: Set<any> = new Set();
  allSelected: boolean = false;
  indeterminate: boolean = false;

  /** Map of column keys to their templates */
  private columnTemplateMap = new Map<string, TemplateRef<any>>();

  ngAfterContentInit(): void {
    // Build template map from content children
    this.columnTemplates.forEach(directive => {
      this.columnTemplateMap.set(directive.columnKey, directive.template);
    });

    // Also add templates from column definitions
    this.columns.forEach(column => {
      if (column.cellTemplate) {
        this.columnTemplateMap.set(column.key, column.cellTemplate);
      }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data']) {
      // Reset selection when data changes
      this.updateSelectionState();
    }

    if (changes['columns']) {
      // Rebuild template map if columns change
      this.columns.forEach(column => {
        if (column.cellTemplate) {
          this.columnTemplateMap.set(column.key, column.cellTemplate);
        }
      });
    }
  }

  onSort(column: TableColumn): void {
    if (!column.sortable) {
      return;
    }

    let newDirection: 'asc' | 'desc';

    if (this.sortKey === column.key) {
      // Toggle direction
      newDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      // Default to ascending for new column
      newDirection = 'asc';
    }

    this.sortKey = column.key;
    this.sortDirection = newDirection;

    this.sortChange.emit({
      key: column.key,
      direction: newDirection
    });
  }

  onRowClick(row: any): void {
    if (!this.loading) {
      this.rowClick.emit(row);
    }
  }

  onRowSelectionChange(row: any, event: Event): void {
    event.stopPropagation();

    if (this.multiSelect) {
      if (this.selectedRows.has(row)) {
        this.selectedRows.delete(row);
      } else {
        this.selectedRows.add(row);
      }
    } else {
      // Single select - clear all and add only this row
      this.selectedRows.clear();
      this.selectedRows.add(row);
    }

    this.updateSelectionState();
    this.emitSelection();
  }

  onSelectAll(event: Event): void {
    const target = event.target as HTMLInputElement;

    if (target.checked) {
      // Select all
      this.selectedRows = new Set(this.data);
    } else {
      // Deselect all
      this.selectedRows.clear();
    }

    this.updateSelectionState();
    this.emitSelection();
  }

  isRowSelected(row: any): boolean {
    return this.selectedRows.has(row);
  }

  private updateSelectionState(): void {
    const selectedCount = this.selectedRows.size;
    const totalCount = this.data.length;

    this.allSelected = selectedCount > 0 && selectedCount === totalCount;
    this.indeterminate = selectedCount > 0 && selectedCount < totalCount;
  }

  private emitSelection(): void {
    this.selectionChange.emit(Array.from(this.selectedRows));
  }

  getSortIconClass(column: TableColumn): string {
    if (!column.sortable) {
      return '';
    }

    if (this.sortKey === column.key) {
      return this.sortDirection === 'asc' ? 'sort-icon--asc' : 'sort-icon--desc';
    }

    return 'sort-icon--neutral';
  }

  getRowClasses(row: any, index: number): string[] {
    return [
      'table__row',
      this.hoverable ? 'table__row--hoverable' : '',
      this.striped && index % 2 === 1 ? 'table__row--striped' : '',
      this.isRowSelected(row) ? 'table__row--selected' : '',
    ].filter(Boolean);
  }

  getCellValue(row: any, column: TableColumn): any {
    const value = row[column.key];

    // Use custom formatter if provided
    if (column.formatter) {
      return column.formatter(value, row);
    }

    return value;
  }

  /**
   * Gets the custom template for a column if available
   */
  getColumnTemplate(column: TableColumn): TemplateRef<any> | null {
    return this.columnTemplateMap.get(column.key) || null;
  }

  /**
   * Checks if a column has a custom template
   */
  hasCustomTemplate(column: TableColumn): boolean {
    return this.columnTemplateMap.has(column.key);
  }
}
