// ═══════════════════════════════════════════════════════════
// BAR CHART COMPONENT
// Fortune 500-grade data visualization for comparisons
// ═══════════════════════════════════════════════════════════

import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxEchartsModule } from 'ngx-echarts';
import { EChartsOption } from 'echarts';

@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [CommonModule, NgxEchartsModule],
  template: `
    <div class="bar-chart-container" [style.height]="height">
      @if (hasData) {
        <div echarts [options]="chartOptions" class="chart"></div>
      } @else {
        <div class="empty-state">
          <div class="empty-icon">📊</div>
          <div class="empty-text">No data available</div>
        </div>
      }
    </div>
  `,
  styles: [`
    .bar-chart-container {
      width: 100%;
      min-height: 200px;
    }

    .chart {
      width: 100%;
      height: 100%;
    }

    .empty-state {
      width: 100%;
      height: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      color: #8D8D8D;
      min-height: 200px;
    }

    .empty-icon {
      font-size: 48px;
      opacity: 0.3;
    }

    .empty-text {
      font-size: 14px;
      font-weight: 500;
    }
  `]
})
export class BarChartComponent implements OnInit, OnChanges {
  @Input() data: number[] = [];
  @Input() labels: string[] = [];
  @Input() color: string = '#0F62FE';
  @Input() height: string = '300px';
  @Input() horizontal: boolean = false;

  chartOptions: EChartsOption = {};

  get hasData(): boolean {
    return this.data.length > 0 && this.labels.length > 0;
  }

  ngOnInit() {
    this.updateChart();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['data'] || changes['labels']) {
      this.updateChart();
    }
  }

  private updateChart() {
    this.chartOptions = {
      tooltip: {
        trigger: 'axis',
        backgroundColor: 'rgba(255, 255, 255, 0.98)',
        borderColor: '#E0E0E0',
        borderWidth: 1,
        borderRadius: 8,
        textStyle: {
          color: '#161616',
          fontSize: 14,
          fontWeight: 500
        },
        padding: [16, 20],
        axisPointer: {
          type: 'shadow',
          shadowStyle: {
            color: 'rgba(15, 98, 254, 0.08)'
          }
        },
        // Format tooltip with proper number formatting
        formatter: (params: any) => {
          const param = Array.isArray(params) ? params[0] : params;
          const value = param.value;
          const formattedValue = this.formatNumber(value);
          return `
            <div style="font-weight: 600; margin-bottom: 8px; color: #161616;">${param.name}</div>
            <div style="display: flex; align-items: center;">
              <span style="display: inline-block; width: 10px; height: 10px; border-radius: 2px; background: ${this.color}; margin-right: 8px;"></span>
              <span style="color: #525252; margin-right: 12px;">Count:</span>
              <span style="font-weight: 600; color: #161616;">${formattedValue}</span>
            </div>
          `;
        }
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '3%',
        top: '10%',
        containLabel: true
      },
      xAxis: this.horizontal ? {
        type: 'value',
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: {
          color: '#6F6F6F',
          fontSize: 12,
          fontWeight: 500,
          // Format X-axis labels with K/M abbreviations
          formatter: (value: number) => this.formatNumber(value)
        },
        splitLine: {
          lineStyle: {
            color: '#F4F4F4',
            width: 1,
            type: 'solid'
          }
        }
      } : {
        type: 'category',
        data: this.labels,
        axisLine: { lineStyle: { color: '#E0E0E0' } },
        axisTick: { show: false },
        axisLabel: {
          color: '#6F6F6F',
          fontSize: 12,
          fontWeight: 500
        }
      },
      yAxis: this.horizontal ? {
        type: 'category',
        data: this.labels,
        axisLine: { lineStyle: { color: '#E0E0E0' } },
        axisTick: { show: false },
        axisLabel: {
          color: '#6F6F6F',
          fontSize: 12,
          fontWeight: 500
        }
      } : {
        type: 'value',
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: {
          color: '#6F6F6F',
          fontSize: 12,
          fontWeight: 500,
          // Format Y-axis labels with K/M abbreviations
          formatter: (value: number) => this.formatNumber(value)
        },
        splitLine: {
          lineStyle: {
            color: '#F4F4F4',
            width: 1,
            type: 'solid'
          }
        }
      },
      series: [{
        data: this.data,
        type: 'bar',
        barWidth: '50%',
        itemStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: this.horizontal ? 1 : 0,
            y2: this.horizontal ? 0 : 1,
            colorStops: [
              { offset: 0, color: this.color },
              { offset: 1, color: this.adjustBrightness(this.color, -12) }
            ]
          },
          borderRadius: this.horizontal ? [0, 8, 8, 0] : [8, 8, 0, 0],
          shadowBlur: 4,
          shadowColor: 'rgba(0, 0, 0, 0.08)',
          shadowOffsetY: 2
        },
        emphasis: {
          itemStyle: {
            shadowBlur: 16,
            shadowColor: 'rgba(0, 0, 0, 0.25)',
            shadowOffsetY: 4
          }
        },
        // Smooth animation on load
        animation: true,
        animationDuration: 1000,
        animationEasing: 'cubicOut',
        animationDelay: (idx: number) => idx * 50 // Stagger bars
      }]
    };
  }

  // Format numbers with K/M abbreviations
  private formatNumber(value: number): string {
    if (value >= 1000000) {
      return `${(value / 1000000).toFixed(1)}M`;
    } else if (value >= 1000) {
      return `${(value / 1000).toFixed(1)}K`;
    }
    return value.toFixed(0);
  }

  private adjustBrightness(color: string, percent: number): string {
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) + amt;
    const G = (num >> 8 & 0x00FF) + amt;
    const B = (num & 0x0000FF) + amt;
    return '#' + (0x1000000 + (R<255?R<1?0:R:255)*0x10000
      + (G<255?G<1?0:G:255)*0x100
      + (B<255?B<1?0:B:255))
      .toString(16)
      .slice(1);
  }
}
