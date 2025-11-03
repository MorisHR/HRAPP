import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { EmployeeService } from '../../../core/services/employee.service';

@Component({
  selector: 'app-tenant-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    RouterModule
  ],
  templateUrl: './tenant-dashboard.component.html',
  styleUrl: './tenant-dashboard.component.scss'
})
export class TenantDashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private themeService = inject(ThemeService);
  private employeeService = inject(EmployeeService);

  user = this.authService.user;
  isDark = this.themeService.isDark;

  stats = signal({
    totalEmployees: 0,
    presentToday: 0,
    pendingLeaves: 0,
    activePayrollCycles: 1
  });

  menuItems = [
    { icon: 'dashboard', label: 'Dashboard', route: '/tenant/dashboard' },
    { icon: 'people', label: 'Employees', route: '/tenant/employees' },
    { icon: 'schedule', label: 'Attendance', route: '/tenant/attendance' },
    { icon: 'event_available', label: 'Leave Management', route: '/tenant/leave' },
    { icon: 'payments', label: 'Payroll', route: '/tenant/payroll' },
    { icon: 'analytics', label: 'Reports', route: '/tenant/reports' },
  ];

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.employeeService.getEmployees().subscribe({
      next: (employees) => {
        this.stats.set({
          totalEmployees: employees.length,
          presentToday: Math.floor(employees.length * 0.85), // Mock
          pendingLeaves: 5, // Mock
          activePayrollCycles: 1 // Mock
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
