// ═══════════════════════════════════════════════════════════
// PREMIUM CHECKBOX COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Production-ready checkbox with full accessibility support
// ═══════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, AfterViewInit, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-checkbox',
  imports: [CommonModule],
  templateUrl: './checkbox.html',
  styleUrl: './checkbox.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CheckboxComponent),
      multi: true
    }
  ]
})
export class CheckboxComponent implements AfterViewInit, ControlValueAccessor {
  @Input() label: string = '';
  @Input() checked: boolean = false;
  @Input() indeterminate: boolean = false;
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;

  @Output() checkedChange = new EventEmitter<boolean>();

  @ViewChild('checkboxInput') checkboxInput!: ElementRef<HTMLInputElement>;

  // Unique ID for accessibility
  checkboxId = `checkbox-${Math.random().toString(36).substr(2, 9)}`;

  // ControlValueAccessor implementation
  private onChange: (value: boolean) => void = () => {};
  private onTouched: () => void = () => {};

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
    this.onChange(this.checked);
    this.onTouched();
  }

  get checkboxClasses(): string[] {
    return [
      'checkbox',
      this.checked ? 'checkbox--checked' : '',
      this.indeterminate ? 'checkbox--indeterminate' : '',
      this.disabled ? 'checkbox--disabled' : '',
    ].filter(Boolean);
  }

  // ControlValueAccessor methods
  writeValue(value: boolean): void {
    this.checked = value;
  }

  registerOnChange(fn: (value: boolean) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
