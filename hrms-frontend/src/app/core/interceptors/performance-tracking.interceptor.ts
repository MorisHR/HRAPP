import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap, catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { PerformanceMonitoringService } from '../services/performance-monitoring.service';

/**
 * HTTP Interceptor for tracking API call performance
 * Automatically measures duration and status of all HTTP requests
 * Minimal overhead: <1ms per request
 */
export const performanceTrackingInterceptor: HttpInterceptorFn = (req, next) => {
  const performanceMonitoring = inject(PerformanceMonitoringService);
  const startTime = performance.now();

  // Extract endpoint path (remove base URL)
  const endpoint = extractEndpoint(req.url);

  return next(req).pipe(
    tap(event => {
      // Check if it's an HTTP response
      if (event.type === 4) { // HttpEventType.Response
        const duration = performance.now() - startTime;
        const response = event as any;

        performanceMonitoring.trackApiCall(
          endpoint,
          req.method,
          duration,
          response.status || 200
        );
      }
    }),
    catchError(error => {
      const duration = performance.now() - startTime;
      const statusCode = error.status || 0;

      performanceMonitoring.trackApiCall(
        endpoint,
        req.method,
        duration,
        statusCode
      );

      return throwError(() => error);
    })
  );
};

/**
 * Extract endpoint path from full URL
 */
function extractEndpoint(url: string): string {
  try {
    // Remove query parameters
    const urlWithoutQuery = url.split('?')[0];

    // If it's a full URL, extract pathname
    if (urlWithoutQuery.startsWith('http')) {
      const urlObj = new URL(urlWithoutQuery);
      return urlObj.pathname;
    }

    // Already a relative path
    return urlWithoutQuery;
  } catch {
    return url;
  }
}
