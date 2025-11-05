import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';

/**
 * Guard to protect the tenant login route
 * Ensures subdomain has been selected before accessing login
 * Redirects to /auth/subdomain if no subdomain is found
 */
export const subdomainGuard: CanActivateFn = () => {
  const router = inject(Router);
  const subdomain = localStorage.getItem('hrms_subdomain');

  if (!subdomain) {
    // No subdomain selected, redirect to subdomain entry page
    return router.createUrlTree(['/auth/subdomain']);
  }

  // Subdomain exists, allow access to login
  return true;
};
