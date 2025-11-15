import { Component, signal, inject } from '@angular/core';

// Material imports (keeping temporarily for backwards compatibility)
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterModule } from '@angular/router';

// Custom UI components
import { CardComponent } from '../../../shared/ui/components/card/card';
import { IconComponent } from '../../../shared/ui/components/icon/icon';
import { Toolbar } from '../../../shared/ui/components/toolbar/toolbar';

import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [
    // Material imports (keeping for now)
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    RouterModule,
    // Custom UI components
    CardComponent,
    IconComponent,
    Toolbar
],
  templateUrl: './employee-dashboard.component.html',
  styleUrl: './employee-dashboard.component.scss'
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
