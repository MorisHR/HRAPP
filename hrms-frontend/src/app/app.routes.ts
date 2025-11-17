import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { superAdminGuard, hrGuard } from './core/guards/role.guard';
import { subdomainGuard } from './core/guards/subdomain.guard';
import { alreadyLoggedInGuard } from './core/guards/already-logged-in.guard';

export const routes: Routes = [
  // Landing Page - Marketing homepage
  {
    path: '',
    loadComponent: () => import('./features/marketing/landing-page.component').then(m => m.LandingPageComponent),
    pathMatch: 'full'
  },

  // Authentication routes
  // SECURITY NOTES:
  // - /subdomain and /superadmin routes do NOT have alreadyLoggedInGuard
  //   This allows logout flow to navigate to these pages after clearing auth state
  // - Other login routes CAN have the guard to prevent authenticated users from accessing them
  {
    path: 'auth',
    children: [
      {
        path: 'subdomain',
        // NO GUARD - Allow logout to navigate here
        loadComponent: () => import('./features/auth/subdomain/subdomain.component').then(m => m.SubdomainComponent)
      },
      {
        path: 'login',
        canActivate: [subdomainGuard, alreadyLoggedInGuard],
        loadComponent: () => import('./features/auth/login/tenant-login.component').then(m => m.TenantLoginComponent)
      },
      {
        path: 'superadmin',
        // NO GUARD - Allow logout to navigate here
        loadComponent: () => import('./features/auth/superadmin/superadmin-login.component').then(m => m.SuperAdminLoginComponent)
      },
      {
        path: 'activate',
        loadComponent: () => import('./features/auth/activate/activate.component').then(m => m.ActivateComponent)
      },
      {
        path: 'resend-activation',
        loadComponent: () => import('./features/auth/resend-activation/resend-activation.component').then(m => m.ResendActivationComponent)
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
      },
      {
        path: 'reset-password',
        loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
      },
      {
        path: 'set-password',
        loadComponent: () => import('./features/auth/set-password/set-password.component').then(m => m.SetPasswordComponent)
      }
    ]
  },

  // Legacy login route (kept for backward compatibility)
  {
    path: 'login',
    redirectTo: '/auth/subdomain',
    pathMatch: 'full'
  },

  // Admin Portal Routes - ALL WRAPPED IN SHARED LAYOUT
  {
    path: 'admin',
    canActivate: [superAdminGuard],
    loadComponent: () => import('./shared/layouts/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/admin/dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'tenants',
        loadComponent: () => import('./features/admin/tenant-management/tenant-list.component').then(m => m.TenantListComponent)
      },
      {
        path: 'tenants/new',
        loadComponent: () => import('./features/admin/tenant-management/tenant-form.component').then(m => m.TenantFormComponent)
      },
      {
        path: 'tenants/:id',
        loadComponent: () => import('./features/admin/tenant-management/tenant-detail.component').then(m => m.TenantDetailComponent)
      },
      {
        path: 'tenants/:id/edit',
        loadComponent: () => import('./features/admin/tenant-management/tenant-form.component').then(m => m.TenantFormComponent)
      },
      {
        path: 'audit-logs',
        loadComponent: () => import('./features/admin/audit-logs/audit-logs.component').then(m => m.AdminAuditLogsComponent),
        data: { title: 'System Audit Logs' }
      },
      {
        path: 'security-alerts',
        children: [
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
          },
          {
            path: 'dashboard',
            loadComponent: () => import('./features/admin/security-alerts/components/dashboard/security-alerts-dashboard.component').then(m => m.SecurityAlertsDashboardComponent),
            data: { title: 'Security Alerts Dashboard' }
          },
          {
            path: 'list',
            loadComponent: () => import('./features/admin/security-alerts/components/list/alert-list.component').then(m => m.AlertListComponent),
            data: { title: 'Security Alerts List' }
          },
          {
            path: 'detail/:id',
            loadComponent: () => import('./features/admin/security-alerts/components/detail/alert-detail.component').then(m => m.AlertDetailComponent),
            data: { title: 'Alert Details' }
          }
        ]
      },
      {
        path: 'anomaly-detection',
        loadComponent: () => import('./features/admin/anomaly-detection/anomaly-detection-dashboard.component').then(m => m.AnomalyDetectionDashboardComponent),
        data: { title: 'Anomaly Detection' }
      },
      {
        path: 'legal-hold',
        loadComponent: () => import('./features/admin/legal-hold/legal-hold-list.component').then(m => m.LegalHoldListComponent),
        data: { title: 'Legal Hold Management' }
      },
      {
        path: 'compliance-reports',
        loadComponent: () => import('./features/admin/compliance-reports/compliance-reports.component').then(m => m.ComplianceReportsComponent),
        data: { title: 'Compliance Reports' }
      },
      {
        path: 'activity-correlation',
        loadComponent: () => import('./features/admin/activity-correlation/activity-correlation.component').then(m => m.ActivityCorrelationComponent),
        data: { title: 'Activity Correlation' }
      },
      {
        path: 'subscriptions',
        loadComponent: () => import('./features/admin/subscription-management/subscription-dashboard.component').then(m => m.SubscriptionDashboardComponent),
        data: { title: 'Subscription Management' }
      },
      // Location Management Routes (Super Admin)
      {
        path: 'locations',
        loadComponent: () => import('./features/admin/locations/location-list.component').then(m => m.LocationListComponent),
        data: { title: 'Location Management' }
      },
      {
        path: 'locations/new',
        loadComponent: () => import('./features/admin/locations/location-form.component').then(m => m.LocationFormComponent),
        data: { title: 'Create Location' }
      },
      {
        path: 'locations/:id/edit',
        loadComponent: () => import('./features/admin/locations/location-form.component').then(m => m.LocationFormComponent),
        data: { title: 'Edit Location' }
      },
      // Monitoring Routes - Real-time observability and system health
      {
        path: 'monitoring',
        children: [
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
          },
          {
            path: 'dashboard',
            loadComponent: () => import('./features/admin/monitoring/dashboard/monitoring-dashboard.component').then(m => m.MonitoringDashboardComponent),
            data: { title: 'Monitoring Dashboard' }
          },
          {
            path: 'infrastructure',
            loadComponent: () => import('./features/admin/monitoring/infrastructure/infrastructure-health.component').then(m => m.InfrastructureHealthComponent),
            data: { title: 'Infrastructure Health' }
          },
          {
            path: 'api-performance',
            loadComponent: () => import('./features/admin/monitoring/api-performance/api-performance.component').then(m => m.ApiPerformanceComponent),
            data: { title: 'API Performance' }
          },
          {
            path: 'tenants',
            loadComponent: () => import('./features/admin/monitoring/tenants/tenant-activity.component').then(m => m.TenantActivityComponent),
            data: { title: 'Tenant Activity' }
          },
          {
            path: 'security',
            loadComponent: () => import('./features/admin/monitoring/security/security-events.component').then(m => m.SecurityEventsComponent),
            data: { title: 'Security Events' }
          },
          {
            path: 'alerts',
            loadComponent: () => import('./features/admin/monitoring/alerts/alerts.component').then(m => m.AlertsComponent),
            data: { title: 'System Alerts' }
          }
        ]
      }
    ]
  },

  // Tenant Portal Routes - ALL WRAPPED IN SHARED LAYOUT
  {
    path: 'tenant',
    canActivate: [hrGuard],
    loadComponent: () => import('./shared/layouts/tenant-layout.component').then(m => m.TenantLayoutComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/tenant/dashboard/tenant-dashboard.component').then(m => m.TenantDashboardComponent)
      },
      {
        path: 'employees',
        loadComponent: () => import('./features/tenant/employees/employee-list.component').then(m => m.EmployeeListComponent)
      },
      {
        path: 'employees/new',
        loadComponent: () => import('./features/tenant/employees/comprehensive-employee-form.component').then(m => m.ComprehensiveEmployeeFormComponent)
      },
      {
        path: 'employees/:id',
        loadComponent: () => import('./features/tenant/employees/comprehensive-employee-form.component').then(m => m.ComprehensiveEmployeeFormComponent)
      },
      {
        path: 'timesheets/approvals',
        loadComponent: () => import('./features/tenant/timesheets/timesheet-approvals.component').then(m => m.TimesheetApprovalsComponent)
      },
      // Department Management Routes
      {
        path: 'settings/organization/departments',
        loadComponent: () => import('./features/tenant/organization/departments/department-list.component').then(m => m.DepartmentListComponent)
      },
      {
        path: 'settings/organization/departments/add',
        loadComponent: () => import('./features/tenant/organization/departments/department-form.component').then(m => m.DepartmentFormComponent)
      },
      {
        path: 'settings/organization/departments/edit/:id',
        loadComponent: () => import('./features/tenant/organization/departments/department-form.component').then(m => m.DepartmentFormComponent)
      },
      // Location Management Routes
      {
        path: 'organization/locations',
        loadComponent: () => import('./features/tenant/organization/locations/location-list.component').then(m => m.LocationListComponent)
      },
      {
        path: 'organization/locations/new',
        loadComponent: () => import('./features/tenant/organization/locations/location-list.component').then(m => m.LocationListComponent) // Stub for now
      },
      {
        path: 'organization/locations/:id',
        loadComponent: () => import('./features/tenant/organization/locations/location-list.component').then(m => m.LocationListComponent) // Stub for now
      },
      {
        path: 'organization/locations/:id/edit',
        loadComponent: () => import('./features/tenant/organization/locations/location-list.component').then(m => m.LocationListComponent) // Stub for now
      },
      // Biometric Device Management Routes
      {
        path: 'organization/devices',
        loadComponent: () => import('./features/tenant/organization/devices/biometric-device-list.component').then(m => m.BiometricDeviceListComponent),
        data: { title: 'Biometric Devices' }
      },
      {
        path: 'organization/devices/new',
        loadComponent: () => import('./features/tenant/organization/devices/biometric-device-form.component').then(m => m.BiometricDeviceFormComponent),
        data: { title: 'Register New Device' }
      },
      {
        path: 'organization/devices/:id/edit',
        loadComponent: () => import('./features/tenant/organization/devices/biometric-device-form.component').then(m => m.BiometricDeviceFormComponent),
        data: { title: 'Edit Device' }
      },
      {
        path: 'audit-logs',
        loadComponent: () => import('./features/tenant/audit-logs/tenant-audit-logs.component').then(m => m.TenantAuditLogsComponent),
        data: { title: 'Audit Trail' }
      },
      // ✅ FIXED: Added missing tenant routes (attendance, leave, payroll, reports)
      {
        path: 'attendance',
        loadComponent: () => import('./features/tenant/attendance/attendance-dashboard.component').then(m => m.AttendanceDashboardComponent),
        data: { title: 'Attendance Management' }
      },
      {
        path: 'leave',
        loadComponent: () => import('./features/tenant/leave/leave-dashboard.component').then(m => m.LeaveDashboardComponent),
        data: { title: 'Leave Management' }
      },
      {
        path: 'payroll',
        children: [
          {
            path: '',
            loadComponent: () => import('./features/tenant/payroll/payroll-dashboard.component').then(m => m.PayrollDashboardComponent),
            data: { title: 'Payroll Management' }
          },
          {
            path: 'salary-components',
            loadComponent: () => import('./features/tenant/payroll/salary-components.component').then(m => m.SalaryComponentsComponent),
            data: { title: 'Salary Components' }
          }
        ]
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/tenant/reports/reports-dashboard.component').then(m => m.ReportsDashboardComponent),
        data: { title: 'Reports & Analytics' }
      },
      // Billing & Subscription Management
      {
        path: 'billing',
        loadComponent: () => import('./features/tenant/billing/billing-overview.component').then(m => m.BillingOverviewComponent),
        data: { title: 'Billing & Subscription' }
      }
    ]
  },

  // Employee Portal Routes
  {
    path: 'employee',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/employee/dashboard/employee-dashboard.component').then(m => m.EmployeeDashboardComponent)
      },
      {
        path: 'attendance',
        loadComponent: () => import('./features/employee/attendance/employee-attendance.component').then(m => m.EmployeeAttendanceComponent),
        data: { title: 'My Attendance' }
      },
      {
        path: 'leave',
        loadComponent: () => import('./features/employee/leave/employee-leave.component').then(m => m.EmployeeLeaveComponent),
        data: { title: 'My Leave Requests' }
      },
      {
        path: 'timesheets',
        loadComponent: () => import('./features/employee/timesheets/timesheet-list.component').then(m => m.TimesheetListComponent)
      },
      {
        path: 'timesheets/:id',
        loadComponent: () => import('./features/employee/timesheets/timesheet-detail.component').then(m => m.TimesheetDetailComponent)
      },
      {
        path: 'payslips',
        loadComponent: () => import('./features/employee/payslips/payslip-list.component').then(m => m.PayslipListComponent)
      },
      {
        path: 'payslips/:id',
        loadComponent: () => import('./features/employee/payslips/payslip-detail.component').then(m => m.PayslipDetailComponent)
      }
    ]
  },

  // Wildcard route
  {
    path: '**',
    redirectTo: '/auth/subdomain'
  }
];
