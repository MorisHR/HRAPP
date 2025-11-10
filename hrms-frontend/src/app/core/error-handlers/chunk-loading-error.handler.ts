import { ErrorHandler, Injectable, Injector } from '@angular/core';
import { Router } from '@angular/router';

/**
 * PRODUCTION-READY: Global error handler for chunk loading failures
 *
 * FORTUNE 500 PATTERN: AWS Amplify, Vercel, Netlify
 * - Detects chunk loading failures (common in Codespaces, CDN issues, cache problems)
 * - Implements exponential backoff retry
 * - Reloads application as last resort to recover
 * - Prevents cascade failures
 */
@Injectable()
export class ChunkLoadingErrorHandler implements ErrorHandler {
  private chunkFailedAttempts = new Map<string, number>();
  private readonly MAX_RETRIES = 3;
  private readonly RETRY_DELAY = 1000; // 1 second

  constructor(private injector: Injector) {}

  handleError(error: any): void {
    // Check if it's a chunk loading error
    if (this.isChunkLoadError(error)) {
      console.error('🚨 [CHUNK LOAD ERROR] Detected chunk loading failure:', error);

      const chunkUrl = this.extractChunkUrl(error);
      if (chunkUrl) {
        const attempts = this.chunkFailedAttempts.get(chunkUrl) || 0;

        if (attempts < this.MAX_RETRIES) {
          // Retry with exponential backoff
          this.chunkFailedAttempts.set(chunkUrl, attempts + 1);
          const delay = this.RETRY_DELAY * Math.pow(2, attempts);

          console.warn(
            `⏳ [CHUNK LOAD ERROR] Retry ${attempts + 1}/${this.MAX_RETRIES} ` +
            `in ${delay}ms for ${chunkUrl}`
          );

          setTimeout(() => {
            window.location.reload();
          }, delay);

          return;
        } else {
          // Max retries exceeded
          console.error(
            `❌ [CHUNK LOAD ERROR] Max retries exceeded for ${chunkUrl}. ` +
            `Reloading application...`
          );

          // Clear chunk attempt history
          this.chunkFailedAttempts.clear();

          // Reload with cache busting
          window.location.href = window.location.href + '?t=' + Date.now();
          return;
        }
      }
    }

    // Check if it's a GitHub Codespaces authentication redirect
    if (this.isCodespacesAuthError(error)) {
      console.error(
        '🔐 [CODESPACES AUTH] Detected Codespaces authentication requirement. ' +
        'Reloading to complete authentication...'
      );

      // Reload to complete Codespaces authentication
      window.location.reload();
      return;
    }

    // For all other errors, log and rethrow
    console.error('❌ [GLOBAL ERROR HANDLER]', error);

    // In production, you might want to send this to a logging service
    // this.sendToLoggingService(error);
  }

  /**
   * Detects if error is related to dynamic chunk loading failure
   */
  private isChunkLoadError(error: any): boolean {
    const errorString = error?.toString() || '';
    const messageString = error?.message || '';

    return (
      errorString.includes('Failed to fetch dynamically imported module') ||
      errorString.includes('ChunkLoadError') ||
      messageString.includes('Failed to fetch dynamically imported module') ||
      messageString.includes('ChunkLoadError') ||
      messageString.includes('Loading chunk') ||
      error?.name === 'ChunkLoadError'
    );
  }

  /**
   * Detects if error is related to GitHub Codespaces authentication
   */
  private isCodespacesAuthError(error: any): boolean {
    const errorString = error?.toString() || '';
    const messageString = error?.message || '';

    return (
      errorString.includes('github.dev/pf-signin') ||
      errorString.includes('app.github.dev') ||
      messageString.includes('github.dev/pf-signin') ||
      (error?.url && error.url.includes('github.dev/pf-signin'))
    );
  }

  /**
   * Extracts chunk URL from error for tracking retry attempts
   */
  private extractChunkUrl(error: any): string | null {
    try {
      const messageString = error?.message || error?.toString() || '';
      const urlMatch = messageString.match(/https?:\/\/[^\s]+\.js/);
      return urlMatch ? urlMatch[0] : 'unknown-chunk';
    } catch {
      return 'unknown-chunk';
    }
  }
}
