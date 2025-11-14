// ═══════════════════════════════════════════════════════════
// PREMIUM TOOLTIP DIRECTIVE
// Production-ready replacement for mat-tooltip
// Enterprise-grade implementation with accessibility
// ═══════════════════════════════════════════════════════════

import {
  Directive,
  ElementRef,
  HostListener,
  Input,
  OnDestroy,
  Renderer2,
  NgZone
} from '@angular/core';

export type TooltipPosition = 'top' | 'bottom' | 'left' | 'right';

interface TooltipDimensions {
  width: number;
  height: number;
}

interface Position {
  top: number;
  left: number;
}

@Directive({
  selector: '[appTooltip]',
  standalone: true
})
export class TooltipDirective implements OnDestroy {
  @Input() appTooltip = '';
  @Input() tooltipPosition: TooltipPosition = 'top';
  @Input() tooltipDelay = 500;

  private tooltipElement: HTMLElement | null = null;
  private showTimeout: any;
  private hideTimeout: any;
  private isVisible = false;
  private isTouchDevice = false;
  private touchTimeout: any;

  constructor(
    private elementRef: ElementRef,
    private renderer: Renderer2,
    private ngZone: NgZone
  ) {
    // Detect touch device
    this.isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
  }

  @HostListener('mouseenter')
  onMouseEnter(): void {
    if (this.isTouchDevice) return;
    this.scheduleShow();
  }

  @HostListener('mouseleave')
  onMouseLeave(): void {
    if (this.isTouchDevice) return;
    this.scheduleHide();
  }

  @HostListener('focus')
  onFocus(): void {
    if (this.isTouchDevice) return;
    this.scheduleShow();
  }

  @HostListener('blur')
  onBlur(): void {
    if (this.isTouchDevice) return;
    this.scheduleHide();
  }

