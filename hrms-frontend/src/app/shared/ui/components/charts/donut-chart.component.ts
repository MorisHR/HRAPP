// ═══════════════════════════════════════════════════════════
// DONUT CHART COMPONENT
// Fortune 500-grade data visualization for distributions
// ═══════════════════════════════════════════════════════════

import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxEchartsModule } from 'ngx-echarts';
import { EChartsOption } from 'echarts';

export interface DonutChartData {
  name: string;
  value: number;
  color?: string;
}

@Component({
  selector: 'app-donut-chart',
  standalone: true,
  imports: [CommonModule, NgxEchartsModule],
  template: `
    <div class="donut-chart-container" [style.height]="height">
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
    .donut-chart-container {
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
export class DonutChartComponent implements OnInit, OnChanges {
  @Input() data: DonutChartData[] = [];
  @Input() height: string = '300px';
  @Input() showLegend: boolean = true;
  @Input() centerText?: string;

  chartOptions: EChartsOption = {};

  get hasData(): boolean {
    return this.data.length > 0;
  }

  ngOnInit() {
    this.updateChart();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['data']) {
      this.updateChart();
    }
  }

  private updateChart() {
    const colors = ['#0F62FE', '#8A3FFC', '#009D9A', '#FF7EB6', '#F1C21B', '#24A148', '#FF832B', '#DA1E28'];

    const total = this.data.reduce((sum, item) => sum + item.value, 0);

    this.chartOptions = {
      tooltip: {
        trigger: 'item',
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
        // Format tooltip with proper number formatting and rich styling
        formatter: (params: any) => {
          const value = params.value;
          const percent = params.percent;
          const formattedValue = this.formatNumber(value);
          return `
            <div style="font-weight: 600; margin-bottom: 8px; color: #161616;">${params.name}</div>
            <div style="display: flex; align-items: center; margin: 6px 0;">
              <span style="display: inline-block; width: 10px; height: 10px; border-radius: 50%; background: ${params.color}; margin-right: 8px;"></span>
              <span style="color: #525252; margin-right: 12px;">Count:</span>
              <span style="font-weight: 600; color: #161616;">${formattedValue}</span>
            </div>
            <div style="color: #525252; font-size: 12px; margin-top: 4px;">
              ${percent.toFixed(1)}% of total
            </div>
          `;
        }
      },
      legend: this.showLegend ? {
        orient: 'vertical',
        right: '10%',
        top: 'center',
        textStyle: {
          color: '#525252',
          fontSize: 13,
          fontWeight: 500
        },
        itemWidth: 14,
        itemHeight: 14,
        icon: 'circle',
        itemGap: 16
      } : undefined,
      series: [{
        type: 'pie',
        radius: ['50%', '70%'],
        center: this.showLegend ? ['35%', '50%'] : ['50%', '50%'],
        avoidLabelOverlap: false,
        itemStyle: {
          borderRadius: 8,
          borderColor: '#fff',
          borderWidth: 3,
          shadowBlur: 4,
          shadowColor: 'rgba(0, 0, 0, 0.08)',
          shadowOffsetY: 2
        },
        label: {
          show: false
        },
        emphasis: {
          scale: true,
          scaleSize: 10,
          label: {
            show: true,
            fontSize: 18,
            fontWeight: 'bold',
            color: '#161616',
            formatter: (params: any) => {
              const percent = params.percent;
              return `${percent.toFixed(1)}%`;
            }
          },
          itemStyle: {
            shadowBlur: 20,
            shadowOffsetX: 0,
            shadowOffsetY: 4,
            shadowColor: 'rgba(0, 0, 0, 0.25)'
          }
        },
        labelLine: {
          show: false
        },
        // Smooth animation on load
        animation: true,
        animationDuration: 1200,
        animationEasing: 'cubicOut',
        animationDelay: (idx: number) => idx * 100,
        data: this.data.map((item, index) => ({
          value: item.value,
          name: item.name,
          itemStyle: {
            color: item.color || colors[index % colors.length]
          }
        }))
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
}
