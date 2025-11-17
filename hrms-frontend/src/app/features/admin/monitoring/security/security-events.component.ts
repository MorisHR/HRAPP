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
import { Toggle } from '../../../../shared/ui/components/toggle/toggle';

// Services
import { MonitoringService } from '../../../../core/services/monitoring.service';
import { SecurityEvent } from '../../../../core/models/monitoring.models';

@Component({
  selector: 'app-security-events',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardComponent,
    Badge,
    ButtonComponent,
    SelectComponent,
    Paginator,
    Toggle
  ],
  templateUrl: './security-events.component.html',
  styleUrl: './security-events.component.scss'
})
export class SecurityEventsComponent implements OnInit {
  private monitoringService = inject(MonitoringService);

  // State
  events = signal<SecurityEvent[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  selectedEvent = signal<SecurityEvent | null>(null);
  showEventDialog = signal(false);
  reviewNotes = signal('');

  // Filters
  selectedEventType = signal<string | null>(null);
  selectedSeverity = signal<string | null>(null);
  selectedTenant = signal<string | null>(null);
  showReviewedOnly = signal(false);
  showUnreviewedOnly = signal(false);

  eventTypeOptions: SelectOption[] = [
    { value: null, label: 'All Event Types' },
    { value: 'failed-login', label: 'Failed Login' },
    { value: 'suspicious-ip', label: 'Suspicious IP' },
    { value: 'privilege-escalation', label: 'Privilege Escalation' },
    { value: 'data-export', label: 'Data Export' },
    { value: 'brute-force', label: 'Brute Force' }
  ];

  severityOptions: SelectOption[] = [
    { value: null, label: 'All Severities' },
    { value: 'Critical', label: 'Critical' },
    { value: 'High', label: 'High' },
    { value: 'Medium', label: 'Medium' },
    { value: 'Low', label: 'Low' }
  ];

  tenantOptions: SelectOption[] = [
    { value: null, label: 'All Tenants' },
    { value: 'acme', label: 'Acme Corporation' },
    { value: 'techstart', label: 'TechStart Inc' },
    { value: 'enterprise-co', label: 'Enterprise Co' },
    { value: 'smallbiz', label: 'Small Business LLC' }
  ];

  // Pagination
  pageSize = signal(10);
  pageIndex = signal(0);

  // Sorting
  sortKey = signal<string | null>('timestamp');
  sortDirection = signal<'asc' | 'desc'>('desc');

  // Computed values
  filteredEvents = computed(() => {
    let filtered = [...this.events()];

    if (this.selectedEventType()) {
      filtered = filtered.filter(e => e.eventType === this.selectedEventType());
    }

    if (this.selectedSeverity()) {
      filtered = filtered.filter(e => e.severity === this.selectedSeverity());
    }

    if (this.selectedTenant()) {
      filtered = filtered.filter(e => e.tenantSubdomain === this.selectedTenant());
    }

    if (this.showReviewedOnly()) {
      filtered = filtered.filter(e => e.reviewedBy !== null);
    }

    if (this.showUnreviewedOnly()) {
      filtered = filtered.filter(e => e.reviewedBy === null);
    }

    return filtered;
  });

  sortedEvents = computed(() => {
    const events = [...this.filteredEvents()];
    const key = this.sortKey();
    const direction = this.sortDirection();

    if (!key) return events;

    return events.sort((a: any, b: any) => {
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

  paginatedEvents = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    const end = start + this.pageSize();
    return this.sortedEvents().slice(start, end);
  });

  totalEvents = computed(() => this.filteredEvents().length);

  criticalEvents = computed(() => {
    return this.events().filter(e => e.severity === 'Critical' && e.reviewedBy === null);
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    const filters = {
      eventType: this.selectedEventType() || undefined,
      severity: this.selectedSeverity() || undefined,
      reviewed: this.showReviewedOnly() ? true : this.showUnreviewedOnly() ? false : undefined
    };

    this.monitoringService.getSecurityEvents(filters).subscribe({
      next: (data) => {
        this.events.set(data);
        this.loading.set(false);
      },
      error: (err: Error) => {
        console.error('Error loading security events:', err);
        this.error.set('Failed to load security events');
        this.loading.set(false);
      }
    });
  }

  onEventTypeChange(value: string | null): void {
    this.selectedEventType.set(value);
    this.pageIndex.set(0);
  }

  onSeverityChange(value: string | null): void {
    this.selectedSeverity.set(value);
    this.pageIndex.set(0);
  }

  onTenantChange(value: string | null): void {
    this.selectedTenant.set(value);
    this.pageIndex.set(0);
  }

  onReviewedFilterChange(value: boolean): void {
    this.showReviewedOnly.set(value);
    if (value) this.showUnreviewedOnly.set(false);
    this.pageIndex.set(0);
  }

  onUnreviewedFilterChange(value: boolean): void {
    this.showUnreviewedOnly.set(value);
    if (value) this.showReviewedOnly.set(false);
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

  onRowClick(event: SecurityEvent): void {
    this.selectedEvent.set(event);
    this.reviewNotes.set('');
    this.showEventDialog.set(true);
  }

  closeDialog(): void {
    this.showEventDialog.set(false);
    this.selectedEvent.set(null);
    this.reviewNotes.set('');
  }

  submitReview(): void {
    const event = this.selectedEvent();
    if (!event) return;

    this.monitoringService.markSecurityEventReviewed(event.eventId, this.reviewNotes()).subscribe({
      next: () => {
        // Update the event in the list
        const events = this.events();
        const index = events.findIndex(e => e.eventId === event.eventId);
        if (index !== -1) {
          events[index].isReviewed = true;
          events[index].reviewedBy = 'Current Admin';
          events[index].reviewedAt = new Date();
          this.events.set([...events]);
        }
        this.closeDialog();
      },
      error: (err: Error) => {
        console.error('Error reviewing event:', err);
        alert('Failed to submit review');
      }
    });
  }

  exportToCSV(): void {
    console.log('Exporting security events to CSV...');
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

  getEventTypeLabel(eventType: string): string {
    return eventType.split('-').map(word =>
      word.charAt(0).toUpperCase() + word.slice(1)
    ).join(' ');
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
}
