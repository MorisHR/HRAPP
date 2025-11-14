// ═══════════════════════════════════════════════════════════
// PREMIUM TABLE COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Production-ready table component to replace mat-table
// ═══════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

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
  imports: [CommonModule],
  templateUrl: './table.html',
  styleUrl: './table.scss',
})
export class TableComponent implements OnChanges {
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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data']) {
      // Reset selection when data changes
      this.updateSelectionState();
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
    return row[column.key];
  }
}
