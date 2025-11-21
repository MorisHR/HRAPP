import { Component, OnInit, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { UiModule } from '../../../../../shared/ui/ui.module';
import { AdminDashboardService } from '../../../../../core/services/admin-dashboard.service';
import { CriticalAlert } from '../../../../../core/models/dashboard.model';

@Component({
  selector: 'app-critical-alerts',
  standalone: true,
  imports: [CommonModule, RouterModule, UiModule],
  templateUrl: './critical-alerts.component.html',
  styleUrl: './critical-alerts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CriticalAlertsComponent implements OnInit {
  private dashboardService = inject(AdminDashboardService);

  // Signals for reactive state
  alerts = signal<CriticalAlert[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadAlerts();
  }

  private loadAlerts(): void {
    this.loading.set(true);
    this.dashboardService.getCriticalAlerts().subscribe({
      next: (alerts) => {
        this.alerts.set(alerts);
        this.loading.set(false);
        this.error.set(null);
      },
      error: (err) => {
        this.error.set('Failed to load alerts');
        this.loading.set(false);
      }
    });
  }

  getSeverityColor(severity: 'critical' | 'high' | 'medium' | 'low'): string {
    return this.dashboardService.getAlertSeverityColor(severity);
  }

  getSeverityIcon(severity: 'critical' | 'high' | 'medium' | 'low'): string {
    return this.dashboardService.getAlertSeverityIcon(severity);
  }

  acknowledgeAlert(alert: CriticalAlert): void {
    this.dashboardService.acknowledgeAlert(alert.id).subscribe({
      next: () => {
        this.loadAlerts();
      }
    });
  }

  resolveAlert(alert: CriticalAlert): void {
    this.dashboardService.resolveAlert(alert.id).subscribe({
      next: () => {
        this.loadAlerts();
      }
    });
  }

  getRelativeTime(date: Date | string): string {
    const now = new Date();
    // Convert to Date object if it's a string
    const dateObj = typeof date === 'string' ? new Date(date) : date;

    // Validate that we have a valid date
    if (!dateObj || isNaN(dateObj.getTime())) {
      return 'Unknown';
    }

    const diffMs = now.getTime() - dateObj.getTime();
    const diffHours = Math.floor(diffMs / 3600000);

    if (diffHours < 1) return 'Just now';
    if (diffHours === 1) return '1 hour ago';
    if (diffHours < 24) return `${diffHours} hours ago`;

    const diffDays = Math.floor(diffHours / 24);
    if (diffDays === 1) return '1 day ago';
    return `${diffDays} days ago`;
  }

  refresh(): void {
    this.loadAlerts();
  }

  handleAlertAction(alert: CriticalAlert, action: { action: string; label: string; primary?: boolean }): void {
    this.dashboardService.handleAlertAction(alert.id, action.action).subscribe({
      next: () => {
        this.loadAlerts();
      },
      error: (err) => {
        console.error('Failed to handle alert action:', err);
        this.error.set(`Failed to ${action.label.toLowerCase()}`);
      }
    });
  }
}
