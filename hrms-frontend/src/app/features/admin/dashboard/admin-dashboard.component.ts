import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterModule } from '@angular/router';
import { TenantService } from '../../../core/services/tenant.service';
import { ThemeService } from '../../../core/services/theme.service';
import { AuthService } from '../../../core/services/auth.service';

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
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    RouterModule
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  private tenantService = inject(TenantService);
  private themeService = inject(ThemeService);
  private authService = inject(AuthService);

  // Signals for reactive state
  stats = signal<DashboardStats>({
    totalTenants: 0,
    activeTenants: 0,
    totalEmployees: 0,
    monthlyRevenue: 0
  });

  user = this.authService.user;
  isDark = this.themeService.isDark;

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

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.authService.logout();
  }
}
