import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// Custom UI Components
import { CardComponent } from '../../../../shared/ui/components/card/card';
import { SortEvent } from '../../../../shared/ui/components/table/table';
import { Badge } from '../../../../shared/ui/components/badge/badge';
import { ButtonComponent } from '../../../../shared/ui/components/button/button';
import { SelectComponent, SelectOption } from '../../../../shared/ui/components/select/select';
import { Paginator, PageEvent } from '../../../../shared/ui/components/paginator/paginator';

// Services
import { MonitoringService } from '../../../../core/services/monitoring.service';
import { Alert } from '../../../../core/models/monitoring.models';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardComponent,
    Badge,
    ButtonComponent,
    SelectComponent,
    Paginator
  ],
  templateUrl: './alerts.component.html',
  styleUrl: './alerts.component.scss'
})
export class AlertsComponent implements OnInit {
  private monitoringService = inject(MonitoringService);

  // State
  alerts = signal<Alert[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  selectedAlert = signal<Alert | null>(null);
  showAlertDialog = signal(false);
  resolutionNotes = signal('');

  // Filters
  selectedStatus = signal<string | null>(null);
  selectedSeverity = signal<string | null>(null);
  selectedType = signal<string | null>(null);
  selectedTenant = signal<string | null>(null);

  statusOptions: SelectOption[] = [
    { value: null, label: 'All Status' },
    { value: 'Active', label: 'Active' },
    { value: 'Acknowledged', label: 'Acknowledged' },
    { value: 'Resolved', label: 'Resolved' }
  ];

  severityOptions: SelectOption[] = [
    { value: null, label: 'All Severities' },
    { value: 'Critical', label: 'Critical' },
    { value: 'High', label: 'High' },
    { value: 'Medium', label: 'Medium' },
    { value: 'Low', label: 'Low' }
  ];

  typeOptions: SelectOption[] = [
    { value: null, label: 'All Types' },
    { value: 'Performance', label: 'Performance' },
    { value: 'Security', label: 'Security' },
    { value: 'Capacity', label: 'Capacity' },
    { value: 'Availability', label: 'Availability' },
    { value: 'Compliance', label: 'Compliance' }
  ];

  tenantOptions: SelectOption[] = [
    { value: null, label: 'All Tenants' },
    { value: 'acme', label: 'Acme Corporation' },
    { value: 'techstart', label: 'TechStart Inc' },
    { value: 'enterprise-co', label: 'Enterprise Co' }
  ];

  // Pagination
  pageSize = signal(10);
  pageIndex = signal(0);

  // Sorting
  sortKey = signal<string | null>('triggeredAt');
  sortDirection = signal<'asc' | 'desc'>('desc');

  // Computed values
  filteredAlerts = computed(() => {
    let filtered = [...this.alerts()];

    if (this.selectedStatus()) {
      filtered = filtered.filter(a => a.status === this.selectedStatus());
    }

    if (this.selectedSeverity()) {
      filtered = filtered.filter(a => a.severity === this.selectedSeverity());
    }

    if (this.selectedType()) {
      filtered = filtered.filter(a => a.alertType === this.selectedType());
    }

    if (this.selectedTenant()) {
      filtered = filtered.filter(a =>
        a.tenantSubdomain === this.selectedTenant()
      );
    }

    return filtered;
  });

  sortedAlerts = computed(() => {
    const alerts = [...this.filteredAlerts()];
    const key = this.sortKey();
    const direction = this.sortDirection();

    if (!key) return alerts;

    return alerts.sort((a: any, b: any) => {
      const aVal = a[key];
      const bVal = b[key];

      if (aVal instanceof Date && bVal instanceof Date) {
        return direction === 'asc'
          ? aVal.getTime() - bVal.getTime()
          : bVal.getTime() - aVal.getTime();
      }

      if (typeof aVal === 'string') {
        return direction === 'asc'
          ? aVal.localeCompare(bVal)
          : bVal.localeCompare(aVal);
      }

      return direction === 'asc' ? aVal - bVal : bVal - aVal;
    });
  });

  paginatedAlerts = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    const end = start + this.pageSize();
    return this.sortedAlerts().slice(start, end);
  });

  totalAlerts = computed(() => this.filteredAlerts().length);

  activeAlerts = computed(() => {
    return this.alerts().filter(a =>
      a.status === 'Active' && (a.severity === 'Critical' || a.severity === 'High')
    );
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    const filters = {
      status: this.selectedStatus() || undefined,
      severity: this.selectedSeverity() || undefined,
      type: this.selectedType() || undefined
    };

    this.monitoringService.getAlerts(filters).subscribe({
      next: (data) => {
        this.alerts.set(data);
        this.loading.set(false);
      },
      error: (err: Error) => {
        console.error('Error loading alerts:', err);
        this.error.set('Failed to load alerts');
        this.loading.set(false);
      }
    });
  }

  onStatusChange(value: string | null): void {
    this.selectedStatus.set(value);
    this.pageIndex.set(0);
  }

  onSeverityChange(value: string | null): void {
    this.selectedSeverity.set(value);
    this.pageIndex.set(0);
  }

  onTypeChange(value: string | null): void {
    this.selectedType.set(value);
    this.pageIndex.set(0);
  }

  onTenantChange(value: string | null): void {
    this.selectedTenant.set(value);
    this.pageIndex.set(0);
  }

  onSortChange(event: SortEvent): void {
    this.sortKey.set(event.key);
    this.sortDirection.set(event.direction);
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  onRowClick(alert: Alert): void {
    this.selectedAlert.set(alert);
    this.resolutionNotes.set('');
    this.showAlertDialog.set(true);
  }

  closeDialog(): void {
    this.showAlertDialog.set(false);
    this.selectedAlert.set(null);
    this.resolutionNotes.set('');
  }

  acknowledgeAlert(): void {
    const alert = this.selectedAlert();
    if (!alert) return;

    this.monitoringService.acknowledgeAlert(alert.alertId).subscribe({
      next: () => {
        // Update the alert in the list
        const alerts = this.alerts();
        const index = alerts.findIndex(a => a.alertId === alert.alertId);
        if (index !== -1) {
          alerts[index].status = 'Acknowledged';
          this.alerts.set([...alerts]);
        }
        this.closeDialog();
      },
      error: (err: Error) => {
        console.error('Error acknowledging alert:', err);
        window.alert('Failed to acknowledge alert');
      }
    });
  }

  resolveAlert(): void {
    const alert = this.selectedAlert();
    if (!alert) return;

    this.monitoringService.resolveAlert(alert.alertId, this.resolutionNotes()).subscribe({
      next: () => {
        // Update the alert in the list
        const alerts = this.alerts();
        const index = alerts.findIndex(a => a.alertId === alert.alertId);
        if (index !== -1) {
          alerts[index].status = 'Resolved';
          this.alerts.set([...alerts]);
        }
        this.closeDialog();
      },
      error: (err: Error) => {
        console.error('Error resolving alert:', err);
        window.alert('Failed to resolve alert');
      }
    });
  }

  exportToCSV(): void {
    console.log('Exporting alerts to CSV...');
    alert('CSV export functionality coming soon!');
  }

  getSeverityColor(severity: string): 'error' | 'warning' | 'primary' {
    switch (severity) {
      case 'Critical': return 'error';
      case 'High': return 'error';
      case 'Medium': return 'warning';
      case 'Low': return 'primary';
      default: return 'primary';
    }
  }

  getStatusColor(status: string): 'error' | 'warning' | 'success' {
    switch (status) {
      case 'Active': return 'error';
      case 'Acknowledged': return 'warning';
      case 'Resolved': return 'success';
      default: return 'primary' as any;
    }
  }

  getTypeColor(type: string): 'error' | 'warning' | 'primary' {
    switch (type) {
      case 'Performance': return 'warning';
      case 'Security': return 'error';
      case 'Capacity': return 'primary';
      case 'Availability': return 'error';
      case 'Compliance': return 'primary';
      default: return 'primary';
    }
  }

  formatTimestamp(date: Date): string {
    return new Date(date).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatDuration(minutes: number): string {
    if (minutes < 60) {
      return `${minutes}m`;
    } else if (minutes < 1440) {
      const hours = Math.floor(minutes / 60);
      const mins = minutes % 60;
      return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
    } else {
      const days = Math.floor(minutes / 1440);
      const hours = Math.floor((minutes % 1440) / 60);
      return hours > 0 ? `${days}d ${hours}h` : `${days}d`;
    }
  }
}
