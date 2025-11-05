import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

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
    console.log('❌ AUTH GUARD: Access DENIED - redirecting to /login');
    console.log('📍 Return URL will be:', state.url);
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  console.log('✅ AUTH GUARD: Access GRANTED');
  return true;
};
