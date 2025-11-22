import { Component, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';

// Material imports
import { MatButtonModule } from '@angular/material/button';

// Custom UI Module with all components
import { UiModule } from '../../../shared/ui/ui.module';

import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [
    RouterModule,
    MatButtonModule,
    UiModule
  ],
  templateUrl: './employee-dashboard.component.html',
  styleUrl: './employee-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmployeeDashboardComponent {
  private authService = inject(AuthService);
  private themeService = inject(ThemeService);

  user = this.authService.user;
  isDark = this.themeService.isDark;

  stats = signal({
    attendanceRate: 95,
    leaveBalance: 12,
    pendingLeaves: 0,
    lastPayslip: 'December 2025'
  });

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.authService.logout();
  }
}
