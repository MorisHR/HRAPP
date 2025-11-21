import { Component, signal, inject, OnInit, computed } from '@angular/core';

import { RouterModule, Router } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { List, ListItem } from '@app/shared/ui';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  description?: string;
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [
    RouterModule,
    MatSidenavModule,
    MatToolbarModule,
    List,
    ListItem,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule
],
  template: `
    <mat-sidenav-container class="admin-layout-container">
      <!-- Sidebar Navigation -->
      <mat-sidenav
        mode="side"
        opened
        class="admin-sidenav"
        [class.admin-sidenav-collapsed]="sidenavCollapsed()">

        <!-- Logo/Brand -->
        <div class="sidenav-header">
          @if (!sidenavCollapsed()) {
            <h2>HRMS Admin</h2>
          } @else {
            <mat-icon>admin_panel_settings</mat-icon>
          }
        </div>

        <!-- Navigation Items -->
        <app-list>
          @for (item of navItems; track item.route) {
            <app-list-item [clickable]="true">
              <a
                [routerLink]="item.route"
                routerLinkActive="active-link"
                [matTooltip]="sidenavCollapsed() ? item.label : ''"
                matTooltipPosition="right">
                <mat-icon class="item-icon">{{ item.icon }}</mat-icon>
                @if (!sidenavCollapsed()) {
                  <span class="item-label">{{ item.label }}</span>
                }
              </a>
            </app-list-item>
          }
        </app-list>

        <!-- Collapse/Expand Toggle -->
        <div class="sidenav-footer">
          <button
            mat-icon-button
            (click)="toggleSidenav()"
            [matTooltip]="sidenavCollapsed() ? 'Expand' : 'Collapse'"
            matTooltipPosition="right">
            <mat-icon>{{ sidenavCollapsed() ? 'chevron_right' : 'chevron_left' }}</mat-icon>
          </button>
        </div>
      </mat-sidenav>

      <!-- Main Content -->
      <mat-sidenav-content>
        <!-- Toolbar -->
        <mat-toolbar color="primary" class="admin-toolbar">
          <span class="toolbar-title">{{ pageTitle() }}</span>
          <span class="toolbar-spacer"></span>

          @if (user()) {
            <span class="user-name">{{ user()?.firstName }} {{ user()?.lastName }}</span>
          }

          <!-- Dark Mode Toggle -->
          <button
            mat-icon-button
            (click)="toggleTheme()"
            [matTooltip]="isDark() ? 'Light mode' : 'Dark mode'"
            matTooltipPosition="below">
            <mat-icon>{{ isDark() ? 'light_mode' : 'dark_mode' }}</mat-icon>
          </button>

          <!-- Logout -->
          <button
            mat-icon-button
            (click)="logout()"
            matTooltip="Logout"
            matTooltipPosition="below">
            <mat-icon>logout</mat-icon>
          </button>
        </mat-toolbar>

        <!-- Page Content -->
        <div class="admin-content">
          <router-outlet></router-outlet>
        </div>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .admin-layout-container {
      height: 100vh;
      width: 100%;
    }

    .admin-sidenav {
      width: 260px;
      transition: width 0.3s ease;
      border-right: 1px solid rgba(0, 0, 0, 0.12);
    }

    .admin-sidenav-collapsed {
      width: 64px;
    }

    .sidenav-header {
      padding: 20px 16px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-bottom: 1px solid rgba(0, 0, 0, 0.12);
      min-height: 64px;
    }

    .sidenav-header h2 {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 500;
    }

    .sidenav-footer {
      position: absolute;
      bottom: 0;
      left: 0;
      right: 0;
      padding: 8px;
      display: flex;
      justify-content: center;
      border-top: 1px solid rgba(0, 0, 0, 0.12);
    }

    app-list {
      padding-top: 8px;
      padding-bottom: 64px;

      a {
        margin: 4px 8px;
        padding: 12px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        gap: 12px;
        text-decoration: none;
        color: #525252;
        font-weight: 500;
        position: relative;
        border-left: 4px solid transparent;
        transition: all 250ms cubic-bezier(0.4, 0.0, 0.2, 1);
        background-color: transparent;

        &:hover {
          background-color: rgba(15, 98, 254, 0.04);
          color: #161616;
          transform: translateX(2px);

          .item-icon {
            color: #0F62FE;
          }
        }

        .item-icon {
          font-size: 20px;
          width: 20px;
          height: 20px;
          color: #6F6F6F;
          transition: color 250ms cubic-bezier(0.4, 0.0, 0.2, 1);
        }

        .item-label {
          flex: 1;
          transition: color 250ms cubic-bezier(0.4, 0.0, 0.2, 1);
        }
      }

      // Active link styling - Fortune 500 grade with left accent border
      a.active-link {
        background-color: rgba(15, 98, 254, 0.06) !important;
        color: #0F62FE !important;
        font-weight: 600 !important;
        border-left: 4px solid #0F62FE !important;
        padding-left: 10px !important;
        margin-left: 8px !important;

        .item-icon {
          color: #0F62FE !important;
          font-weight: 600;
        }

        .item-label {
          color: #0F62FE !important;
        }

        &:hover {
          background-color: rgba(15, 98, 254, 0.08) !important;
          transform: none !important;
        }
      }
    }

    .admin-toolbar {
      position: sticky;
      top: 0;
      z-index: 10;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .toolbar-title {
      font-size: 1.25rem;
      font-weight: 500;
    }

    .toolbar-spacer {
      flex: 1 1 auto;
    }

    .user-name {
      margin-right: 16px;
      font-size: 0.875rem;
    }

    .admin-content {
      padding: 24px;
      min-height: calc(100vh - 64px);
      background-color: #f5f5f5;
    }

    /* Dark mode adjustments */
    :host-context(.dark-theme) .admin-sidenav {
      border-right-color: rgba(255, 255, 255, 0.12);
    }

    :host-context(.dark-theme) .sidenav-header {
      border-bottom-color: rgba(255, 255, 255, 0.12);
    }

    :host-context(.dark-theme) .sidenav-footer {
      border-top-color: rgba(255, 255, 255, 0.12);
    }

    :host-context(.dark-theme) .admin-content {
      background-color: #303030;
    }
  `]
})
export class AdminLayoutComponent implements OnInit {
  private authService = inject(AuthService);
  private themeService = inject(ThemeService);
  private router = inject(Router);

