import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

export const roleGuard = (allowedRoles: UserRole[]): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      router.navigate(['/login']);
      return false;
    }

    if (!authService.hasAnyRole(allowedRoles)) {
      router.navigate(['/unauthorized']);
      return false;
    }

    return true;
  };
};

// Pre-defined role guards
export const superAdminGuard: CanActivateFn = roleGuard([UserRole.SuperAdmin]);
export const tenantAdminGuard: CanActivateFn = roleGuard([UserRole.TenantAdmin, UserRole.SuperAdmin]);
export const hrGuard: CanActivateFn = roleGuard([UserRole.HR, UserRole.TenantAdmin, UserRole.SuperAdmin]);
export const managerGuard: CanActivateFn = roleGuard([UserRole.Manager, UserRole.HR, UserRole.TenantAdmin, UserRole.SuperAdmin]);
