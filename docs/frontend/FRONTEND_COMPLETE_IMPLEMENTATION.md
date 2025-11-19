# Complete Frontend Implementation - Production Ready

## ✅ FILES ALREADY CREATED

1. **Models:** `hrms-frontend/src/app/models/audit-log.model.ts` ✅
2. **Service:** `hrms-frontend/src/app/services/audit-log.service.ts` ✅

---

## 📁 REMAINING FILES TO CREATE

### 1. SuperAdmin Audit Logs Page

**File:** `hrms-frontend/src/app/features/admin/audit-logs/audit-logs.component.ts`

```typescript
import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
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
  selectedTab = 0;

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
  displayedColumns: string[] = [
    'performedAt',
    'tenantName',
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
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadAuditLogs();
    this.loadStatistics();
  }

  loadAuditLogs(): void {
    this.loading = true;
    this.auditLogService.getSystemAuditLogs(this.filter).subscribe({
      next: (result: PagedResult<AuditLog>) => {
        this.auditLogs = result.items;
        this.totalCount = result.totalCount;
        this.pageNumber = result.pageNumber;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading audit logs', error);
        this.snackBar.open('Error loading audit logs', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  loadStatistics(): void {
    this.loadingStats = true;
    const startDate = this.filter.startDate || new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
    const endDate = this.filter.endDate || new Date();

    this.auditLogService.getSystemStatistics(startDate, endDate).subscribe({
      next: (stats) => {
        this.statistics = stats;
        this.loadingStats = false;
      },
      error: (error) => {
        console.error('Error loading statistics', error);
        this.loadingStats = false;
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
        this.snackBar.open('Error loading details', 'Close', { duration: 3000 });
      }
    });
  }

  exportLogs(): void {
    this.auditLogService.exportSystemLogs(this.filter).subscribe({
      next: (blob) => {
        const filename = `audit_logs_system_${new Date().getTime()}.csv`;
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
```

**File:** `hrms-frontend/src/app/features/admin/audit-logs/audit-logs.component.html`

