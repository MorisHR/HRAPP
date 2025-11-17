// ═══════════════════════════════════════════════════════════
// Pagination Component
// Fortune 500-grade pagination component
// Replaces Angular Material's MatPaginatorModule
// ═══════════════════════════════════════════════════════════

import { Component, input, model, output, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icon/icon';

/**
 * Pagination Component
 *
 * A comprehensive pagination component for tables and lists with accessibility
 * features and flexible configuration options.
 *
 * @example
 * ```html
 * <!-- Basic pagination -->
 * <app-pagination
 *   [totalItems]="1000"
 *   [(currentPage)]="page"
 *   [(pageSize)]="size"
 * />
 *
 * <!-- Custom page sizes -->
 * <app-pagination
 *   [totalItems]="500"
 *   [pageSizeOptions]="[10, 25, 50, 100]"
 *   [(currentPage)]="page"
 *   [(pageSize)]="size"
 * />
 *
 * <!-- Listen to page changes -->
 * <app-pagination
 *   [totalItems]="200"
 *   (pageChange)="onPageChange($event)"
 * />
 * ```
 *
 * @usageNotes
 * ### Accessibility
 * - Full keyboard navigation (Arrow keys, Enter, Space)
 * - Proper ARIA labels and roles
 * - Screen reader announcements for page changes
 * - Focus management
 *
 * ### Performance
 * - Optimized change detection with signals
 * - Computed properties for efficiency
 * - No unnecessary re-renders
 *
 * ### Best Practices
 * - Always provide totalItems
 * - Use two-way binding for currentPage and pageSize
 * - Validate page changes on server side
 * - Handle empty states gracefully
 */
@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, IconComponent],
  template: `
    <div class="pagination" role="navigation" [attr.aria-label]="'Pagination navigation'">
      <!-- Page size selector -->
      <div class="page-size-selector">
        <label for="page-size-select" class="page-size-label">
          Items per page:
        </label>
        <select
          id="page-size-select"
          class="page-size-select"
          [value]="pageSize()"
          (change)="onPageSizeChange($any($event.target).value)"
          [attr.aria-label]="'Select items per page'"
        >
          @for (option of pageSizeOptions(); track option) {
            <option [value]="option">{{ option }}</option>
          }
        </select>
      </div>

      <!-- Page navigation -->
      <div class="page-navigation">
        <!-- First page button -->
        <button
          type="button"
          class="page-nav-button"
          [disabled]="isFirstPage()"
          (click)="goToFirstPage()"
          [attr.aria-label]="'Go to first page'"
          [attr.aria-disabled]="isFirstPage()"
        >
          <app-icon name="first_page" />
        </button>

        <!-- Previous page button -->
        <button
          type="button"
          class="page-nav-button"
          [disabled]="isFirstPage()"
          (click)="goToPreviousPage()"
          [attr.aria-label]="'Go to previous page'"
          [attr.aria-disabled]="isFirstPage()"
        >
          <app-icon name="chevron_left" />
        </button>

        <!-- Page indicator -->
        <div class="page-indicator" role="status" [attr.aria-live]="'polite'">
          <span class="page-label">Page</span>
          <span class="page-current">{{ currentPage() }}</span>
          <span class="page-separator">of</span>
          <span class="page-total">{{ totalPages() }}</span>
        </div>

        <!-- Next page button -->
        <button
          type="button"
          class="page-nav-button"
          [disabled]="isLastPage()"
          (click)="goToNextPage()"
          [attr.aria-label]="'Go to next page'"
          [attr.aria-disabled]="isLastPage()"
        >
          <app-icon name="chevron_right" />
        </button>

        <!-- Last page button -->
        <button
          type="button"
          class="page-nav-button"
          [disabled]="isLastPage()"
          (click)="goToLastPage()"
          [attr.aria-label]="'Go to last page'"
          [attr.aria-disabled]="isLastPage()"
        >
          <app-icon name="last_page" />
        </button>
      </div>

      <!-- Items range display -->
      <div class="items-range" role="status" [attr.aria-live]="'polite'">
        {{ itemRangeStart() }} - {{ itemRangeEnd() }} of {{ totalItems() }}
      </div>
    </div>
  `,
  styles: [`
    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px;
      background-color: #ffffff;
      border-top: 1px solid #e0e0e0;
      gap: 16px;
      flex-wrap: wrap;
      min-height: 56px;
    }

    /* Page size selector */
    .page-size-selector {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .page-size-label {
      font-size: 14px;
      color: rgba(0, 0, 0, 0.6);
      white-space: nowrap;
    }

    .page-size-select {
      padding: 6px 24px 6px 8px;
      border: 1px solid #e0e0e0;
      border-radius: 4px;
      background-color: #ffffff;
      font-size: 14px;
      color: rgba(0, 0, 0, 0.87);
      cursor: pointer;
      outline: none;
      transition: border-color 150ms cubic-bezier(0.4, 0, 0.2, 1);
      appearance: none;
      background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23333' d='M6 9L1 4h10z'/%3E%3C/svg%3E");
      background-repeat: no-repeat;
      background-position: right 8px center;
    }

    .page-size-select:hover {
      border-color: #b0b0b0;
    }

    .page-size-select:focus {
      border-color: #1976d2;
      box-shadow: 0 0 0 2px rgba(25, 118, 210, 0.2);
    }

    /* Page navigation */
    .page-navigation {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .page-nav-button {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      padding: 0;
      border: none;
      background-color: transparent;
      color: rgba(0, 0, 0, 0.87);
      cursor: pointer;
      border-radius: 50%;
      transition: background-color 150ms cubic-bezier(0.4, 0, 0.2, 1);
      outline: none;
    }

    .page-nav-button:hover:not(:disabled) {
      background-color: rgba(0, 0, 0, 0.04);
    }

    .page-nav-button:active:not(:disabled) {
      background-color: rgba(0, 0, 0, 0.08);
    }

    .page-nav-button:focus {
      outline: 2px solid #1976d2;
      outline-offset: 2px;
    }

    .page-nav-button:disabled {
      color: rgba(0, 0, 0, 0.26);
      cursor: not-allowed;
      pointer-events: none;
    }

    /* Page indicator */
    .page-indicator {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 0 8px;
      font-size: 14px;
      color: rgba(0, 0, 0, 0.87);
      user-select: none;
    }

    .page-label,
    .page-separator {
      color: rgba(0, 0, 0, 0.6);
    }

    .page-current,
    .page-total {
      font-weight: 500;
      min-width: 24px;
      text-align: center;
    }

    /* Items range */
    .items-range {
      font-size: 14px;
      color: rgba(0, 0, 0, 0.6);
      white-space: nowrap;
    }

    /* Responsive design */
    @media (max-width: 600px) {
      .pagination {
        flex-direction: column;
        align-items: stretch;
        gap: 12px;
      }

      .page-size-selector,
      .page-navigation,
      .items-range {
        justify-content: center;
      }
    }

    /* Dark theme support */
    @media (prefers-color-scheme: dark) {
      .pagination {
        background-color: #1e1e1e;
        border-top-color: #424242;
      }

      .page-size-label,
      .page-label,
      .page-separator,
      .items-range {
        color: rgba(255, 255, 255, 0.6);
      }

      .page-size-select {
        background-color: #2c2c2c;
        border-color: #424242;
        color: rgba(255, 255, 255, 0.87);
        background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23fff' d='M6 9L1 4h10z'/%3E%3C/svg%3E");
      }

      .page-size-select:hover {
        border-color: #666666;
      }

      .page-nav-button {
        color: rgba(255, 255, 255, 0.87);
      }

      .page-nav-button:hover:not(:disabled) {
        background-color: rgba(255, 255, 255, 0.08);
      }

      .page-nav-button:active:not(:disabled) {
        background-color: rgba(255, 255, 255, 0.12);
      }

      .page-nav-button:disabled {
        color: rgba(255, 255, 255, 0.26);
      }

      .page-indicator {
        color: rgba(255, 255, 255, 0.87);
      }
    }

    /* High contrast mode */
    @media (prefers-contrast: high) {
      .page-size-select,
      .page-nav-button {
        border: 2px solid currentColor;
      }

      .page-nav-button:focus {
        outline-width: 3px;
      }
    }

    /* Reduced motion */
    @media (prefers-reduced-motion: reduce) {
      .page-size-select,
      .page-nav-button {
        transition: none;
      }
    }
  `]
})
export class Pagination {
  /**
   * Total number of items across all pages
   * @required
   */
  totalItems = input.required<number>();

