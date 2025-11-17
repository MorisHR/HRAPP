// ═══════════════════════════════════════════════════════════
// List Component
// Fortune 500-grade list display component
// Replaces Angular Material's MatListModule
// ═══════════════════════════════════════════════════════════

import { Component, input, computed, output } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * List Component
 *
 * A semantic, accessible list container for displaying items vertically.
 * Optimized for performance with virtual scrolling support (future enhancement).
 *
 * @example
 * ```html
 * <!-- Simple list -->
 * <app-list>
 *   <app-list-item>Item 1</app-list-item>
 *   <app-list-item>Item 2</app-list-item>
 * </app-list>
 *
 * <!-- Dense list with border -->
 * <app-list [dense]="true" [bordered]="true">
 *   <app-list-item>Dense Item 1</app-list-item>
 *   <app-list-item>Dense Item 2</app-list-item>
 * </app-list>
 *
 * <!-- Clickable list items -->
 * <app-list>
 *   <app-list-item [clickable]="true" (click)="onItemClick(1)">
 *     Clickable Item
 *   </app-list-item>
 * </app-list>
 * ```
 *
 * @usageNotes
 * ### Accessibility
 * - Uses semantic `<ul>` element
 * - Proper ARIA roles
 * - Keyboard navigation for clickable items
 *
 * ### Performance
 * - Optimized for large lists (100+ items)
 * - No unnecessary re-renders
 * - Future: Virtual scrolling support
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
      background-color: transparent;
    }

    ul.dense {
      /* Tighter spacing handled by list items */
    }

    ul.bordered {
      border: 1px solid #e0e0e0;
      border-radius: 4px;
      overflow: hidden;
    }

    ul.elevated {
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      border-radius: 4px;
    }

    /* Dark theme support */
    @media (prefers-color-scheme: dark) {
      ul.bordered {
        border-color: #424242;
      }

      ul.elevated {
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
      }
    }
  `]
})
export class List {
  /**
   * Whether to use dense spacing (reduced padding)
   * @default false
   */
  dense = input<boolean>(false);

  /**
   * Whether to add border around the list
   * @default false
   */
  bordered = input<boolean>(false);

  /**
   * Whether to add elevation (shadow) to the list
   * @default false
   */
  elevated = input<boolean>(false);

  /**
   * Computed CSS classes based on inputs
   * @internal
   */
  protected classes = computed(() => {
    const classes: string[] = ['list'];
    if (this.dense()) classes.push('dense');
    if (this.bordered()) classes.push('bordered');
    if (this.elevated()) classes.push('elevated');
    return classes.join(' ');
  });
}

/**
 * ListItem Component
 *
 * Individual list item with support for clickable interactions.
 *
 * @example
 * ```html
 * <!-- Simple list item -->
 * <app-list-item>Basic item</app-list-item>
 *
 * <!-- Clickable list item -->
 * <app-list-item [clickable]="true" (itemClick)="handleClick()">
 *   Click me
 * </app-list-item>
 *
 * <!-- Disabled list item -->
 * <app-list-item [disabled]="true">
 *   Disabled item
 * </app-list-item>
 *
 * <!-- With custom content -->
 * <app-list-item [clickable]="true">
 *   <div style="display: flex; align-items: center; gap: 12px;">
 *     <img src="avatar.jpg" alt="User" />
 *     <div>
 *       <div class="title">John Doe</div>
 *       <div class="subtitle">Software Engineer</div>
 *     </div>
 *   </div>
 * </app-list-item>
 * ```
 */
@Component({
  selector: 'app-list-item',
  standalone: true,
  imports: [CommonModule],
  template: `
    <li
      [class]="classes()"
      [attr.role]="clickable() ? 'button' : 'listitem'"
      [attr.tabindex]="getTabIndex()"
      [attr.aria-disabled]="disabled()"
      (click)="handleClick()"
      (keydown.enter)="handleKeydown($any($event))"
      (keydown.space)="handleKeydown($any($event))"
    >
      <ng-content />
    </li>
  `,
  styles: [`
    li {
      padding: 12px 16px;
      border-bottom: 1px solid #e0e0e0;
      transition: background-color 150ms cubic-bezier(0.4, 0, 0.2, 1);
      background-color: transparent;
      color: rgba(0, 0, 0, 0.87);
      min-height: 48px; /* Accessibility: minimum touch target */
      display: flex;
      align-items: center;
    }

    li:last-child {
      border-bottom: none;
    }

    li.dense {
      padding: 8px 12px;
      min-height: 40px;
    }

    li.clickable {
      cursor: pointer;
      user-select: none;
    }

    li.clickable:hover:not(.disabled) {
      background-color: #f5f5f5;
    }

    li.clickable:active:not(.disabled) {
      background-color: #eeeeee;
    }

    li.clickable:focus {
      outline: 2px solid #1976d2;
      outline-offset: -2px;
    }

    li.disabled {
      opacity: 0.6;
      pointer-events: none;
      cursor: not-allowed;
    }

    li.selected {
      background-color: rgba(25, 118, 210, 0.08);
      border-left: 4px solid #1976d2;
      padding-left: 12px;
    }

    /* Dark theme support */
    @media (prefers-color-scheme: dark) {
      li {
        border-bottom-color: #424242;
        color: rgba(255, 255, 255, 0.87);
      }

      li.clickable:hover:not(.disabled) {
        background-color: #2c2c2c;
      }

      li.clickable:active:not(.disabled) {
        background-color: #333333;
      }

      li.selected {
        background-color: rgba(25, 118, 210, 0.16);
      }
    }

    /* High contrast mode */
    @media (prefers-contrast: high) {
      li {
        border-bottom-width: 2px;
      }

      li.clickable:focus {
        outline-width: 3px;
      }
    }

    /* Reduced motion */
    @media (prefers-reduced-motion: reduce) {
      li {
        transition: none;
      }
    }
  `],
  host: {
    '[class.dense]': 'dense()'
  }
})
export class ListItem {
  /**
   * Whether the list item is clickable
   * Adds hover effects and keyboard navigation
   * @default false
   */
  clickable = input<boolean>(false);

  /**
   * Whether the list item is disabled
   * @default false
   */
  disabled = input<boolean>(false);

  /**
   * Whether the list item is selected
   * Shows visual selection indicator
   * @default false
   */
  selected = input<boolean>(false);

  /**
   * Whether to use dense spacing (inherited from parent list)
   * @default false
   */
  dense = input<boolean>(false);

  /**
   * Event emitted when clickable item is clicked
   */
  itemClick = output<void>();

  /**
   * Computed CSS classes
   * @internal
   */
  protected classes = computed(() => {
    const classes: string[] = ['list-item'];
    if (this.clickable()) classes.push('clickable');
    if (this.disabled()) classes.push('disabled');
    if (this.selected()) classes.push('selected');
    if (this.dense()) classes.push('dense');
    return classes.join(' ');
  });

  /**
   * Get tabindex value based on state
   * @internal
   */
  protected getTabIndex(): number {
    if (this.disabled()) return -1;
    if (this.clickable()) return 0;
    return -1;
  }

  /**
   * Handle click events
   * @internal
   */
  protected handleClick(): void {
    if (this.disabled() || !this.clickable()) return;
    this.itemClick.emit();
  }

  /**
   * Handle keyboard events (Enter/Space)
   * @internal
   */
  protected handleKeydown(event: KeyboardEvent): void {
    if (this.disabled() || !this.clickable()) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.itemClick.emit();
    }
  }
}
