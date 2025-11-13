import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { EnvironmentDetectionService } from './environment-detection.service';
import { SubdomainService } from './subdomain.service';

/**
 * PRODUCTION-GRADE TENANT CONTEXT SERVICE
 *
 * Provides unified tenant context management across all environments:
 * - Codespaces: localStorage-based (no subdomain support)
 * - Localhost: Subdomain-based (subdomain.localhost:4200)
 * - Production: Subdomain-based (subdomain.morishr.com)
 *
 * AUTOMATIC ENVIRONMENT DETECTION - NO MANUAL CONFIGURATION
 *
 * Usage:
 * ```typescript
 * const tenant = tenantContextService.getCurrentTenant();
 * tenantContextService.setTenant('acme');
 * tenantContextService.navigateToLogin('siraaj');
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class TenantContextService {
  private readonly STORAGE_KEY = 'hrms_tenant_subdomain';
  private environmentDetection = inject(EnvironmentDetectionService);
  private subdomainService = inject(SubdomainService);
  private router = inject(Router);

  constructor() {
    // Log environment info on startup
    this.environmentDetection.logEnvironmentInfo();
    this.logCurrentTenantContext();
  }

  /**
   * Get the current tenant subdomain
   * Automatically selects the appropriate method based on environment
   *
   * @returns Tenant subdomain or null if not set
   */
  getCurrentTenant(): string | null {
    const supportsSubdomains = this.environmentDetection.supportsSubdomainRouting();

    if (supportsSubdomains) {
      // Subdomain-based mode (localhost/production)
      const tenant = this.subdomainService.getSubdomainFromUrl();
      console.log(`📍 Tenant Context (URL): ${tenant || 'none'}`);
      return tenant;
    } else {
      // Storage-based mode (Codespaces)
      const tenant = localStorage.getItem(this.STORAGE_KEY);
      console.log(`📍 Tenant Context (Storage): ${tenant || 'none'}`);
      return tenant;
    }
  }

  /**
   * Set the current tenant subdomain
   * Automatically uses the appropriate method based on environment
   *
   * @param subdomain Tenant subdomain to set
   */
  setTenant(subdomain: string): void {
    if (!subdomain || subdomain.trim().length === 0) {
      console.warn('⚠️  Attempted to set empty tenant subdomain');
      return;
    }

    const supportsSubdomains = this.environmentDetection.supportsSubdomainRouting();

    if (supportsSubdomains) {
      // Subdomain-based mode: Store in localStorage as fallback only
      // (URL is the source of truth, but we store for consistency)
      localStorage.setItem(this.STORAGE_KEY, subdomain);
      console.log(`💾 Tenant stored in localStorage (fallback): ${subdomain}`);
    } else {
      // Storage-based mode: localStorage is the source of truth
      localStorage.setItem(this.STORAGE_KEY, subdomain);
      console.log(`💾 Tenant stored in localStorage (primary): ${subdomain}`);
    }
  }

  /**
   * Clear the tenant context
   */
  clearTenant(): void {
    localStorage.removeItem(this.STORAGE_KEY);
    console.log('🗑️  Tenant context cleared');
  }

  /**
   * Check if tenant context is currently set
   */
  hasTenant(): boolean {
    return this.getCurrentTenant() !== null;
  }

  /**
   * Navigate to tenant login page
   * Environment-aware: redirects to subdomain in prod/localhost, navigates in Codespaces
   *
   * @param subdomain Tenant subdomain
   * @param path Path to navigate to (default: /auth/login)
   */
  navigateToLogin(subdomain: string, path: string = '/auth/login'): void {
    if (!subdomain || subdomain.trim().length === 0) {
      console.error('❌ Cannot navigate to login: subdomain is empty');
      return;
    }

    console.log(`🔑 navigateToLogin called with subdomain: ${subdomain}, path: ${path}`);

    // Store tenant in localStorage first
    this.setTenant(subdomain);
    console.log(`✅ Tenant stored in localStorage: ${subdomain}`);

    const supportsSubdomains = this.environmentDetection.supportsSubdomainRouting();
    console.log(`🌐 Supports subdomains: ${supportsSubdomains}`);

    if (supportsSubdomains) {
      // SUBDOMAIN-BASED ROUTING
      // Use SubdomainService to redirect browser to tenant subdomain
      console.log(`🔄 Redirecting to subdomain: ${subdomain}`);
      this.subdomainService.redirectToTenant(subdomain, path);
    } else {
      // STORAGE-BASED ROUTING (Codespaces)
      // Use Angular router (no real subdomain available)
      console.log(`🔄 Navigating within app (Codespaces): ${path}`);
      this.router.navigate([path]).then(
        success => {
          if (success) {
            console.log(`✅ Navigation to ${path} succeeded`);
          } else {
            console.error(`❌ Navigation to ${path} failed (returned false)`);
          }
        },
        error => {
          console.error(`❌ Navigation to ${path} rejected with error:`, error);
        }
      );
    }
  }

  /**
   * Navigate to subdomain selection page
   * Environment-aware: redirects to main domain in prod/localhost, navigates in Codespaces
   *
   * @param path Path to navigate to (default: /auth/subdomain)
   */
  navigateToSubdomainSelection(path: string = '/auth/subdomain'): void {
    const supportsSubdomains = this.environmentDetection.supportsSubdomainRouting();

    // Clear tenant context
    this.clearTenant();

    if (supportsSubdomains) {
      // SUBDOMAIN-BASED ROUTING
      // Redirect to main domain (remove subdomain from URL)
      console.log('🔄 Redirecting to main domain for subdomain selection');
      this.subdomainService.redirectToMainDomain(path);
    } else {
      // STORAGE-BASED ROUTING (Codespaces)
      // Use Angular router
      console.log(`🔄 Navigating within app (Codespaces): ${path}`);
      this.router.navigate([path]);
    }
  }

  /**
   * Validate tenant context for protected routes
   * Returns true if tenant context is valid, false otherwise
   *
   * @param requiredForRoute Optional - specific route that requires tenant
   */
  validateTenantContext(requiredForRoute?: string): boolean {
    const tenant = this.getCurrentTenant();

    if (!tenant) {
      console.warn(`⚠️  No tenant context for route: ${requiredForRoute || 'unknown'}`);
      return false;
    }

    console.log(`✅ Valid tenant context: ${tenant}`);
    return true;
  }

  /**
   * Check if currently on tenant subdomain (for subdomain-based mode only)
   * In storage-based mode (Codespaces), this always returns false
   */
  isOnTenantSubdomain(): boolean {
    const supportsSubdomains = this.environmentDetection.supportsSubdomainRouting();

    if (!supportsSubdomains) {
      // Codespaces: not on subdomain by definition
      return false;
    }

    // Localhost/Production: check if on tenant subdomain
    return this.subdomainService.isOnTenantSubdomain();
  }

  /**
   * Get tenant context for API requests
   * Returns the subdomain to be sent in X-Tenant-Subdomain header
   */
  getTenantForApiRequest(): string | null {
    return this.getCurrentTenant();
  }

  /**
   * Get environment-specific routing mode
   */
  getRoutingMode(): 'subdomain' | 'storage' {
    return this.environmentDetection.supportsSubdomainRouting() ? 'subdomain' : 'storage';
  }

  /**
   * Log current tenant context for debugging
   */
  private logCurrentTenantContext(): void {
    const tenant = this.getCurrentTenant();
    const mode = this.getRoutingMode();
    const isOnSubdomain = this.isOnTenantSubdomain();

    console.log('📋 TENANT CONTEXT:');
    console.log('   Mode:', mode);
    console.log('   Current Tenant:', tenant || 'none');
    console.log('   On Subdomain:', isOnSubdomain);
  }

  /**
   * Get detailed tenant context info for debugging
   */
  getTenantContextInfo(): TenantContextInfo {
    return {
      currentTenant: this.getCurrentTenant(),
      routingMode: this.getRoutingMode(),
      hasTenant: this.hasTenant(),
      isOnSubdomain: this.isOnTenantSubdomain(),
      environmentType: this.environmentDetection.getEnvironmentType(),
      supportsSubdomains: this.environmentDetection.supportsSubdomainRouting()
    };
  }
}

/**
 * Tenant context information for debugging
 */
export interface TenantContextInfo {
  currentTenant: string | null;
  routingMode: 'subdomain' | 'storage';
  hasTenant: boolean;
  isOnSubdomain: boolean;
  environmentType: string;
  supportsSubdomains: boolean;
}
