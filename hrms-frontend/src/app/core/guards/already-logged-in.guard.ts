import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

/**
 * Guard to prevent already-authenticated users from accessing login/auth pages
 *
 * SECURITY: Prevents confusing UX where authenticated users can be on login pages
 * - If user is authenticated, redirects to appropriate dashboard
 * - If user is NOT authenticated, allows access to login page
 *
 * This prevents browser history bypass confusion:
 * - User logs in → navigates back to login → this guard redirects them forward to dashboard
 * - User cannot "be on" login page while authenticated
 */
export const alreadyLoggedInGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  console.log('🔐 ALREADY-LOGGED-IN GUARD: Checking access to:', state.url);

  const isAuth = authService.isAuthenticated();
  const user = authService.user();

  if (!isAuth) {
    console.log('✅ ALREADY-LOGGED-IN GUARD: User NOT authenticated - allowing access to login page');
    return true; // Not authenticated, allow access to login page
  }

  // User IS authenticated - redirect to appropriate dashboard
  console.log('🔄 ALREADY-LOGGED-IN GUARD: User IS authenticated - redirecting to dashboard');
  console.log('   User:', user?.email, 'Role:', user?.role);

  const userRole = user?.role;

  // Redirect based on role
  switch (userRole) {
    case UserRole.SuperAdmin:
      console.log('   → Redirecting SuperAdmin to /admin/dashboard');
      return router.createUrlTree(['/admin/dashboard']);

    case UserRole.TenantAdmin:
    case UserRole.HR:
      console.log('   → Redirecting TenantAdmin/HR to /tenant/dashboard');
      return router.createUrlTree(['/tenant/dashboard']);

    case UserRole.Manager:
    case UserRole.Employee:
      console.log('   → Redirecting Employee to /employee/dashboard');
      return router.createUrlTree(['/employee/dashboard']);

    default:
      console.log('   → Unknown role - redirecting to /tenant/dashboard');
      return router.createUrlTree(['/tenant/dashboard']);
  }
};
