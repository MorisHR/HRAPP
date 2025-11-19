// ═══════════════════════════════════════════════════════════
// LINE CHART COMPONENT
// Fortune 500-grade data visualization for trends
// ═══════════════════════════════════════════════════════════

import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxEchartsModule } from 'ngx-echarts';
import { EChartsOption } from 'echarts';

export interface LineChartData {
  name: string;
  data: number[];
  color?: string;
}

@Component({
  selector: 'app-line-chart',
  standalone: true,
  imports: [CommonModule, NgxEchartsModule],
  template: `
    <div class="line-chart-container">
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
    .line-chart-container {
      width: 100%;
      height: 100%;
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
export class LineChartComponent implements OnInit, OnChanges {
  @Input() data: LineChartData[] = [];
  @Input() labels: string[] = [];
  @Input() height: string = '300px';
  @Input() showLegend: boolean = true;
  @Input() showGrid: boolean = true;
  @Input() smooth: boolean = true;

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
          type: 'line',
          lineStyle: {
            color: '#0F62FE',
            width: 2,
            type: 'solid',
            opacity: 0.3
          }
        },
        // Format tooltip with proper number formatting
        formatter: (params: any) => {
          if (!Array.isArray(params)) params = [params];
          let result = `<div style="font-weight: 600; margin-bottom: 8px; color: #161616;">${params[0].axisValueLabel}</div>`;
          params.forEach((param: any) => {
            const value = param.value;
            const formattedValue = this.formatNumber(value);
            result += `
              <div style="display: flex; align-items: center; margin: 6px 0;">
                <span style="display: inline-block; width: 10px; height: 10px; border-radius: 50%; background: ${param.color}; margin-right: 8px;"></span>
                <span style="color: #525252; margin-right: 12px;">${param.seriesName}:</span>
                <span style="font-weight: 600; color: #161616;">${formattedValue}</span>
              </div>
            `;
          });
          return result;
        }
      },
      legend: this.showLegend ? {
        bottom: 0,
        textStyle: {
          color: '#525252',
          fontSize: 13
        },
        itemWidth: 20,
        itemHeight: 12,
        icon: 'roundRect'
      } : undefined,
      grid: {
        left: '3%',
        right: '4%',
        bottom: this.showLegend ? '15%' : '3%',
        top: '10%',
        containLabel: true
      },
      xAxis: {
        type: 'category',
        data: this.labels,
        boundaryGap: false,
        axisLine: {
          lineStyle: {
            color: '#E0E0E0'
          }
        },
        axisLabel: {
          color: '#6F6F6F',
          fontSize: 12
        },
        splitLine: {
          show: false
        }
      },
      yAxis: {
        type: 'value',
        axisLine: {
          show: false
        },
        axisTick: {
          show: false
        },
        axisLabel: {
          color: '#6F6F6F',
          fontSize: 12,
          fontWeight: 500,
          // Format Y-axis labels with K/M abbreviations
          formatter: (value: number) => this.formatNumber(value)
        },
        splitLine: {
          show: this.showGrid,
          lineStyle: {
            color: '#F4F4F4',
            width: 1,
            type: 'solid'
          }
        }
      },
      series: this.data.map((series, index) => ({
        name: series.name,
        type: 'line',
        data: series.data,
        smooth: this.smooth,
        symbol: 'circle',
        symbolSize: 8,
        showSymbol: false, // Only show on hover
        lineStyle: {
          width: 3,
          color: series.color || this.getDefaultColor(index),
          shadowBlur: 4,
          shadowColor: 'rgba(0, 0, 0, 0.1)',
          shadowOffsetY: 2
        },
        itemStyle: {
          color: series.color || this.getDefaultColor(index),
          borderWidth: 3,
          borderColor: '#FFFFFF'
        },
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              {
                offset: 0,
                color: this.hexToRgba(series.color || this.getDefaultColor(index), 0.2)
              },
              {
                offset: 1,
                color: this.hexToRgba(series.color || this.getDefaultColor(index), 0.02)
              }
            ]
          }
        },
        emphasis: {
          focus: 'series',
          scale: true,
          itemStyle: {
            shadowBlur: 20,
            shadowColor: this.hexToRgba(series.color || this.getDefaultColor(index), 0.5),
            borderWidth: 4
          }
        },
        // Smooth animation on load
        animation: true,
        animationDuration: 1000,
        animationEasing: 'cubicOut'
      }))
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

  private getDefaultColor(index: number): string {
    const colors = ['#0F62FE', '#8A3FFC', '#009D9A', '#FF7EB6', '#F1C21B', '#24A148', '#FF832B', '#DA1E28'];
    return colors[index % colors.length];
  }

  private hexToRgba(hex: string, alpha: number): string {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  }
}
