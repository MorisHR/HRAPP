import { Component, signal, inject, OnInit, computed, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Chip, ChipColor, ButtonComponent, IconComponent, InputComponent, MenuComponent, MenuItem } from '@app/shared/ui';
import { UiModule } from '../../../shared/ui/ui.module';
import { TenantService } from '../../../core/services/tenant.service';
import { Tenant, TenantStatus } from '../../../core/models/tenant.model';
import { TableComponent, TableColumn } from '../../../shared/ui/components/table/table';
import { SuspendTenantModalComponent } from './modals/suspend-tenant-modal.component';
import { HardDeleteTenantModalComponent } from './modals/hard-delete-tenant-modal.component';
import { ReactivateTenantModalComponent } from './modals/reactivate-tenant-modal.component';

/**
 * FORTUNE 500 PATTERN: Enterprise tenant list with bulk operations
 * Features:
 * - Bulk selection with checkboxes
 * - Bulk action toolbar (suspend/reactivate/archive)
 * - Enterprise modals for lifecycle operations
 * - Optimistic updates (instant UI feedback)
 * - Real-time progress tracking
 * - Smart context menus based on tenant status
 */
@Component({
  selector: 'app-tenant-list',
  standalone: true,
  imports: [
    CommonModule,
    TableComponent,
    ButtonComponent,
    IconComponent,
    InputComponent,
    Chip,
    MenuComponent,
    UiModule,
    RouterModule
  ],
  templateUrl: './tenant-list.component.html',
  styleUrl: './tenant-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantListComponent implements OnInit {
  private tenantService = inject(TenantService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  // Reactive state from service
  tenants = this.tenantService.tenants;
  loading = this.tenantService.loading;
  selectedTenants = this.tenantService.selectedTenants;
  selectedCount = this.tenantService.selectedCount;

  // Local state
  searchTerm = signal<string>('');
  showArchived = signal<boolean>(false);
  bulkOperationInProgress = signal<boolean>(false);

  // Computed: Filtered tenants based on search
  filteredTenants = computed(() => {
    const search = this.searchTerm().toLowerCase();
    const allTenants = this.tenants();

    if (!search) return allTenants;

    return allTenants.filter(tenant =>
      tenant.companyName?.toLowerCase().includes(search) ||
      tenant.subdomain?.toLowerCase().includes(search)
    );
  });

  // Computed: Check if all visible tenants are selected
  allSelected = computed(() => {
    const filtered = this.filteredTenants();
    if (filtered.length === 0) return false;
    return filtered.every(t => this.tenantService.isSelected(t.id));
  });

  // Computed: Check if some (but not all) tenants are selected
  someSelected = computed(() => {
    const filtered = this.filteredTenants();
    const selected = filtered.filter(t => this.tenantService.isSelected(t.id));
    return selected.length > 0 && selected.length < filtered.length;
  });

  // Table columns configuration
  tableColumns: TableColumn[] = [
    { key: 'checkbox', label: '', width: '48px' },
    { key: 'companyName', label: 'Company', sortable: true },
    { key: 'subdomain', label: 'Subdomain', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'employeeTierDisplay', label: 'Tier', sortable: true },
    { key: 'currentUserCount', label: 'Users', sortable: true },
    { key: 'actions', label: '', width: '64px' }
  ];

  ngOnInit(): void {
    this.loadTenants();
  }

  private loadTenants(): void {
    this.tenantService.getTenants(true).subscribe({
      error: (error) => {
        this.showError('Failed to load tenants. Please try again.');
      }
    });
  }

  refresh(): void {
    this.loadTenants();
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.searchTerm.set(filterValue.trim());
  }

  // ═══════════════════════════════════════════════════════════════
  // BULK SELECTION
  // ═══════════════════════════════════════════════════════════════

  toggleSelectAll(): void {
    if (this.allSelected()) {
      this.tenantService.deselectAll();
    } else {
      const filtered = this.filteredTenants();
      filtered.forEach(t => this.tenantService.selectTenant(t.id));
    }
  }

  toggleTenantSelection(tenant: Tenant): void {
    this.tenantService.toggleTenantSelection(tenant.id);
  }

  isSelected(tenant: Tenant): boolean {
    return this.tenantService.isSelected(tenant.id);
  }

  clearSelection(): void {
    this.tenantService.deselectAll();
  }

  // ═══════════════════════════════════════════════════════════════
  // SINGLE TENANT OPERATIONS
  // ═══════════════════════════════════════════════════════════════

  openSuspendModal(tenant: Tenant): void {
    const dialogRef = this.dialog.open(SuspendTenantModalComponent, {
      data: { tenant },
      width: '600px',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.confirmed) {
        this.tenantService.suspendTenant(tenant.id, result.reason).subscribe({
          next: () => {
            this.showSuccess(`${tenant.companyName} has been suspended`);
          },
          error: (error) => {
            this.showError(`Failed to suspend tenant: ${error.message || 'Unknown error'}`);
          }
        });
      }
    });
  }

  openReactivateModal(tenant: Tenant): void {
    const dialogRef = this.dialog.open(ReactivateTenantModalComponent, {
      data: { tenant },
      width: '550px',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.confirmed) {
        this.tenantService.reactivateTenant(tenant.id).subscribe({
          next: () => {
            this.showSuccess(`${tenant.companyName} has been reactivated`);
          },
          error: (error) => {
            this.showError(`Failed to reactivate tenant: ${error.message || 'Unknown error'}`);
          }
        });
      }
    });
  }

  openArchiveModal(tenant: Tenant): void {
    const confirmed = confirm(
      `Archive ${tenant.companyName}?\n\n` +
      `This will:\n` +
      `• Block all user access\n` +
      `• Keep data for 30 days\n` +
      `• Allow restoration within grace period\n\n` +
      `Type reason for archiving:`
    );

    if (confirmed) {
      const reason = prompt('Reason for archiving:');
      if (reason) {
        this.tenantService.softDeleteTenant(tenant.id, reason).subscribe({
          next: () => {
            this.showSuccess(`${tenant.companyName} has been archived (30-day grace period)`);
          },
          error: (error) => {
            this.showError(`Failed to archive tenant: ${error.message || 'Unknown error'}`);
          }
        });
      }
    }
  }

  openHardDeleteModal(tenant: Tenant): void {
    const dialogRef = this.dialog.open(HardDeleteTenantModalComponent, {
      data: { tenant },
      width: '700px',
      disableClose: true // Force user to complete all steps or cancel
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.confirmed) {
        this.tenantService.hardDeleteTenant(tenant.id, result.confirmationName).subscribe({
          next: () => {
            this.showSuccess(`${tenant.companyName} has been permanently deleted`);
          },
          error: (error) => {
            this.showError(`Failed to delete tenant: ${error.message || 'Unknown error'}`);
          }
        });
      }
    });
  }

  // ═══════════════════════════════════════════════════════════════
  // BULK OPERATIONS
  // ═══════════════════════════════════════════════════════════════

  bulkSuspend(): void {
    const selected = this.getSelectedTenants();
    if (selected.length === 0) return;

    const reason = prompt(
      `Suspend ${selected.length} tenant(s)?\n\n` +
      `Enter reason for suspension:`
    );

    if (reason) {
      this.bulkOperationInProgress.set(true);
      const ids = selected.map(t => t.id);

      this.tenantService.bulkSuspendTenants(ids, reason).subscribe({
        next: (result) => {
          this.bulkOperationInProgress.set(false);
          this.tenantService.deselectAll();
          this.showSuccess(
            `Bulk suspend complete: ${result.success} succeeded, ${result.failed} failed`
          );
          this.loadTenants();
        },
        error: (error) => {
          this.bulkOperationInProgress.set(false);
          this.showError(`Bulk suspend failed: ${error.message || 'Unknown error'}`);
        }
      });
    }
  }

  bulkReactivate(): void {
    const selected = this.getSelectedTenants();
    if (selected.length === 0) return;

    const confirmed = confirm(
      `Reactivate ${selected.length} tenant(s)?\n\n` +
      `This will restore access for all selected tenants.`
    );

    if (confirmed) {
      this.bulkOperationInProgress.set(true);
      const ids = selected.map(t => t.id);

      this.tenantService.bulkReactivateTenants(ids).subscribe({
        next: (result) => {
          this.bulkOperationInProgress.set(false);
          this.tenantService.deselectAll();
          this.showSuccess(
            `Bulk reactivate complete: ${result.success} succeeded, ${result.failed} failed`
          );
          this.loadTenants();
        },
        error: (error) => {
          this.bulkOperationInProgress.set(false);
          this.showError(`Bulk reactivate failed: ${error.message || 'Unknown error'}`);
        }
      });
    }
  }

  bulkArchive(): void {
    const selected = this.getSelectedTenants();
    if (selected.length === 0) return;

    const reason = prompt(
      `Archive ${selected.length} tenant(s)?\n\n` +
      `Data will be kept for 30 days.\n` +
      `Enter reason for archiving:`
    );

    if (reason) {
      this.bulkOperationInProgress.set(true);
      const ids = selected.map(t => t.id);

      this.tenantService.bulkArchiveTenants(ids, reason).subscribe({
        next: (result) => {
          this.bulkOperationInProgress.set(false);
          this.tenantService.deselectAll();
          this.showSuccess(
            `Bulk archive complete: ${result.success} succeeded, ${result.failed} failed`
          );
          this.loadTenants();
        },
        error: (error) => {
          this.bulkOperationInProgress.set(false);
          this.showError(`Bulk archive failed: ${error.message || 'Unknown error'}`);
        }
      });
    }
  }

  private getSelectedTenants(): Tenant[] {
    return this.filteredTenants().filter(t => this.isSelected(t));
  }

  // ═══════════════════════════════════════════════════════════════
  // CONTEXT MENU (Smart menu based on tenant status)
  // ═══════════════════════════════════════════════════════════════

  getTenantMenuItems(tenant: Tenant): MenuItem[] {
    const baseItems: MenuItem[] = [
      { label: 'View Details', value: 'view', icon: 'visibility' },
      { label: 'Edit', value: 'edit', icon: 'edit' }
    ];

    // Status-specific actions
    if (tenant.status === TenantStatus.Active) {
      baseItems.push(
        { label: 'Suspend', value: 'suspend', icon: 'cancel' }
      );
    } else if (tenant.status === TenantStatus.Suspended) {
      baseItems.push(
        { label: 'Reactivate', value: 'reactivate', icon: 'check_circle' }
      );
    }

    // Archive/Delete options
    if (!tenant.softDeleteDate) {
      baseItems.push(
        { label: 'Archive', value: 'archive', icon: 'archive' }
      );
    } else {
      // If already archived, show restore or permanent delete
      baseItems.push(
        { label: 'Restore', value: 'reactivate', icon: 'restore' },
        { label: 'Delete Permanently', value: 'hard_delete', icon: 'delete_forever' }
      );
    }

    return baseItems;
  }

  handleTenantMenuClick(value: string, tenant: Tenant): void {
    switch (value) {
      case 'view':
        this.router.navigate(['/admin/tenants', tenant.id]);
        break;
      case 'edit':
        this.router.navigate(['/admin/tenants', tenant.id, 'edit']);
        break;
      case 'suspend':
        this.openSuspendModal(tenant);
        break;
      case 'reactivate':
        this.openReactivateModal(tenant);
        break;
      case 'archive':
        this.openArchiveModal(tenant);
        break;
      case 'hard_delete':
        this.openHardDeleteModal(tenant);
        break;
    }
  }

  // ═══════════════════════════════════════════════════════════════
  // UI HELPERS
  // ═══════════════════════════════════════════════════════════════

  getStatusColor(status: TenantStatus): ChipColor {
    switch (status) {
      case TenantStatus.Active:
        return 'success';
      case TenantStatus.Trial:
        return 'warning';
      case TenantStatus.Suspended:
        return 'error';
      default:
        return 'neutral';
    }
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['success-snackbar']
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 7000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['error-snackbar']
    });
  }
}
