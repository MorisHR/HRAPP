import { Component, OnInit, ChangeDetectionStrategy, signal, inject, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { UiModule } from '../../../../../shared/ui/ui.module';
import { ActivityLogService } from '../../../../../core/services/activity-log.service';
import { ActivityLog } from '../../../../../core/models/dashboard.model';

@Component({
  selector: 'app-recent-activity',
  standalone: true,
  imports: [CommonModule, RouterModule, UiModule],
  templateUrl: './recent-activity.component.html',
  styleUrl: './recent-activity.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecentActivityComponent implements OnInit {
  private activityService = inject(ActivityLogService);

  @Input() limit: number = 10;
  @Input() showViewAll: boolean = true;

  // Signals for reactive state
  activities = signal<ActivityLog[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadActivities();
  }

  private loadActivities(): void {
    this.loading.set(true);
    this.activityService.getRecentActivity(this.limit).subscribe({
      next: (activities) => {
        this.activities.set(activities);
        this.loading.set(false);
        this.error.set(null);
      },
      error: (err) => {
        this.error.set('Failed to load recent activity');
        this.loading.set(false);
      }
    });
  }

  getActivityIcon(activity: ActivityLog): string {
    return this.activityService.getActivityIcon(activity.type);
  }

  getSeverityColor(severity: 'info' | 'warning' | 'error' | 'success'): string {
    return this.activityService.getSeverityColor(severity);
  }

  getRelativeTime(date: Date): string {
    return this.activityService.getRelativeTime(date);
  }

  refresh(): void {
    this.loadActivities();
  }
}