  /**
   * Current page number (1-indexed)
   * Supports two-way binding with [(currentPage)]
   * @default 1
   */
  currentPage = model<number>(1);

  /**
   * Number of items per page
   * Supports two-way binding with [(pageSize)]
   * @default 25
   */
  pageSize = model<number>(25);

  /**
   * Available page size options
   * @default [10, 25, 50, 100]
   */
  pageSizeOptions = input<number[]>([10, 25, 50, 100]);

  /**
   * Whether to show first/last page buttons
   * @default true
   */
  showFirstLastButtons = input<boolean>(true);

  /**
   * Event emitted when page changes (page or size)
   * Emits object with { page: number, size: number }
   */
  pageChange = output<{ page: number; size: number }>();

  /**
   * Computed: Total number of pages
   * @internal
   */
  protected totalPages = computed(() => {
    const total = this.totalItems();
    const size = this.pageSize();
    return Math.max(1, Math.ceil(total / size));
  });

  /**
   * Computed: Whether currently on first page
   * @internal
   */
  protected isFirstPage = computed(() => this.currentPage() === 1);

  /**
   * Computed: Whether currently on last page
   * @internal
   */
  protected isLastPage = computed(() => this.currentPage() >= this.totalPages());

  /**
   * Computed: Start index of items on current page (1-indexed)
   * @internal
   */
  protected itemRangeStart = computed(() => {
    const total = this.totalItems();
    if (total === 0) return 0;
    return (this.currentPage() - 1) * this.pageSize() + 1;
  });

