// ═══════════════════════════════════════════════════════════
// TREND INDICATOR COMPONENT
// Shows trend direction with percentage change
// ═══════════════════════════════════════════════════════════

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icon/icon';

export type TrendDirection = 'up' | 'down' | 'neutral';

@Component({
  selector: 'app-trend-indicator',
  standalone: true,
  imports: [CommonModule, IconComponent],
  template: `
    <div class="trend-indicator" [class]="'trend-indicator--' + direction">
      <app-icon [name]="iconName" class="trend-icon"></app-icon>
      <span class="trend-value">{{ formattedValue }}</span>
      @if (label) {
        <span class="trend-label">{{ label }}</span>
      }
    </div>
  `,
  styles: [`
    .trend-indicator {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 13px;
      font-weight: 500;
      line-height: 1;
    }

    .trend-indicator--up {
      color: #24A148;
      background: #DEFBE6;
    }

    .trend-indicator--down {
      color: #DA1E28;
      background: #FFE5E5;
    }

    .trend-indicator--neutral {
      color: #6F6F6F;
      background: #F4F4F4;
    }

    .trend-icon {
      font-size: 14px;
    }

    .trend-value {
      font-weight: 600;
    }

    .trend-label {
      margin-left: 2px;
      opacity: 0.8;
      font-size: 12px;
    }
  `]
})
export class TrendIndicatorComponent {
  @Input() value!: number;
  @Input() direction?: TrendDirection;
  @Input() label?: string;
  @Input() showSign: boolean = true;

  get formattedValue(): string {
    // Handle zero state elegantly
    if (this.value === 0) {
      return '—';
    }
    const sign = this.showSign && this.value > 0 ? '+' : '';
    return `${sign}${Math.abs(this.value).toFixed(1)}%`;
  }

  get iconName(): string {
    const dir = this.direction || this.getAutoDirection();
    switch (dir) {
      case 'up':
        return 'trending_up';
      case 'down':
        return 'trending_down';
      default:
        return 'trending_flat';
    }
  }

  private getAutoDirection(): TrendDirection {
    if (this.value > 0) return 'up';
    if (this.value < 0) return 'down';
    return 'neutral';
  }
}
