// ═══════════════════════════════════════════════════════════
// DIALOG REF CLASS
// Part of the Fortune 500-grade HRMS design system
// Manages individual dialog instance lifecycle and communication
// ═══════════════════════════════════════════════════════════

import { ComponentRef } from '@angular/core';
import { Subject, Observable } from 'rxjs';

/**
 * Reference to an opened dialog.
 * Provides methods to close the dialog and observe its lifecycle.
 */
export class DialogRef<T = any, R = any> {
  /** Emits when the dialog has been closed */
  private readonly _afterClosed = new Subject<R | undefined>();

  /** Emits when the dialog has been opened and is visible */
  private readonly _afterOpened = new Subject<void>();

  /** Emits before the dialog begins closing */
  private readonly _beforeClosed = new Subject<R | undefined>();

  /** Reference to the dialog container component */
  public containerRef?: ComponentRef<any>;

  /** Reference to the content component inside the dialog */
  public componentRef?: ComponentRef<T>;

  constructor(
    public id: string,
    public data?: any
  ) {}

  /**
   * Closes the dialog with an optional result value.
   * @param result Optional result data to pass back to the caller
   */
  close(result?: R): void {
    this._beforeClosed.next(result);
    this._beforeClosed.complete();

    // Emit the result and complete the observable
    this._afterClosed.next(result);
    this._afterClosed.complete();
  }

  /**
   * Marks the dialog as opened.
   * Called internally by the dialog service.
   */
  _emitOpened(): void {
    this._afterOpened.next();
    this._afterOpened.complete();
  }

  /**
   * Observable that emits when the dialog has been closed.
   * @returns Observable that emits the result data
   */
  afterClosed(): Observable<R | undefined> {
    return this._afterClosed.asObservable();
  }

  /**
   * Observable that emits when the dialog has been opened.
   * @returns Observable that emits when dialog is visible
   */
  afterOpened(): Observable<void> {
    return this._afterOpened.asObservable();
  }

  /**
   * Observable that emits before the dialog closes.
   * @returns Observable that emits the result data
   */
  beforeClosed(): Observable<R | undefined> {
    return this._beforeClosed.asObservable();
  }
}
