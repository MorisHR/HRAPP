// ═══════════════════════════════════════════════════════════
// TOAST CONTAINER COMPONENT
// Part of the Fortune 500-grade HRMS design system
// Container for displaying stacked toast notifications
// ═══════════════════════════════════════════════════════════

import { Component, HostBinding } from '@angular/core';
import { CommonModule } from '@angular/common';
import { trigger, transition, style, animate, state } from '@angular/animations';
import { ToastData, ToastPosition } from '../../services/toast';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.scss',
  animations: [
    trigger('toastAnimation', [
      transition(':enter', [
        style({
          transform: 'translateX(100%)',
          opacity: 0
        }),
        animate('300ms cubic-bezier(0.4, 0.0, 0.2, 1)', style({
          transform: 'translateX(0)',
          opacity: 1
        }))
      ]),
      transition(':leave', [
        animate('200ms cubic-bezier(0.4, 0.0, 1, 1)', style({
          transform: 'translateX(100%)',
          opacity: 0,
          height: 0,
          marginBottom: 0,
          paddingTop: 0,
          paddingBottom: 0
        }))
      ])
    ]),
    trigger('progressAnimation', [
      state('active', style({
        width: '0%'
      })),
      transition('* => active', [
        animate('{{ duration }}ms linear', style({
          width: '100%'
        }))
      ])
    ])
  ]
})
export class ToastContainerComponent {
  /** Position of this container */
  position: ToastPosition = 'top-right';

  /** List of active toasts in this container */
  toasts: ToastData[] = [];

  /** Track hover state for pause/resume */
  private hoverStates = new Map<string, { paused: boolean; startTime: number; remainingTime: number }>();

  @HostBinding('class')
  get hostClasses(): string {
    return `toast-container toast-container--${this.position}`;
  }

  /**
   * Adds a toast to this container.
   */
  addToast(toast: ToastData): void {
    this.toasts.push(toast);

    // Initialize hover state
    if (toast.config.duration) {
      this.hoverStates.set(toast.id, {
        paused: false,
        startTime: Date.now(),
        remainingTime: toast.config.duration
      });
    }
  }

  /**
   * Removes a toast from this container.
   */
  removeToast(id: string): void {
    const index = this.toasts.findIndex(t => t.id === id);
    if (index !== -1) {
      this.toasts.splice(index, 1);
      this.hoverStates.delete(id);
    }
  }

  /**
   * Dismisses a toast.
   */
  dismissToast(toast: ToastData): void {
    if (toast.config.dismissible !== false) {
      toast.toastRef.dismiss();
    }
  }

  /**
   * Triggers the action for a toast.
   */
  triggerAction(toast: ToastData): void {
    toast.toastRef.triggerAction();
  }

  /**
   * Handles mouse enter on a toast (pause auto-dismiss).
   */
  onToastMouseEnter(toast: ToastData): void {
    const state = this.hoverStates.get(toast.id);
    if (state && !state.paused) {
      const elapsed = Date.now() - state.startTime;
      state.remainingTime = Math.max(0, state.remainingTime - elapsed);
      state.paused = true;
      toast.toastRef.pauseAutoDismiss();
    }
  }

  /**
   * Handles mouse leave on a toast (resume auto-dismiss).
   */
  onToastMouseLeave(toast: ToastData): void {
    const state = this.hoverStates.get(toast.id);
    if (state && state.paused) {
      state.startTime = Date.now();
      state.paused = false;
      toast.toastRef.resumeAutoDismiss(
        () => toast.toastRef.dismiss(),
        state.remainingTime
      );
    }
  }

  /**
   * Gets the icon for a toast type.
   */
  getIcon(type: string): string {
    const icons: Record<string, string> = {
      success: '✓',
      error: '✕',
      warning: '⚠',
      info: 'ℹ'
    };
    return icons[type] || '';
  }

  /**
   * Gets CSS classes for a toast.
   */
  getToastClasses(toast: ToastData): string {
    const classes = [
      'toast',
      `toast--${toast.config.type}`
    ];

    if (toast.config.customClass) {
      classes.push(toast.config.customClass);
    }

    if (toast.config.dismissible !== false) {
      classes.push('toast--dismissible');
    }

    return classes.join(' ');
  }

  /**
   * Gets animation parameters for progress bar.
   */
  getProgressParams(toast: ToastData): { value: string; params: { duration: number } } {
    return {
      value: 'active',
      params: {
        duration: toast.config.duration || 3000
      }
    };
  }

  /**
   * Tracks toasts by ID for *ngFor performance.
   */
  trackByToastId(index: number, toast: ToastData): string {
    return toast.id;
  }
}
