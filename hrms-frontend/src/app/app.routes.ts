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
  // SECURITY: All login routes protected by alreadyLoggedInGuard
  // This prevents authenticated users from accessing login pages
  // If user is already logged in and navigates to login page (via back button, etc.),
  // they will be redirected to their appropriate dashboard
  {
    path: 'auth',
    children: [
      {
        path: 'subdomain',
        canActivate: [alreadyLoggedInGuard],
        loadComponent: () => import('./features/auth/subdomain/subdomain.component').then(m => m.SubdomainComponent)
      },
      {
        path: 'login',
        canActivate: [subdomainGuard, alreadyLoggedInGuard],
        loadComponent: () => import('./features/auth/login/tenant-login.component').then(m => m.TenantLoginComponent)
      },
      {
        path: 'superadmin',
        canActivate: [alreadyLoggedInGuard],
        loadComponent: () => import('./features/auth/superadmin/superadmin-login.component').then(m => m.SuperAdminLoginComponent)
      },
      {
        path: 'activate',
        loadComponent: () => import('./features/auth/activate/activate.component').then(m => m.ActivateComponent)
      }
    ]
  },

  // Legacy login route (kept for backward compatibility)
  {
    path: 'login',
    redirectTo: '/auth/subdomain',
    pathMatch: 'full'
  },

  // Admin Portal Routes
  {
    path: 'admin',
    canActivate: [superAdminGuard],
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
      // TODO: Add more tenant routes (attendance, leave, payroll, reports)
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
