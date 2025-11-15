import { Component, signal, inject, OnInit } from '@angular/core';

// Material imports (keeping temporarily for backwards compatibility)
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { RouterModule } from '@angular/router';

// Custom UI components
import { CardComponent } from '../../../shared/ui/components/card/card';
import { IconComponent } from '../../../shared/ui/components/icon/icon';

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
    // Material imports (keeping for now)
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    RouterModule,
    // Custom UI components
    CardComponent,
    IconComponent
],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
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
