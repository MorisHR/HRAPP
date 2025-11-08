import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  console.log('🛡️ AUTH GUARD: Checking access to:', state.url);

  const isAuth = authService.isAuthenticated();
  const token = authService.getToken();
  const user = authService.user();

  console.log('🔐 Auth Status:', {
    isAuthenticated: isAuth,
    hasToken: !!token,
    tokenPreview: token ? token.substring(0, 30) + '...' : 'none',
    user: user ? { email: user.email, role: user.role } : 'none'
  });

  if (!isAuth) {
    // Get last user role to redirect to correct login page
    const lastUserRole = authService.getLastUserRole();
    console.log('❌ AUTH GUARD: Access DENIED');
    console.log('📍 Last user role:', lastUserRole);

    // Redirect based on last user type
    if (lastUserRole === UserRole.SuperAdmin) {
      console.log('🔄 AUTH GUARD: Redirecting to SuperAdmin login');
      router.navigate(['/auth/superadmin'], { queryParams: { returnUrl: state.url } });
    } else {
      console.log('🔄 AUTH GUARD: Redirecting to tenant login');
      router.navigate(['/auth/subdomain'], { queryParams: { returnUrl: state.url } });
    }
    return false;
  }

  console.log('✅ AUTH GUARD: Access GRANTED');
  return true;
};