```html
<div class="audit-logs-container">
  <!-- Header -->
  <div class="page-header">
    <div class="header-content">
      <h1>System Audit Logs</h1>
      <p class="subtitle">Complete audit trail across all tenants</p>
    </div>
    <div class="header-actions">
      <button mat-raised-button (click)="refreshData()" [disabled]="loading">
        <mat-icon>refresh</mat-icon>
        Refresh
      </button>
      <button mat-raised-button color="primary" (click)="exportLogs()" [disabled]="loading">
        <mat-icon>download</mat-icon>
        Export CSV
      </button>
    </div>
  </div>

  <!-- Tabs -->
  <mat-tab-group [(selectedIndex)]="selectedTab">
    <!-- Audit Logs Tab -->
    <mat-tab label="Audit Logs">
      <!-- Filter Panel -->
      <div class="filter-panel">
        <mat-expansion-panel>
          <mat-expansion-panel-header>
            <mat-panel-title>
              <mat-icon>filter_list</mat-icon>
              Filters
            </mat-panel-title>
          </mat-expansion-panel-header>

          <div class="filter-content">
            <div class="filter-row">
              <!-- Date Range -->
              <mat-form-field appearance="outline">
                <mat-label>Start Date</mat-label>
                <input matInput [matDatepicker]="startPicker" [(ngModel)]="filter.startDate" (dateChange)="onFilterChange({startDate: $event.value})">
                <mat-datepicker-toggle matSuffix [for]="startPicker"></mat-datepicker-toggle>
                <mat-datepicker #startPicker></mat-datepicker>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>End Date</mat-label>
                <input matInput [matDatepicker]="endPicker" [(ngModel)]="filter.endDate" (dateChange)="onFilterChange({endDate: $event.value})">
                <mat-datepicker-toggle matSuffix [for]="endPicker"></mat-datepicker-toggle>
                <mat-datepicker #endPicker></mat-datepicker>
              </mat-form-field>

              <!-- User Email Search -->
              <mat-form-field appearance="outline">
                <mat-label>User Email</mat-label>
                <input matInput [(ngModel)]="filter.userEmail" (ngModelChange)="onFilterChange({userEmail: $event})">
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>

              <!-- Entity Type -->
              <mat-form-field appearance="outline">
                <mat-label>Entity Type</mat-label>
                <input matInput [(ngModel)]="filter.entityType" (ngModelChange)="onFilterChange({entityType: $event})">
              </mat-form-field>
            </div>

            <div class="filter-actions">
              <button mat-button (click)="clearFilters()">Clear All</button>
              <button mat-raised-button color="primary" (click)="loadAuditLogs()">Apply Filters</button>
            </div>
          </div>
        </mat-expansion-panel>
      </div>

      <!-- Table -->
      <div class="table-container">
        <div class="table-wrapper" *ngIf="!loading && auditLogs.length > 0">
          <table mat-table [dataSource]="auditLogs" class="audit-logs-table">
            <!-- Timestamp Column -->
            <ng-container matColumnDef="performedAt">
              <th mat-header-cell *matHeaderCellDef (click)="onSortChange('PerformedAt')">
                Timestamp
                <mat-icon *ngIf="filter.sortBy === 'PerformedAt'">
                  {{ filter.sortDescending ? 'arrow_downward' : 'arrow_upward' }}
                </mat-icon>
              </th>
              <td mat-cell *matCellDef="let log">
                <div class="timestamp">
                  <div class="date">{{ log.performedAt | date:'short' }}</div>
                  <div class="relative">{{ getRelativeTime(log.performedAt) }}</div>
                </div>
              </td>
            </ng-container>

            <!-- Tenant Column -->
            <ng-container matColumnDef="tenantName">
              <th mat-header-cell *matHeaderCellDef>Tenant</th>
              <td mat-cell *matCellDef="let log">
                <span class="tenant-name">{{ log.tenantName || 'System' }}</span>
              </td>
            </ng-container>

            <!-- User Column -->
            <ng-container matColumnDef="userEmail">
              <th mat-header-cell *matHeaderCellDef>User</th>
              <td mat-cell *matCellDef="let log">
                <div class="user-info">
                  <div class="user-avatar">{{ log.userEmail?.charAt(0)?.toUpperCase() || 'S' }}</div>
                  <div class="user-details">
                    <div class="email">{{ log.userEmail || 'System' }}</div>
                    <div class="role" *ngIf="log.userRole">{{ log.userRole }}</div>
                  </div>
                </div>
              </td>
            </ng-container>

            <!-- Action Type Column -->
            <ng-container matColumnDef="actionType">
              <th mat-header-cell *matHeaderCellDef>Action</th>
              <td mat-cell *matCellDef="let log">
                <span class="action-badge">{{ log.actionTypeName }}</span>
              </td>
            </ng-container>

            <!-- Category Column -->
            <ng-container matColumnDef="category">
              <th mat-header-cell *matHeaderCellDef>Category</th>
              <td mat-cell *matCellDef="let log">
                <span class="category-badge" [ngClass]="getCategoryClass(log.category)">
                  {{ log.categoryName }}
                </span>
              </td>
            </ng-container>

            <!-- Severity Column -->
            <ng-container matColumnDef="severity">
              <th mat-header-cell *matHeaderCellDef>Severity</th>
              <td mat-cell *matCellDef="let log">
                <span class="severity-badge" [ngClass]="getSeverityClass(log.severity)">
                  <mat-icon>{{ log.severity === 1 ? 'info' : log.severity === 2 ? 'warning' : 'error' }}</mat-icon>
                  {{ log.severityName }}
                </span>
              </td>
            </ng-container>

            <!-- Entity Type Column -->
            <ng-container matColumnDef="entityType">
              <th mat-header-cell *matHeaderCellDef>Entity</th>
              <td mat-cell *matCellDef="let log">
                {{ log.entityType || '-' }}
              </td>
            </ng-container>

            <!-- Success Column -->
            <ng-container matColumnDef="success">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let log">
                <mat-icon [class.success]="log.success" [class.failed]="!log.success">
                  {{ log.success ? 'check_circle' : 'error' }}
                </mat-icon>
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let log">
                <button mat-icon-button (click)="viewDetails(log)" matTooltip="View Details">
                  <mat-icon>visibility</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns; sticky: true"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="audit-row"></tr>
          </table>

          <!-- Paginator -->
          <mat-paginator
            [length]="totalCount"
            [pageSize]="pageSize"
            [pageSizeOptions]="pageSizeOptions"
            [pageIndex]="pageNumber - 1"
            (page)="onPageChange($event)"
            showFirstLastButtons>
          </mat-paginator>
        </div>

        <!-- Loading State -->
        <div class="loading-state" *ngIf="loading">
          <mat-spinner diameter="50"></mat-spinner>
          <p>Loading audit logs...</p>
        </div>

        <!-- Empty State -->
        <div class="empty-state" *ngIf="!loading && auditLogs.length === 0">
          <mat-icon>history</mat-icon>
          <h3>No Audit Logs Found</h3>
          <p>Try adjusting your filters or date range</p>
        </div>
      </div>
    </mat-tab>

    <!-- Statistics Tab -->
    <mat-tab label="Statistics">
      <div class="statistics-container" *ngIf="!loadingStats && statistics">
        <div class="stats-grid">
          <!-- Summary Cards -->
          <mat-card class="stat-card">
            <mat-card-header>
              <mat-icon>analytics</mat-icon>
              <mat-card-title>Total Actions</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="stat-value">{{ statistics.totalActions | number }}</div>
              <div class="stat-label">All time</div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card">
            <mat-card-header>
              <mat-icon>today</mat-icon>
              <mat-card-title>Today</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="stat-value">{{ statistics.actionsToday | number }}</div>
              <div class="stat-label">Actions</div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card warning">
            <mat-card-header>
              <mat-icon>lock</mat-icon>
              <mat-card-title>Failed Logins</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="stat-value">{{ statistics.failedLogins | number }}</div>
              <div class="stat-label">Security alerts</div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card critical">
            <mat-card-header>
              <mat-icon>error</mat-icon>
              <mat-card-title>Critical Events</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="stat-value">{{ statistics.criticalEvents | number }}</div>
              <div class="stat-label">Requires attention</div>
            </mat-card-content>
          </mat-card>
        </div>

        <!-- Most Active Users -->
        <mat-card class="activity-card">
          <mat-card-header>
            <mat-card-title>Most Active Users</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="user-list" *ngIf="statistics.mostActiveUsers.length > 0">
              <div class="user-item" *ngFor="let user of statistics.mostActiveUsers">
                <div class="user-avatar">{{ user.userEmail.charAt(0).toUpperCase() }}</div>
                <div class="user-info">
                  <div class="user-email">{{ user.userEmail }}</div>
                  <div class="user-stats">{{ user.actionCount }} actions</div>
                </div>
                <div class="last-activity">{{ user.lastActivity | date:'short' }}</div>
              </div>
            </div>
            <div *ngIf="statistics.mostActiveUsers.length === 0" class="no-data">
              No activity data available
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="loadingStats">
        <mat-spinner diameter="50"></mat-spinner>
        <p>Loading statistics...</p>
      </div>
    </mat-tab>
  </mat-tab-group>
</div>
```

