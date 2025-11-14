// ═══════════════════════════════════════════════════════════
// PREMIUM CHECKBOX COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Production-ready checkbox with full accessibility support
// ═══════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-checkbox',
  imports: [CommonModule],
  templateUrl: './checkbox.html',
  styleUrl: './checkbox.scss',
})
export class CheckboxComponent implements AfterViewInit {
  @Input() label: string = '';
  @Input() checked: boolean = false;
  @Input() indeterminate: boolean = false;
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;

  @Output() checkedChange = new EventEmitter<boolean>();

  @ViewChild('checkboxInput') checkboxInput!: ElementRef<HTMLInputElement>;

  // Unique ID for accessibility
  checkboxId = `checkbox-${Math.random().toString(36).substr(2, 9)}`;

  ngAfterViewInit(): void {
    // Set indeterminate state on the native element
    this.updateIndeterminateState();
  }

  ngOnChanges(): void {
    // Update indeterminate state when it changes
    this.updateIndeterminateState();
  }

  private updateIndeterminateState(): void {
    if (this.checkboxInput?.nativeElement) {
      this.checkboxInput.nativeElement.indeterminate = this.indeterminate;
    }
  }

  onCheckboxChange(event: Event): void {
    if (this.disabled) {
      return;
    }

    const target = event.target as HTMLInputElement;
    this.checked = target.checked;

    // Clear indeterminate state when user interacts
    if (this.indeterminate) {
      this.indeterminate = false;
      this.updateIndeterminateState();
    }

    this.checkedChange.emit(this.checked);
  }

  get checkboxClasses(): string[] {
    return [
      'checkbox',
      this.checked ? 'checkbox--checked' : '',
      this.indeterminate ? 'checkbox--indeterminate' : '',
      this.disabled ? 'checkbox--disabled' : '',
    ].filter(Boolean);
  }
}