  sidenavCollapsed = signal(false);
  user = this.authService.user;
  isDark = this.themeService.isDark;

  navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard', route: '/admin/dashboard' },
    { label: 'Tenants', icon: 'business', route: '/admin/tenants' },
    { label: 'Subscriptions', icon: 'payment', route: '/admin/subscriptions', description: 'Subscription & Payment Management' },
    { label: 'Revenue Analytics', icon: 'trending_up', route: '/admin/revenue-analytics', description: 'Revenue tracking & forecasting' },
    { label: 'Audit Trail', icon: 'history', route: '/admin/audit-logs', description: 'System-wide audit logs' },
    { label: 'Security Alerts', icon: 'security', route: '/admin/security-alerts', description: 'Real-time security alerts' },
    { label: 'Threat Detection', icon: 'shield-alert', route: '/admin/threat-detection', description: 'Rule-based threat monitoring' },
    { label: 'Legal Hold', icon: 'gavel', route: '/admin/legal-hold', description: 'Litigation & e-discovery' },
    { label: 'Compliance Reports', icon: 'assessment', route: '/admin/compliance-reports', description: 'SOX, GDPR, regulatory reports' },
    { label: 'Security Analytics', icon: 'timeline', route: '/admin/security-analytics', description: 'User behavior analytics' }
  ];

  pageTitle = computed(() => {
    const currentRoute = this.router.url;
    const navItem = this.navItems.find(item => currentRoute.startsWith(item.route));
    return navItem ? navItem.label : 'Admin Portal';
  });

  ngOnInit(): void {
    // Load collapsed state from localStorage
    const saved = localStorage.getItem('adminSidenavCollapsed');
    if (saved !== null) {
      this.sidenavCollapsed.set(saved === 'true');
    }
  }

  toggleSidenav(): void {
    this.sidenavCollapsed.update(val => !val);
    localStorage.setItem('adminSidenavCollapsed', this.sidenavCollapsed().toString());
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.authService.logout();
  }
}
