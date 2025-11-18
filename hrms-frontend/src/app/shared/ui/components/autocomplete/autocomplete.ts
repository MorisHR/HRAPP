/**
 * Production-ready Autocomplete Component
 *
 * @example
 * // Basic usage with string array
 * <app-autocomplete
 *   label="Select Country"
 *   [options]="countries"
 *   [(value)]="selectedCountry"
 *   placeholder="Type to search...">
 * </app-autocomplete>
 *
 * @example
 * // Advanced usage with objects and custom display/filter
 * <app-autocomplete
 *   label="Select User"
 *   [options]="users"
 *   [displayWith]="getUserDisplay"
 *   [filterWith]="userFilter"
 *   [(value)]="selectedUser"
 *   [loading]="isLoading"
 *   required>
 * </app-autocomplete>
 *
 * getUserDisplay = (user: User) => user ? `${user.firstName} ${user.lastName}` : '';
 * userFilter = (user: User, filter: string) => {
 *   const searchText = filter.toLowerCase();
 *   return user.firstName.toLowerCase().includes(searchText) ||
 *          user.lastName.toLowerCase().includes(searchText) ||
 *          user.email.toLowerCase().includes(searchText);
 * };
 */

import { Component, Input, Output, EventEmitter, HostListener, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  selector: 'app-autocomplete',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './autocomplete.html',
  styleUrl: './autocomplete.scss',
})
export class Autocomplete implements OnInit, OnDestroy {
  @Input() label: string = '';
  @Input() options: any[] = [];
  @Input() displayWith: (value: any) => string = (value) => value?.toString() || '';
  @Input() filterWith: (option: any, filter: string) => boolean = this.defaultFilter.bind(this);
  @Input() value: any = null;
  @Input() placeholder: string = 'Search...';
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;
  @Input() loading: boolean = false;
  @Output() valueChange = new EventEmitter<any>();

  inputValue: string = '';
  isOpen: boolean = false;
  filteredOptions: any[] = [];
  highlightedIndex: number = -1;

  constructor(
    private elementRef: ElementRef,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    if (this.value) {
      this.inputValue = this.displayWith(this.value);
    }
    this.filteredOptions = [...this.options];
  }

  ngOnDestroy(): void {
    // Cleanup if needed
  }

  private defaultFilter(option: any, filter: string): boolean {
    const displayText = this.displayWith(option).toLowerCase();
    return displayText.includes(filter.toLowerCase());
  }

  onInputChange(value: string): void {
    this.inputValue = value;
    this.filterOptions(value);

    if (!this.isOpen && value) {
      this.isOpen = true;
    }

    // If input is cleared, emit null value
    if (!value) {
      this.value = null;
      this.valueChange.emit(null);
      this.highlightedIndex = -1;
    }
  }

  onInputFocus(): void {
    if (!this.disabled) {
      this.isOpen = true;
      this.filterOptions(this.inputValue);
    }
  }

  private filterOptions(filterValue: string): void {
    if (!filterValue) {
      this.filteredOptions = [...this.options];
    } else {
      this.filteredOptions = this.options.filter(option =>
        this.filterWith(option, filterValue)
      );
    }
    this.highlightedIndex = this.filteredOptions.length > 0 ? 0 : -1;
  }

  selectOption(option: any): void {
    this.value = option;
    this.inputValue = this.displayWith(option);
    this.valueChange.emit(option);
    this.closeDropdown();
  }

  closeDropdown(): void {
    this.isOpen = false;
    this.highlightedIndex = -1;
  }

  onKeyDown(event: KeyboardEvent): void {
    if (this.disabled) return;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        if (!this.isOpen) {
          this.isOpen = true;
          this.filterOptions(this.inputValue);
        } else {
          this.highlightedIndex = Math.min(
            this.highlightedIndex + 1,
            this.filteredOptions.length - 1
          );
          this.scrollToHighlighted();
        }
        break;

      case 'ArrowUp':
        event.preventDefault();
        if (this.isOpen) {
          this.highlightedIndex = Math.max(this.highlightedIndex - 1, 0);
          this.scrollToHighlighted();
        }
        break;

      case 'Enter':
        event.preventDefault();
        if (this.isOpen && this.highlightedIndex >= 0 && this.filteredOptions[this.highlightedIndex]) {
          this.selectOption(this.filteredOptions[this.highlightedIndex]);
        }
        break;

      case 'Escape':
        event.preventDefault();
        this.closeDropdown();
        break;

      case 'Tab':
        if (this.isOpen) {
          this.closeDropdown();
        }
        break;
    }
  }

  private scrollToHighlighted(): void {
    setTimeout(() => {
      const highlightedElement = this.elementRef.nativeElement.querySelector('.option.highlighted');
      if (highlightedElement) {
        highlightedElement.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      }
    }, 0);
  }

  /**
   * SECURITY FIX: XSS Prevention
   * Highlights matching text with proper HTML escaping to prevent XSS attacks
   * Complies with OWASP A03:2021 - Injection
   */
  highlightMatch(text: string): SafeHtml {
    if (!this.inputValue || !text) {
      return this.escapeHTML(text);
    }

    // Step 1: Escape HTML entities in the original text to prevent XSS
    const escapedText = this.escapeHTML(text);

    // Step 2: Apply highlighting with <mark> tags (safe after escaping)
    const regex = new RegExp(`(${this.escapeRegExp(this.inputValue)})`, 'gi');
    const highlighted = escapedText.replace(regex, '<mark>$1</mark>');

    // Step 3: Return as SafeHtml (Angular will render it safely)
    return this.sanitizer.bypassSecurityTrustHtml(highlighted);
  }

  /**
   * Escapes HTML entities to prevent XSS
   */
  private escapeHTML(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  /**
   * Escapes special regex characters
   */
  private escapeRegExp(str: string): string {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.closeDropdown();
    }
  }

  getDisplayText(option: any): string {
    return this.displayWith(option);
  }

  get hasFilteredOptions(): boolean {
    return this.filteredOptions.length > 0;
  }

  get showEmptyState(): boolean {
    return this.isOpen && !this.loading && !this.hasFilteredOptions;
  }

  get showLoadingState(): boolean {
    return this.isOpen && this.loading;
  }
}
