import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, signal, inject, Input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { UiModule } from '../../../../../shared/ui/ui.module';
import { ActivityLogService } from '../../../../../core/services/activity-log.service';
import { ActivityReadTrackerService } from '../../../../../core/services/activity-read-tracker.service';
import { ActivityExportService } from '../../../../../core/services/activity-export.service';
import { ActivityLog, ActivityType } from '../../../../../core/models/dashboard.model';
import { ActivityDetailModalComponent } from '../activity-detail-modal/activity-detail-modal.component';
import { ActivityFiltersComponent, ActivityFilters } from '../activity-filters/activity-filters.component';
import { ActivityMetricsComponent } from '../activity-metrics/activity-metrics.component';

@Component({
  selector: 'app-recent-activity',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatDialogModule,
    ScrollingModule,
    UiModule,
    ActivityFiltersComponent,
    ActivityMetricsComponent
  ],
  templateUrl: './recent-activity.component.html',
  styleUrl: './recent-activity.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecentActivityComponent implements OnInit, OnDestroy {
  private activityService = inject(ActivityLogService);
  private readTracker = inject(ActivityReadTrackerService);
  private exportService = inject(ActivityExportService);
  private dialog = inject(MatDialog);
  private router = inject(Router);

  @Input() limit: number = 50;
  @Input() showViewAll: boolean = true;
  @Input() showFilters: boolean = true;
  @Input() showMetrics: boolean = true;
  @Input() enableVirtualScroll: boolean = true;

  // Signals for reactive state
  private allActivities = signal<ActivityLog[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  currentFilters = signal<ActivityFilters>({
    types: [],
    severities: [],
    searchQuery: ''
  });

  // Computed filtered activities
  filteredActivities = computed(() => {
    const activities = this.allActivities();
    const filters = this.currentFilters();

    let filtered = activities;

    // Filter by type
    if (filters.types.length > 0) {
      filtered = filtered.filter(a => filters.types.includes(a.type));
    }

    // Filter by severity
    if (filters.severities.length > 0) {
      filtered = filtered.filter(a => filters.severities.includes(a.severity));
    }

    // Filter by search query
    if (filters.searchQuery) {
      const query = filters.searchQuery.toLowerCase();
      filtered = filtered.filter(a =>
        a.title.toLowerCase().includes(query) ||
        a.description.toLowerCase().includes(query) ||
        a.tenantName?.toLowerCase().includes(query) ||
        a.userName?.toLowerCase().includes(query)
      );
    }

    return filtered;
  });

  // Alias for backward compatibility
  activities = this.filteredActivities;

  // Unread count
  unreadCount = computed(() => {
    const allIds = this.filteredActivities().map(a => a.id);
    return this.readTracker.getUnreadCount(allIds);
  });

  // Auto-refresh interval
  private refreshInterval?: number;

  ngOnInit(): void {
    this.loadActivities();
    this.setupAutoRefresh();
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  private loadActivities(): void {
    this.loading.set(true);
    this.activityService.getRecentActivity(this.limit).subscribe({
      next: (activities) => {
        this.allActivities.set(activities);
        this.loading.set(false);
        this.error.set(null);
      },
      error: (err) => {
        this.error.set('Failed to load recent activity');
        this.loading.set(false);
      }
    });
  }

  private setupAutoRefresh(): void {
    // Auto-refresh every 30 seconds
    this.refreshInterval = window.setInterval(() => {
      if (!this.loading()) {
        this.loadActivities();
      }
    }, 30000);
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

  isUnread(activityId: string): boolean {
    return !this.readTracker.isRead(activityId);
  }

  refresh(): void {
    this.loadActivities();
  }

  onFiltersChanged(filters: ActivityFilters): void {
    this.currentFilters.set(filters);
  }

  onExportRequested(format: 'csv' | 'pdf'): void {
    const activities = this.filteredActivities();
    const filterDesc = this.getFilterDescription();

    if (format === 'csv') {
      this.exportService.exportToCSV(activities, `activity-log-${filterDesc}`);
    } else {
      this.exportService.exportToPDF(activities, `activity-log-${filterDesc}`);
    }
  }

  private getFilterDescription(): string {
    const filters = this.currentFilters();
    const parts: string[] = [];

    if (filters.types.length > 0) {
      parts.push(`types-${filters.types.length}`);
    }
    if (filters.severities.length > 0) {
      parts.push(`severity-${filters.severities.join('-')}`);
    }
    if (filters.searchQuery) {
      parts.push('filtered');
    }

    return parts.length > 0 ? parts.join('_') : 'all';
  }

  openActivityDetail(activity: ActivityLog): void {
    // Mark as read
    this.readTracker.markAsRead(activity.id);

    // Open modal
    this.dialog.open(ActivityDetailModalComponent, {
      data: activity,
      width: '700px',
      maxWidth: '90vw',
      maxHeight: '90vh',
      panelClass: 'activity-detail-dialog',
      autoFocus: false
    });
  }

  markAllAsRead(): void {
    const allIds = this.filteredActivities().map(a => a.id);
    this.readTracker.markAllAsRead(allIds);
  }

  trackByActivityId(index: number, activity: ActivityLog): string {
    return activity.id;
  }

  /**
   * Navigate to tenant detail page
   */
  navigateToTenant(tenantId: string): void {
    this.router.navigate(['/admin/tenant-management'], {
      queryParams: { tenantId }
    });
  }

  /**
   * Check if activity can be dismissed
   */
  canDismiss(activity: ActivityLog): boolean {
    // Only allow dismissing info and success activities
    // Critical items (errors/warnings) should remain visible
    return activity.severity === 'info' || activity.severity === 'success';
  }

  /**
   * Dismiss an activity (mark as read and hide)
   */
  dismissActivity(activity: ActivityLog): void {
    // Mark as read
    this.readTracker.markAsRead(activity.id);

    // Remove from the list
    const currentActivities = this.allActivities();
    const filtered = currentActivities.filter(a => a.id !== activity.id);
    this.allActivities.set(filtered);
  }
}
