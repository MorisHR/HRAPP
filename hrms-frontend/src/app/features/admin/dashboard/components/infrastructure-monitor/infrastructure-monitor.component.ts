import { Component, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UiModule } from '../../../../../shared/ui/ui.module';
import { InfrastructureMetricsService } from '../../../../../core/services/infrastructure-metrics.service';
import { InfrastructureMetrics } from '../../../../../core/models/dashboard.model';

@Component({
  selector: 'app-infrastructure-monitor',
  standalone: true,
  imports: [CommonModule, UiModule],
  templateUrl: './infrastructure-monitor.component.html',
  styleUrl: './infrastructure-monitor.component.scss'
})
export class InfrastructureMonitorComponent implements OnInit, OnDestroy {
  private infrastructureService = inject(InfrastructureMetricsService);

  metrics = signal<InfrastructureMetrics | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  selectedTab = signal<'overview' | 'database' | 'api' | 'cache' | 'cdn' | 'jobs'>('overview');

  // Auto-refresh interval
  private refreshInterval?: number;

  // Computed health status
  overallHealth = computed(() => {
    const m = this.metrics();
    if (!m) return 'unknown';

    const dbHealth = m.databasePerformance.overallHealth;
    const cacheHealth = m.cacheMetrics.health;
    const queueHealth = m.queueMetrics.failedJobs > 20 ? 'critical' : 'healthy';

    if (dbHealth === 'critical' || cacheHealth === 'critical' || queueHealth === 'critical') {
      return 'critical';
    }
    if (dbHealth === 'degraded' || cacheHealth === 'degraded') {
      return 'degraded';
    }
    return 'healthy';
  });

  ngOnInit(): void {
    this.loadMetrics();
    this.setupAutoRefresh();
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  private loadMetrics(): void {
    this.loading.set(true);
    this.infrastructureService.getInfrastructureMetrics().subscribe({
      next: (metrics) => {
        this.metrics.set(metrics);
        this.loading.set(false);
        this.error.set(null);
      },
      error: (err) => {
        this.error.set('Failed to load infrastructure metrics');
        this.loading.set(false);
      }
    });
  }

  private setupAutoRefresh(): void {
    // Auto-refresh every 60 seconds
    this.refreshInterval = window.setInterval(() => {
      if (!this.loading()) {
        this.loadMetrics();
      }
    }, 60000);
  }

  refresh(): void {
    this.loadMetrics();
  }

  selectTab(tab: 'overview' | 'database' | 'api' | 'cache' | 'cdn' | 'jobs'): void {
    this.selectedTab.set(tab);
  }

  getHealthColor(health: 'healthy' | 'degraded' | 'critical' | 'warning' | 'unknown'): string {
    const colorMap = {
      healthy: 'success',
      degraded: 'warning',
      critical: 'error',
      warning: 'warning',
      unknown: 'secondary'
    };
    return colorMap[health];
  }

  getHealthIcon(health: 'healthy' | 'degraded' | 'critical' | 'warning' | 'unknown'): string {
    const iconMap = {
      healthy: 'check_circle',
      degraded: 'warning',
      critical: 'error',
      warning: 'warning',
      unknown: 'help'
    };
    return iconMap[health];
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  formatNumber(num: number): string {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    }
    if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
  }

  formatDuration(ms: number): string {
    if (ms < 1000) return `${ms.toFixed(0)}ms`;
    if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`;
    if (ms < 3600000) return `${(ms / 60000).toFixed(1)}m`;
    return `${(ms / 3600000).toFixed(1)}h`;
  }

  formatPercent(value: number): string {
    return `${value.toFixed(1)}%`;
  }
}
