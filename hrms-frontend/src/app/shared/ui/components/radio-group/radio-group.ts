import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Radio } from '../radio/radio';

export interface RadioOption {
  label: string;
  value: any;
  disabled?: boolean;
}

@Component({
  selector: 'app-radio-group',
  standalone: true,
  imports: [CommonModule, Radio],
  templateUrl: './radio-group.html',
  styleUrl: './radio-group.scss',
})
export class RadioGroup {
  @Input() options: RadioOption[] = [];
  @Input() value: any;
  @Input() name: string = `radio-group-${Math.random().toString(36).substr(2, 9)}`;
  @Input() label: string = '';
  @Input() required: boolean = false;
  @Input() disabled: boolean = false;
  @Input() layout: 'vertical' | 'horizontal' = 'vertical';
  @Output() valueChange = new EventEmitter<any>();

  onRadioChange(value: any): void {
    if (!this.disabled) {
      this.value = value;
      this.valueChange.emit(value);
    }
  }

  isChecked(optionValue: any): boolean {
    return this.value === optionValue;
  }

  isDisabled(option: RadioOption): boolean {
    return this.disabled || !!option.disabled;
  }

  onKeyDown(event: KeyboardEvent, currentIndex: number): void {
    if (this.disabled) return;

    let newIndex = currentIndex;

    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowRight':
        event.preventDefault();
        newIndex = this.getNextEnabledIndex(currentIndex);
        break;
      case 'ArrowUp':
      case 'ArrowLeft':
        event.preventDefault();
        newIndex = this.getPreviousEnabledIndex(currentIndex);
        break;
      default:
        return;
    }

    if (newIndex !== currentIndex && newIndex !== -1) {
      this.onRadioChange(this.options[newIndex].value);
      // Focus management would happen here
    }
  }

  private getNextEnabledIndex(currentIndex: number): number {
    for (let i = currentIndex + 1; i < this.options.length; i++) {
      if (!this.options[i].disabled) {
        return i;
      }
    }
    // Wrap around to the beginning
    for (let i = 0; i < currentIndex; i++) {
      if (!this.options[i].disabled) {
        return i;
      }
    }
    return currentIndex;
  }

  private getPreviousEnabledIndex(currentIndex: number): number {
    for (let i = currentIndex - 1; i >= 0; i--) {
      if (!this.options[i].disabled) {
        return i;
      }
    }
    // Wrap around to the end
    for (let i = this.options.length - 1; i > currentIndex; i--) {
      if (!this.options[i].disabled) {
        return i;
      }
    }
    return currentIndex;
  }

  trackByValue(index: number, option: RadioOption): any {
    return option.value;
  }
}
