import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

/**
 * Service for handling subdomain-based multi-tenant routing
 * Follows industry best practices (Slack, Shopify, Atlassian model)
 */
@Injectable({
  providedIn: 'root'
})
export class SubdomainService {

  /**
   * Extracts the subdomain from the current window location
   * @returns The subdomain or null if not found
   *
   * Examples:
   * - acme.hrms.com → "acme"
   * - acme.localhost:4200 → "acme"
   * - www.hrms.com → "www"
   * - hrms.com → null
   * - localhost:4200 → null
   */
  getSubdomainFromUrl(): string | null {
    if (typeof window === 'undefined') {
      return null; // SSR safety
    }

    const host = window.location.hostname;
    return this.extractSubdomain(host);
  }

  /**
   * Extracts subdomain from a given hostname
   * @param hostname The hostname to parse
   * @returns The subdomain or null
   */
  extractSubdomain(hostname: string): string | null {
    if (!hostname) {
      return null;
    }

    // Split by dots
    const parts = hostname.split('.');

    // localhost or single-level domain (no subdomain)
    if (parts.length === 1) {
      return null;
    }

    // Multi-level domain: subdomain.domain.tld or subdomain.localhost
    if (parts.length >= 2) {
      const firstPart = parts[0].toLowerCase();

      // Exclude common non-tenant subdomains
      const reservedSubdomains = ['www', 'api', 'admin', 'cdn', 'static', 'assets'];
      if (reservedSubdomains.includes(firstPart)) {
        // These are special subdomains, not tenant subdomains
        // You can customize this logic based on your needs
        return firstPart === 'admin' ? null : firstPart;
      }

      return firstPart;
    }

    return null;
  }

  /**
   * Checks if the current URL is on a tenant subdomain
   */
  isOnTenantSubdomain(): boolean {
    const subdomain = this.getSubdomainFromUrl();
    const reservedSubdomains = ['www', 'api', 'admin', 'cdn', 'static', 'assets'];
    return subdomain !== null && !reservedSubdomains.includes(subdomain);
  }

  /**
   * Checks if the current URL is on the main domain (no subdomain or www)
   */
  isOnMainDomain(): boolean {
    const subdomain = this.getSubdomainFromUrl();
    return subdomain === null || subdomain === 'www';
  }

  /**
   * Builds a tenant-specific URL
   * @param subdomain The tenant subdomain
   * @param path The path to navigate to (e.g., '/auth/login')
   * @returns Full URL with subdomain
   *
   * Examples:
   * - buildTenantUrl('acme', '/auth/login') → 'http://acme.localhost:4200/auth/login' (dev)
   * - buildTenantUrl('acme', '/auth/login') → 'https://acme.hrms.com/auth/login' (prod)
   */
  buildTenantUrl(subdomain: string, path: string = '/'): string {
    const protocol = window.location.protocol; // http: or https:
    const port = window.location.port;

    // Get the base domain (without subdomain)
    const baseDomain = this.getBaseDomain();

    // Build the URL
    const portPart = port ? `:${port}` : '';
    const cleanPath = path.startsWith('/') ? path : `/${path}`;

    return `${protocol}//${subdomain}.${baseDomain}${portPart}${cleanPath}`;
  }

  /**
   * Gets the base domain (without subdomain)
   * @returns Base domain like "hrms.com" or "localhost"
   */
  private getBaseDomain(): string {
    const hostname = window.location.hostname;
    const parts = hostname.split('.');

    if (parts.length === 1) {
      // localhost
      return hostname;
    }

    if (parts.length === 2) {
      // Already base domain (domain.tld or subdomain.localhost)
      if (parts[1] === 'localhost') {
        return 'localhost';
      }
      return hostname;
    }

    // subdomain.domain.tld → domain.tld
    // subdomain.localhost → localhost
    if (parts[parts.length - 1] === 'localhost') {
      return 'localhost';
    }

    // Return last two parts (domain.tld)
    return parts.slice(-2).join('.');
  }

  /**
   * Redirects to a tenant-specific URL
   * @param subdomain The tenant subdomain
   * @param path The path to navigate to
   */
  redirectToTenant(subdomain: string, path: string = '/auth/login'): void {
    const url = this.buildTenantUrl(subdomain, path);
    window.location.href = url;
  }

  /**
   * Redirects to the main domain (removes subdomain)
   * @param path The path to navigate to
   */
  redirectToMainDomain(path: string = '/'): void {
    const protocol = window.location.protocol;
    const baseDomain = this.getBaseDomain();
    const port = window.location.port;
    const portPart = port ? `:${port}` : '';
    const cleanPath = path.startsWith('/') ? path : `/${path}`;

    window.location.href = `${protocol}//${baseDomain}${portPart}${cleanPath}`;
  }

  /**
   * Gets the API URL for the current subdomain context
   * For development: Uses X-Tenant-Subdomain header approach
   * For production: API should be on api.domain.com or same domain
   */
  getApiUrl(): string {
    // In development, use configured API URL from environment
    // In production, you might use api.domain.com or relative URLs
    return environment.apiUrl;
  }

  /**
   * Gets the current subdomain for API requests
   * Used in HTTP interceptor to add X-Tenant-Subdomain header (dev only)
   */
  getSubdomainForApiRequest(): string | null {
    return this.getSubdomainFromUrl();
  }
}
