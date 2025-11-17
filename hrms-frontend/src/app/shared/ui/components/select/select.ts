import { Component, Input, Output, EventEmitter, HostListener, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface SelectOption {
  value: any;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-select',
  imports: [CommonModule, FormsModule],
  templateUrl: './select.html',
  styleUrl: './select.scss'
})
export class SelectComponent {
  @Input() label: string = '';
  @Input() placeholder: string = 'Select an option';
  @Input() options: SelectOption[] = [];
  @Input() value: any = null;
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;
  @Input() error: string | null = null;
  @Input() hint: string = '';
  @Input() searchable: boolean = false;
  @Input() clearable: boolean = false;
  @Input() multiple: boolean = false;

  @Output() valueChange = new EventEmitter<any>();
  @Output() blur = new EventEmitter<FocusEvent>();

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  isOpen: boolean = false;
  searchQuery: string = '';
  focusedIndex: number = -1;

  constructor(private elementRef: ElementRef) {}

  get selectedOption(): SelectOption | null {
    if (this.multiple) {
      return null;
    }
    return this.options.find(opt => opt.value === this.value) || null;
  }

  get selectedOptions(): SelectOption[] {
    if (!this.multiple || !Array.isArray(this.value)) {
      return [];
    }
    return this.options.filter(opt => this.value.includes(opt.value));
  }

  get displayValue(): string {
    if (this.multiple) {
      const selected = this.selectedOptions;
      if (selected.length === 0) {
        return this.placeholder;
      }
      if (selected.length === 1) {
        return selected[0].label;
      }
      return `${selected.length} selected`;
    }
    return this.selectedOption?.label || this.placeholder;
  }

  get filteredOptions(): SelectOption[] {
    if (!this.searchable || !this.searchQuery) {
      return this.options;
    }
    const query = this.searchQuery.toLowerCase();
    return this.options.filter(opt =>
      opt.label.toLowerCase().includes(query)
    );
  }

  get hasValue(): boolean {
    if (this.multiple) {
      return Array.isArray(this.value) && this.value.length > 0;
    }
    return this.value !== null && this.value !== undefined && this.value !== '';
  }

  toggleDropdown(): void {
    if (this.disabled) {
      return;
    }

    this.isOpen = !this.isOpen;

    if (this.isOpen) {
      this.searchQuery = '';
      this.focusedIndex = -1;

      // Focus search input if searchable
      if (this.searchable) {
        setTimeout(() => {
          this.searchInput?.nativeElement.focus();
        }, 0);
      }
    }
  }

  selectOption(option: SelectOption): void {
    if (option.disabled) {
      return;
    }

    if (this.multiple) {
      const currentValue = Array.isArray(this.value) ? [...this.value] : [];
      const index = currentValue.indexOf(option.value);

      if (index > -1) {
        currentValue.splice(index, 1);
      } else {
        currentValue.push(option.value);
      }

      this.value = currentValue;
      this.valueChange.emit(this.value);
    } else {
      this.value = option.value;
      this.valueChange.emit(this.value);
      this.isOpen = false;
    }
  }

  isSelected(option: SelectOption): boolean {
    if (this.multiple) {
      return Array.isArray(this.value) && this.value.includes(option.value);
    }
    return this.value === option.value;
  }

  clearSelection(event: Event): void {
    event.stopPropagation();

    if (this.multiple) {
      this.value = [];
    } else {
      this.value = null;
    }

    this.valueChange.emit(this.value);
  }

  handleBlur(event: FocusEvent): void {
    // Small timeout to allow click events on dropdown to register
    setTimeout(() => {
      if (!this.elementRef.nativeElement.contains(document.activeElement)) {
        this.isOpen = false;
        this.blur.emit(event);
      }
    }, 200);
  }

  handleKeyDown(event: KeyboardEvent): void {
    if (this.disabled) {
      return;
    }

    const options = this.filteredOptions;

    switch (event.key) {
      case 'Enter':
      case ' ':
        if (!this.isOpen) {
          event.preventDefault();
          this.toggleDropdown();
        } else if (this.focusedIndex >= 0 && this.focusedIndex < options.length) {
          event.preventDefault();
          this.selectOption(options[this.focusedIndex]);
        }
        break;

      case 'Escape':
        if (this.isOpen) {
          event.preventDefault();
          this.isOpen = false;
        }
        break;

      case 'ArrowDown':
        event.preventDefault();
        if (!this.isOpen) {
          this.toggleDropdown();
        } else {
          this.focusedIndex = Math.min(this.focusedIndex + 1, options.length - 1);
        }
        break;

      case 'ArrowUp':
        event.preventDefault();
        if (this.isOpen) {
          this.focusedIndex = Math.max(this.focusedIndex - 1, 0);
        }
        break;

      case 'Tab':
        if (this.isOpen) {
          this.isOpen = false;
        }
        break;
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  trackByValue(index: number, option: SelectOption): any {
    return option.value;
  }
}
