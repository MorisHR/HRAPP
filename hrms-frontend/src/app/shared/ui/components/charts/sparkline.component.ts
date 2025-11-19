// ═══════════════════════════════════════════════════════════
// SPARKLINE CHART COMPONENT
// Minimal inline charts for dashboard metrics
// ═══════════════════════════════════════════════════════════

import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxEchartsModule } from 'ngx-echarts';
import { EChartsOption } from 'echarts';

@Component({
  selector: 'app-sparkline',
  standalone: true,
  imports: [CommonModule, NgxEchartsModule],
  template: `
    <div class="sparkline-container" [style.height]="height">
      <div echarts [options]="chartOptions" class="chart"></div>
    </div>
  `,
  styles: [`
    .sparkline-container {
      width: 100%;
      height: 100%;
    }

    .chart {
      width: 100%;
      height: 100%;
    }
  `]
})
export class SparklineComponent implements OnInit, OnChanges {
  @Input() data: number[] = [];
  @Input() color: string = '#0F62FE';
  @Input() height: string = '60px';
  @Input() type: 'line' | 'bar' = 'line';

  chartOptions: EChartsOption = {};

  ngOnInit() {
    this.updateChart();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['data']) {
      this.updateChart();
    }
  }

  private updateChart() {
    this.chartOptions = {
      grid: {
        left: 0,
        right: 0,
        top: 0,
        bottom: 0
      },
      xAxis: {
        type: 'category',
        show: false,
        data: this.data.map((_, i) => i.toString())
      },
      yAxis: {
        type: 'value',
        show: false
      },
      series: [{
        data: this.data,
        type: this.type,
        smooth: true,
        symbol: 'none',
        lineStyle: {
          width: 2,
          color: this.color
        },
        areaStyle: this.type === 'line' ? {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: this.hexToRgba(this.color, 0.3) },
              { offset: 1, color: this.hexToRgba(this.color, 0) }
            ]
          }
        } : undefined,
        itemStyle: this.type === 'bar' ? {
          color: this.color,
          borderRadius: [4, 4, 0, 0]
        } : undefined,
        barWidth: '70%'
      }]
    };
  }

  private hexToRgba(hex: string, alpha: number): string {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  }
}
