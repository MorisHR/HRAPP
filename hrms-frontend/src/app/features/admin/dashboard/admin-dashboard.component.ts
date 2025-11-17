import { Component, signal, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';

// Custom UI Module with all components
import { UiModule } from '../../../shared/ui/ui.module';

import { TenantService } from '../../../core/services/tenant.service';

interface DashboardStats {
  totalTenants: number;
  activeTenants: number;
  totalEmployees: number;
  monthlyRevenue: number;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    RouterModule,
    UiModule
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDashboardComponent implements OnInit {
  private tenantService = inject(TenantService);

  // Signals for reactive state
  stats = signal<DashboardStats>({
    totalTenants: 0,
    activeTenants: 0,
    totalEmployees: 0,
    monthlyRevenue: 0
  });

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.tenantService.getTenants().subscribe({
      next: (tenants) => {
        const activeTenants = tenants.filter(t => t.status === 'Active').length;
        const totalEmployees = tenants.reduce((sum, t) => sum + t.employeeCount, 0);

        this.stats.set({
          totalTenants: tenants.length,
          activeTenants: activeTenants,
          totalEmployees: totalEmployees,
          monthlyRevenue: activeTenants * 99 // Mock calculation
        });
      }
    });
  }
}
