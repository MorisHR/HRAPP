import { Component, Input, Output, EventEmitter, HostListener, ElementRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface MenuItem {
  label: string;
  value: any;
  icon?: string;
  disabled?: boolean;
  divider?: boolean;
  submenu?: MenuItem[];
}

export type MenuPosition = 'bottom-left' | 'bottom-right' | 'top-left' | 'top-right';

@Component({
  selector: 'app-menu',
  imports: [CommonModule],
  templateUrl: './menu.html',
  styleUrl: './menu.scss'
})
export class MenuComponent implements OnDestroy {
  @Input() trigger!: HTMLElement;
  @Input() items: MenuItem[] = [];
  @Input() position: MenuPosition = 'bottom-left';
  @Output() itemClick = new EventEmitter<any>();

  isOpen: boolean = false;
  focusedIndex: number = -1;
  menuPosition: { top: string; left: string; right?: string; bottom?: string } = { top: '0', left: '0' };

  private clickListener?: () => void;

  constructor(private elementRef: ElementRef) {}

  ngOnDestroy(): void {
    if (this.clickListener) {
      this.trigger?.removeEventListener('click', this.clickListener);
    }
  }

  open(): void {
    if (this.isOpen) return;

    this.isOpen = true;
    this.focusedIndex = -1;
    this.calculatePosition();

    // Set up trigger click listener
    if (this.trigger && !this.clickListener) {
      this.clickListener = () => this.toggle();
      this.trigger.addEventListener('click', this.clickListener);
    }
  }

  close(): void {
    this.isOpen = false;
    this.focusedIndex = -1;
  }

  toggle(): void {
    if (this.isOpen) {
      this.close();
    } else {
      this.open();
    }
  }

  handleItemClick(item: MenuItem, event: Event): void {
    event.stopPropagation();

    if (item.disabled || item.divider) {
      return;
    }

    // If item has submenu, don't close and emit
    if (item.submenu && item.submenu.length > 0) {
      return;
    }

    this.itemClick.emit(item.value);
    this.close();
  }

  private calculatePosition(): void {
    if (!this.trigger) return;

    const triggerRect = this.trigger.getBoundingClientRect();
    const menuElement = this.elementRef.nativeElement.querySelector('.menu-panel');

    // Wait for next frame to get accurate menu dimensions
    requestAnimationFrame(() => {
      if (!menuElement) return;

      const menuRect = menuElement.getBoundingClientRect();
      const viewportWidth = window.innerWidth;
      const viewportHeight = window.innerHeight;
      const spacing = 4; // Gap between trigger and menu

      let top = 0;
      let left = 0;

      // Calculate vertical position
      if (this.position.startsWith('top')) {
        top = triggerRect.top - menuRect.height - spacing;
        // Check if it overflows top
        if (top < 0) {
          top = triggerRect.bottom + spacing;
        }
      } else {
        top = triggerRect.bottom + spacing;
        // Check if it overflows bottom
        if (top + menuRect.height > viewportHeight) {
          top = triggerRect.top - menuRect.height - spacing;
        }
      }

      // Calculate horizontal position
      if (this.position.endsWith('right')) {
        left = triggerRect.right - menuRect.width;
        // Check if it overflows left
        if (left < 0) {
          left = triggerRect.left;
        }
      } else {
        left = triggerRect.left;
        // Check if it overflows right
        if (left + menuRect.width > viewportWidth) {
          left = triggerRect.right - menuRect.width;
        }
      }

      this.menuPosition = {
        top: `${top}px`,
        left: `${left}px`
      };
    });
  }

  handleKeyDown(event: KeyboardEvent): void {
    if (!this.isOpen) return;

    const validItems = this.items.filter(item => !item.disabled && !item.divider);

    switch (event.key) {
      case 'Escape':
        event.preventDefault();
        this.close();
        break;

      case 'ArrowDown':
        event.preventDefault();
        this.focusedIndex = Math.min(this.focusedIndex + 1, validItems.length - 1);
        if (this.focusedIndex === -1 && validItems.length > 0) {
          this.focusedIndex = 0;
        }
        break;

      case 'ArrowUp':
        event.preventDefault();
        this.focusedIndex = Math.max(this.focusedIndex - 1, 0);
        break;

      case 'Home':
        event.preventDefault();
        this.focusedIndex = 0;
        break;

      case 'End':
        event.preventDefault();
        this.focusedIndex = validItems.length - 1;
        break;

      case 'Enter':
      case ' ':
        event.preventDefault();
        if (this.focusedIndex >= 0 && this.focusedIndex < validItems.length) {
          const item = validItems[this.focusedIndex];
          if (!item.submenu || item.submenu.length === 0) {
            this.itemClick.emit(item.value);
            this.close();
          }
        }
        break;
    }
  }

  isFocused(item: MenuItem): boolean {
    const validItems = this.items.filter(i => !i.disabled && !i.divider);
    const itemIndex = validItems.indexOf(item);
    return itemIndex === this.focusedIndex;
  }

  onMouseEnterItem(item: MenuItem): void {
    if (item.disabled || item.divider) return;
    const validItems = this.items.filter(i => !i.disabled && !i.divider);
    this.focusedIndex = validItems.indexOf(item);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;

    // Check if click is outside menu and trigger
    if (this.isOpen &&
        !this.elementRef.nativeElement.contains(target) &&
        !this.trigger?.contains(target)) {
      this.close();
    }
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeyDown(event: KeyboardEvent): void {
    this.handleKeyDown(event);
  }

  trackByIndex(index: number, item: MenuItem): number {
    return index;
  }
}
