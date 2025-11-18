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
import { Datepicker } from '../../../../shared/ui/components/datepicker/datepicker';
import { ChipColor } from '../../../../shared/ui';

// Services
import { MonitoringService } from '../../../../core/services/monitoring.service';
import { ApiPerformance, ApiPerformanceParams } from '../../../../core/models/monitoring.models';

@Component({
  selector: 'app-api-performance',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardComponent,
    Badge,
    ButtonComponent,
    SelectComponent,
    Paginator,
    Datepicker
  ],
  templateUrl: './api-performance.component.html',
  styleUrl: './api-performance.component.scss'
})
export class ApiPerformanceComponent implements OnInit {
  private monitoringService = inject(MonitoringService);

  // State
  endpoints = signal<ApiPerformance[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Filters
  selectedTenant = signal<string | null>(null);
  selectedEndpoint = signal<string | null>(null);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);

  tenantOptions: SelectOption[] = [
    { value: null, label: 'All Tenants' },
    { value: 'acme', label: 'Acme Corporation' },
    { value: 'techstart', label: 'TechStart Inc' },
    { value: 'enterprise-co', label: 'Enterprise Co' }
  ];

  endpointOptions: SelectOption[] = [
    { value: null, label: 'All Endpoints' },
    { value: '/api/employees', label: 'GET /api/employees' },
    { value: '/api/payroll/calculate', label: 'POST /api/payroll/calculate' },
    { value: '/api/attendance/checkin', label: 'POST /api/attendance/checkin' },
    { value: '/api/reports/generate', label: 'GET /api/reports/generate' }
  ];

  // Pagination
  pageSize = signal(10);
  pageIndex = signal(0);

  // Sorting
  sortKey = signal<string | null>('totalRequests');
  sortDirection = signal<'asc' | 'desc'>('desc');

  // Computed values
  sortedEndpoints = computed(() => {
    const endpoints = [...this.endpoints()];
    const key = this.sortKey();
    const direction = this.sortDirection();

    if (!key) return endpoints;

    return endpoints.sort((a: any, b: any) => {
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

  paginatedEndpoints = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    const end = start + this.pageSize();
    return this.sortedEndpoints().slice(start, end);
  });

  totalEndpoints = computed(() => this.endpoints().length);

  slaViolations = computed(() => {
    return this.endpoints().filter(e => e.performanceStatus === 'Critical').length;
  });

  averageErrorRate = computed(() => {
    const eps = this.endpoints();
    if (eps.length === 0) return 0;
    return eps.reduce((sum, e) => sum + e.errorRate, 0) / eps.length;
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    const filters = {
      tenantSubdomain: this.selectedTenant() || undefined,
      endpoint: this.selectedEndpoint() || undefined
    } as ApiPerformanceParams;

    this.monitoringService.getApiPerformance(filters).subscribe({
      next: (data) => {
        this.endpoints.set(data);
        this.loading.set(false);
      },
      error: (err: Error) => {
        console.error('Error loading API endpoints:', err);
        this.error.set('Failed to load API endpoints');
        this.loading.set(false);
      }
    });
  }

  onTenantChange(value: string | null): void {
    this.selectedTenant.set(value);
    this.pageIndex.set(0);
    this.loadData();
  }

  onEndpointChange(value: string | null): void {
    this.selectedEndpoint.set(value);
    this.pageIndex.set(0);
    this.loadData();
  }

  onDateFromChange(value: Date | null): void {
    this.dateFrom.set(value);
    // In production, this would reload data with date filter
  }

  onDateToChange(value: Date | null): void {
    this.dateTo.set(value);
    // In production, this would reload data with date filter
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
    console.log('Exporting API performance data to CSV...');
    alert('CSV export functionality coming soon!');
  }

  getStatusColor(status: string): ChipColor {
    switch (status) {
      case 'Excellent': return 'success';
      case 'Good': return 'success';
      case 'Warning': return 'warning';
      case 'Critical': return 'error';
      default: return 'neutral';
    }
  }

  getMethodColor(method: string): ChipColor {
    switch (method) {
      case 'GET': return 'primary';
      case 'POST': return 'success';
      case 'PUT': return 'warning';
      case 'DELETE': return 'error';
      default: return 'neutral';
    }
  }

  isSlaViolation(endpoint: ApiPerformance): boolean {
    // SLA violation if P95 > 1000ms or error rate > 5%
    return endpoint.p95ResponseTimeMs > 1000 || endpoint.errorRate > 5;
  }
}
