import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'ghost' | 'danger' | 'text';
export type ButtonSize = 'small' | 'medium' | 'large';

@Component({
  selector: 'app-button',
  imports: [CommonModule],
  templateUrl: './button.html',
  styleUrl: './button.scss'
})
export class ButtonComponent {
  @Input() variant: ButtonVariant = 'primary';
  @Input() size: ButtonSize = 'medium';
  @Input() disabled: boolean = false;
  @Input() fullWidth: boolean = false;
  @Input() loading: boolean = false;
  @Input() type: 'button' | 'submit' | 'reset' = 'button';

  @Output() clicked = new EventEmitter<MouseEvent>();

  handleClick(event: MouseEvent): void {
    if (!this.disabled && !this.loading) {
      this.clicked.emit(event);
    }
  }

  get buttonClasses(): string[] {
    // Map variant aliases to actual classes
    const variantClass = this.variant === 'danger' ? 'error' :
                         this.variant === 'text' ? 'ghost' :
                         this.variant;

    return [
      'btn',
      `btn--${variantClass}`,
      `btn--${this.size}`,
      this.disabled ? 'btn--disabled' : '',
      this.fullWidth ? 'btn--full-width' : '',
      this.loading ? 'btn--loading' : ''
    ].filter(Boolean);
  }
}