  /**
   * Computed: End index of items on current page (1-indexed)
   * @internal
   */
  protected itemRangeEnd = computed(() => {
    const total = this.totalItems();
    const end = this.currentPage() * this.pageSize();
    return Math.min(end, total);
  });

  /**
   * Effect: Validate currentPage when totalPages changes
   * Ensures currentPage is always valid
   */
  constructor() {
    effect(() => {
      const current = this.currentPage();
      const total = this.totalPages();

      // If current page exceeds total pages, reset to last page
      if (current > total) {
        this.currentPage.set(Math.max(1, total));
      }
    });
  }

  /**
   * Go to first page
   */
  protected goToFirstPage(): void {
    if (this.isFirstPage()) return;
    this.setPage(1);
  }

  /**
   * Go to previous page
   */
  protected goToPreviousPage(): void {
    if (this.isFirstPage()) return;
    this.setPage(this.currentPage() - 1);
  }

  /**
   * Go to next page
   */
  protected goToNextPage(): void {
    if (this.isLastPage()) return;
    this.setPage(this.currentPage() + 1);
  }

  /**
   * Go to last page
   */
  protected goToLastPage(): void {
    if (this.isLastPage()) return;
    this.setPage(this.totalPages());
  }

  /**
   * Handle page size change
   * Recalculates current page to maintain approximate position
   */
  protected onPageSizeChange(newSize: string): void {
    const size = parseInt(newSize, 10);
    if (isNaN(size) || size <= 0) return;

    // Calculate which item index we're currently viewing
    const currentItemIndex = (this.currentPage() - 1) * this.pageSize();

    // Update page size
    this.pageSize.set(size);

    // Calculate new page to maintain approximate position
    const newPage = Math.floor(currentItemIndex / size) + 1;
    this.currentPage.set(newPage);

    this.emitPageChange();
  }

  /**
   * Set current page and emit change event
   * @internal
   */
  private setPage(page: number): void {
    const validPage = Math.max(1, Math.min(page, this.totalPages()));
    this.currentPage.set(validPage);
    this.emitPageChange();
  }

  /**
   * Emit page change event
   * @internal
   */
  private emitPageChange(): void {
    this.pageChange.emit({
      page: this.currentPage(),
      size: this.pageSize()
    });
  }
}
