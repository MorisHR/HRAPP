import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * CSRF Protection Service - Fortune 500 Compliance
 *
 * Manages CSRF tokens for protecting against Cross-Site Request Forgery attacks
 * Fetches token from backend on app initialization
 * Provides token to HTTP interceptor for state-changing requests
 *
 * @security CRITICAL - Required for all POST, PUT, DELETE, PATCH requests
 */
@Injectable({
  providedIn: 'root'
})
export class CsrfService {
  private csrfToken: string | null = null;
  private tokenFetchAttempted = false;

  constructor(private http: HttpClient) {}

  /**
   * Initialize CSRF token - called on app startup
   * Fetches token from backend and stores it
   */
  async initializeCsrfToken(): Promise<void> {
    // Prevent multiple concurrent fetch attempts
    if (this.tokenFetchAttempted) {
      return;
    }

    this.tokenFetchAttempted = true;

    try {
      const apiUrl = environment.apiUrl || 'http://localhost:5090';
      const response = await firstValueFrom(
        this.http.get<{ success: boolean; token: string; message: string }>(
          `${apiUrl}/api/auth/csrf-token`
        )
      );

      if (response.success && response.token) {
        this.csrfToken = response.token;
        console.log('[CSRF] Token initialized successfully');
      } else {
        console.error('[CSRF] Failed to initialize token:', response.message);
      }
    } catch (error) {
      console.error('[CSRF] Error fetching CSRF token:', error);
      // Don't throw - allow app to continue without CSRF token
      // The backend will reject state-changing requests without valid token
    }
  }

  /**
   * Get current CSRF token
   * @returns CSRF token or null if not initialized
   */
  getToken(): string | null {
    return this.csrfToken;
  }

  /**
   * Check if CSRF token is available
   * @returns true if token is available
   */
  hasToken(): boolean {
    return this.csrfToken !== null && this.csrfToken !== '';
  }

  /**
   * Clear CSRF token (e.g., on logout)
   */
  clearToken(): void {
    this.csrfToken = null;
    this.tokenFetchAttempted = false;
  }

  /**
   * Refresh CSRF token (e.g., after token expiration)
   */
  async refreshToken(): Promise<void> {
    this.tokenFetchAttempted = false;
    await this.initializeCsrfToken();
  }
}
