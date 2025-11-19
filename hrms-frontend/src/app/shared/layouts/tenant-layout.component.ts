import { Component, signal, inject } from '@angular/core';

import { RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { List, ListItem } from '@app/shared/ui';
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
    List,
    ListItem,
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

        <app-list>
          @for (item of menuItems; track item.label) {
            @if (item.route) {
              <!-- Regular menu item with route -->
              <app-list-item [clickable]="true">
                <a [routerLink]="item.route" routerLinkActive="active">
                  <mat-icon class="item-icon">{{ item.icon }}</mat-icon>
                  <span class="item-label">{{ item.label }}</span>
                </a>
              </app-list-item>
            } @else if (item.children) {
              <!-- Expandable menu item -->
              <app-list-item [clickable]="true" (itemClick)="toggleMenu(item)" class="expandable-item">
                <mat-icon class="item-icon">{{ item.icon }}</mat-icon>
                <span class="item-label">{{ item.label }}</span>
                <mat-icon class="expand-icon">{{ item.expanded ? 'expand_less' : 'expand_more' }}</mat-icon>
              </app-list-item>

              <!-- Submenu items -->
              @if (item.expanded) {
                <div class="submenu">
                  @for (child of item.children; track child.label) {
                    @if (child.route) {
                      <app-list-item [clickable]="true" class="submenu-item">
                        <a [routerLink]="child.route" routerLinkActive="active">
                          <mat-icon class="item-icon">{{ child.icon }}</mat-icon>
                          <span class="item-label">{{ child.label }}</span>
                        </a>
                      </app-list-item>
                    } @else if (child.children) {
                      <!-- Nested expandable item -->
                      <app-list-item [clickable]="true" (itemClick)="toggleMenu(child)" class="submenu-item expandable-item">
                        <mat-icon class="item-icon">{{ child.icon }}</mat-icon>
                        <span class="item-label">{{ child.label }}</span>
                        <mat-icon class="expand-icon">{{ child.expanded ? 'expand_less' : 'expand_more' }}</mat-icon>
                      </app-list-item>

                      <!-- Nested submenu -->
                      @if (child.expanded) {
                        <div class="submenu submenu-nested">
                          @for (nestedChild of child.children; track nestedChild.label) {
                            <app-list-item [clickable]="true" class="submenu-item-nested">
                              <a [routerLink]="nestedChild.route" routerLinkActive="active">
                                <mat-icon class="item-icon">{{ nestedChild.icon }}</mat-icon>
                                <span class="item-label">{{ nestedChild.label }}</span>
                              </a>
                            </app-list-item>
                          }
                        </div>
                      }
                    }
                  }
                </div>
              }
            }
          }
        </app-list>
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

    app-list {
      padding-top: 8px;

      a {
        margin: 4px 8px;
        padding: 12px;
        border-radius: 10px;
        transition: all 250ms cubic-bezier(0.4, 0.0, 0.2, 1);
        display: flex;
        align-items: center;
        gap: 12px;
        text-decoration: none;
        color: #525252;
        font-weight: 500;
        position: relative;
        border: 2px solid transparent;

        &:hover {
          background-color: rgba(15, 98, 254, 0.04);
          color: #161616;
          transform: translateX(2px);

          .item-icon {
            color: #0F62FE;
          }
        }

        &.active {
          background: rgba(15, 98, 254, 0.02);
          color: #0F62FE;
          font-weight: 600;
          border-left: 4px solid #0F62FE;
          padding-left: 10px;

          .item-icon {
            color: #0F62FE;
            font-weight: 600;
          }

          .item-label {
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
          font-size: 0.9rem;

          a {
            padding: 10px 12px 10px 24px;
            font-size: 0.9rem;

            &.active {
              background: rgba(15, 98, 254, 0.02);
              border-left: 4px solid #0F62FE;
              padding-left: 22px;
            }
          }

          .item-icon {
            font-size: 18px;
            width: 18px;
            height: 18px;
          }
        }

        .submenu-nested {
          padding-left: 16px;

          .submenu-item-nested {
            margin: 2px 8px;
            font-size: 0.85rem;

            a {
              padding: 8px 12px 8px 32px;
              font-size: 0.85rem;

              &.active {
                background: rgba(15, 98, 254, 0.02);
                border-left: 4px solid #0F62FE;
                padding-left: 30px;
              }
            }

            .item-icon {
              font-size: 16px;
              width: 16px;
              height: 16px;
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
