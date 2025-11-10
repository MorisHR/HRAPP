import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
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
  selector: 'app-tenant-audit-logs',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatExpansionModule,
    MatTabsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule
  ],
  templateUrl: './tenant-audit-logs.component.html',
  styleUrls: ['./tenant-audit-logs.component.scss']
})
export class TenantAuditLogsComponent implements OnInit {
  // Data
  auditLogs: AuditLog[] = [];
  statistics: AuditLogStatistics | null = null;

  // State
  loading = false;
  loadingStats = false;
  selectedTab = 0;

  // Pagination
  totalCount = 0;
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

  // Display columns (NO tenant column for tenant admin)
  displayedColumns: string[] = [
    'performedAt',
    'userEmail',
    'actionType',
    'category',
    'severity',
    'entityType',
    'success',
    'actions'
  ];

  constructor(
    private auditLogService: AuditLogService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAuditLogs();
    this.loadStatistics();
  }

  loadAuditLogs(): void {
    this.loading = true;
    this.cdr.detectChanges();

    // IMPORTANT: Using getTenantAuditLogs() - backend auto-filters by tenant
    this.auditLogService.getTenantAuditLogs(this.filter).subscribe({
      next: (result: PagedResult<AuditLog>) => {
        this.auditLogs = result.items;
        this.totalCount = result.totalCount;
        this.pageNumber = result.pageNumber;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading audit logs', error);
        this.snackBar.open('Error loading audit logs', 'Close', { duration: 3000 });
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

    this.auditLogService.getTenantStatistics(startDate, endDate).subscribe({
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
    this.auditLogService.getTenantAuditLogById(log.id).subscribe({
      next: (detail) => {
        console.log('Audit log detail:', detail);
        // Open detail modal
      },
      error: (error) => {
        console.error('Error loading audit log detail', error);
        this.snackBar.open('Error loading details', 'Close', { duration: 3000 });
      }
    });
  }

  exportLogs(): void {
    this.auditLogService.exportTenantLogs(this.filter).subscribe({
      next: (blob) => {
        const filename = `audit_logs_${new Date().getTime()}.csv`;
        this.auditLogService.downloadBlob(blob, filename);
        this.snackBar.open('Export completed', 'Close', { duration: 3000 });
      },
      error: (error) => {
        console.error('Export failed', error);
        this.snackBar.open('Export failed', 'Close', { duration: 3000 });
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
}
