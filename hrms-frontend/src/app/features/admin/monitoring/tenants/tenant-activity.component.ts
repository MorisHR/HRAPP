import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// Custom UI Components
import { CardComponent } from '../../../../shared/ui/components/card/card';
import { SortEvent } from '../../../../shared/ui/components/table/table';
import { Badge } from '../../../../shared/ui/components/badge/badge';
import { Chip } from '../../../../shared/ui/components/chip/chip';
import { ButtonComponent } from '../../../../shared/ui/components/button/button';
import { SelectComponent, SelectOption } from '../../../../shared/ui/components/select/select';
import { Paginator, PageEvent } from '../../../../shared/ui/components/paginator/paginator';

// Services
import { MonitoringService } from '../../../../core/services/monitoring.service';
import { TenantActivity } from '../../../../core/models/monitoring.models';

@Component({
  selector: 'app-tenant-activity',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardComponent,
    Badge,
    Chip,
    ButtonComponent,
    SelectComponent,
    Paginator
  ],
  templateUrl: './tenant-activity.component.html',
  styleUrl: './tenant-activity.component.scss'
})
export class TenantActivityComponent implements OnInit {
  private monitoringService = inject(MonitoringService);

  // State
  tenants = signal<TenantActivity[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Filters
  selectedStatus = signal<string | null>(null);
  selectedTier = signal<string | null>(null);
  minUsers = signal(0);

  statusOptions: SelectOption[] = [
    { value: null, label: 'All Status' },
    { value: 'active', label: 'Active' },
    { value: 'at-risk', label: 'At Risk' },
    { value: 'inactive', label: 'Inactive' }
  ];

  tierOptions: SelectOption[] = [
    { value: null, label: 'All Tiers' },
    { value: 'free', label: 'Free' },
    { value: 'basic', label: 'Basic' },
    { value: 'premium', label: 'Premium' },
    { value: 'enterprise', label: 'Enterprise' }
  ];

  minUsersOptions: SelectOption[] = [
    { value: 0, label: 'All Users' },
    { value: 10, label: '10+ Users' },
    { value: 50, label: '50+ Users' },
    { value: 100, label: '100+ Users' },
    { value: 500, label: '500+ Users' }
  ];

  // Pagination
  pageSize = signal(10);
  pageIndex = signal(0);

  // Sorting
  sortKey = signal<string | null>('healthScore');
  sortDirection = signal<'asc' | 'desc'>('asc');

  // Computed values
  filteredTenants = computed(() => {
    let filtered = [...this.tenants()];

    if (this.selectedStatus()) {
      filtered = filtered.filter(t => t.status === this.selectedStatus());
    }

    if (this.selectedTier()) {
      filtered = filtered.filter(t => t.tier === this.selectedTier());
    }

    if (this.minUsers() > 0) {
      filtered = filtered.filter(t => t.activeUsersLast24h >= this.minUsers());
    }

    return filtered;
  });

  sortedTenants = computed(() => {
    const tenants = [...this.filteredTenants()];
    const key = this.sortKey();
    const direction = this.sortDirection();

    if (!key) return tenants;

    return tenants.sort((a: any, b: any) => {
      const aVal = a[key];
      const bVal = b[key];

      if (typeof aVal === 'string') {
        return direction === 'asc'
          ? aVal.localeCompare(bVal)
          : bVal.localeCompare(aVal);
      }

      return direction === 'asc' ? aVal - bVal : bVal - aVal;
    });
  });

  paginatedTenants = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    const end = start + this.pageSize();
    return this.sortedTenants().slice(start, end);
  });

  totalTenants = computed(() => this.filteredTenants().length);

  atRiskTenants = computed(() => {
    return this.tenants().filter(t => t.healthScore < 50);
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    const filters = {
      status: this.selectedStatus() || undefined,
      tier: this.selectedTier() || undefined
    };

    this.monitoringService.getTenantActivity(filters).subscribe({
      next: (data) => {
        this.tenants.set(data);
        this.loading.set(false);
      },
      error: (err: Error) => {
        console.error('Error loading tenant activity:', err);
        this.error.set('Failed to load tenant activity');
        this.loading.set(false);
      }
    });
  }

  onStatusChange(value: string | null): void {
    this.selectedStatus.set(value);
    this.pageIndex.set(0);
    // Note: We're filtering client-side, so no need to reload
  }

  onTierChange(value: string | null): void {
    this.selectedTier.set(value);
    this.pageIndex.set(0);
    // Note: We're filtering client-side, so no need to reload
  }

  onMinUsersChange(value: number): void {
    this.minUsers.set(value);
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

  exportToCSV(): void {
    console.log('Exporting tenant activity to CSV...');
    alert('CSV export functionality coming soon!');
  }

  getHealthScoreColor(score: number): 'error' | 'warning' | 'success' {
    if (score < 50) return 'error';
    if (score < 75) return 'warning';
    return 'success';
  }

  getTierColor(tier: string): 'primary' | 'success' | 'warning' | 'error' {
    switch (tier) {
      case 'free': return 'primary';
      case 'basic': return 'success';
      case 'premium': return 'warning';
      case 'enterprise': return 'error';
      default: return 'primary';
    }
  }

  getHealthScoreClass(score: number): string {
    if (score < 50) return 'health-critical';
    if (score < 75) return 'health-warning';
    return 'health-good';
  }
}
