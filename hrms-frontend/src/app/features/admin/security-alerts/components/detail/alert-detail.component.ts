import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SecurityAlertService } from '../../../../../services/security-alert.service';
import { NotificationService } from '../../../../../services/notification.service';
import {
  SecurityAlert,
  SecurityAlertStatus,
  AuditSeverity,
  SecurityAlertHelpers,
  AssignAlertRequest,
  ResolveAlertRequest,
  FalsePositiveRequest,
  EscalateAlertRequest
} from '../../../../../models/security-alert.model';

@Component({
  selector: 'app-alert-detail',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './alert-detail.component.html',
  styleUrls: ['./alert-detail.component.css']
})
export class AlertDetailComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Alert data
  alert: SecurityAlert | null = null;
  loading = true;
  error: string | null = null;

  // Action forms visibility
  showAssignForm = false;
  showResolveForm = false;
  showFalsePositiveForm = false;
  showEscalateForm = false;

  // Form data
  assignToEmail = '';
  resolutionNotes = '';
  falsePositiveReason = '';
  escalatedTo = '';

  // Enums for template
  SecurityAlertStatus = SecurityAlertStatus;
  AuditSeverity = AuditSeverity;

  // Helper methods
  helpers = SecurityAlertHelpers;

  // Action loading states
  actionLoading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private securityAlertService: SecurityAlertService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    const alertId = this.route.snapshot.paramMap.get('id');
    if (alertId) {
      this.loadAlert(alertId);
    } else {
      this.error = 'Alert ID not provided';
      this.loading = false;
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load alert details
   */
  loadAlert(alertId: string): void {
    this.loading = true;
    this.error = null;

    this.securityAlertService.getAlertById(alertId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (alert) => {
          this.alert = alert;
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading alert:', error);
          this.error = 'Failed to load alert details. The alert may not exist or you may not have permission to view it.';
          this.loading = false;
        }
      });
  }

  /**
   * Navigate back to list
   */
  goBack(): void {
    this.router.navigate(['/admin/security-alerts/list']);
  }

  /**
   * Refresh alert data
   */
  refresh(): void {
    if (this.alert) {
      this.loadAlert(this.alert.id);
    }
  }

  // ============================================
  // ACTION METHODS
  // ============================================

  /**
   * Acknowledge alert
   */
  acknowledgeAlert(): void {
    if (!this.alert || !confirm('Are you sure you want to acknowledge this alert?')) {
      return;
    }

    this.actionLoading = true;
    this.securityAlertService.acknowledgeAlert(this.alert.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedAlert) => {
          this.alert = updatedAlert;
          this.actionLoading = false;
          this.notificationService.success('Alert acknowledged successfully');
        },
        error: (error) => {
          console.error('Error acknowledging alert:', error);
          this.notificationService.error('Failed to acknowledge alert. Please try again.');
          this.actionLoading = false;
        }
      });
  }

  /**
   * Mark alert as in progress
   */
  markInProgress(): void {
    if (!this.alert) return;

    this.actionLoading = true;
    this.securityAlertService.markAlertInProgress(this.alert.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedAlert) => {
          this.alert = updatedAlert;
          this.actionLoading = false;
          this.notificationService.success('Alert marked as in progress');
        },
        error: (error) => {
          console.error('Error updating alert:', error);
          this.notificationService.error('Failed to update alert. Please try again.');
          this.actionLoading = false;
        }
      });
  }

  /**
   * Show assign form
   */
  toggleAssignForm(): void {
    this.showAssignForm = !this.showAssignForm;
    this.assignToEmail = '';
  }

  /**
   * Assign alert
   */
  assignAlert(): void {
    if (!this.alert || !this.assignToEmail.trim()) {
      this.notificationService.warning('Please enter an email address');
      return;
    }

    this.actionLoading = true;
    const request: AssignAlertRequest = {
      // TODO: Add user lookup to get Guid for assignedTo field once user service is available
      assignedToEmail: this.assignToEmail
    };

    this.securityAlertService.assignAlert(this.alert.id, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedAlert) => {
          this.alert = updatedAlert;
          this.showAssignForm = false;
          this.assignToEmail = '';
          this.actionLoading = false;
          this.notificationService.success('Alert assigned successfully');
        },
        error: (error) => {
          console.error('Error assigning alert:', error);
          this.notificationService.error('Failed to assign alert. Please try again.');
          this.actionLoading = false;
        }
      });
  }

  /**
   * Show resolve form
   */
  toggleResolveForm(): void {
    this.showResolveForm = !this.showResolveForm;
    this.resolutionNotes = '';
  }

  /**
   * Resolve alert
   */
  resolveAlert(): void {
    if (!this.alert || !this.resolutionNotes.trim()) {
      this.notificationService.warning('Please enter resolution notes');
      return;
    }

    if (!confirm('Are you sure you want to resolve this alert?')) {
      return;
    }

    this.actionLoading = true;
    const request: ResolveAlertRequest = {
      resolutionNotes: this.resolutionNotes
    };

    this.securityAlertService.resolveAlert(this.alert.id, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedAlert) => {
          this.alert = updatedAlert;
          this.showResolveForm = false;
          this.resolutionNotes = '';
          this.actionLoading = false;
          this.notificationService.success('Alert resolved successfully');
        },
        error: (error) => {
          console.error('Error resolving alert:', error);
          this.notificationService.error('Failed to resolve alert. Please try again.');
          this.actionLoading = false;
        }
      });
  }

  /**
   * Show false positive form
   */
  toggleFalsePositiveForm(): void {
    this.showFalsePositiveForm = !this.showFalsePositiveForm;
    this.falsePositiveReason = '';
  }

  /**
   * Mark as false positive
   */
  markAsFalsePositive(): void {
    if (!this.alert || !this.falsePositiveReason.trim()) {
      this.notificationService.warning('Please enter a reason for marking as false positive');
      return;
    }

    if (!confirm('Are you sure you want to mark this as a false positive?')) {
      return;
    }

    this.actionLoading = true;
    const request: FalsePositiveRequest = {
      reason: this.falsePositiveReason
    };

    this.securityAlertService.markAlertAsFalsePositive(this.alert.id, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedAlert) => {
          this.alert = updatedAlert;
          this.showFalsePositiveForm = false;
          this.falsePositiveReason = '';
          this.actionLoading = false;
          this.notificationService.success('Alert marked as false positive');
        },
        error: (error) => {
          console.error('Error marking alert as false positive:', error);
          this.notificationService.error('Failed to mark alert as false positive. Please try again.');
          this.actionLoading = false;
        }
      });
  }

  /**
   * Show escalate form
   */
  toggleEscalateForm(): void {
    this.showEscalateForm = !this.showEscalateForm;
    this.escalatedTo = '';
  }

  /**
   * Escalate alert
   */
  escalateAlert(): void {
    if (!this.alert || !this.escalatedTo.trim()) {
      this.notificationService.warning('Please enter an escalation recipient');
      return;
    }

    if (!confirm('Are you sure you want to escalate this alert?')) {
      return;
    }

    this.actionLoading = true;
    const request: EscalateAlertRequest = {
      escalatedTo: this.escalatedTo
    };

    this.securityAlertService.escalateAlert(this.alert.id, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedAlert) => {
          this.alert = updatedAlert;
          this.showEscalateForm = false;
          this.escalatedTo = '';
          this.actionLoading = false;
          this.notificationService.success('Alert escalated successfully');
        },
        error: (error) => {
          console.error('Error escalating alert:', error);
          this.notificationService.error('Failed to escalate alert. Please try again.');
          this.actionLoading = false;
        }
      });
  }

  // ============================================
  // HELPER METHODS
  // ============================================

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
   * Get status color
   */
  getStatusColor(status: SecurityAlertStatus): string {
    return SecurityAlertHelpers.getStatusColor(status);
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
   * Check if action is available based on status
   */
  canAcknowledge(): boolean {
    return this.alert?.status === SecurityAlertStatus.NEW;
  }

  canMarkInProgress(): boolean {
    return this.alert?.status === SecurityAlertStatus.ACKNOWLEDGED;
  }

  canAssign(): boolean {
    return this.alert?.status !== SecurityAlertStatus.RESOLVED &&
           this.alert?.status !== SecurityAlertStatus.CLOSED &&
           this.alert?.status !== SecurityAlertStatus.FALSE_POSITIVE;
  }

  canResolve(): boolean {
    return this.alert?.status !== SecurityAlertStatus.RESOLVED &&
           this.alert?.status !== SecurityAlertStatus.CLOSED &&
           this.alert?.status !== SecurityAlertStatus.FALSE_POSITIVE;
  }

  canMarkFalsePositive(): boolean {
    return this.alert?.status !== SecurityAlertStatus.FALSE_POSITIVE &&
           this.alert?.status !== SecurityAlertStatus.RESOLVED &&
           this.alert?.status !== SecurityAlertStatus.CLOSED;
  }

  canEscalate(): boolean {
    return this.alert?.status !== SecurityAlertStatus.RESOLVED &&
           this.alert?.status !== SecurityAlertStatus.CLOSED &&
           this.alert?.status !== SecurityAlertStatus.FALSE_POSITIVE;
  }
}
