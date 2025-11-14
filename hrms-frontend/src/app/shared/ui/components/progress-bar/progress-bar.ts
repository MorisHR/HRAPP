import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ProgressBarColor = 'primary' | 'success' | 'warning' | 'error';
export type ProgressBarHeight = 'small' | 'medium' | 'large';

@Component({
  selector: 'app-progress-bar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './progress-bar.html',
  styleUrl: './progress-bar.scss',
})
export class ProgressBar {
  @Input() set value(val: number) {
    this.valueSignal.set(Math.max(0, Math.min(100, val)));
  }

  @Input() color: ProgressBarColor = 'primary';
  @Input() height: ProgressBarHeight = 'medium';
  @Input() showLabel: boolean = false;
  @Input() indeterminate: boolean = false;

  private valueSignal = signal(0);

  protected readonly progressValue = computed(() => this.valueSignal());
  protected readonly progressWidth = computed(() => `${this.valueSignal()}%`);
}
