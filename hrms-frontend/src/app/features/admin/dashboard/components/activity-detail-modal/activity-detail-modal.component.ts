import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { UiModule } from '../../../../../shared/ui/ui.module';
import { ActivityLog, ActivityType } from '../../../../../core/models/dashboard.model';
import { ActivityLogService } from '../../../../../core/services/activity-log.service';

interface QuickAction {
  label: string;
  icon: string;
  action: string;
  variant?: 'primary' | 'secondary' | 'danger';
  requiresConfirmation?: boolean;
}

@Component({
  selector: 'app-activity-detail-modal',
  standalone: true,
  imports: [CommonModule, MatDialogModule, UiModule],
  templateUrl: './activity-detail-modal.component.html',
  styleUrl: './activity-detail-modal.component.scss'
})
export class ActivityDetailModalComponent {
  private dialogRef = inject(MatDialogRef<ActivityDetailModalComponent>);
  private router = inject(Router);
  protected activityService = inject(ActivityLogService);

  data = inject<ActivityLog>(MAT_DIALOG_DATA);

  processing = signal(false);

  get quickActions(): QuickAction[] {
    const actions: QuickAction[] = [];

    switch (this.data.type) {
      case ActivityType.PaymentFailed:
        actions.push(
          { label: 'Retry Payment', icon: 'refresh', action: 'retry_payment', variant: 'primary' },
          { label: 'Contact Tenant', icon: 'email', action: 'contact_tenant', variant: 'secondary' },
          { label: 'Suspend Tenant', icon: 'cancel', action: 'suspend_tenant', variant: 'danger', requiresConfirmation: true }
        );
        break;

      case ActivityType.SecurityAlert:
        actions.push(
          { label: 'View Details', icon: 'visibility', action: 'view_details', variant: 'primary' },
          { label: 'Block IP', icon: 'cancel', action: 'block_ip', variant: 'danger', requiresConfirmation: true },
          { label: 'Contact Tenant', icon: 'email', action: 'contact_tenant', variant: 'secondary' }
        );
        break;

      case ActivityType.TenantCreated:
      case ActivityType.TenantUpgraded:
      case ActivityType.TenantDowngraded:
        actions.push(
          { label: 'View Tenant', icon: 'business', action: 'view_tenant', variant: 'primary' },
          { label: 'Edit Tenant', icon: 'edit', action: 'edit_tenant', variant: 'secondary' }
        );
        break;

      case ActivityType.TenantSuspended:
        actions.push(
          { label: 'View Tenant', icon: 'business', action: 'view_tenant', variant: 'primary' },
          { label: 'Reactivate', icon: 'check_circle', action: 'reactivate_tenant', variant: 'secondary' }
        );
        break;

      case ActivityType.SystemError:
        actions.push(
          { label: 'View Logs', icon: 'description', action: 'view_logs', variant: 'primary' },
          { label: 'Report Issue', icon: 'bug_report', action: 'report_issue', variant: 'secondary' }
        );
        break;
    }

    return actions;
  }

  get metadataEntries(): { key: string; value: string; icon?: string }[] {
    if (!this.data.metadata) return [];

    return Object.entries(this.data.metadata).map(([key, value]) => {
      let icon: string | undefined;
      let displayValue = String(value);

      // Format specific metadata types
      switch (key) {
        case 'amount':
          icon = 'attach_money';
          displayValue = `$${value}`;
          break;
        case 'ip':
          icon = 'wifi';
          break;
        case 'attempts':
          icon = 'lock';
          displayValue = `${value} attempts`;
          break;
        case 'requestCount':
          icon = 'api';
          displayValue = `${value} requests`;
          break;
        case 'sizeGB':
          icon = 'storage';
          displayValue = `${value} GB`;
          break;
        case 'duration':
          icon = 'schedule';
          displayValue = `${value} seconds`;
          break;
      }

      return {
        key: this.formatKey(key),
        value: displayValue,
        icon
      };
    });
  }

  private formatKey(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  }

  handleQuickAction(actionOrEvent: QuickAction | MouseEvent): void {
    // Handle both direct action calls and event emissions
    const action = 'action' in actionOrEvent ? actionOrEvent as QuickAction : actionOrEvent as any;

    if (action.requiresConfirmation) {
      if (!confirm(`Are you sure you want to ${action.label.toLowerCase()}?`)) {
        return;
      }
    }

    this.processing.set(true);

    // Execute the action based on type
    switch (action.action) {
      case 'view_tenant':
        this.close();
        this.router.navigate(['/admin/tenant-management'], {
          queryParams: { tenantId: this.data.tenantId }
        });
        break;

      case 'edit_tenant':
        this.close();
        this.router.navigate(['/admin/tenant-management'], {
          queryParams: { tenantId: this.data.tenantId, edit: true }
        });
        break;

      case 'suspend_tenant':
        // TODO: Implement actual suspend logic via API
        console.log('Suspending tenant:', this.data.tenantId);
        setTimeout(() => {
          this.processing.set(false);
          alert('Tenant suspended successfully (mock)');
        }, 1000);
        break;

      case 'reactivate_tenant':
        // TODO: Implement actual reactivate logic via API
        console.log('Reactivating tenant:', this.data.tenantId);
        setTimeout(() => {
          this.processing.set(false);
          alert('Tenant reactivated successfully (mock)');
        }, 1000);
        break;

      case 'retry_payment':
        // TODO: Implement actual payment retry via API
        console.log('Retrying payment for tenant:', this.data.tenantId);
        setTimeout(() => {
          this.processing.set(false);
          alert('Payment retry initiated (mock)');
        }, 1000);
        break;

      case 'contact_tenant':
        // TODO: Implement email compose or contact modal
        console.log('Opening contact form for tenant:', this.data.tenantId);
        setTimeout(() => {
          this.processing.set(false);
          alert('Contact form opened (mock)');
        }, 500);
        break;

      case 'block_ip':
        // TODO: Implement IP blocking via API
        const ip = this.data.metadata?.['ip'];
        console.log('Blocking IP:', ip);
        setTimeout(() => {
          this.processing.set(false);
          alert(`IP ${ip} blocked successfully (mock)`);
        }, 1000);
        break;

      case 'view_details':
      case 'view_logs':
        // Navigate to detailed logs page
        this.close();
        this.router.navigate(['/admin/activity-logs'], {
          queryParams: {
            tenantId: this.data.tenantId,
            type: this.data.type
          }
        });
        break;

      case 'report_issue':
        // TODO: Open issue reporting modal/form
        console.log('Opening issue report form');
        setTimeout(() => {
          this.processing.set(false);
          alert('Issue report form opened (mock)');
        }, 500);
        break;

      default:
        console.log('Unknown action:', action.action);
        this.processing.set(false);
    }
  }

  getActivityIcon(): string {
    return this.activityService.getActivityIcon(this.data.type);
  }

  getFormattedTimestamp(): string {
    return this.data.timestamp.toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      timeZoneName: 'short'
    });
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      console.log('Copied to clipboard:', text);
      // TODO: Show success toast
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
