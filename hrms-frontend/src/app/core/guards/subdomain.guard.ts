import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { TenantContextService } from '../services/tenant-context.service';

/**
 * Guard to protect TENANT-SPECIFIC routes
 * Ensures tenant context is set before accessing tenant routes
 * Redirects to /auth/subdomain if tenant context is missing
 *
 * SECURITY: This guard SKIPS:
 * - ALL admin routes (/admin/*) - handled by superAdminGuard
 * - ALL auth routes (/auth/*) - public routes
 *
 * This guard ONLY checks tenant routes that require tenant context
 *
 * ENVIRONMENT-AWARE: Automatically adapts to runtime environment
 * - Codespaces: Checks localStorage for tenant context
 * - Localhost: Checks URL subdomain (subdomain.localhost:4200)
 * - Production: Checks URL subdomain (subdomain.morishr.com)
 */
export const subdomainGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const router = inject(Router);
  const tenantContext = inject(TenantContextService);

  console.log('🔍 SUBDOMAIN GUARD: Checking route:', state.url);

  // ✅ CRITICAL FIX: Skip ALL admin routes (including nested like /admin/tenants/:id/edit)
  if (state.url.startsWith('/admin')) {
    console.log('✅ SUBDOMAIN GUARD: Admin route - bypassing (handled by superAdminGuard)');
    return true;
  }

  // ✅ Skip ALL auth routes (public routes, no tenant context needed)
  if (state.url.startsWith('/auth')) {
    console.log('✅ SUBDOMAIN GUARD: Auth route - bypassing (public route)');
    return true;
  }

  // ✅ ENVIRONMENT-AWARE TENANT CONTEXT CHECK
  // Validates tenant context regardless of environment
  const hasTenant = tenantContext.validateTenantContext(state.url);
  const tenant = tenantContext.getCurrentTenant();
  const routingMode = tenantContext.getRoutingMode();

  console.log('📍 Routing mode:', routingMode);
  console.log('🏢 Current tenant:', tenant || 'none');
  console.log('✅ Has tenant context:', hasTenant);

  if (!hasTenant) {
    console.log('❌ SUBDOMAIN GUARD: No tenant context - redirecting to subdomain selection');

    // Navigate to subdomain selection (environment-aware)
    tenantContext.navigateToSubdomainSelection('/auth/subdomain');
    return false;
  }

  console.log('✅ SUBDOMAIN GUARD: Tenant context valid:', tenant);
  return true;
};
