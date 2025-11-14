// ═══════════════════════════════════════════════════════════
// DIALOG CONTAINER COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Container for dialog content with backdrop, animations, and accessibility
// ═══════════════════════════════════════════════════════════

import {
  Component,
  ComponentRef,
  ViewChild,
  ViewContainerRef,
  HostListener,
  OnInit,
  OnDestroy,
  ElementRef,
  AfterViewInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogRef } from '../../services/dialog-ref';
import { DialogConfig } from '../../services/dialog';

@Component({
  selector: 'app-dialog-container',
  imports: [CommonModule],
  templateUrl: './dialog-container.html',
  styleUrl: './dialog-container.scss',
  host: {
    'class': 'dialog-overlay',
    '[@fadeIn]': 'animationState',
    '(@fadeIn.done)': 'onAnimationDone($event)'
  }
})
export class DialogContainerComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly elementRef = inject(ElementRef);
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild('contentContainer', { read: ViewContainerRef })
  contentContainer!: ViewContainerRef;

  /** Dialog reference */
  dialogRef!: DialogRef;

  /** Dialog configuration */
  config!: DialogConfig;

  /** Animation state */
  animationState: 'void' | 'enter' | 'exit' = 'enter';

  /** Whether the dialog is currently animating */
  private isAnimating = false;

  /** Component reference for the dialog content */
  private contentRef?: ComponentRef<any>;

  ngOnInit(): void {
    // Apply initial focus trap
    this.trapFocus();
  }

  ngAfterViewInit(): void {
    // Auto-focus first focusable element if configured
    if (this.config.autoFocus !== false) {
      this.focusFirstElement();
    }
  }

  ngOnDestroy(): void {
    // Clean up the content component
    if (this.contentRef) {
      this.contentRef.destroy();
    }
  }

  /**
   * Attaches a component to the dialog content area.
   */
  attachContentComponent(componentRef: ComponentRef<any>): void {
    this.contentRef = componentRef;

    // Attach the component's view to the container
    if (this.contentContainer) {
      this.contentContainer.insert(componentRef.hostView);
    }

    this.cdr.detectChanges();
  }

  /**
   * Closes the dialog.
   */
  close(result?: any): void {
    if (this.isAnimating) {
      return;
    }

    this.startExitAnimation();
    this.dialogRef.close(result);
  }

  /**
   * Handles backdrop click events.
   */
  onBackdropClick(event: MouseEvent): void {
    // Only close if backdrop was clicked (not the dialog content)
    if (event.target === event.currentTarget && !this.config.disableClose) {
      this.close();
    }
  }

  /**
   * Handles close button click.
   */
  onCloseButtonClick(): void {
    this.close();
  }

  /**
   * Handles escape key press.
   */
  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    if (!this.config.disableClose && !this.isAnimating) {
      keyboardEvent.preventDefault();
      this.close();
    }
  }

  /**
   * Gets the dialog panel styles based on configuration.
   */
  get dialogPanelStyles(): any {
    return {
      width: this.config.width,
      height: this.config.height,
      maxWidth: this.config.maxWidth,
      maxHeight: this.config.maxHeight,
      minWidth: this.config.minWidth,
      minHeight: this.config.minHeight
    };
  }

  /**
   * Gets the backdrop CSS classes.
   */
  get backdropClasses(): string[] {
    const classes = ['dialog-backdrop'];
    if (this.config.backdropClass) {
      if (Array.isArray(this.config.backdropClass)) {
        classes.push(...this.config.backdropClass);
      } else {
        classes.push(this.config.backdropClass);
      }
    }
    return classes;
  }

  /**
   * Gets the dialog panel CSS classes.
   */
  get panelClasses(): string[] {
    const classes = ['dialog-panel'];
    if (this.config.panelClass) {
      if (Array.isArray(this.config.panelClass)) {
        classes.push(...this.config.panelClass);
      } else {
        classes.push(this.config.panelClass);
      }
    }
    return classes;
  }

  /**
   * Starts the exit animation.
   */
  private startExitAnimation(): void {
    this.isAnimating = true;
    this.animationState = 'exit';
  }

  /**
   * Called when animation completes.
   */
  onAnimationDone(event: any): void {
    if (event.toState === 'exit') {
      this.isAnimating = false;
    } else if (event.toState === 'enter') {
      this.isAnimating = false;
    }
  }

  /**
   * Traps focus within the dialog.
   */
  private trapFocus(): void {
    const element = this.elementRef.nativeElement as HTMLElement;

    // Listen for tab key to trap focus
    element.addEventListener('keydown', (event: KeyboardEvent) => {
      if (event.key === 'Tab') {
        this.handleTabKey(event);
      }
    });
  }

  /**
   * Handles tab key press for focus trapping.
   */
  private handleTabKey(event: KeyboardEvent): void {
    const focusableElements = this.getFocusableElements();
    if (focusableElements.length === 0) {
      return;
    }

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    if (event.shiftKey) {
      // Shift + Tab
      if (document.activeElement === firstElement) {
        event.preventDefault();
        lastElement.focus();
      }
    } else {
      // Tab
      if (document.activeElement === lastElement) {
        event.preventDefault();
        firstElement.focus();
      }
    }
  }

  /**
   * Gets all focusable elements within the dialog.
   */
  private getFocusableElements(): HTMLElement[] {
    const element = this.elementRef.nativeElement as HTMLElement;
    const selector = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
    return Array.from(element.querySelectorAll(selector)) as HTMLElement[];
  }

  /**
   * Focuses the first focusable element.
   */
  private focusFirstElement(): void {
    setTimeout(() => {
      const focusableElements = this.getFocusableElements();
      if (focusableElements.length > 0) {
        focusableElements[0].focus();
      }
    }, 0);
  }
}
