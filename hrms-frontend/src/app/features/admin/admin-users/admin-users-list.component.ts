import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

// Custom UI Components
import { CardComponent } from '../../../shared/ui/components/card/card';
import { ButtonComponent } from '../../../shared/ui/components/button/button';
import { Badge } from '../../../shared/ui/components/badge/badge';
import { Paginator, PageEvent } from '../../../shared/ui/components/paginator/paginator';

// Services
import { AdminUserService, AdminUser } from '../../../core/services/admin-user.service';

@Component({
  selector: 'app-admin-users-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardComponent,
    ButtonComponent,
    Badge,
    Paginator
  ],
  templateUrl: './admin-users-list.component.html',
  styleUrl: './admin-users-list.component.scss'
})
export class AdminUsersListComponent implements OnInit {
  private readonly adminUserService = inject(AdminUserService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  // State
  users = signal<AdminUser[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Pagination
  pageIndex = signal(0);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));

  // Filters
  searchTerm = signal('');
  activeFilter = signal<boolean | undefined>(undefined);

  ngOnInit(): void {
    this.loadUsers();
  }

  /**
   * Load SuperAdmin users with current filters
   */
  loadUsers(): void {
    this.loading.set(true);
    this.error.set(null);

    this.adminUserService
      .getAll(
        this.pageIndex(),
        this.pageSize(),
        this.searchTerm() || undefined,
        this.activeFilter()
      )
      .subscribe({
        next: (response) => {
          this.users.set(response.data);
          this.totalCount.set(response.pagination.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Failed to load admin users:', err);
          this.error.set('Failed to load admin users. Please try again.');
          this.loading.set(false);
          this.snackBar.open('Failed to load admin users', 'Close', { duration: 3000 });
        }
      });
  }

  /**
   * Handle search input
   */
  onSearch(): void {
    this.pageIndex.set(0); // Reset to first page
    this.loadUsers();
  }

  /**
   * Handle filter change
   */
  onFilterChange(active: boolean | undefined): void {
    this.activeFilter.set(active);
    this.pageIndex.set(0);
    this.loadUsers();
  }

  /**
   * Handle page change
   */
  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadUsers();
  }

  /**
   * Navigate to create user
   */
  createUser(): void {
    this.router.navigate(['/admin/admin-users/create']);
  }

  /**
   * Navigate to edit user
   */
  editUser(user: AdminUser): void {
    this.router.navigate(['/admin/admin-users', user.id, 'edit']);
  }

  /**
   * View user details
   */
  viewUser(user: AdminUser): void {
    this.router.navigate(['/admin/admin-users', user.id]);
  }

  /**
   * Change user password
   */
  changePassword(user: AdminUser): void {
    const newPassword = prompt('Enter new password (minimum 12 characters):');
    if (!newPassword) return;

    if (newPassword.length < 12) {
      this.snackBar.open('Password must be at least 12 characters long', 'Close', { duration: 3000 });
      return;
    }

    this.adminUserService.changePassword(user.id, { newPassword }).subscribe({
      next: (response) => {
        this.snackBar.open(response.message, 'Close', { duration: 3000 });
      },
      error: (err) => {
        console.error('Failed to change password:', err);
        this.snackBar.open(err.error?.message || 'Failed to change password', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Unlock user account
   */
  unlockAccount(user: AdminUser): void {
    if (confirm(`Are you sure you want to unlock the account for ${user.email}?`)) {
      this.adminUserService.unlockAccount(user.id).subscribe({
        next: (response) => {
          this.snackBar.open(response.message, 'Close', { duration: 3000 });
          this.loadUsers();
        },
        error: (err) => {
          console.error('Failed to unlock account:', err);
          this.snackBar.open(err.error?.message || 'Failed to unlock account', 'Close', { duration: 3000 });
        }
      });
    }
  }

  /**
   * Toggle user active status
   */
  toggleActive(user: AdminUser): void {
    const action = user.isActive ? 'deactivate' : 'activate';

    if (confirm(`Are you sure you want to ${action} ${user.email}?`)) {
      this.adminUserService.update(user.id, { isActive: !user.isActive }).subscribe({
        next: (response) => {
          this.snackBar.open(response.message, 'Close', { duration: 3000 });
          this.loadUsers();
        },
        error: (err) => {
          console.error(`Failed to ${action} user:`, err);
          this.snackBar.open(err.error?.message || `Failed to ${action} user`, 'Close', { duration: 3000 });
        }
      });
    }
  }

  /**
   * Delete user
   */
  deleteUser(user: AdminUser): void {
    if (user.isInitialSetupAccount) {
      this.snackBar.open('Cannot delete the initial setup account', 'Close', { duration: 3000 });
      return;
    }

    if (confirm(`Are you sure you want to delete ${user.email}? This action cannot be undone.`)) {
      this.adminUserService.delete(user.id).subscribe({
        next: (response) => {
          this.snackBar.open(response.message, 'Close', { duration: 3000 });
          this.loadUsers();
        },
        error: (err) => {
          console.error('Failed to delete user:', err);
          this.snackBar.open(err.error?.message || 'Failed to delete user', 'Close', { duration: 3000 });
        }
      });
    }
  }

  /**
   * Get status badge color
   */
  getStatusColor(user: AdminUser): 'success' | 'warning' | 'error' | 'primary' {
    if (user.isLocked) return 'error';
    if (!user.isActive) return 'warning';
    if (user.mustChangePassword) return 'primary';
    return 'success';
  }

  /**
   * Get status badge text
   */
  getStatusText(user: AdminUser): string {
    if (user.isLocked) return 'Locked';
    if (!user.isActive) return 'Inactive';
    if (user.mustChangePassword) return 'Password Reset Required';
    return 'Active';
  }

  /**
   * Format date
   */
  formatDate(date: string | undefined): string {
    if (!date) return 'Never';
    return new Date(date).toLocaleDateString();
  }

  /**
   * Check if password is expired
   */
  isPasswordExpired(user: AdminUser): boolean {
    if (!user.passwordExpiresAt) return false;
    return new Date(user.passwordExpiresAt) < new Date();
  }
}
