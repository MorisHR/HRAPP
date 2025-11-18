import { Component, OnInit, ChangeDetectorRef, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogService, Tabs, Tab, TooltipDirective, ExpansionPanel, ExpansionPanelGroup } from '../../../shared/ui';
import { ToastService } from '../../../shared/ui';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { UiModule } from '../../../shared/ui/ui.module';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { AuditLogService } from '../../../services/audit-log.service';
import {
  AuditLog,
  AuditLogFilter,
  AuditLogStatistics,
  PagedResult,
  AuditLogHelper
} from '../../../models/audit-log.model';
import { TableComponent, TableColumn } from '../../../shared/ui/components/table/table';

@Component({
  selector: 'app-tenant-audit-logs',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableComponent,
    MatDatepickerModule,
    MatNativeDateModule,
    ExpansionPanel,
    ExpansionPanelGroup,
    TooltipDirective,
    MatButtonModule,
    Tabs,
    UiModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule
  ],
  templateUrl: './tenant-audit-logs.component.html',
  styleUrls: ['./tenant-audit-logs.component.scss']
})
export class TenantAuditLogsComponent implements OnInit {
  // Data
  auditLogs = signal<AuditLog[]>([]);
  statistics = signal<AuditLogStatistics | null>(null);

  // State
  loading = signal(false);
  loadingStats = signal(false);

  // Tabs configuration
  tabs: Tab[] = [
    { label: 'Audit Logs', value: 'logs' },
    { label: 'Statistics', value: 'statistics' }
  ];
  activeTab = 'logs';

  // Pagination
  totalCount = signal(0);
  pageNumber = 1;
  pageSize = 50;
  pageSizeOptions = [25, 50, 100];

  // Filter
  filter: AuditLogFilter = {
    pageNumber: 1,
    pageSize: 50,
    sortBy: 'PerformedAt',
    sortDescending: true
  };

  // Table columns (NO tenant column for tenant admin)
  tableColumns: TableColumn[] = [
    { key: 'performedAt', label: 'Timestamp', sortable: true },
    { key: 'userEmail', label: 'User', sortable: true },
    { key: 'actionType', label: 'Action', sortable: true },
    { key: 'category', label: 'Category', sortable: true },
    { key: 'severity', label: 'Severity', sortable: true },
    { key: 'entityType', label: 'Entity', sortable: true },
    { key: 'success', label: 'Status', sortable: true },
    { key: 'actions', label: 'Actions', sortable: false }
  ];

  private toastService = inject(ToastService);
  private dialogService = inject(DialogService);

  constructor(
    private auditLogService: AuditLogService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAuditLogs();
    this.loadStatistics();
  }

  loadAuditLogs(): void {
    this.loading.set(true);
    this.cdr.detectChanges();

    // IMPORTANT: Using getTenantAuditLogs() - backend auto-filters by tenant
    this.auditLogService.getTenantAuditLogs(this.filter).subscribe({
      next: (result: PagedResult<AuditLog>) => {
        this.auditLogs.set(result.items);
        this.totalCount.set(result.totalCount);
        this.pageNumber = result.pageNumber;
        this.loading.set(false);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading audit logs', error);
        this.toastService.error('Error loading audit logs', 3000);
        this.loading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  loadStatistics(): void {
    this.loadingStats.set(true);
    this.cdr.detectChanges();

    const startDate = this.filter.startDate || new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
    const endDate = this.filter.endDate || new Date();

    this.auditLogService.getTenantStatistics(startDate, endDate).subscribe({
      next: (stats) => {
        this.statistics.set(stats);
        this.loadingStats.set(false);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading statistics', error);
        this.loadingStats.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  onFilterChange(newFilter: Partial<AuditLogFilter>): void {
    this.filter = { ...this.filter, ...newFilter, pageNumber: 1 };
    this.loadAuditLogs();
    if (newFilter.startDate || newFilter.endDate) {
      this.loadStatistics();
    }
  }

  onPageChange(event: any): void {
    this.filter.pageNumber = event.pageIndex + 1;
    this.filter.pageSize = event.pageSize;
    this.loadAuditLogs();
  }

  onSortChange(sortBy: string): void {
    if (this.filter.sortBy === sortBy) {
      this.filter.sortDescending = !this.filter.sortDescending;
    } else {
      this.filter.sortBy = sortBy;
      this.filter.sortDescending = true;
    }
    this.loadAuditLogs();
  }

  viewDetails(log: AuditLog): void {
    this.auditLogService.getTenantAuditLogById(log.id).subscribe({
      next: (detail) => {
        console.log('Audit log detail:', detail);
        // Open detail modal
      },
      error: (error) => {
        console.error('Error loading audit log detail', error);
        this.toastService.error('Error loading details', 3000);
      }
    });
  }

  exportLogs(): void {
    this.auditLogService.exportTenantLogs(this.filter).subscribe({
      next: (blob) => {
        const filename = `audit_logs_${new Date().getTime()}.csv`;
        this.auditLogService.downloadBlob(blob, filename);
        this.toastService.success('Export completed', 3000);
      },
      error: (error) => {
        console.error('Export failed', error);
        this.toastService.error('Export failed', 3000);
      }
    });
  }

  refreshData(): void {
    this.loadAuditLogs();
    this.loadStatistics();
  }

  clearFilters(): void {
    this.filter = {
      pageNumber: 1,
      pageSize: 50,
      sortBy: 'PerformedAt',
      sortDescending: true
    };
    this.loadAuditLogs();
    this.loadStatistics();
  }

  getSeverityClass(severity: number): string {
    return `severity-${AuditLogHelper.getSeverityColor(severity)}`;
  }

  getCategoryClass(category: number): string {
    return `category-${AuditLogHelper.getCategoryColor(category)}`;
  }

  getRelativeTime(date: Date): string {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) return `${days}d ago`;
    if (hours > 0) return `${hours}h ago`;
    if (minutes > 0) return `${minutes}m ago`;
    return 'Just now';
  }

  onTabChange(value: string): void {
    this.activeTab = value;
  }
}
