import {
  Component,
  Input,
  Output,
  EventEmitter,
  HostListener,
  ElementRef,
  OnDestroy,
  AfterViewInit,
  ViewChild,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  trigger,
  state,
  style,
  transition,
  animate,
} from '@angular/animations';

@Component({
  selector: 'app-sidenav',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sidenav.html',
  styleUrl: './sidenav.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('backdropAnimation', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('300ms ease-in-out', style({ opacity: 1 })),
      ]),
      transition(':leave', [
        animate('300ms ease-in-out', style({ opacity: 0 })),
      ]),
    ]),
  ],
})
export class Sidenav implements AfterViewInit, OnDestroy {
  @Input() opened: boolean = false;
  @Input() mode: 'over' | 'push' | 'side' = 'over';
  @Input() position: 'left' | 'right' = 'left';
  @Input() width: string = '280px';
  @Input() backdrop: boolean = true;

  @Output() openedChange = new EventEmitter<boolean>();

  @ViewChild('sidenavContent', { static: false }) sidenavContent?: ElementRef;

  private focusableElements: HTMLElement[] = [];
  private previousActiveElement: HTMLElement | null = null;
  private currentFocusIndex = 0;

  constructor(private elementRef: ElementRef) {}

  ngAfterViewInit(): void {
    this.updateFocusableElements();
  }

  ngOnDestroy(): void {
    this.restoreFocus();
  }

  /**
   * Opens the sidenav
   */
  open(): void {
    if (!this.opened) {
      this.opened = true;
      this.openedChange.emit(true);
      this.trapFocus();
    }
  }

  /**
   * Closes the sidenav
   */
  close(): void {
    if (this.opened) {
      this.opened = false;
      this.openedChange.emit(false);
      this.restoreFocus();
    }
  }

  /**
   * Toggles the sidenav open/closed state
   */
  toggle(): void {
    if (this.opened) {
      this.close();
    } else {
      this.open();
    }
  }

  /**
   * Handles backdrop click
   */
  onBackdropClick(): void {
    if (this.backdrop && this.mode !== 'side') {
      this.close();
    }
  }

  /**
   * Handles ESC key to close sidenav
   */
  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.opened && this.mode !== 'side') {
      this.close();
    }
  }

  /**
   * Handles Tab key for focus trap
   */
  @HostListener('document:keydown.tab', ['$event'])
  onTabKey(event: Event): void {
    if (this.opened && this.focusableElements.length > 0 && event instanceof KeyboardEvent) {
      this.handleFocusTrap(event);
    }
  }

  /**
   * Gets the transform value for the sidenav
   */
  getSidenavTransform(): string {
    if (this.mode === 'side') {
      return 'translateX(0)';
    }

    if (!this.opened) {
      return this.position === 'left' ? 'translateX(-100%)' : 'translateX(100%)';
    }

    return 'translateX(0)';
  }

  /**
   * Gets the margin for content area in push mode
   */
  getContentMargin(): string {
    if (this.mode !== 'push' || !this.opened) {
      return '0';
    }

    return this.position === 'left' ? this.width : `-${this.width}`;
  }

  /**
   * Checks if backdrop should be shown
   */
  showBackdrop(): boolean {
    return this.backdrop && this.opened && this.mode === 'over';
  }

  /**
   * Traps focus within sidenav when open
   */
  private trapFocus(): void {
    this.previousActiveElement = document.activeElement as HTMLElement;
    this.updateFocusableElements();

    setTimeout(() => {
      if (this.focusableElements.length > 0) {
        this.focusableElements[0].focus();
        this.currentFocusIndex = 0;
      }
    }, 100);
  }

  /**
   * Restores focus to previous element
   */
  private restoreFocus(): void {
    if (this.previousActiveElement) {
      this.previousActiveElement.focus();
      this.previousActiveElement = null;
    }
  }

  /**
   * Updates the list of focusable elements
   */
  private updateFocusableElements(): void {
    if (!this.sidenavContent) {
      this.focusableElements = [];
      return;
    }

    const focusableSelectors = [
      'a[href]',
      'button:not([disabled])',
      'textarea:not([disabled])',
      'input:not([disabled])',
      'select:not([disabled])',
      '[tabindex]:not([tabindex="-1"])',
    ];

    const elements = this.sidenavContent.nativeElement.querySelectorAll(
      focusableSelectors.join(', ')
    );

    this.focusableElements = Array.from(elements) as HTMLElement[];
  }

  /**
   * Handles focus trap when Tab is pressed
   */
  private handleFocusTrap(event: KeyboardEvent): void {
    const activeElement = document.activeElement as HTMLElement;
    const currentIndex = this.focusableElements.indexOf(activeElement);

    if (event.shiftKey) {
      // Shift + Tab (backwards)
      if (currentIndex <= 0) {
        event.preventDefault();
        this.focusableElements[this.focusableElements.length - 1].focus();
      }
    } else {
      // Tab (forwards)
      if (currentIndex >= this.focusableElements.length - 1) {
        event.preventDefault();
        this.focusableElements[0].focus();
      }
    }
  }
}
