import { Component, signal, inject } from '@angular/core';

import { RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

interface MenuItem {
  label: string;
  icon: string;
  route?: string;
  children?: MenuItem[];
  expanded?: boolean;
}

@Component({
  selector: 'app-tenant-layout',
  standalone: true,
  imports: [
    RouterModule,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule
],
  template: `
    <mat-sidenav-container class="tenant-container">
      <!-- Sidebar -->
      <mat-sidenav mode="side" opened class="sidenav">
        <div class="sidenav-header">
          <h2>HRMS</h2>
          @if (user()) {
            <p class="tenant-name">{{ user()?.tenantName }}</p>
          }
        </div>

        <mat-nav-list>
          @for (item of menuItems; track item.label) {
            @if (item.route) {
              <!-- Regular menu item with route -->
              <a mat-list-item [routerLink]="item.route" routerLinkActive="active">
                <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
                <span matListItemTitle>{{ item.label }}</span>
              </a>
            } @else if (item.children) {
              <!-- Expandable menu item -->
              <a mat-list-item (click)="toggleMenu(item)" class="expandable-item">
                <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
                <span matListItemTitle>{{ item.label }}</span>
                <mat-icon class="expand-icon">{{ item.expanded ? 'expand_less' : 'expand_more' }}</mat-icon>
              </a>

              <!-- Submenu items -->
              @if (item.expanded) {
                <div class="submenu">
                  @for (child of item.children; track child.label) {
                    @if (child.route) {
                      <a mat-list-item [routerLink]="child.route" routerLinkActive="active" class="submenu-item">
                        <mat-icon matListItemIcon>{{ child.icon }}</mat-icon>
                        <span matListItemTitle>{{ child.label }}</span>
                      </a>
                    } @else if (child.children) {
                      <!-- Nested expandable item -->
                      <a mat-list-item (click)="toggleMenu(child)" class="submenu-item expandable-item">
                        <mat-icon matListItemIcon>{{ child.icon }}</mat-icon>
                        <span matListItemTitle>{{ child.label }}</span>
                        <mat-icon class="expand-icon">{{ child.expanded ? 'expand_less' : 'expand_more' }}</mat-icon>
                      </a>

                      <!-- Nested submenu -->
                      @if (child.expanded) {
                        <div class="submenu submenu-nested">
                          @for (nestedChild of child.children; track nestedChild.label) {
                            <a mat-list-item [routerLink]="nestedChild.route" routerLinkActive="active" class="submenu-item-nested">
                              <mat-icon matListItemIcon>{{ nestedChild.icon }}</mat-icon>
                              <span matListItemTitle>{{ nestedChild.label }}</span>
                            </a>
                          }
                        </div>
                      }
                    }
                  }
                </div>
              }
            }
          }
        </mat-nav-list>
      </mat-sidenav>

      <!-- Main Content -->
      <mat-sidenav-content>
        <mat-toolbar color="primary">
          <span class="toolbar-title">HR Portal</span>
          <span class="toolbar-spacer"></span>
          @if (user()) {
            <span class="user-name">{{ user()?.firstName }} {{ user()?.lastName }}</span>
          }
          <button mat-icon-button (click)="toggleTheme()">
            <mat-icon>{{ isDark() ? 'light_mode' : 'dark_mode' }}</mat-icon>
          </button>
          <button mat-icon-button (click)="logout()">
            <mat-icon>logout</mat-icon>
          </button>
        </mat-toolbar>

        <div class="content-wrapper">
          <router-outlet></router-outlet>
        </div>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .tenant-container {
      height: 100vh;
    }

    .sidenav {
      width: 250px;
      background: #ffffff;
      border-right: 1px solid #e0e0e0;
    }

    .sidenav-header {
      padding: 24px 16px;
      border-bottom: 1px solid #e0e0e0;

      h2 {
        margin: 0;
        font-size: 1.5rem;
        font-weight: 700;
        color: #000000;
      }

      .tenant-name {
        margin: 8px 0 0 0;
        font-size: 0.875rem;
        color: #666;
      }
    }

    mat-nav-list {
      padding-top: 8px;

      a {
        margin: 4px 8px;
        border-radius: 8px;
        transition: background-color 0.2s;

        &:hover {
          background-color: rgba(0, 0, 0, 0.04);
        }

        &.active {
          background-color: #000000;
          color: #ffffff;

          mat-icon {
            color: #ffffff;
          }
        }
      }

      .expandable-item {
        cursor: pointer;
        user-select: none;

        .expand-icon {
          margin-left: auto;
          font-size: 20px;
          width: 20px;
          height: 20px;
          transition: transform 0.2s;
        }
      }

      .submenu {
        padding-left: 16px;
        margin-top: 4px;

        .submenu-item {
          margin: 2px 8px;
          padding-left: 24px;
          font-size: 0.9rem;

          mat-icon {
            font-size: 20px;
            width: 20px;
            height: 20px;
          }
        }

        .submenu-nested {
          padding-left: 16px;

          .submenu-item-nested {
            margin: 2px 8px;
            padding-left: 32px;
            font-size: 0.85rem;

            mat-icon {
              font-size: 18px;
              width: 18px;
              height: 18px;
            }
          }
        }
      }
    }

    mat-toolbar {
      background: #ffffff;
      color: #000000;
      border-bottom: 1px solid #e0e0e0;
    }

    .toolbar-title {
      font-size: 1.25rem;
      font-weight: 600;
    }

    .toolbar-spacer {
      flex: 1 1 auto;
    }

    .user-name {
      margin-right: 16px;
      font-size: 0.875rem;
      color: #666;
    }

    .content-wrapper {
      padding: 24px;
      min-height: calc(100vh - 64px);
      background: #fafafa;
    }
  `]
})
export class TenantLayoutComponent {
  private authService = inject(AuthService);
  private themeService = inject(ThemeService);

  user = this.authService.user;
  isDark = this.themeService.isDark;

  menuItems: MenuItem[] = [
    { label: 'Dashboard', icon: 'dashboard', route: '/tenant/dashboard' },
    { label: 'Employees', icon: 'people', route: '/tenant/employees' },
    { label: 'Attendance', icon: 'event_available', route: '/tenant/attendance' },
    { label: 'Leave Management', icon: 'beach_access', route: '/tenant/leave' },
    { label: 'Timesheets', icon: 'schedule', route: '/tenant/timesheets/approvals' },
    { label: 'Payroll', icon: 'payments', route: '/tenant/payroll' },
    { label: 'Reports', icon: 'assessment', route: '/tenant/reports' },
    { label: 'Billing', icon: 'credit_card', route: '/tenant/billing' },
    { label: 'Audit Logs', icon: 'history', route: '/tenant/audit-logs' },
    {
      label: 'Settings',
      icon: 'settings',
      expanded: false,
      children: [
        {
          label: 'Organization',
          icon: 'business',
          expanded: false,
          children: [
            { label: 'Departments', icon: 'corporate_fare', route: '/tenant/settings/organization/departments' },
            { label: 'Locations', icon: 'location_on', route: '/tenant/organization/locations' },
            { label: 'Biometric Devices', icon: 'devices', route: '/tenant/organization/devices' }
          ]
        }
      ]
    }
  ];

  toggleMenu(item: MenuItem): void {
    if (item.children) {
      item.expanded = !item.expanded;
    }
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.authService.logout();
  }
}
