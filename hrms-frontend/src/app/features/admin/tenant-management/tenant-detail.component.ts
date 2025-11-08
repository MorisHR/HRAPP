import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { TenantService } from '../../../core/services/tenant.service';
import { Tenant, TenantStatus } from '../../../core/models/tenant.model';

@Component({
  selector: 'app-tenant-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatDividerModule
  ],
  template: `
    <div class="tenant-detail-container">
      <mat-toolbar color="primary">
        <button mat-icon-button routerLink="/admin/tenants">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <span>Tenant Details</span>
        <span class="toolbar-spacer"></span>
        @if (tenant()) {
          <button mat-raised-button color="accent" [routerLink]="['/admin/tenants', tenant()!.id, 'edit']">
            <mat-icon>edit</mat-icon>
            Edit
          </button>
        }
      </mat-toolbar>

      <div class="content">
        @if (loading()) {
          <div class="loading-container">
            <mat-spinner></mat-spinner>
            <p>Loading tenant details...</p>
          </div>
        } @else if (error()) {
          <mat-card class="error-card">
            <mat-card-content>
              <mat-icon color="warn">error</mat-icon>
              <h2>Error Loading Tenant</h2>
              <p>{{ error() }}</p>
              <button mat-raised-button color="primary" routerLink="/admin/tenants">
                Back to List
              </button>
            </mat-card-content>
          </mat-card>
        } @else if (tenant()) {
          <mat-card class="detail-card">
            <mat-card-header>
              <mat-card-title>{{ tenant()!.companyName }}</mat-card-title>
              <mat-card-subtitle>
                <mat-chip [color]="getStatusColor(tenant()!.status)">
                  {{ tenant()!.status }}
                </mat-chip>
              </mat-card-subtitle>
            </mat-card-header>

            <mat-card-content>
              <div class="info-grid">
                <!-- Company Information -->
                <div class="info-section">
                  <h3>Company Information</h3>
                  <mat-divider></mat-divider>

                  <div class="info-row">
                    <span class="label">Company Name:</span>
                    <span class="value">{{ tenant()!.companyName }}</span>
                  </div>

                  <div class="info-row">
                    <span class="label">Subdomain:</span>
                    <span class="value">{{ tenant()!.subdomain }}</span>
                  </div>

                  <div class="info-row">
                    <span class="label">Contact Email:</span>
                    <span class="value">{{ tenant()!.contactEmail || 'N/A' }}</span>
                  </div>

                  <div class="info-row">
                    <span class="label">Contact Phone:</span>
                    <span class="value">{{ tenant()!.contactPhone || 'N/A' }}</span>
                  </div>

                  <div class="info-row">
                    <span class="label">Employee Count:</span>
                    <span class="value">{{ tenant()!.employeeCount || 0 }}</span>
                  </div>
                </div>

                <!-- Subscription Details -->
                <div class="info-section">
                  <h3>Subscription Details</h3>
                  <mat-divider></mat-divider>

                  <div class="info-row">
                    <span class="label">Status:</span>
                    <span class="value">
                      <mat-chip [color]="getStatusColor(tenant()!.status)">
                        {{ tenant()!.status }}
                      </mat-chip>
                    </span>
                  </div>

                  <div class="info-row">
                    <span class="label">Max Users:</span>
                    <span class="value">{{ tenant()!.maxUsers || 'Unlimited' }}</span>
                  </div>

                  <div class="info-row">
                    <span class="label">Storage Limit:</span>
                    <span class="value">{{ tenant()!.maxStorageGB || 'Unlimited' }} GB</span>
                  </div>

                  <div class="info-row">
                    <span class="label">API Calls/Month:</span>
                    <span class="value">{{ formatNumber(tenant()!.apiCallsPerMonth) || 'Unlimited' }}</span>
                  </div>
                </div>

                <!-- Timestamps -->
                <div class="info-section">
                  <h3>System Information</h3>
                  <mat-divider></mat-divider>

                  <div class="info-row">
                    <span class="label">Tenant ID:</span>
                    <span class="value">{{ tenant()!.id }}</span>
                  </div>

                  <div class="info-row">
                    <span class="label">Created At:</span>
                    <span class="value">{{ formatDate(tenant()!.createdAt) }}</span>
                  </div>

                  <div class="info-row">
                    <span class="label">Subscription Tier:</span>
                    <span class="value">{{ tenant()!.employeeTierDisplay || tenant()!.employeeTier }}</span>
                  </div>
                </div>
              </div>
            </mat-card-content>

            <mat-card-actions>
              <button mat-button routerLink="/admin/tenants">
                <mat-icon>arrow_back</mat-icon>
                Back to List
              </button>
              <button mat-raised-button color="accent" [routerLink]="['/admin/tenants', tenant()!.id, 'edit']">
                <mat-icon>edit</mat-icon>
                Edit Tenant
              </button>
            </mat-card-actions>
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .tenant-detail-container {
      height: 100%;
      display: flex;
      flex-direction: column;
    }

    .toolbar-spacer {
      flex: 1 1 auto;
    }

    .content {
      flex: 1;
      padding: 24px;
      overflow-y: auto;
      background-color: #f5f5f5;
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      gap: 16px;
    }

    .error-card {
      text-align: center;
      padding: 32px;
    }

    .error-card mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
    }

    .detail-card {
      max-width: 1200px;
      margin: 0 auto;
    }

    mat-card-header {
      margin-bottom: 24px;
    }

    .info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
      gap: 24px;
    }

    .info-section {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .info-section h3 {
      margin: 0;
      color: rgba(0, 0, 0, 0.87);
      font-size: 18px;
      font-weight: 500;
    }

    .info-row {
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
    }

    .info-row .label {
      font-weight: 500;
      color: rgba(0, 0, 0, 0.6);
    }

    .info-row .value {
      color: rgba(0, 0, 0, 0.87);
      text-align: right;
    }

    mat-card-actions {
      display: flex;
      gap: 12px;
      padding: 16px;
      justify-content: flex-end;
    }
  `]
})
export class TenantDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private tenantService = inject(TenantService);
  // Force recompile

  tenant = signal<Tenant | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('No tenant ID provided');
      this.loading.set(false);
      return;
    }

    console.log('🔍 Loading tenant details for ID:', id);

    this.tenantService.getTenantById(id).subscribe({
      next: (tenant) => {
        console.log('✅ Tenant loaded successfully:', tenant);
        this.tenant.set(tenant);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading tenant:', error);
        this.error.set('Failed to load tenant details. Please try again.');
        this.loading.set(false);
      }
    });
  }

  getStatusColor(status: TenantStatus): string {
    switch (status) {
      case TenantStatus.Active:
        return 'primary';
      case TenantStatus.Trial:
        return 'accent';
      case TenantStatus.Suspended:
        return 'warn';
      default:
        return '';
    }
  }

  formatDate(date: string | Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatNumber(num: number | null | undefined): string {
    if (!num) return 'N/A';
    return num.toLocaleString();
  }
}
