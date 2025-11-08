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
      // Get last user role to redirect to correct login page
      const lastUserRole = authService.getLastUserRole();
      console.log('❌ ROLE GUARD: User not authenticated');
      console.log('📍 Last user role:', lastUserRole);

      // Redirect based on last user type
      if (lastUserRole === UserRole.SuperAdmin) {
        console.log('🔄 ROLE GUARD: Redirecting to SuperAdmin login');
        router.navigate(['/auth/superadmin']);
      } else {
        console.log('🔄 ROLE GUARD: Redirecting to tenant login');
        router.navigate(['/auth/subdomain']);
      }
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
