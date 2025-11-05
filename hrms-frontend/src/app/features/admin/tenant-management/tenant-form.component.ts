import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TenantService } from '../../../core/services/tenant.service';
import { PricingTierService, type TierType, type PricingTier } from '../../../core/services/pricing-tier.service';

@Component({
  selector: 'app-tenant-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatIconModule,
    MatToolbarModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './tenant-form.component.html',
  styleUrl: './tenant-form.component.scss'
})
export class TenantFormComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private tenantService = inject(TenantService);
  pricingService = inject(PricingTierService); // Public for template access
  private snackBar = inject(MatSnackBar);

  loading = signal(false);
  selectedTier = signal<PricingTier | null>(null);
  hidePassword = signal(true);

  tenantForm: FormGroup;

  // Get all available pricing tiers
  pricingTiers = this.pricingService.getAllTiers();
  enterpriseFeatures = this.pricingService.getEnterpriseFeatures();

  constructor() {
    this.tenantForm = this.fb.group({
      // Company Information
      companyName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      subdomain: ['', [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(63),
        Validators.pattern(/^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$/)
      ]],
      contactEmail: ['', [Validators.required, Validators.email]],
      contactPhone: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{1,14}$/)]],

      // Employee Tier (replaces old subscription plan)
      employeeTier: ['', [Validators.required]],

      // Hidden fields - auto-populated from tier
      maxUsers: [0],
      maxStorageGB: [0],
      apiCallsPerMonth: [0],
      monthlyPrice: [0],

      // Admin User
      adminUserName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      adminEmail: ['', [Validators.required, Validators.email]],
      adminPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$/)
      ]]
    });

    // Auto-update resource limits when employee tier changes
    this.tenantForm.get('employeeTier')?.valueChanges.subscribe(tierId => {
      this.onTierChange(tierId as TierType);
    });
  }

  onTierChange(tierId: TierType): void {
    const tier = this.pricingService.getTier(tierId);
    this.selectedTier.set(tier);

    // Auto-populate limits based on selected tier
    if (tier && tierId !== 'Custom') {
      this.tenantForm.patchValue({
        maxUsers: tier.maxUsers,
        maxStorageGB: tier.storageGB,
        apiCallsPerMonth: tier.apiCallsMonth,
        monthlyPrice: tier.price
      }, { emitEvent: false });
    } else if (tierId === 'Custom') {
      // For custom tier, use high defaults
      this.tenantForm.patchValue({
        maxUsers: 5000,
        maxStorageGB: 1000,
        apiCallsPerMonth: 5000000,
        monthlyPrice: 0
      }, { emitEvent: false });
    }
  }

  // Helper methods for template
  getTierPrice(): string {
    const tier = this.selectedTier();
    return tier ? this.pricingService.formatPrice(tier.price) : '';
  }

  getTierStorage(): string {
    const tier = this.selectedTier();
    return tier ? this.pricingService.formatStorage(tier.storageGB) : '';
  }

  getTierApiCalls(): string {
    const tier = this.selectedTier();
    return tier ? this.pricingService.formatApiCalls(tier.apiCallsMonth) : '';
  }

  getTierMaxUsers(): string | number {
    const tier = this.selectedTier();
    return tier ? tier.maxUsers : '';
  }

  onSubmit(): void {
    if (this.tenantForm.invalid) {
      this.tenantForm.markAllAsTouched();
      this.snackBar.open('Please fix all validation errors before submitting', 'Close', {
        duration: 5000,
        panelClass: ['error-snackbar']
      });
      return;
    }

    this.loading.set(true);

    this.tenantService.createTenant(this.tenantForm.value).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.snackBar.open('Tenant created successfully!', 'Close', {
          duration: 5000,
          panelClass: ['success-snackbar']
        });
        this.router.navigate(['/admin/tenants']);
      },
      error: (error) => {
        this.loading.set(false);
        const errorMessage = error.message || 'Failed to create tenant. Please try again.';
        this.snackBar.open(errorMessage, 'Close', {
          duration: 7000,
          panelClass: ['error-snackbar']
        });
        console.error('Error creating tenant:', error);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/tenants']);
  }

  getErrorMessage(fieldName: string): string {
    const control = this.tenantForm.get(fieldName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return 'This field is required';
    if (control.errors['email']) return 'Please enter a valid email address';
    if (control.errors['minlength']) return `Minimum ${control.errors['minlength'].requiredLength} characters required`;
    if (control.errors['maxlength']) return `Maximum ${control.errors['maxlength'].requiredLength} characters allowed`;
    if (control.errors['pattern']) {
      if (fieldName === 'subdomain') return 'Subdomain must be lowercase letters, numbers, and hyphens only';
      if (fieldName === 'contactPhone') return 'Please enter a valid phone number (e.g., +1234567890)';
      if (fieldName === 'adminPassword') return 'Password must contain uppercase, lowercase, number, and special character';
    }
    if (control.errors['min']) return `Minimum value is ${control.errors['min'].min}`;
    if (control.errors['max']) return `Maximum value is ${control.errors['max'].max}`;

    return 'Invalid value';
  }

  togglePasswordVisibility(): void {
    this.hidePassword.set(!this.hidePassword());
  }
}