---

### 2. Tenant Admin Audit Logs Page

**File:** `hrms-frontend/src/app/features/tenant/audit-logs/tenant-audit-logs.component.ts`

```typescript
import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
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
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadAuditLogs();
    this.loadStatistics();
  }

  loadAuditLogs(): void {
    this.loading = true;
    // IMPORTANT: Using getTenantAuditLogs() - backend auto-filters by tenant
    this.auditLogService.getTenantAuditLogs(this.filter).subscribe({
      next: (result: PagedResult<AuditLog>) => {
        this.auditLogs = result.items;
        this.totalCount = result.totalCount;
        this.pageNumber = result.pageNumber;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading audit logs', error);
        this.snackBar.open('Error loading audit logs', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  loadStatistics(): void {
    this.loadingStats = true;
    const startDate = this.filter.startDate || new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
    const endDate = this.filter.endDate || new Date();

    this.auditLogService.getTenantStatistics(startDate, endDate).subscribe({
      next: (stats) => {
        this.statistics = stats;
        this.loadingStats = false;
      },
      error: (error) => {
        console.error('Error loading statistics', error);
        this.loadingStats = false;
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
```

**Template:** Use the SAME HTML template as SuperAdmin (audit-logs.component.html) but change:
- Title: "Your Organization's Audit Trail"
- Subtitle: "Complete activity history for your organization"
- Remove tenant column from `displayedColumns` array

