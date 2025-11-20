import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { UiModule } from '../../../../../shared/ui/ui.module';
import { SystemHealthService } from '../../../../../core/services/system-health.service';
import { SystemHealth, ServiceHealth } from '../../../../../core/models/dashboard.model';

@Component({
  selector: 'app-system-health',
  standalone: true,
  imports: [CommonModule, UiModule],
  templateUrl: './system-health.component.html',
  styleUrl: './system-health.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SystemHealthComponent implements OnInit, OnDestroy {
  private healthService = inject(SystemHealthService);
  private subscription?: Subscription;

  // Signals for reactive state
  health = signal<SystemHealth | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadSystemHealth();
    // Poll for updates every 30 seconds
    this.startHealthMonitoring();
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  private loadSystemHealth(): void {
    this.loading.set(true);
    this.healthService.getSystemHealth().subscribe({
      next: (health) => {
        this.health.set(health);
        this.loading.set(false);
        this.error.set(null);
      },
      error: (err) => {
        this.error.set('Failed to load system health');
        this.loading.set(false);
      }
    });
  }

  private startHealthMonitoring(): void {
    this.subscription = this.healthService.startHealthMonitoring(30000).subscribe({
      next: (health) => {
        this.health.set(health);
      }
    });
  }

  getStatusColor(status: 'healthy' | 'degraded' | 'down'): string {
    return this.healthService.getStatusColor(status);
  }

  getStatusIcon(status: 'healthy' | 'degraded' | 'down'): string {
    return this.healthService.getStatusIcon(status);
  }

  getStatusText(status: 'healthy' | 'degraded' | 'down'): string {
    switch (status) {
      case 'healthy': return 'All Systems Operational';
      case 'degraded': return 'Performance Degraded';
      case 'down': return 'System Down';
    }
  }

  refresh(): void {
    this.loadSystemHealth();
  }
}
