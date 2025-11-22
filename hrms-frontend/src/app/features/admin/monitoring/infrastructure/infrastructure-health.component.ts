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
import { InfrastructureHealth, SlowQuery } from '../../../../core/models/monitoring.models';

@Component({
  selector: 'app-infrastructure-health',
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
  templateUrl: './infrastructure-health.component.html',
  styleUrl: './infrastructure-health.component.scss'
})
export class InfrastructureHealthComponent implements OnInit {
  private monitoringService = inject(MonitoringService);

  // State
  metrics = signal<InfrastructureHealth | null>(null);
  slowQueries = signal<SlowQuery[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Filters
  minExecutionTime = signal(0);
  minExecutionOptions: SelectOption[] = [
    { value: 0, label: 'All queries' },
    { value: 0.5, label: '> 0.5s' },
    { value: 1.0, label: '> 1.0s' },
    { value: 2.0, label: '> 2.0s' }
  ];

  // Pagination
  pageSize = signal(10);
  pageIndex = signal(0);

  // Sorting
  sortKey = signal<string | null>('avgExecutionTimeMs');
  sortDirection = signal<'asc' | 'desc'>('desc');

  // Computed values
  sortedQueries = computed(() => {
    const queries = [...this.slowQueries()];
    const key = this.sortKey();
    const direction = this.sortDirection();

    if (!key) return queries;

    return queries.sort((a: any, b: any) => {
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

  paginatedQueries = computed(() => {
    const start = this.pageIndex() * this.pageSize();
    const end = start + this.pageSize();
    return this.sortedQueries().slice(start, end);
  });

  totalQueries = computed(() => this.slowQueries().length);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    // Load metrics
    this.monitoringService.getInfrastructureHealth().subscribe({
      next: (data) => {
        this.metrics.set(data);
      },
      error: (err: Error) => {
        console.error('Error loading infrastructure metrics:', err);
        this.error.set('Failed to load infrastructure metrics');
      }
    });

    // Load slow queries
    this.loadSlowQueries();
  }

  loadSlowQueries(): void {
    this.loading.set(true);

    this.monitoringService.getSlowQueries({ minExecutionTimeMs: this.minExecutionTime() }).subscribe({
      next: (data) => {
        this.slowQueries.set(data);
        this.loading.set(false);
      },
      error: (err: Error) => {
        console.error('Error loading slow queries:', err);
        this.error.set('Failed to load slow queries');
        this.loading.set(false);
      }
    });
  }

  onFilterChange(value: number): void {
    this.minExecutionTime.set(value);
    this.pageIndex.set(0);
    this.loadSlowQueries();
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
    const queries = this.paginatedQueries();
    if (!queries || queries.length === 0) {
      alert('No data to export');
      return;
    }

    // CSV headers
    const headers = [
      'Query Text',
      'Tenant',
      'Avg Execution Time (ms)',
      'Max Execution Time (ms)',
      'P95 Execution Time (ms)',
      'Execution Count',
      'Last Executed',
      'Severity',
      'Schema'
    ];

    // CSV rows
    const rows = queries.map(q => [
      `"${(q.queryText || '').replace(/"/g, '""')}"`, // Escape quotes
      q.tenantSubdomain || 'N/A',
      q.avgExecutionTimeMs.toFixed(2),
      q.maxExecutionTimeMs.toFixed(2),
      q.p95ExecutionTimeMs.toFixed(2),
      q.executionCount,
      new Date(q.lastExecuted).toLocaleString(),
      q.severity,
      q.schemaName || 'N/A'
    ]);

    // Combine headers and rows
    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.join(','))
    ].join('\n');

    // Create and download file
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', `slow-queries-${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    console.log(`Exported ${queries.length} slow queries to CSV`);
  }

  getMetricStatusClass(value: number, threshold: number): string {
    if (value >= threshold) return 'metric-critical';
    if (value >= threshold * 0.8) return 'metric-warning';
    return 'metric-healthy';
  }

  getSeverityColor(severity: 'Critical' | 'High' | 'Medium' | 'Low'): 'error' | 'warning' | 'success' | 'primary' {
    switch (severity) {
      case 'Critical': return 'error';
      case 'High': return 'warning';
      case 'Medium': return 'warning';
      case 'Low': return 'success';
      default: return 'primary';
    }
  }
}
