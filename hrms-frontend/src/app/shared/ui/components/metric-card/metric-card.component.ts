// ═══════════════════════════════════════════════════════════
// PREMIUM METRIC CARD COMPONENT
// Fortune 500-grade data cards with visual hierarchy
// ═══════════════════════════════════════════════════════════

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icon/icon';
import { TrendIndicatorComponent } from '../trend-indicator/trend-indicator.component';
import { SparklineComponent } from '../charts/sparkline.component';

export type MetricSize = 'small' | 'medium' | 'large' | 'hero';
export type MetricTheme = 'default' | 'primary' | 'success' | 'warning' | 'error';

@Component({
  selector: 'app-metric-card',
  standalone: true,
  imports: [CommonModule, IconComponent, TrendIndicatorComponent, SparklineComponent],
  template: `
    <div class="metric-card"
         [class]="'metric-card--' + size + ' metric-card--' + theme"
         [class.metric-card--clickable]="clickable">
      <div class="metric-card__header">
        <div class="metric-card__icon-wrapper" *ngIf="icon">
          <app-icon [name]="icon" class="metric-card__icon"></app-icon>
        </div>
        <div class="metric-card__title-wrapper">
          <h3 class="metric-card__title">{{ title }}</h3>
          @if (subtitle) {
            <p class="metric-card__subtitle">{{ subtitle }}</p>
          }
        </div>
      </div>

      <div class="metric-card__body">
        <div class="metric-card__value-row">
          <div class="metric-card__value">{{ formattedValue }}</div>
          @if (trend !== undefined && trend !== 0) {
            <app-trend-indicator
              [value]="trend"
              [label]="trendLabel">
            </app-trend-indicator>
          }
        </div>

        @if (context) {
          <p class="metric-card__context">{{ context }}</p>
        }

        @if (sparklineData) {
          <div class="metric-card__sparkline">
            <app-sparkline
              [data]="sparklineData"
              [color]="sparklineColor || getThemeColor()"
              height="48px">
            </app-sparkline>
          </div>
        }
      </div>

      @if (footer) {
        <div class="metric-card__footer">
          {{ footer }}
        </div>
      }
    </div>
  `,
  styles: [`
    .metric-card {
      background: var(--bg-card, #FFFFFF);
      border-radius: 16px;
      padding: 24px;
      border: 1px solid rgba(0, 0, 0, 0.06);
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.06), 0 1px 2px rgba(0, 0, 0, 0.03);
      transition: transform 300ms cubic-bezier(0.4, 0.0, 0.2, 1),
                  box-shadow 300ms cubic-bezier(0.4, 0.0, 0.2, 1),
                  border-color 300ms cubic-bezier(0.4, 0.0, 0.2, 1);
      height: 100%;
      display: flex;
      flex-direction: column;
      will-change: transform;
    }

    .metric-card:hover {
      box-shadow: 0 8px 20px rgba(15, 98, 254, 0.10),
                  0 4px 10px rgba(0, 0, 0, 0.08);
      transform: translateY(-3px) scale(1.005);
      border-color: rgba(15, 98, 254, 0.15);
    }

    .metric-card--clickable {
      cursor: pointer;
    }

    .metric-card--clickable:hover {
      box-shadow: 0 8px 24px rgba(15, 98, 254, 0.12),
                  0 4px 12px rgba(0, 0, 0, 0.10);
      transform: translateY(-4px) scale(1.008);
      border-color: rgba(15, 98, 254, 0.2);
    }

    .metric-card--clickable:active {
      transform: translateY(-2px) scale(1.003);
      transition: transform 150ms cubic-bezier(0.4, 0.0, 0.2, 1);
    }

    /* SIZES */
    .metric-card--small {
      padding: 16px;
    }

    .metric-card--hero {
      padding: 32px;
    }

    /* THEMES */
    .metric-card--primary {
      background: linear-gradient(135deg, #0F62FE 0%, #0043CE 100%);
      color: #FFFFFF;
    }

    .metric-card--success {
      background: linear-gradient(135deg, #24A148 0%, #1C7F37 100%);
      color: #FFFFFF;
    }

    .metric-card--warning {
      background: linear-gradient(135deg, #F1C21B 0%, #D4A017 100%);
      color: #161616;
    }

    .metric-card--error {
      background: linear-gradient(135deg, #DA1E28 0%, #A2191F 100%);
      color: #FFFFFF;
    }

    /* HEADER */
    .metric-card__header {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      margin-bottom: 16px;
    }

    .metric-card__icon-wrapper {
      flex-shrink: 0;
      width: 48px;
      height: 48px;
      border-radius: 12px;
      background: rgba(15, 98, 254, 0.08);
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .metric-card--primary .metric-card__icon-wrapper,
    .metric-card--success .metric-card__icon-wrapper,
    .metric-card--error .metric-card__icon-wrapper {
      background: rgba(255, 255, 255, 0.15);
    }

    .metric-card__icon {
      font-size: 24px;
      color: #0F62FE;
    }

    .metric-card--primary .metric-card__icon,
    .metric-card--success .metric-card__icon,
    .metric-card--error .metric-card__icon {
      color: #FFFFFF;
    }

    .metric-card__title-wrapper {
      flex: 1;
      min-width: 0;
    }

    .metric-card__title {
      font-size: 14px;
      font-weight: 600;
      color: #525252;
      margin: 0;
      line-height: 1.4;
      letter-spacing: -0.005em;
      text-transform: uppercase;
      font-size: 12px;
    }

    .metric-card--hero .metric-card__title {
      font-size: 14px;
    }

    .metric-card--primary .metric-card__title,
    .metric-card--success .metric-card__title,
    .metric-card--error .metric-card__title {
      color: rgba(255, 255, 255, 0.9);
    }

    .metric-card--warning .metric-card__title {
      color: rgba(22, 22, 22, 0.8);
    }

    .metric-card__subtitle {
      font-size: 12px;
      color: #8D8D8D;
      margin: 4px 0 0;
      line-height: 1.3;
    }

    .metric-card--primary .metric-card__subtitle,
    .metric-card--success .metric-card__subtitle,
    .metric-card--error .metric-card__subtitle {
      color: rgba(255, 255, 255, 0.7);
    }

    /* BODY */
    .metric-card__body {
      flex: 1;
      display: flex;
      flex-direction: column;
    }

    .metric-card__value-row {
      display: flex;
      align-items: baseline;
      gap: 12px;
      margin-bottom: 8px;
    }

    .metric-card__value {
      font-size: 36px;
      font-weight: 800;
      color: #161616;
      line-height: 1;
      letter-spacing: -0.03em;
      font-feature-settings: 'tnum' 1;
    }

    .metric-card--small .metric-card__value {
      font-size: 28px;
      font-weight: 700;
    }

    .metric-card--hero .metric-card__value {
      font-size: 48px;
      font-weight: 800;
    }

    .metric-card--primary .metric-card__value,
    .metric-card--success .metric-card__value,
    .metric-card--error .metric-card__value {
      color: #FFFFFF;
    }

    .metric-card__context {
      font-size: 13px;
      color: #6F6F6F;
      margin: 0 0 12px;
      line-height: 1.4;
    }

    .metric-card--primary .metric-card__context,
    .metric-card--success .metric-card__context,
    .metric-card--error .metric-card__context {
      color: rgba(255, 255, 255, 0.8);
    }

    .metric-card--warning .metric-card__context {
      color: rgba(22, 22, 22, 0.7);
    }

    .metric-card__sparkline {
      margin-top: auto;
      margin-left: -8px;
      margin-right: -8px;
      margin-bottom: -8px;
    }

    /* FOOTER */
    .metric-card__footer {
      margin-top: 16px;
      padding-top: 16px;
      border-top: 1px solid #E0E0E0;
      font-size: 13px;
      color: #6F6F6F;
    }

    .metric-card--primary .metric-card__footer,
    .metric-card--success .metric-card__footer,
    .metric-card--error .metric-card__footer {
      border-top-color: rgba(255, 255, 255, 0.2);
      color: rgba(255, 255, 255, 0.8);
    }
  `]
})
export class MetricCardComponent {
  @Input() title!: string;
  @Input() subtitle?: string;
  @Input() value!: string | number;
  @Input() icon?: string;
  @Input() trend?: number;
  @Input() trendLabel?: string;
  @Input() context?: string;
  @Input() footer?: string;
  @Input() sparklineData?: number[];
  @Input() sparklineColor?: string;
  @Input() size: MetricSize = 'medium';
  @Input() theme: MetricTheme = 'default';
  @Input() clickable: boolean = false;

  get formattedValue(): string | number {
    // Handle zero states elegantly
    if (this.value === 0 || this.value === '0' || this.value === null || this.value === undefined) {
      return '—';
    }

    // Handle string values that are empty or whitespace
    if (typeof this.value === 'string' && this.value.trim() === '') {
      return '—';
    }

    return this.value;
  }

  getThemeColor(): string {
    switch (this.theme) {
      case 'primary': return '#FFFFFF';
      case 'success': return '#FFFFFF';
      case 'warning': return '#161616';
      case 'error': return '#FFFFFF';
      default: return '#0F62FE';
    }
  }
}
