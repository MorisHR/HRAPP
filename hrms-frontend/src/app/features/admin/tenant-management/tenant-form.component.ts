import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { UiModule } from '../../../shared/ui/ui.module';
import { CardComponent, ButtonComponent, IconComponent, InputComponent, SelectComponent, ToastService, ProgressSpinner } from '../../../shared/ui';
import { TenantService } from '../../../core/services/tenant.service';
import { PricingTierService, type TierType, type PricingTier } from '../../../core/services/pricing-tier.service';
import { IndustrySectorService } from '../../../core/services/industry-sector.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-tenant-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    UiModule,
    CardComponent,
    ButtonComponent,
    IconComponent,
    InputComponent,
    SelectComponent,
    ProgressSpinner,
  ],
  templateUrl: './tenant-form.component.html',
  styleUrl: './tenant-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private tenantService = inject(TenantService);
  pricingService = inject(PricingTierService); // Public for template access
  sectorService = inject(IndustrySectorService); // Public for template access
  private toastService = inject(ToastService);

  loading = signal(false);
  selectedTier = signal<PricingTier | null>(null);
  hidePassword = signal(true);

  // Edit mode tracking
  isEditMode = signal(false);
  tenantId = signal<string | null>(null);

  tenantForm: FormGroup;

  // Get all available pricing tiers
  pricingTiers = this.pricingService.getAllTiers();
  enterpriseFeatures = this.pricingService.getEnterpriseFeatures();

  // FORTUNE 500: Load industry sectors with caching (1 request per session)
  sectors$ = this.sectorService.getSectors();

  // Computed options for select dropdowns
  sectorOptions = signal<Array<{ value: number | string | null; label: string }>>([
    { value: null, label: 'Not specified' }
  ]);

  tierOptions = computed(() =>
    this.pricingTiers.map(tier => ({
      value: tier.id,
      label: `${tier.name} - ${this.pricingService.formatPrice(tier.price)}/mo (${this.pricingService.formatStorage(tier.storageGB)} Storage, ${this.pricingService.formatApiCalls(tier.apiCallsMonth)} API calls/month)`
    }))
  );

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

      // Industry Sector (optional for backwards compatibility)
      sectorId: [null], // Nullable = optional field

      // Employee Tier (replaces old subscription plan)
      employeeTier: ['', [Validators.required]],

      // Hidden fields - auto-populated from tier
      maxUsers: [0],
      maxStorageGB: [0],
      apiCallsPerMonth: [0],
      monthlyPrice: [0],

      // Admin User (only for create mode)
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

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    // Load sector options
    this.sectors$.subscribe(sectors => {
      this.sectorOptions.set([
        { value: null, label: 'Not specified' },
        ...sectors.map(s => ({ value: s.id, label: `${s.code} - ${s.name}` }))
      ]);
    });

    if (id) {
      // Edit mode
      this.isEditMode.set(true);
      this.tenantId.set(id);
      this.loadTenantData(id);

      // Make admin user fields optional in edit mode
      this.tenantForm.get('adminUserName')?.clearValidators();
      this.tenantForm.get('adminEmail')?.clearValidators();
      this.tenantForm.get('adminPassword')?.clearValidators();
      this.tenantForm.get('adminUserName')?.updateValueAndValidity();
      this.tenantForm.get('adminEmail')?.updateValueAndValidity();
      this.tenantForm.get('adminPassword')?.updateValueAndValidity();
    }
  }

  private loadTenantData(id: string): void {
    this.loading.set(true);
    console.log('📝 Loading tenant data for edit mode, ID:', id);

    this.tenantService.getTenantById(id).subscribe({
      next: (tenant) => {
        console.log('✅ Tenant data loaded:', tenant);

        // Populate form with tenant data
        this.tenantForm.patchValue({
          companyName: tenant.companyName,
          subdomain: tenant.subdomain,
          contactEmail: tenant.contactEmail || '',
          contactPhone: tenant.contactPhone || '',
          sectorId: tenant.sectorId || null, // FORTUNE 500: Load sector
          employeeTier: tenant.employeeTier || 'Startup',
          maxUsers: tenant.maxUsers || 0,
          maxStorageGB: tenant.maxStorageGB || 0,
          apiCallsPerMonth: tenant.apiCallsPerMonth || 0,
          monthlyPrice: tenant.monthlyPrice || 0
        });

        // Disable subdomain field in edit mode (can't change subdomain)
        this.tenantForm.get('subdomain')?.disable();

        this.loading.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading tenant data:', error);
        this.loading.set(false);
        this.toastService.error('Failed to load tenant data. Please try again.', 5000);
        this.router.navigate(['/admin/tenants']);
      }
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
      this.toastService.error('Please fix all validation errors before submitting', 5000);
      return;
    }

    this.loading.set(true);

    if (this.isEditMode()) {
      // UPDATE MODE
      const id = this.tenantId();
      if (!id) {
        this.toastService.error('Invalid tenant ID', 5000);
        this.loading.set(false);
        return;
      }

      // Get form value (subdomain will be excluded because it's disabled)
      const formValue = this.tenantForm.getRawValue();

      // Prepare update payload (exclude admin fields in edit mode)
      const updatePayload = {
        companyName: formValue.companyName,
        contactEmail: formValue.contactEmail,
        contactPhone: formValue.contactPhone,
        sectorId: formValue.sectorId, // FORTUNE 500: Include sector
        employeeTier: formValue.employeeTier,
        maxUsers: formValue.maxUsers,
        maxStorageGB: formValue.maxStorageGB,
        apiCallsPerMonth: formValue.apiCallsPerMonth,
        monthlyPrice: formValue.monthlyPrice
      };

      console.log('📝 Updating tenant:', id, updatePayload);

      this.tenantService.updateTenant(id, updatePayload).subscribe({
        next: (response) => {
          this.loading.set(false);
          this.toastService.success('Tenant updated successfully!', 5000);
          this.router.navigate(['/admin/tenants']);
        },
        error: (error) => {
          this.loading.set(false);
          const errorMessage = error.error?.message || error.message || 'Failed to update tenant. Please try again.';
          this.toastService.error(errorMessage, 7000);
          console.error('Error updating tenant:', error);
        }
      });
    } else {
      // CREATE MODE
      this.tenantService.createTenant(this.tenantForm.value).subscribe({
        next: (response) => {
          this.loading.set(false);
          this.toastService.success('Tenant created successfully!', 5000);
          this.router.navigate(['/admin/tenants']);
        },
        error: (error) => {
          this.loading.set(false);
          const errorMessage = error.error?.message || error.message || 'Failed to create tenant. Please try again.';
          this.toastService.error(errorMessage, 7000);
          console.error('Error creating tenant:', error);
        }
      });
    }
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