---

### 3. Shared Styles

**File:** `hrms-frontend/src/app/features/admin/audit-logs/audit-logs.component.scss`

```scss
.audit-logs-container {
  padding: 24px;
  background: #f5f5f5;
  min-height: calc(100vh - 64px);
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
  background: white;
  padding: 24px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

  .header-content {
    h1 {
      margin: 0;
      font-size: 28px;
      font-weight: 600;
      color: #1a1a1a;
    }

    .subtitle {
      margin: 4px 0 0;
      color: #666;
      font-size: 14px;
    }
  }

  .header-actions {
    display: flex;
    gap: 12px;

    button {
      mat-icon {
        margin-right: 8px;
      }
    }
  }
}

.filter-panel {
  margin-bottom: 24px;

  mat-expansion-panel {
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }

  .filter-content {
    padding: 16px 0;
  }

  .filter-row {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 16px;
    margin-bottom: 16px;
  }

  .filter-actions {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
    margin-top: 16px;
  }
}

.table-container {
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  overflow: hidden;

  .table-wrapper {
    overflow-x: auto;
  }

  .audit-logs-table {
    width: 100%;

    th {
      background: #f8f9fa;
      font-weight: 600;
      color: #333;
      padding: 16px;
      cursor: pointer;
      user-select: none;

      &:hover {
        background: #e9ecef;
      }

      mat-icon {
        vertical-align: middle;
        margin-left: 4px;
        font-size: 18px;
        width: 18px;
        height: 18px;
      }
    }

    td {
      padding: 16px;
      border-bottom: 1px solid #e0e0e0;
    }

    .audit-row {
      transition: background-color 0.2s;

      &:hover {
        background: #f8f9fa;
      }
    }
  }
}

.timestamp {
  .date {
    font-size: 14px;
    color: #333;
  }

  .relative {
    font-size: 12px;
    color: #999;
    margin-top: 2px;
  }
}

.tenant-name {
  font-weight: 500;
  color: #333;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;

  .user-avatar {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 600;
    font-size: 14px;
  }

  .user-details {
    .email {
      font-size: 14px;
      color: #333;
      font-weight: 500;
    }

    .role {
      font-size: 12px;
      color: #666;
      margin-top: 2px;
    }
  }
}

.action-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 12px;
  background: #e3f2fd;
  color: #1976d2;
  font-size: 12px;
  font-weight: 500;
}

.category-badge {
  display: inline-flex;
  align-items: center;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
  color: white;

  &.category-authentication {
    background: #8b5cf6;
  }

  &.category-authorization {
    background: #6366f1;
  }

  &.category-data-change {
    background: #3b82f6;
  }

  &.category-tenant-lifecycle {
    background: #10b981;
  }

  &.category-system-admin {
    background: #f59e0b;
  }

  &.category-security-event {
    background: #ef4444;
  }

  &.category-system-event {
    background: #6b7280;
  }

  &.category-compliance {
    background: #14b8a6;
  }
}

.severity-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
  color: white;

  mat-icon {
    font-size: 16px;
    width: 16px;
    height: 16px;
  }

  &.severity-info {
    background: #3b82f6;
  }

  &.severity-warning {
    background: #f59e0b;
  }

  &.severity-critical {
    background: #ef4444;
  }

  &.severity-emergency {
    background: #7f1d1d;
  }
}

mat-icon {
  &.success {
    color: #10b981;
  }

  &.failed {
    color: #ef4444;
  }
}

.loading-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: #666;

  mat-icon {
    font-size: 64px;
    width: 64px;
    height: 64px;
    color: #ccc;
    margin-bottom: 16px;
  }

  h3 {
    margin: 16px 0 8px;
    color: #333;
  }

  p {
    margin: 0;
    color: #999;
  }
}

.statistics-container {
  padding: 24px;

  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 24px;
    margin-bottom: 24px;
  }

  .stat-card {
    mat-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 16px;

      mat-icon {
        color: #667eea;
        font-size: 32px;
        width: 32px;
        height: 32px;
      }

      mat-card-title {
        margin: 0;
        font-size: 14px;
        font-weight: 600;
        color: #666;
      }
    }

    .stat-value {
      font-size: 36px;
      font-weight: 700;
      color: #1a1a1a;
    }

    .stat-label {
      font-size: 12px;
      color: #999;
      margin-top: 4px;
    }

    &.warning {
      mat-card-header mat-icon {
        color: #f59e0b;
      }
    }

    &.critical {
      mat-card-header mat-icon {
        color: #ef4444;
      }
    }
  }

  .activity-card {
    .user-list {
      .user-item {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 12px;
        border-bottom: 1px solid #e0e0e0;

        &:last-child {
          border-bottom: none;
        }

        .user-avatar {
          width: 40px;
          height: 40px;
          border-radius: 50%;
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          color: white;
          display: flex;
          align-items: center;
          justify-content: center;
          font-weight: 600;
        }

        .user-info {
          flex: 1;

          .user-email {
            font-weight: 500;
            color: #333;
          }

          .user-stats {
            font-size: 12px;
            color: #666;
            margin-top: 2px;
          }
        }

        .last-activity {
          font-size: 12px;
          color: #999;
        }
      }
    }

    .no-data {
      text-align: center;
      padding: 40px;
      color: #999;
    }
  }
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
    gap: 16px;

    .header-actions {
      width: 100%;

      button {
        flex: 1;
      }
    }
  }

  .filter-row {
    grid-template-columns: 1fr;
  }

  .stats-grid {
    grid-template-columns: 1fr;
  }
}
```

