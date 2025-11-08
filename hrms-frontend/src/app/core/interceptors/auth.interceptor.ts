import { HttpInterceptorFn, HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { TenantContextService } from '../services/tenant-context.service';
import { SessionManagementService } from '../services/session-management.service';
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
  const tenantContext = inject(TenantContextService);
  const sessionManagement = inject(SessionManagementService);
  const router = inject(Router);
  const token = authService.getToken();

  // Record API calls as user activity (extends session)
  if (authService.isAuthenticated()) {
    sessionManagement.recordActivity();
  }

  // Log every API request for debugging
  console.log(`🔵 API Request: ${req.method} ${req.url}`);
  if (token) {
    console.log(`🔑 Token present: ${token.substring(0, 20)}...`);
  } else {
    console.log(`⚠️  No token present`);
  }

  // ✅ SECURITY FIX: Don't add tenant subdomain header for admin routes
  // SuperAdmin endpoints at /api/Tenants, /api/AdminUsers, etc. don't need tenant context
  const isAdminRoute = req.url.includes('/Tenants') ||
                       req.url.includes('/AdminUsers') ||
                       req.url.includes('/auth/');

  // Get tenant subdomain from context (environment-aware)
  // Works in both Codespaces (localStorage) and production (URL)
  const tenantSubdomain = isAdminRoute ? null : tenantContext.getTenantForApiRequest();

  if (isAdminRoute) {
    console.log('👑 Admin API route - no tenant subdomain needed');
  } else if (tenantSubdomain) {
    console.log(`🏢 Tenant API route - subdomain: ${tenantSubdomain}`);
    console.log(`📍 Routing mode: ${tenantContext.getRoutingMode()}`);
  }

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

          // Enhanced diagnostic logging
          console.error('🚨 401 UNAUTHORIZED - DETAILED DIAGNOSTICS 🚨');
          console.error('📍 Request Details:', {
            url: error.url || req.url,
            method: req.method,
            headers: Array.from(req.headers.keys()),
            hasAuthHeader: req.headers.has('Authorization'),
            hasToken: !!authService.getToken(),
            tokenPreview: authService.getToken()?.substring(0, 30) + '...',
            currentRoute: router.url,
            isRetry: isRetryRequest(req),
            errorMessage: error.error?.message || error.message,
            errorDetails: error.error
          });

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
