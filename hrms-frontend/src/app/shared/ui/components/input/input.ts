// ═══════════════════════════════════════════════════════════
// PREMIUM INPUT COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Production-ready text input with floating labels and validation
// ═══════════════════════════════════════════════════════════

import { Component, Input, Output, EventEmitter, forwardRef, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'url';

@Component({
  selector: 'app-input',
  imports: [CommonModule],
  templateUrl: './input.html',
  styleUrl: './input.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ]
})
export class InputComponent implements ControlValueAccessor, OnChanges {
  @Input() type: InputType = 'text';
  @Input() label: string = '';
  @Input() placeholder: string = '';
  @Input() value: string | number = '';
  @Input() disabled: boolean = false;
  @Input() readonly: boolean = false;
  @Input() required: boolean = false;
  @Input() error: string | null = null;
  @Input() hint: string = '';
  @Input() maxLength: number | undefined;
  @Input() showCharacterCount: boolean = false;
  @Input() clearable: boolean = false;
  @Input() id: string = `input-${Math.random().toString(36).substr(2, 9)}`;

  @Output() valueChange = new EventEmitter<string | number>();
  @Output() blur = new EventEmitter<FocusEvent>();
  @Output() focus = new EventEmitter<FocusEvent>();

  isFocused: boolean = false;
  internalValue: string | number = '';

  // Sync value input to internalValue when it changes
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['value'] && changes['value'].currentValue !== this.internalValue) {
      this.internalValue = changes['value'].currentValue || '';
    }
  }

  // ControlValueAccessor implementation
  private onChange: (value: string | number) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | number): void {
    this.internalValue = value || '';
  }

  registerOnChange(fn: (value: string | number) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  // Event handlers
  onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    const newValue = this.type === 'number' ? target.valueAsNumber : target.value;

    this.internalValue = newValue;
    this.value = newValue;
    this.valueChange.emit(newValue);
    this.onChange(newValue);
  }

  onFocus(event: FocusEvent): void {
    this.isFocused = true;
    this.focus.emit(event);
  }

  onBlur(event: FocusEvent): void {
    this.isFocused = false;
    this.onTouched();
    this.blur.emit(event);
  }

  clearValue(): void {
    if (!this.disabled && !this.readonly) {
      this.internalValue = '';
      this.value = '';
      this.valueChange.emit('');
      this.onChange('');
    }
  }

  // Computed properties
  get hasValue(): boolean {
    return this.internalValue !== '' && this.internalValue !== null && this.internalValue !== undefined;
  }

  get characterCount(): number {
    return String(this.internalValue || '').length;
  }

  get showClearButton(): boolean {
    return this.clearable && this.hasValue && !this.disabled && !this.readonly;
  }

  get showLabel(): boolean {
    return !!this.label;
  }

  get showError(): boolean {
    return !!this.error;
  }

  get showHint(): boolean {
    return !!this.hint && !this.showError;
  }

  get showCharCount(): boolean {
    return this.showCharacterCount && !!this.maxLength;
  }

  get inputClasses(): string[] {
    return [
      'input-field',
      this.hasValue || this.isFocused ? 'input-field--filled' : '',
      this.showError ? 'input-field--error' : '',
      this.disabled ? 'input-field--disabled' : '',
      this.readonly ? 'input-field--readonly' : '',
      this.isFocused ? 'input-field--focused' : ''
    ].filter(Boolean);
  }

  get containerClasses(): string[] {
    return [
      'input-container',
      this.showError ? 'input-container--error' : '',
      this.disabled ? 'input-container--disabled' : ''
    ].filter(Boolean);
  }
}
