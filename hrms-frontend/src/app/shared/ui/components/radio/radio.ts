import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-radio',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './radio.html',
  styleUrl: './radio.scss',
})
export class Radio {
  @Input() label: string = '';
  @Input() value: any;
  @Input() checked: boolean = false;
  @Input() disabled: boolean = false;
  @Input() name: string = '';
  @Output() change = new EventEmitter<any>();

  onRadioClick(): void {
    if (!this.disabled && !this.checked) {
      this.checked = true;
      this.change.emit(this.value);
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onRadioClick();
    }
  }
}
