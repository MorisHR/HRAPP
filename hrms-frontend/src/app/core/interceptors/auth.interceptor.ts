import { HttpInterceptorFn, HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { SubdomainService } from '../services/subdomain.service';
import { catchError, switchMap, throwError, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

// ============================================
// PRODUCTION-GRADE HTTP INTERCEPTOR
// Implements proper token refresh with retry logic
// ============================================

/**
 * Marker to prevent infinite retry loops
 * Tracks requests that are retries of failed requests
 */
const RETRY_REQUEST_MARKER = 'X-Retry-Request';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const subdomainService = inject(SubdomainService);
  const router = inject(Router);
  const token = authService.getToken();

  // Log every API request for debugging
  console.log(`🔵 API Request: ${req.method} ${req.url}`);
  if (token) {
    console.log(`🔑 Token present: ${token.substring(0, 20)}...`);
  } else {
    console.log(`⚠️  No token present`);
  }

  // Get tenant subdomain from URL (proper multi-tenant routing)
  const tenantSubdomain = subdomainService.getSubdomainForApiRequest();

  // Clone request and add authorization header if token exists
  // Always set withCredentials for CORS (enables HttpOnly cookies)
  if (token && !req.url.includes('/auth/login') && !req.url.includes('/auth/tenant/login')) {
    const headers: any = {
      Authorization: `Bearer ${token}`
    };

    // Add tenant subdomain header for multi-tenant support (DEVELOPMENT ONLY)
    // In production, subdomain is extracted from request host by backend
    if (tenantSubdomain && !environment.production) {
      headers['X-Tenant-Subdomain'] = tenantSubdomain;
      console.log(`📍 Development: Adding X-Tenant-Subdomain header: ${tenantSubdomain}`);
    }

    req = req.clone({
      setHeaders: headers,
      withCredentials: true
    });
  } else {
    // Set withCredentials even for login requests
    const headers: any = {};

    // Add tenant subdomain header even for login requests (DEVELOPMENT ONLY)
    // In production, subdomain is extracted from request host by backend
    if (tenantSubdomain && !environment.production) {
      headers['X-Tenant-Subdomain'] = tenantSubdomain;
      console.log(`📍 Development: Adding X-Tenant-Subdomain header: ${tenantSubdomain}`);
    }

    req = req.clone({
      setHeaders: Object.keys(headers).length > 0 ? headers : undefined,
      withCredentials: true
    });
  }

  return next(req).pipe(
    tap(response => {
      console.log(`✅ API Success: ${req.method} ${req.url}`, response);
    }),
    catchError((error: HttpErrorResponse) => {
      console.error(`❌ API Error: ${req.method} ${req.url}`, {
        status: error.status,
        statusText: error.statusText,
        message: error.error?.message || error.message,
        url: error.url,
        error: error.error
      });

      // Handle different error types appropriately
      switch (error.status) {
        case 401:
          // ============================================
          // CRITICAL FIX: Proper 401 handling with token refresh
          // ============================================

          // NEVER retry login/refresh endpoints
          if (req.url.includes('/auth/login') ||
              req.url.includes('/auth/tenant/login') ||
              req.url.includes('/auth/refresh')) {
            console.error('🔴 Authentication failed on login/refresh endpoint - NOT attempting refresh');
            return throwError(() => error);
          }

          // NEVER retry if this is already a retry request (prevent infinite loops)
          if (isRetryRequest(req)) {
            console.error('🔴 Retry request failed with 401 - logging out');
            authService.logout();
            return throwError(() => error);
          }

          console.warn('⚠️  Got 401 Unauthorized - Token may be expired');
          console.log('🔄 Attempting token refresh...');

          // Try to refresh token using HttpOnly cookie
          return authService.refreshToken().pipe(
            switchMap((response) => {
              console.log('✅ Token refresh successful, retrying original request');

              // Retry original request with new token
              const newToken = authService.getToken();
              const clonedReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${newToken}`,
                  [RETRY_REQUEST_MARKER]: 'true' // Mark as retry to prevent infinite loops
                },
                withCredentials: true
              });

              return next(clonedReq);
            }),
            catchError(refreshError => {
              console.error('❌ Token refresh failed:', refreshError);
              console.log('🚪 Logging out user due to refresh failure');

              // Only NOW logout - after refresh explicitly failed
              authService.logout();
              return throwError(() => refreshError);
            })
          );

        case 403:
          console.error('🚫 403 Forbidden - User does not have permission for this action');
          // Don't logout - user is authenticated but not authorized
          return throwError(() => error);

        case 404:
          console.error('🔍 404 Not Found - Endpoint does not exist:', req.url);
          // Don't logout - this is a routing/endpoint issue
          return throwError(() => error);

        case 400:
          console.error('⚠️  400 Bad Request - Invalid data sent to server');
          // Don't logout - this is a validation error
          return throwError(() => error);

        case 500:
        case 502:
        case 503:
          console.error('💥 Server Error - Backend is having issues');
          // Don't logout - this is a server error
          return throwError(() => error);

        default:
          console.error(`❓ Unexpected error status: ${error.status}`);
          return throwError(() => error);
      }
    })
  );
};

/**
 * Check if a request is a retry request
 * Prevents infinite retry loops
 */
function isRetryRequest(req: HttpRequest<any>): boolean {
  return req.headers.has(RETRY_REQUEST_MARKER);
}
