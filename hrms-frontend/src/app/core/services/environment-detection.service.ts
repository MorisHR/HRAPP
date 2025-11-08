import { Injectable } from '@angular/core';

/**
 * Service for detecting the runtime environment
 * Automatically determines if running in:
 * - GitHub Codespaces
 * - Local development (localhost)
 * - Production (custom domain)
 *
 * NO MANUAL CONFIGURATION REQUIRED - Fully automatic detection
 */
@Injectable({
  providedIn: 'root'
})
export class EnvironmentDetectionService {
  private _environmentType: EnvironmentType | null = null;
  private _supportsSubdomains: boolean | null = null;

  constructor() {
    this.detectEnvironment();
  }

  /**
   * Detects the current environment type
   * Called once on service initialization, result is cached
   */
  private detectEnvironment(): void {
    if (typeof window === 'undefined') {
      // SSR - assume production for safety
      this._environmentType = EnvironmentType.Production;
      this._supportsSubdomains = true;
      return;
    }

    const hostname = window.location.hostname;
    const host = window.location.host; // includes port

    console.log('🔍 ENVIRONMENT DETECTION');
    console.log('   Hostname:', hostname);
    console.log('   Host:', host);

    // Pattern 1: GitHub Codespaces
    // Examples:
    // - username-repo-hash.preview.app.github.dev
    // - username-repo-hash-4200.preview.app.github.dev
    if (hostname.includes('.preview.app.github.dev') ||
        hostname.includes('.githubpreview.dev') ||
        hostname.includes('.github.dev')) {
      console.log('   ✅ Detected: GitHub Codespaces');
      this._environmentType = EnvironmentType.Codespaces;
      this._supportsSubdomains = false; // Cannot use subdomains in Codespaces
      return;
    }

    // Pattern 2: Localhost (development)
    // Examples:
    // - localhost:4200
    // - subdomain.localhost:4200
    // - 127.0.0.1:4200
    if (hostname === 'localhost' ||
        hostname === '127.0.0.1' ||
        hostname.endsWith('.localhost')) {
      console.log('   ✅ Detected: Local Development');
      this._environmentType = EnvironmentType.LocalDevelopment;
      this._supportsSubdomains = true; // localhost supports subdomains natively
      return;
    }

    // Pattern 3: Production (custom domain)
    // Examples:
    // - morishr.com
    // - www.morishr.com
    // - siraaj.morishr.com
    console.log('   ✅ Detected: Production');
    this._environmentType = EnvironmentType.Production;
    this._supportsSubdomains = true; // Production with wildcard DNS
  }

  /**
   * Get the detected environment type
   */
  getEnvironmentType(): EnvironmentType {
    if (this._environmentType === null) {
      this.detectEnvironment();
    }
    return this._environmentType!;
  }

  /**
   * Check if current environment supports subdomain routing
   * - Codespaces: false (must use localStorage)
   * - Localhost: true (subdomain.localhost works)
   * - Production: true (wildcard DNS configured)
   */
  supportsSubdomainRouting(): boolean {
    if (this._supportsSubdomains === null) {
      this.detectEnvironment();
    }
    return this._supportsSubdomains!;
  }

  /**
   * Check if running in GitHub Codespaces
   */
  isCodespaces(): boolean {
    return this.getEnvironmentType() === EnvironmentType.Codespaces;
  }

  /**
   * Check if running on localhost
   */
  isLocalDevelopment(): boolean {
    return this.getEnvironmentType() === EnvironmentType.LocalDevelopment;
  }

  /**
   * Check if running in production
   */
  isProduction(): boolean {
    return this.getEnvironmentType() === EnvironmentType.Production;
  }

  /**
   * Get a human-readable environment name for debugging
   */
  getEnvironmentName(): string {
    switch (this.getEnvironmentType()) {
      case EnvironmentType.Codespaces:
        return 'GitHub Codespaces';
      case EnvironmentType.LocalDevelopment:
        return 'Local Development';
      case EnvironmentType.Production:
        return 'Production';
      default:
        return 'Unknown';
    }
  }

  /**
   * Get detailed environment information for debugging
   */
  getEnvironmentInfo(): EnvironmentInfo {
    if (typeof window === 'undefined') {
      return {
        type: EnvironmentType.Production,
        name: 'Production (SSR)',
        supportsSubdomains: true,
        hostname: 'unknown',
        protocol: 'unknown',
        port: 'unknown'
      };
    }

    return {
      type: this.getEnvironmentType(),
      name: this.getEnvironmentName(),
      supportsSubdomains: this.supportsSubdomainRouting(),
      hostname: window.location.hostname,
      protocol: window.location.protocol,
      port: window.location.port || '(default)'
    };
  }

  /**
   * Log environment detection results for debugging
   */
  logEnvironmentInfo(): void {
    const info = this.getEnvironmentInfo();
    console.log('🌍 ENVIRONMENT INFO:');
    console.log('   Type:', info.type);
    console.log('   Name:', info.name);
    console.log('   Supports Subdomains:', info.supportsSubdomains);
    console.log('   Hostname:', info.hostname);
    console.log('   Protocol:', info.protocol);
    console.log('   Port:', info.port);
  }
}

/**
 * Environment types supported by the application
 */
export enum EnvironmentType {
  /** GitHub Codespaces - no subdomain support */
  Codespaces = 'CODESPACES',

  /** Local development (localhost) - subdomain support */
  LocalDevelopment = 'LOCAL_DEVELOPMENT',

  /** Production (custom domain) - subdomain support */
  Production = 'PRODUCTION'
}

/**
 * Detailed environment information
 */
export interface EnvironmentInfo {
  type: EnvironmentType;
  name: string;
  supportsSubdomains: boolean;
  hostname: string;
  protocol: string;
  port: string;
}
