// ═══════════════════════════════════════════════════════════
// TOAST REF CLASS
// Part of the Fortune 500-grade HRMS design system
// Manages individual toast instance lifecycle and communication
// ═══════════════════════════════════════════════════════════

import { Subject, Observable } from 'rxjs';

/**
 * Reference to an active toast.
 * Provides methods to dismiss the toast and observe its lifecycle.
 */
export class ToastRef {
  /** Emits when the toast has been dismissed */
  private readonly _afterDismissed = new Subject<void>();

  /** Emits when the toast action button is clicked */
  private readonly _onAction = new Subject<void>();

  /** Timer for auto-dismiss */
  private dismissTimer?: ReturnType<typeof setTimeout>;

  constructor(
    public id: string,
    public duration?: number
  ) {}

  /**
   * Dismisses the toast immediately.
   */
  dismiss(): void {
    if (this.dismissTimer) {
      clearTimeout(this.dismissTimer);
    }

    this._afterDismissed.next();
    this._afterDismissed.complete();
  }

  /**
   * Triggers the action associated with this toast.
   */
  triggerAction(): void {
    this._onAction.next();
    this.dismiss();
  }

  /**
   * Sets up auto-dismiss timer.
   * Called internally by the toast service.
   */
  _setupAutoDismiss(callback: () => void): void {
    if (this.duration && this.duration > 0) {
      this.dismissTimer = setTimeout(() => {
        callback();
      }, this.duration);
    }
  }

  /**
   * Pauses the auto-dismiss timer.
   * Used when user hovers over toast.
   */
  pauseAutoDismiss(): void {
    if (this.dismissTimer) {
      clearTimeout(this.dismissTimer);
    }
  }

  /**
   * Resumes the auto-dismiss timer.
   * Used when user stops hovering over toast.
   */
  resumeAutoDismiss(callback: () => void, remainingTime: number): void {
    if (remainingTime > 0) {
      this.dismissTimer = setTimeout(() => {
        callback();
      }, remainingTime);
    }
  }

  /**
   * Observable that emits when the toast has been dismissed.
   */
  afterDismissed(): Observable<void> {
    return this._afterDismissed.asObservable();
  }

  /**
   * Observable that emits when the action button is clicked.
   */
  onAction(): Observable<void> {
    return this._onAction.asObservable();
  }
}
