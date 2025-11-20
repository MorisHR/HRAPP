import {
  Component,
  Input,
  ViewChild,
  ChangeDetectionStrategy,
  OnChanges,
  SimpleChanges,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  template: `
    <div class="chart-container" [class.loading]="loading()">
      @if (loading()) {
        <div class="chart-loading">
          <div class="spinner"></div>
          <p>Loading chart data...</p>
        </div>
      } @else if (error()) {
        <div class="chart-error">
          <p class="error-message">{{ error() }}</p>
        </div>
      } @else {
        <canvas
          baseChart
          [data]="barChartData"
          [options]="barChartOptions"
          [type]="barChartType"
        ></canvas>
      }
    </div>
  `,
  styles: [`
    .chart-container {
      position: relative;
      width: 100%;
      height: 100%;
      min-height: 300px;
    }

    .chart-loading,
    .chart-error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      min-height: 300px;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid rgba(0, 0, 0, 0.1);
      border-left-color: var(--color-primary, #3b82f6);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .chart-loading p {
      margin-top: 16px;
      color: var(--color-text-secondary);
      font-size: 14px;
    }

    .error-message {
      color: var(--color-error, #ef4444);
      font-size: 14px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BarChartComponent implements OnChanges {
  @Input() data: ChartData<'bar'> | null = null;
  @Input() loading = signal(false);
  @Input() error = signal<string | null>(null);
  @Input() height: number = 300;
  @Input() horizontal: boolean = false;
  @Input() showGrid: boolean = true;
  @Input() showLegend: boolean = true;

  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;

  readonly barChartType: 'bar' = 'bar';
  barChartData: ChartData<'bar'> = {
    labels: [],
    datasets: []
  };

  barChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'x',
    plugins: {
      legend: {
        display: true,
        position: 'top',
        labels: {
          usePointStyle: true,
          padding: 15,
          font: {
            size: 12,
            family: "'Inter', sans-serif"
          }
        }
      },
      tooltip: {
        mode: 'index',
        intersect: false,
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        titleFont: {
          size: 13,
          weight: 'bold'
        },
        bodyFont: {
          size: 12
        },
        borderColor: 'rgba(255, 255, 255, 0.1)',
        borderWidth: 1
      }
    },
    scales: {
      x: {
        grid: {
          display: false
        },
        ticks: {
          font: {
            size: 11
          }
        }
      },
      y: {
        beginAtZero: true,
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)'
        },
        ticks: {
          font: {
            size: 11
          }
        }
      }
    }
  };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data'] && this.data) {
      this.barChartData = this.data;
      this.chart?.update();
    }

    if (changes['horizontal']) {
      this.updateOrientation();
    }

    if (changes['showGrid'] || changes['showLegend']) {
      this.updateOptions();
    }
  }

  private updateOrientation(): void {
    if (this.barChartOptions) {
      this.barChartOptions.indexAxis = this.horizontal ? 'y' : 'x';
      this.chart?.update();
    }
  }

  private updateOptions(): void {
    if (this.barChartOptions) {
      if (this.barChartOptions.plugins?.legend) {
        this.barChartOptions.plugins.legend.display = this.showLegend;
      }

      if (this.barChartOptions.scales) {
        const xGrid = this.barChartOptions.scales['x']?.grid;
        const yGrid = this.barChartOptions.scales['y']?.grid;

        if (xGrid) xGrid.display = this.showGrid;
        if (yGrid) yGrid.display = this.showGrid;
      }

      this.chart?.update();
    }
  }
}