  @HostListener('touchstart')
  onTouchStart(): void {
    // On touch devices, show tooltip on tap and auto-hide after 2s
    if (!this.isTouchDevice) return;

    this.cancelScheduled();

    if (this.isVisible) {
      this.hide();
    } else {
      this.show();
      this.touchTimeout = setTimeout(() => {
        this.hide();
      }, 2000);
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (this.isVisible) {
      this.updatePosition();
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (this.isVisible) {
      this.updatePosition();
    }
  }

  ngOnDestroy(): void {
    this.cancelScheduled();
    this.destroyTooltip();
  }

  private scheduleShow(): void {
    if (!this.appTooltip || this.isVisible) return;

    this.cancelScheduled();
    this.showTimeout = setTimeout(() => {
      this.show();
    }, this.tooltipDelay);
  }

  private scheduleHide(): void {
    this.cancelScheduled();
    this.hideTimeout = setTimeout(() => {
      this.hide();
    }, 100);
  }

  private cancelScheduled(): void {
    if (this.showTimeout) {
      clearTimeout(this.showTimeout);
      this.showTimeout = null;
    }
    if (this.hideTimeout) {
      clearTimeout(this.hideTimeout);
      this.hideTimeout = null;
    }
    if (this.touchTimeout) {
      clearTimeout(this.touchTimeout);
      this.touchTimeout = null;
    }
  }

  private show(): void {
    if (this.isVisible || !this.appTooltip) return;

    this.ngZone.runOutsideAngular(() => {
      this.createTooltip();
      this.isVisible = true;

      // Trigger reflow for animation
      if (this.tooltipElement) {
        this.tooltipElement.offsetHeight;
        this.renderer.addClass(this.tooltipElement, 'app-tooltip--visible');
      }
    });
  }

  private hide(): void {
    if (!this.isVisible) return;

    this.isVisible = false;

    if (this.tooltipElement) {
      this.renderer.removeClass(this.tooltipElement, 'app-tooltip--visible');

      // Wait for fade-out animation before destroying
      setTimeout(() => {
        this.destroyTooltip();
      }, 150);
    }
  }

  private createTooltip(): void {
    if (this.tooltipElement) {
      this.destroyTooltip();
    }

    // Create tooltip container
    this.tooltipElement = this.renderer.createElement('div');
    this.renderer.addClass(this.tooltipElement, 'app-tooltip');
    this.renderer.addClass(this.tooltipElement, `app-tooltip--${this.tooltipPosition}`);
    this.renderer.setAttribute(this.tooltipElement, 'role', 'tooltip');
    this.renderer.setAttribute(this.tooltipElement, 'aria-hidden', 'false');

    // Create tooltip content
    const content = this.renderer.createText(this.appTooltip);
    this.renderer.appendChild(this.tooltipElement, content);

    // Create arrow
    const arrow = this.renderer.createElement('div');
    this.renderer.addClass(arrow, 'app-tooltip__arrow');
    this.renderer.appendChild(this.tooltipElement, arrow);

    // Append to body
    this.renderer.appendChild(document.body, this.tooltipElement);

    // Position tooltip
    this.updatePosition();
  }

  private updatePosition(): void {
    if (!this.tooltipElement) return;

    const hostElement = this.elementRef.nativeElement;
    const tooltipRect = this.tooltipElement.getBoundingClientRect();
    const hostRect = hostElement.getBoundingClientRect();

    const tooltipDimensions: TooltipDimensions = {
      width: tooltipRect.width,
      height: tooltipRect.height
    };

    // Calculate initial position
    let position = this.calculatePosition(hostRect, tooltipDimensions, this.tooltipPosition);

    // Check if tooltip is out of viewport and flip if needed
    const flippedPosition = this.checkAndFlipPosition(
      position,
      tooltipDimensions,
      hostRect,
      this.tooltipPosition
    );

    // Apply position
    this.renderer.setStyle(this.tooltipElement, 'top', `${flippedPosition.top}px`);
    this.renderer.setStyle(this.tooltipElement, 'left', `${flippedPosition.left}px`);
  }

  private calculatePosition(
    hostRect: DOMRect,
    tooltipDimensions: TooltipDimensions,
    position: TooltipPosition
  ): Position {
    const spacing = 8; // Gap between tooltip and host
    const scrollX = window.scrollX || window.pageXOffset;
    const scrollY = window.scrollY || window.pageYOffset;

    switch (position) {
      case 'top':
        return {
          top: hostRect.top + scrollY - tooltipDimensions.height - spacing,
          left: hostRect.left + scrollX + (hostRect.width - tooltipDimensions.width) / 2
        };

      case 'bottom':
        return {
          top: hostRect.bottom + scrollY + spacing,
          left: hostRect.left + scrollX + (hostRect.width - tooltipDimensions.width) / 2
        };

      case 'left':
        return {
          top: hostRect.top + scrollY + (hostRect.height - tooltipDimensions.height) / 2,
          left: hostRect.left + scrollX - tooltipDimensions.width - spacing
        };

      case 'right':
        return {
          top: hostRect.top + scrollY + (hostRect.height - tooltipDimensions.height) / 2,
          left: hostRect.right + scrollX + spacing
        };

      default:
        return { top: 0, left: 0 };
    }
  }

  private checkAndFlipPosition(
    position: Position,
    tooltipDimensions: TooltipDimensions,
    hostRect: DOMRect,
    currentPosition: TooltipPosition
  ): Position {
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const scrollX = window.scrollX || window.pageXOffset;
    const scrollY = window.scrollY || window.pageYOffset;
    const padding = 8;

    let flippedPosition: TooltipPosition | null = null;

    // Check if tooltip is out of viewport
    const isOutOfViewportTop = position.top - scrollY < padding;
    const isOutOfViewportBottom = position.top - scrollY + tooltipDimensions.height > viewportHeight - padding;
    const isOutOfViewportLeft = position.left - scrollX < padding;
    const isOutOfViewportRight = position.left - scrollX + tooltipDimensions.width > viewportWidth - padding;

    // Determine if we need to flip
    if (currentPosition === 'top' && isOutOfViewportTop) {
      flippedPosition = 'bottom';
    } else if (currentPosition === 'bottom' && isOutOfViewportBottom) {
      flippedPosition = 'top';
    } else if (currentPosition === 'left' && isOutOfViewportLeft) {
      flippedPosition = 'right';
    } else if (currentPosition === 'right' && isOutOfViewportRight) {
      flippedPosition = 'left';
    }

    // If we need to flip, recalculate position
    if (flippedPosition) {
      // Update tooltip class for arrow direction
      if (this.tooltipElement) {
        this.renderer.removeClass(this.tooltipElement, `app-tooltip--${currentPosition}`);
        this.renderer.addClass(this.tooltipElement, `app-tooltip--${flippedPosition}`);
      }
      position = this.calculatePosition(hostRect, tooltipDimensions, flippedPosition);
    }

    // Constrain to viewport (horizontal)
    if (position.left - scrollX < padding) {
      position.left = scrollX + padding;
    } else if (position.left - scrollX + tooltipDimensions.width > viewportWidth - padding) {
      position.left = scrollX + viewportWidth - tooltipDimensions.width - padding;
    }

    // Constrain to viewport (vertical)
    if (position.top - scrollY < padding) {
      position.top = scrollY + padding;
    } else if (position.top - scrollY + tooltipDimensions.height > viewportHeight - padding) {
      position.top = scrollY + viewportHeight - tooltipDimensions.height - padding;
    }

    return position;
  }

  private destroyTooltip(): void {
    if (this.tooltipElement) {
      this.renderer.removeChild(document.body, this.tooltipElement);
      this.tooltipElement = null;
    }
  }
}
