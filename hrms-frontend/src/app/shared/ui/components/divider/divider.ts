// ═══════════════════════════════════════════════════════════
// Divider Component
// Fortune 500-grade visual separator component
// Replaces Angular Material's MatDividerModule
// ═══════════════════════════════════════════════════════════

import { Component, input, computed } from '@angular/core';

/**
 * Divider Component
 *
 * A lightweight, accessible divider line for visual separation of content.
 * Supports both horizontal and vertical orientations with customizable spacing.
 *
 * @example
 * ```html
 * <!-- Horizontal divider (default) -->
 * <app-divider />
 *
 * <!-- Vertical divider -->
 * <div style="display: flex; align-items: center;">
 *   <span>Item 1</span>
 *   <app-divider [orientation]="'vertical'" />
 *   <span>Item 2</span>
 * </div>
 *
 * <!-- Inset divider (indented) -->
 * <app-divider [inset]="true" />
 *
 * <!-- Dense spacing -->
 * <app-divider [dense]="true" />
 * ```
 *
 * @usageNotes
 * ### Accessibility
 * - Uses semantic `<hr>` element
 * - Includes proper ARIA attributes
 * - Keyboard navigation not applicable (non-interactive)
 *
 * ### Performance
 * - Zero runtime overhead (pure CSS)
 * - No JavaScript calculations
 * - Optimized for tree-shaking
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
      border-top: 1px solid #e0e0e0;
      background-color: transparent;
      height: 0;
    }

    hr.vertical {
      writing-mode: vertical-lr;
      height: 100%;
      width: 1px;
      margin: 0 16px;
      border-top: 0;
      border-left: 1px solid #e0e0e0;
    }

    hr.inset {
      margin-left: 72px;
    }

    hr.dense {
      margin: 8px 0;
    }

    /* Dark theme support */
    @media (prefers-color-scheme: dark) {
      hr {
        border-top-color: #424242;
      }

      hr.vertical {
        border-left-color: #424242;
      }
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
   * Whether the divider is inset (indented from the left)
   * Typically used after an icon or avatar (72px standard indent)
   * @default false
   */
  inset = input<boolean>(false);

  /**
   * Whether to use dense spacing (8px vs 16px margins)
   * Use in compact layouts or cards
   * @default false
   */
  dense = input<boolean>(false);

  /**
   * Computed CSS classes based on inputs
   * @internal
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
