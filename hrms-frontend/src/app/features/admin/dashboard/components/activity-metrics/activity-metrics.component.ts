import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UiModule } from '../../../../../shared/ui/ui.module';
import { ActivityLog } from '../../../../../core/models/dashboard.model';

interface ActivityMetric {
  label: string;
  value: number;
  icon: string;
  color: 'success' | 'info' | 'warning' | 'error';
  trend?: number; // Percentage change
  subtitle?: string;
}

@Component({
  selector: 'app-activity-metrics',
  standalone: true,
  imports: [CommonModule, UiModule],
  templateUrl: './activity-metrics.component.html',
  styleUrl: './activity-metrics.component.scss'
})
export class ActivityMetricsComponent {
  @Input() set activities(value: ActivityLog[]) {
    this._activities.set(value);
  }

  private _activities = signal<ActivityLog[]>([]);

  // Computed metrics
  metrics = computed((): ActivityMetric[] => {
    const activities = this._activities();

    const totalCount = activities.length;
    const criticalCount = activities.filter(a => a.severity === 'error').length;
    const warningCount = activities.filter(a => a.severity === 'warning').length;
    const successCount = activities.filter(a => a.severity === 'success').length;

    // Calculate today's activities
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const todayCount = activities.filter(a => {
      const activityDate = new Date(a.timestamp);
      activityDate.setHours(0, 0, 0, 0);
      return activityDate.getTime() === today.getTime();
    }).length;

    // Calculate this week's activities
    const weekAgo = new Date();
    weekAgo.setDate(weekAgo.getDate() - 7);
    const weekCount = activities.filter(a => a.timestamp >= weekAgo).length;

    return [
      {
        label: 'Total Events',
        value: totalCount,
        icon: 'event',
        color: 'info',
        subtitle: `${todayCount} today`
      },
      {
        label: 'Critical Alerts',
        value: criticalCount,
        icon: 'error',
        color: 'error',
        subtitle: criticalCount > 0 ? 'Requires attention' : 'All clear'
      },
      {
        label: 'Warnings',
        value: warningCount,
        icon: 'warning',
        color: 'warning',
        subtitle: `${weekCount} this week`
      },
      {
        label: 'Successful',
        value: successCount,
        icon: 'check_circle',
        color: 'success',
        subtitle: `${Math.round((successCount / totalCount) * 100)}% success rate`
      }
    ];
  });
}
