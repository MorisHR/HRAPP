import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { superAdminGuard, hrGuard } from './core/guards/role.guard';
import { subdomainGuard } from './core/guards/subdomain.guard';

export const routes: Routes = [
  // Default route - redirect to subdomain entry
  {
    path: '',
    redirectTo: '/auth/subdomain',
    pathMatch: 'full'
  },

  // Authentication routes
  {
    path: 'auth',
    children: [
      {
        path: 'subdomain',
        loadComponent: () => import('./features/auth/subdomain/subdomain.component').then(m => m.SubdomainComponent)
      },
      {
        path: 'login',
        canActivate: [subdomainGuard],
        loadComponent: () => import('./features/auth/login/tenant-login.component').then(m => m.TenantLoginComponent)
      },
      {
        path: 'superadmin',
        loadComponent: () => import('./features/auth/superadmin/superadmin-login.component').then(m => m.SuperAdminLoginComponent)
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
      }
    ]
  },

  // Wildcard route
  {
    path: '**',
    redirectTo: '/auth/subdomain'
  }
];
