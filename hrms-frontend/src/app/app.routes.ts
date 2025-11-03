import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { superAdminGuard, hrGuard } from './core/guards/role.guard';

export const routes: Routes = [
  // Default route
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full'
  },

  // Login route
  {
    path: 'login',
    loadComponent: () => import('./features/admin/login/login.component').then(m => m.LoginComponent)
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
      }
    ]
  },

  // Tenant Portal Routes
  {
    path: 'tenant',
    canActivate: [hrGuard],
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
      // Add more tenant routes here (employees, attendance, leave, payroll, reports)
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
    redirectTo: '/login'
  }
];
