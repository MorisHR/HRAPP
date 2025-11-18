import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { SecurityAlertService } from '../../../../../services/security-alert.service';
import { ExportService } from '../../../../../services/export.service';
import { NotificationService } from '../../../../../services/notification.service';
import {
  SecurityAlert,
  SecurityAlertFilter,
  SecurityAlertListResponse,
  SecurityAlertStatus,
  AuditSeverity,
  SecurityAlertType,
  SecurityAlertHelpers
} from '../../../../../models/security-alert.model';

@Component({
  selector: 'app-alert-list',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './alert-list.component.html',
  styleUrls: ['./alert-list.component.css']
})
export class AlertListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Expose Math for template
  Math = Math;

  // Alert data
  alerts: SecurityAlert[] = [];
  loading = false;

  // Pagination
  currentPage = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;

  // Filters
  filter: SecurityAlertFilter = {
    pageNumber: 1,
    pageSize: 25,
    sortBy: 'detectedAt',
    sortDescending: true
  };

  // Filter options
  selectedStatus?: SecurityAlertStatus;
  selectedSeverity?: AuditSeverity;
  selectedAlertType?: SecurityAlertType;
  startDate?: Date;
  endDate?: Date;

  // Enums for template access
  SecurityAlertStatus = SecurityAlertStatus;
  AuditSeverity = AuditSeverity;
  SecurityAlertType = SecurityAlertType;

  // Helper methods
  helpers = SecurityAlertHelpers;

  // Status filter options
  statusOptions = [
    { value: SecurityAlertStatus.NEW, label: 'New' },
    { value: SecurityAlertStatus.ACKNOWLEDGED, label: 'Acknowledged' },
    { value: SecurityAlertStatus.IN_PROGRESS, label: 'In Progress' },
    { value: SecurityAlertStatus.RESOLVED, label: 'Resolved' },
    { value: SecurityAlertStatus.FALSE_POSITIVE, label: 'False Positive' },
    { value: SecurityAlertStatus.ESCALATED, label: 'Escalated' },
    { value: SecurityAlertStatus.PENDING_REVIEW, label: 'Pending Review' },
    { value: SecurityAlertStatus.CLOSED, label: 'Closed' }
  ];

  // Severity filter options
  severityOptions = [
    { value: AuditSeverity.INFO, label: 'Info' },
    { value: AuditSeverity.WARNING, label: 'Warning' },
    { value: AuditSeverity.CRITICAL, label: 'Critical' },
    { value: AuditSeverity.EMERGENCY, label: 'Emergency' }
  ];

  // Alert type filter options
  alertTypeOptions = [
    { value: SecurityAlertType.FAILED_LOGIN_THRESHOLD, label: 'Failed Login Threshold' },
    { value: SecurityAlertType.UNAUTHORIZED_ACCESS, label: 'Unauthorized Access' },
    { value: SecurityAlertType.MASS_DATA_EXPORT, label: 'Mass Data Export' },
    { value: SecurityAlertType.AFTER_HOURS_ACCESS, label: 'After Hours Access' },
    { value: SecurityAlertType.SALARY_CHANGE, label: 'Salary Change' },
    { value: SecurityAlertType.PRIVILEGE_ESCALATION, label: 'Privilege Escalation' },
    { value: SecurityAlertType.GEOGRAPHIC_ANOMALY, label: 'Geographic Anomaly' },
    { value: SecurityAlertType.RAPID_HIGH_RISK_ACTIONS, label: 'Rapid High Risk Actions' },
    { value: SecurityAlertType.ACCOUNT_LOCKOUT, label: 'Account Lockout' },
    { value: SecurityAlertType.IMPOSSIBLE_TRAVEL, label: 'Impossible Travel' },
    { value: SecurityAlertType.RATE_LIMIT_EXCEEDED, label: 'Rate Limit Exceeded' },
    { value: SecurityAlertType.SQL_INJECTION_ATTEMPT, label: 'SQL Injection Attempt' },
    { value: SecurityAlertType.XSS_ATTEMPT, label: 'XSS Attempt' },
    { value: SecurityAlertType.CSRF_FAILURE, label: 'CSRF Failure' },
    { value: SecurityAlertType.SESSION_HIJACK, label: 'Session Hijack' },
    { value: SecurityAlertType.MALICIOUS_FILE_UPLOAD, label: 'Malicious File Upload' },
    { value: SecurityAlertType.DATA_BREACH, label: 'Data Breach' },
    { value: SecurityAlertType.INTEGRITY_VIOLATION, label: 'Integrity Violation' },
    { value: SecurityAlertType.COMPLIANCE_VIOLATION, label: 'Compliance Violation' },
    { value: SecurityAlertType.ML_ANOMALY, label: 'ML Anomaly' },
    { value: SecurityAlertType.GENERAL_SECURITY_EVENT, label: 'General Security Event' }
  ];

  // Sorting
  sortBy = 'detectedAt';
  sortDescending = true;

  constructor(
    private securityAlertService: SecurityAlertService,
    private exportService: ExportService,
    private notificationService: NotificationService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Check for query params (from dashboard navigation)
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        if (params['severity']) {
          this.selectedSeverity = +params['severity'];
          this.filter.severity = this.selectedSeverity;
        }
        if (params['status']) {
          this.selectedStatus = +params['status'];
          this.filter.status = this.selectedStatus;
        }
        this.loadAlerts();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load alerts with current filters
   */
  loadAlerts(): void {
    this.loading = true;

    // Update filter with current values
    this.filter.pageNumber = this.currentPage;
    this.filter.pageSize = this.pageSize;
    this.filter.status = this.selectedStatus;
    this.filter.severity = this.selectedSeverity;
    this.filter.alertType = this.selectedAlertType;
    this.filter.startDate = this.startDate;
    this.filter.endDate = this.endDate;
    this.filter.sortBy = this.sortBy;
    this.filter.sortDescending = this.sortDescending;

    this.securityAlertService.getAlerts(this.filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: SecurityAlertListResponse) => {
          this.alerts = response.data;
          this.totalCount = response.pagination.totalCount;
          this.totalPages = response.pagination.totalPages;
          this.currentPage = response.pagination.currentPage;
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading alerts:', error);
          this.loading = false;
        }
      });
  }

  /**
   * Apply filters
   */
  applyFilters(): void {
    this.currentPage = 1;
    this.loadAlerts();
  }

  /**
   * Reset filters
   */
  resetFilters(): void {
    this.selectedStatus = undefined;
    this.selectedSeverity = undefined;
    this.selectedAlertType = undefined;
    this.startDate = undefined;
    this.endDate = undefined;
    this.filter = {
      pageNumber: 1,
      pageSize: 25,
      sortBy: 'detectedAt',
      sortDescending: true
    };
    this.currentPage = 1;
    this.loadAlerts();
  }

  /**
   * Change page
   */
  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadAlerts();
    }
  }

  /**
   * Change page size
   */
  changePageSize(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadAlerts();
  }

  /**
   * Sort by column
   */
  sortByColumn(column: string): void {
    if (this.sortBy === column) {
      this.sortDescending = !this.sortDescending;
    } else {
      this.sortBy = column;
      this.sortDescending = true;
    }
    this.loadAlerts();
  }

  /**
   * Get sort icon
   */
  getSortIcon(column: string): string {
    if (this.sortBy !== column) return 'fa-sort';
    return this.sortDescending ? 'fa-sort-down' : 'fa-sort-up';
  }

  /**
   * View alert detail
   */
  viewAlertDetail(alertId: string): void {
    this.router.navigate(['/admin/security-alerts/detail', alertId]);
  }

  /**
   * Quick acknowledge alert
   */
  acknowledgeAlert(alertId: string, event: Event): void {
    event.stopPropagation();

    if (!confirm('Are you sure you want to acknowledge this alert?')) {
      return;
    }

    this.securityAlertService.acknowledgeAlert(alertId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.loadAlerts();
        },
        error: (error) => {
          console.error('Error acknowledging alert:', error);
          this.notificationService.error('Failed to acknowledge alert. Please try again.');
        }
      });
  }

  /**
   * Quick mark as in progress
   */
  markInProgress(alertId: string, event: Event): void {
    event.stopPropagation();

    this.securityAlertService.markAlertInProgress(alertId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.loadAlerts();
        },
        error: (error) => {
          console.error('Error marking alert in progress:', error);
          this.notificationService.error('Failed to update alert. Please try again.');
        }
      });
  }

  /**
   * Get severity icon
   */
  getSeverityIcon(severity: AuditSeverity): string {
    return SecurityAlertHelpers.getSeverityIcon(severity);
  }

  /**
   * Get severity color
   */
  getSeverityColor(severity: AuditSeverity): string {
    return SecurityAlertHelpers.getSeverityColor(severity);
  }

  /**
   * Get severity chip color
   */
  getSeverityChipColor(severity: AuditSeverity): string {
    return SecurityAlertHelpers.getSeverityChipColor(severity);
  }

  /**
   * Get status color
   */
  getStatusColor(status: SecurityAlertStatus): string {
    return SecurityAlertHelpers.getStatusColor(status);
  }

  /**
   * Get status chip color
   */
  getStatusChipColor(status: SecurityAlertStatus): string {
    return SecurityAlertHelpers.getStatusChipColor(status);
  }

  /**
   * Format time ago
   */
  formatTimeAgo(date: Date): string {
    return SecurityAlertHelpers.formatTimeAgo(date);
  }

  /**
   * Get risk score color
   */
  getRiskScoreColor(score: number): string {
    return SecurityAlertHelpers.getRiskScoreColor(score);
  }

  /**
   * Get risk score label
   */
  getRiskScoreLabel(score: number): string {
    return SecurityAlertHelpers.getRiskScoreLabel(score);
  }

  /**
   * Get page numbers for pagination
   */
  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;

    if (this.totalPages <= maxVisible) {
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      const half = Math.floor(maxVisible / 2);
      let start = Math.max(1, this.currentPage - half);
      let end = Math.min(this.totalPages, start + maxVisible - 1);

      if (end - start < maxVisible - 1) {
        start = Math.max(1, end - maxVisible + 1);
      }

      for (let i = start; i <= end; i++) {
        pages.push(i);
      }
    }

    return pages;
  }

  /**
   * Export current alerts to CSV
   */
  exportToCSV(): void {
    if (this.alerts.length === 0) {
      this.notificationService.warning('No data to export');
      return;
    }

    const filename = `security-alerts-${new Date().toISOString().split('T')[0]}.csv`;
    this.exportService.exportToCSV(this.alerts, filename);
    this.notificationService.success('CSV export started successfully');
  }

  /**
   * Export current alerts to PDF
   */
  exportToPDF(): void {
    if (this.alerts.length === 0) {
      this.notificationService.warning('No data to export');
      return;
    }

    const filename = `security-alerts-${new Date().toISOString().split('T')[0]}.pdf`;
    this.exportService.exportToPDF(this.alerts, filename);
    this.notificationService.info('PDF export dialog opened');
  }

  /**
   * Refresh list
   */
  refresh(): void {
    this.loadAlerts();
  }
}
