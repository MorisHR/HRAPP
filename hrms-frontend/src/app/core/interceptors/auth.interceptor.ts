import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError, tap } from 'rxjs';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();

  // Log every API request for debugging
  console.log(`🔵 API Request: ${req.method} ${req.url}`);
  if (token) {
    console.log(`🔑 Token present: ${token.substring(0, 20)}...`);
  } else {
    console.log(`⚠️ No token present`);
  }

  // Get tenant subdomain from localStorage
  const tenantSubdomain = localStorage.getItem('tenant_subdomain');

  // Clone request and add authorization header if token exists
  // Always set withCredentials for CORS
  if (token && !req.url.includes('/auth/login') && !req.url.includes('/auth/tenant/login')) {
    const headers: any = {
      Authorization: `Bearer ${token}`
    };

    // Add tenant subdomain header for multi-tenant support
    if (tenantSubdomain) {
      headers['X-Tenant-Subdomain'] = tenantSubdomain;
    }

    req = req.clone({
      setHeaders: headers,
      withCredentials: true
    });
  } else {
    // Set withCredentials even for login requests
    const headers: any = {};

    // Add tenant subdomain header even for login requests (for tenant login)
    if (tenantSubdomain) {
      headers['X-Tenant-Subdomain'] = tenantSubdomain;
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
          // Only try to refresh if this is NOT a login or refresh endpoint
          if (req.url.includes('/auth/login') ||
              req.url.includes('/auth/tenant/login') ||
              req.url.includes('/auth/refresh')) {
            console.error('🔴 Authentication failed on login/refresh endpoint - NOT attempting refresh');
            // Don't logout immediately - let the component handle it
            return throwError(() => error);
          }

          console.warn('⚠️ Got 401 Unauthorized - Token may be expired or invalid');
          console.log('🔄 Attempting token refresh...');

          // Try to refresh token
          return authService.refreshToken().pipe(
            switchMap(() => {
              console.log('✅ Token refresh successful, retrying original request');
              // Retry original request with new token
              const newToken = authService.getToken();
              const clonedReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${newToken}`
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
          console.error('⚠️ 400 Bad Request - Invalid data sent to server');
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
