import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

export const roleGuard = (allowedRoles: UserRole[]): CanActivateFn => {
  return (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    console.log('🛡️ ROLE GUARD: Checking access to:', state.url);
    console.log('📋 Required roles:', allowedRoles);

    const isAuth = authService.isAuthenticated();
    const user = authService.user();
    const userRole = user?.role;

    console.log('👤 User info:', {
      isAuthenticated: isAuth,
      email: user?.email,
      currentRole: userRole,
      allowedRoles: allowedRoles
    });

    if (!isAuth) {
      console.log('❌ ROLE GUARD: User not authenticated - redirecting to /login');
      router.navigate(['/login']);
      return false;
    }

    const hasRole = authService.hasAnyRole(allowedRoles);

    if (!hasRole) {
      console.log('🚫 ROLE GUARD: User lacks required role - redirecting to /unauthorized');
      console.log(`   User has: ${userRole}, Required: ${allowedRoles.join(' or ')}`);
      router.navigate(['/unauthorized']);
      return false;
    }

    console.log('✅ ROLE GUARD: Access GRANTED (role match)');
    return true;
  };
};

// Pre-defined role guards
export const superAdminGuard: CanActivateFn = roleGuard([UserRole.SuperAdmin]);
export const tenantAdminGuard: CanActivateFn = roleGuard([UserRole.TenantAdmin, UserRole.SuperAdmin]);
export const hrGuard: CanActivateFn = roleGuard([UserRole.HR, UserRole.TenantAdmin, UserRole.SuperAdmin]);
export const managerGuard: CanActivateFn = roleGuard([UserRole.Manager, UserRole.HR, UserRole.TenantAdmin, UserRole.SuperAdmin]);
