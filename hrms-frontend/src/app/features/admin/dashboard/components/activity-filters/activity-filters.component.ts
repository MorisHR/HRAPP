import { Component, output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { UiModule } from '../../../../../shared/ui/ui.module';
import { ActivityType } from '../../../../../core/models/dashboard.model';

export interface ActivityFilters {
  types: ActivityType[];
  severities: string[];
  searchQuery: string;
  dateRange?: { start: Date; end: Date };
}

@Component({
  selector: 'app-activity-filters',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule,
    UiModule
  ],
  templateUrl: './activity-filters.component.html',
  styleUrl: './activity-filters.component.scss'
})
export class ActivityFiltersComponent {
  // Events
  filtersChanged = output<ActivityFilters>();
  exportRequested = output<'csv' | 'pdf'>();

  // Filter state
  selectedTypes = signal<ActivityType[]>([]);
  selectedSeverities = signal<string[]>([]);
  searchQuery = signal('');

  // Computed filters
  activeFilters = computed(() => {
    const filters = [];
    if (this.selectedTypes().length > 0) {
      filters.push(`${this.selectedTypes().length} type${this.selectedTypes().length > 1 ? 's' : ''}`);
    }
    if (this.selectedSeverities().length > 0) {
      filters.push(`${this.selectedSeverities().length} severity${this.selectedSeverities().length > 1 ? 'ies' : ''}`);
    }
    if (this.searchQuery()) {
      filters.push('search');
    }
    return filters;
  });

  hasActiveFilters = computed(() => this.activeFilters().length > 0);

  // Available options
  readonly activityTypes = [
    { value: ActivityType.TenantCreated, label: 'Tenant Created', icon: 'business' },
    { value: ActivityType.TenantSuspended, label: 'Tenant Suspended', icon: 'block' },
    { value: ActivityType.TenantUpgraded, label: 'Tenant Upgraded', icon: 'upgrade' },
    { value: ActivityType.TenantDowngraded, label: 'Tenant Downgraded', icon: 'downgrade' },
    { value: ActivityType.SecurityAlert, label: 'Security Alert', icon: 'security' },
    { value: ActivityType.PaymentFailed, label: 'Payment Failed', icon: 'payment' },
    { value: ActivityType.PaymentSuccess, label: 'Payment Success', icon: 'check_circle' },
    { value: ActivityType.UserLogin, label: 'User Login', icon: 'login' },
    { value: ActivityType.UserLogout, label: 'User Logout', icon: 'logout' },
    { value: ActivityType.SystemError, label: 'System Error', icon: 'error' },
    { value: ActivityType.BackupCompleted, label: 'Backup Completed', icon: 'backup' },
    { value: ActivityType.MaintenanceScheduled, label: 'Maintenance Scheduled', icon: 'schedule' }
  ];

  readonly severityOptions = [
    { value: 'success', label: 'Success', icon: 'check_circle', color: 'success' },
    { value: 'info', label: 'Info', icon: 'info', color: 'info' },
    { value: 'warning', label: 'Warning', icon: 'warning', color: 'warning' },
    { value: 'error', label: 'Error', icon: 'error', color: 'error' }
  ];

  onTypeSelectionChange(types: ActivityType[]): void {
    this.selectedTypes.set(types);
    this.emitFilters();
  }

  onSeveritySelectionChange(severities: string[]): void {
    this.selectedSeverities.set(severities);
    this.emitFilters();
  }

  onSearchChange(query: string): void {
    this.searchQuery.set(query);
    this.emitFilters();
  }

  clearFilters(): void {
    this.selectedTypes.set([]);
    this.selectedSeverities.set([]);
    this.searchQuery.set('');
    this.emitFilters();
  }

  private emitFilters(): void {
    this.filtersChanged.emit({
      types: this.selectedTypes(),
      severities: this.selectedSeverities(),
      searchQuery: this.searchQuery()
    });
  }

  exportCSV(): void {
    this.exportRequested.emit('csv');
  }

  exportPDF(): void {
    this.exportRequested.emit('pdf');
  }
}
