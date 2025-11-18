import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastService, TableComponent, TableColumn, TableColumnDirective, Tabs, Tab, TooltipDirective, Paginator, ExpansionPanel, ExpansionPanelGroup } from '../../../shared/ui';
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

@Component({
  selector: 'app-admin-audit-logs',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Paginator,
    MatDatepickerModule,
    MatNativeDateModule,
    ExpansionPanel,
    ExpansionPanelGroup,
    TooltipDirective,
    UiModule,
    Tabs,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    TableComponent,
    TableColumnDirective
  ],
  templateUrl: './audit-logs.component.html',
  styleUrls: ['./audit-logs.component.scss']
})
export class AdminAuditLogsComponent implements OnInit {
  // Data
  auditLogs: AuditLog[] = [];
  statistics: AuditLogStatistics | null = null;

  // State
  loading = false;
  loadingStats = false;

  // Tabs configuration
  tabs: Tab[] = [
    { label: 'Audit Logs', value: 'logs' },
    { label: 'Statistics', value: 'statistics' }
  ];
  activeTab = 'logs';

  // Pagination
  totalCount = 0;
  pageNumber = 1;
  pageSize = 50;
  pageSizeOptions = [25, 50, 100, 200];

  // Filter
  filter: AuditLogFilter = {
    pageNumber: 1,
    pageSize: 50,
    sortBy: 'PerformedAt',
    sortDescending: true
  };

  // Display columns
  columns: TableColumn[] = [
    { key: 'performedAt', label: 'Timestamp', sortable: true },
    { key: 'tenantName', label: 'Tenant' },
    { key: 'userEmail', label: 'User' },
    { key: 'actionType', label: 'Action' },
    { key: 'category', label: 'Category' },
    { key: 'severity', label: 'Severity' },
    { key: 'entityType', label: 'Entity' },
    { key: 'success', label: 'Status' },
    { key: 'actions', label: 'Actions' }
  ];

  private toastService = inject(ToastService);

  constructor(
    private auditLogService: AuditLogService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAuditLogs();
    this.loadStatistics();
  }

  loadAuditLogs(): void {
    this.loading = true;
    this.cdr.detectChanges();

    this.auditLogService.getSystemAuditLogs(this.filter).subscribe({
      next: (result: PagedResult<AuditLog>) => {
        this.auditLogs = result.items;
        this.totalCount = result.totalCount;
        this.pageNumber = result.pageNumber;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading audit logs', error);
        this.toastService.error('Error loading audit logs', 3000);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadStatistics(): void {
    this.loadingStats = true;
    this.cdr.detectChanges();

    const startDate = this.filter.startDate || new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
    const endDate = this.filter.endDate || new Date();

    this.auditLogService.getSystemStatistics(startDate, endDate).subscribe({
      next: (stats) => {
        this.statistics = stats;
        this.loadingStats = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading statistics', error);
        this.loadingStats = false;
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
    this.auditLogService.getSystemAuditLogById(log.id).subscribe({
      next: (detail) => {
        // Open detail modal (implement AuditLogDetailDialog component)
        // this.dialog.open(AuditLogDetailDialog, { data: detail, width: '800px' });
        console.log('Audit log detail:', detail);
      },
      error: (error) => {
        console.error('Error loading audit log detail', error);
        this.toastService.error('Error loading details', 3000);
      }
    });
  }

  exportLogs(): void {
    this.auditLogService.exportSystemLogs(this.filter).subscribe({
      next: (blob) => {
        const filename = `audit_logs_system_${new Date().getTime()}.csv`;
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