---

## 🔌 ROUTING CONFIGURATION

**SuperAdmin Routes** - Add to `hrms-frontend/src/app/features/admin/admin-routing.module.ts`:

```typescript
{
  path: 'audit-logs',
  component: AdminAuditLogsComponent,
  canActivate: [AuthGuard],
  data: { roles: ['SuperAdmin'], title: 'Audit Logs' }
}
```

**Tenant Routes** - Add to `hrms-frontend/src/app/features/tenant/tenant-routing.module.ts`:

```typescript
{
  path: 'audit-logs',
  component: TenantAuditLogsComponent,
  canActivate: [AuthGuard, TenantGuard],
  data: { title: 'Audit Trail' }
}
```

---

## 🧩 MODULE DECLARATIONS

Add components to respective modules:

**SuperAdmin Module:**
```typescript
declarations: [
  // ... existing components
  AdminAuditLogsComponent
]
```

**Tenant Module:**
```typescript
declarations: [
  // ... existing components
  TenantAuditLogsComponent
]
```

**Required Angular Material Imports:**
```typescript
imports: [
  // ... existing imports
  MatTableModule,
  MatPaginatorModule,
  MatSortModule,
  MatDatepickerModule,
  MatNativeDateModule,
  MatExpansionModule,
  MatTabsModule,
  MatTooltipModule,
  MatProgressSpinnerModule
]
```

---

## 🚀 DEPLOYMENT CHECKLIST

1. ✅ Backend controllers created
2. ✅ DTOs created
3. ✅ Angular models created
4. ✅ Angular service created
5. ⏳ Create component files from templates above
6. ⏳ Add routing configuration
7. ⏳ Add navigation links to sidebars
8. ⏳ Test SuperAdmin access
9. ⏳ Test Tenant Admin access
10. ⏳ Test tenant isolation

---

## 📝 NEXT STEPS

1. **Copy component files** - Create the TypeScript and HTML files above
2. **Copy SCSS file** - Add the stylesheet
3. **Update routing** - Add routes to both modules
4. **Add navigation** - Add links to sidebars
5. **Test** - Verify everything works!

**The foundation is complete - just add the component files and you're ready to go!**
