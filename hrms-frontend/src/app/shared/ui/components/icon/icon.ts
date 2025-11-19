// ═══════════════════════════════════════════════════════════
// ICON COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Production-ready icon component with multi-library support
// ═══════════════════════════════════════════════════════════

import { Component, Input, OnInit, ChangeDetectionStrategy, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { IconRegistryService, IconLibrary } from '../../services/icon-registry.service';

export type IconSize = 'small' | 'medium' | 'large';

@Component({
  selector: 'app-icon',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './icon.html',
  styleUrls: ['./icon.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  host: {
    '[class]': 'hostClasses',
    '[style.color]': 'color',
    '[attr.role]': '"img"',
    '[attr.aria-label]': 'ariaLabel || name'
  }
})
export class IconComponent implements OnInit {
  /**
   * Icon name (e.g., 'home', 'menu', 'search')
   */
  @Input() name = '';

  /**
   * Icon size: small (16px), medium (24px), large (32px)
   */
  @Input() size: IconSize = 'medium';

  /**
   * Custom color (CSS color or design token)
   */
  @Input() color?: string;

  /**
   * Icon library to use
   */
  @Input() library: IconLibrary = 'material';

  /**
   * Accessibility label
   */
  @Input() ariaLabel?: string;

  /**
   * SVG content to render
   */
  svgContent: SafeHtml = '';

  /**
   * Indicates if the icon was found in the registry
   */
  iconFound = true;

  /**
   * Fallback icon for missing icons
   */
  private readonly fallbackIcon = '<path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>';

  constructor(
    private iconRegistry: IconRegistryService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.loadIcon();
  }

  /**
   * Load icon from registry
   */
  private loadIcon(): void {
    if (!this.name) {
      this.renderFallback();
      return;
    }

    const iconPath = this.iconRegistry.getIcon(this.name, this.library);

    if (iconPath) {
      this.iconFound = true;
      this.renderIcon(iconPath);
    } else {
      this.iconFound = false;
      this.renderFallback();
      console.warn(`Icon "${this.name}" not found in "${this.library}" library. Using fallback icon.`);
    }
  }

  /**
   * Render icon with the given SVG path
   *
   * SECURITY: Using bypassSecurityTrustHtml is safe here because:
   * 1. SVG structure is controlled by createSvg() method
   * 2. Icon paths come from hardcoded IconRegistryService
   * 3. No user input is rendered
   * 4. All paths are validated in IconRegistryService.registerIcon()
   */
  private renderIcon(path: string): void {
    const svg = this.createSvg(path);
    this.svgContent = this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  /**
   * Render fallback icon for missing icons
   *
   * SECURITY: Fallback icon is a hardcoded constant - safe to bypass sanitization
   */
  private renderFallback(): void {
    const svg = this.createSvg(this.fallbackIcon);
    this.svgContent = this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  /**
   * Create SVG string
   */
  private createSvg(path: string): string {
    // Determine if it's a stroke-based icon (heroicons) or fill-based (material/lucide)
    const isStroke = this.library === 'heroicons' || path.includes('stroke-linecap');

    const strokeAttrs = isStroke
      ? 'fill="none" stroke="currentColor" stroke-width="1.5"'
      : 'fill="currentColor"';

    return `
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        ${strokeAttrs}
        aria-hidden="true"
      >
        ${path}
      </svg>
    `;
  }

  /**
   * Get host element classes
   */
  get hostClasses(): string {
    const classes = ['app-icon', `app-icon--${this.size}`];

    if (!this.iconFound) {
      classes.push('app-icon--fallback');
    }

    return classes.join(' ');
  }
}
