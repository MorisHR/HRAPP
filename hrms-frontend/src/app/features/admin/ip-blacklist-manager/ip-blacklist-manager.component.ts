import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SecurityAnalyticsService } from '../../../core/services/security-analytics.service';
import {
  IpBlacklist,
  BlacklistedIp,
  WhitelistedIp,
  AddIpToBlacklistRequest,
  AddIpToWhitelistRequest
} from '../../../core/models/security-analytics.models';
import { firstValueFrom } from 'rxjs';

/**
 * IP BLACKLIST MANAGEMENT COMPONENT
 * Full CRUD interface for IP blacklist/whitelist management
 *
 * FEATURES:
 * - View all blacklisted and whitelisted IPs
 * - Add IP to blacklist (permanent or temporary)
 * - Add IP to whitelist (trusted IPs)
 * - Remove IPs from blacklist/whitelist
 * - Filter and search capabilities
 * - Recent activity audit trail
 *
 * PATTERNS: Cloudflare WAF, AWS WAF, Azure Firewall
 * COMPLIANCE: PCI-DSS 1.3, NIST 800-53 SC-7
 */
@Component({
  selector: 'app-ip-blacklist-manager',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatChipsModule,
    MatSnackBarModule,
    MatTooltipModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './ip-blacklist-manager.component.html',
  styleUrls: ['./ip-blacklist-manager.component.scss']
})
export class IpBlacklistManagerComponent implements OnInit {
  private securityAnalyticsService = inject(SecurityAnalyticsService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  // Data signals
  ipBlacklistData = signal<IpBlacklist | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  // Form groups
  addToBlacklistForm: FormGroup;
  addToWhitelistForm: FormGroup;

  // UI state
  showAddBlacklistForm = signal<boolean>(false);
  showAddWhitelistForm = signal<boolean>(false);

  // Table columns
  blacklistColumns: string[] = ['ipAddress', 'blacklistedAt', 'reason', 'violationCount', 'isPermanent', 'threatLevel', 'actions'];
  whitelistColumns: string[] = ['ipAddress', 'whitelistedAt', 'reason', 'addedBy', 'actions'];
  activityColumns: string[] = ['timestamp', 'ipAddress', 'action', 'reason', 'performedBy'];

  constructor() {
    // Initialize forms
    this.addToBlacklistForm = this.fb.group({
      ipAddress: ['', [Validators.required, Validators.pattern(/^(\d{1,3}\.){3}\d{1,3}$/)]],
      reason: ['', [Validators.required, Validators.minLength(10)]],
      isPermanent: [false],
      expiresAt: [null]
    });

    this.addToWhitelistForm = this.fb.group({
      ipAddress: ['', [Validators.required, Validators.pattern(/^(\d{1,3}\.){3}\d{1,3}$/)]],
      reason: ['', [Validators.required, Validators.minLength(10)]],
      expiresAt: [null]
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  /**
   * Load IP blacklist data
   */
  async loadData(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const data = await firstValueFrom(
        this.securityAnalyticsService.getIpBlacklist()
      );
      this.ipBlacklistData.set(data);
    } catch (err: any) {
      this.error.set(err.message || 'Failed to load IP blacklist data');
      this.showError(this.error()!);
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Toggle add to blacklist form
   */
  toggleAddBlacklistForm(): void {
    this.showAddBlacklistForm.set(!this.showAddBlacklistForm());
    if (!this.showAddBlacklistForm()) {
      this.addToBlacklistForm.reset();
    }
  }

  /**
   * Toggle add to whitelist form
   */
  toggleAddWhitelistForm(): void {
    this.showAddWhitelistForm.set(!this.showAddWhitelistForm());
    if (!this.showAddWhitelistForm()) {
      this.addToWhitelistForm.reset();
    }
  }

  /**
   * Add IP to blacklist
   */
  async addToBlacklist(): Promise<void> {
    if (this.addToBlacklistForm.invalid) {
      return;
    }

    this.loading.set(true);

    try {
      const formValue = this.addToBlacklistForm.value;
      const request: AddIpToBlacklistRequest = {
        ipAddress: formValue.ipAddress.trim(),
        reason: formValue.reason.trim(),
        isPermanent: formValue.isPermanent,
        expiresAt: formValue.isPermanent ? undefined : formValue.expiresAt
      };

      const success = await firstValueFrom(
        this.securityAnalyticsService.addIpToBlacklist(request)
      );

      if (success) {
        this.showSuccess(`IP ${request.ipAddress} successfully added to blacklist`);
        this.addToBlacklistForm.reset();
        this.showAddBlacklistForm.set(false);
        await this.loadData();
      }
    } catch (err: any) {
      this.showError(err.message || 'Failed to add IP to blacklist');
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Remove IP from blacklist
   */
  async removeFromBlacklist(ipAddress: string): Promise<void> {
    if (!confirm(`Are you sure you want to remove ${ipAddress} from the blacklist?`)) {
      return;
    }

    this.loading.set(true);

    try {
      const success = await firstValueFrom(
        this.securityAnalyticsService.removeIpFromBlacklist(ipAddress)
      );

      if (success) {
        this.showSuccess(`IP ${ipAddress} removed from blacklist`);
        await this.loadData();
      }
    } catch (err: any) {
      this.showError(err.message || 'Failed to remove IP from blacklist');
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Add IP to whitelist
   */
  async addToWhitelist(): Promise<void> {
    if (this.addToWhitelistForm.invalid) {
      return;
    }

    this.loading.set(true);

    try {
      const formValue = this.addToWhitelistForm.value;
      const request: AddIpToWhitelistRequest = {
        ipAddress: formValue.ipAddress.trim(),
        reason: formValue.reason.trim(),
        expiresAt: formValue.expiresAt
      };

      const success = await firstValueFrom(
        this.securityAnalyticsService.addIpToWhitelist(request)
      );

      if (success) {
        this.showSuccess(`IP ${request.ipAddress} successfully added to whitelist`);
        this.addToWhitelistForm.reset();
        this.showAddWhitelistForm.set(false);
        await this.loadData();
      }
    } catch (err: any) {
      this.showError(err.message || 'Failed to add IP to whitelist');
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Remove IP from whitelist
   */
  async removeFromWhitelist(ipAddress: string): Promise<void> {
    if (!confirm(`Are you sure you want to remove ${ipAddress} from the whitelist?`)) {
      return;
    }

    this.loading.set(true);

    try {
      const success = await firstValueFrom(
        this.securityAnalyticsService.removeIpFromWhitelist(ipAddress)
      );

      if (success) {
        this.showSuccess(`IP ${ipAddress} removed from whitelist`);
        await this.loadData();
      }
    } catch (err: any) {
      this.showError(err.message || 'Failed to remove IP from whitelist');
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Get threat level color class
   */
  getThreatLevelClass(level: string): string {
    switch (level) {
      case 'Critical':
        return 'threat-critical';
      case 'High':
        return 'threat-high';
      case 'Medium':
        return 'threat-medium';
      case 'Low':
        return 'threat-low';
      default:
        return '';
    }
  }

  /**
   * Get action color class
   */
  getActionClass(action: string): string {
    switch (action) {
      case 'Blocked':
        return 'action-blocked';
      case 'Unblocked':
        return 'action-unblocked';
      case 'Whitelisted':
        return 'action-whitelisted';
      case 'Removed':
        return 'action-removed';
      default:
        return '';
    }
  }

  /**
   * Format timestamp
   */
  formatTimestamp(date: Date): string {
    return new Date(date).toLocaleString();
  }

  /**
   * Show success message
   */
  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['success-snackbar']
    });
  }

  /**
   * Show error message
   */
  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 7000,
      panelClass: ['error-snackbar']
    });
  }

  /**
   * Refresh data
   */
  async refresh(): Promise<void> {
    await this.loadData();
  }
}
