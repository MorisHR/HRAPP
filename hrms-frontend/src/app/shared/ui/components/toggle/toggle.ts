import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-toggle',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toggle.html',
  styleUrl: './toggle.scss',
})
export class Toggle {
  @Input() checked: boolean = false;
  @Input() disabled: boolean = false;
  @Input() label: string = '';
  @Input() labelPosition: 'before' | 'after' = 'after';
  @Input() color: 'primary' | 'success' | 'warning' = 'primary';
  @Output() checkedChange = new EventEmitter<boolean>();

  /**
   * Handle toggle click event
   */
  onToggle(): void {
    if (this.disabled) return;

    this.checked = !this.checked;
    this.checkedChange.emit(this.checked);
  }

  /**
   * Handle keyboard events for accessibility
   */
  onKeyDown(event: KeyboardEvent): void {
    if (this.disabled) return;

    // Toggle on Space key
    if (event.key === ' ' || event.key === 'Spacebar') {
      event.preventDefault();
      this.onToggle();
    }
  }
}
