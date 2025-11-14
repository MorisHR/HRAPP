import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ProgressSpinnerColor = 'primary' | 'success' | 'warning' | 'error';
export type ProgressSpinnerSize = 'small' | 'medium' | 'large';

@Component({
  selector: 'app-progress-spinner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './progress-spinner.html',
  styleUrl: './progress-spinner.scss',
})
export class ProgressSpinner {
  @Input() size: ProgressSpinnerSize = 'medium';
  @Input() color: ProgressSpinnerColor = 'primary';
  @Input() set thickness(val: number) {
    this.thicknessSignal.set(Math.max(1, Math.min(10, val)));
  }

  private thicknessSignal = signal(4);

  protected readonly spinnerThickness = computed(() => `${this.thicknessSignal()}px`);
}
