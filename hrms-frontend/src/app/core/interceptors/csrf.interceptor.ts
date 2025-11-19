import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CsrfService } from '../services/csrf.service';

/**
 * CSRF Interceptor - Fortune 500 Compliance
 *
 * Automatically adds CSRF token to all state-changing HTTP requests
 * Protects against Cross-Site Request Forgery attacks
 *
 * @security CRITICAL - Adds X-XSRF-TOKEN header to POST, PUT, DELETE, PATCH requests
 */
@Injectable()
export class CsrfInterceptor implements HttpInterceptor {
  /**
   * HTTP methods that require CSRF protection
   * State-changing operations that modify data on the server
   */
  private readonly statefulMethods = ['POST', 'PUT', 'DELETE', 'PATCH'];

  /**
   * Endpoints that should skip CSRF validation
   * Typically login and token refresh endpoints
   */
  private readonly exemptEndpoints = [
    '/api/auth/login',
    '/api/auth/csrf-token',
    '/api/auth/refresh',
    '/api/auth/system-'
  ];

  constructor(private csrfService: CsrfService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Only add CSRF token to state-changing requests
    const isStatefulRequest = this.statefulMethods.includes(request.method.toUpperCase());

    // Check if endpoint is exempt from CSRF validation
    const isExempt = this.isExemptEndpoint(request.url);

    if (isStatefulRequest && !isExempt) {
      const token = this.csrfService.getToken();

      if (token) {
        // Clone request and add CSRF token header
        request = request.clone({
          setHeaders: {
            'X-XSRF-TOKEN': token
          }
        });
      } else {
        // Log warning if token is missing for state-changing request
        console.warn(
          '[CSRF] No CSRF token available for state-changing request:',
          request.method,
          request.url
        );
      }
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // Handle CSRF validation failures
        if (error.status === 403 && this.isCsrfError(error)) {
          console.error('[CSRF] Token validation failed. Refreshing token...');

          // Attempt to refresh CSRF token
          this.csrfService.refreshToken().catch(err => {
            console.error('[CSRF] Failed to refresh token:', err);
          });

          // You could also trigger a token refresh and retry the request here
          // For now, just pass the error through
        }

        return throwError(() => error);
      })
    );
  }

  /**
   * Check if endpoint should be exempt from CSRF validation
   */
  private isExemptEndpoint(url: string): boolean {
    return this.exemptEndpoints.some(endpoint =>
      url.includes(endpoint)
    );
  }

  /**
   * Check if error is a CSRF validation error
   */
  private isCsrfError(error: HttpErrorResponse): boolean {
    return (
      error.status === 403 &&
      (error.error?.error === 'CSRF_TOKEN_INVALID' ||
       error.error?.message?.toLowerCase().includes('csrf'))
    );
  }
}
