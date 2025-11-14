// ═══════════════════════════════════════════════════════════
// TOAST SERVICE
// Part of the Fortune 500-grade HRMS design system
// Manages toast/snackbar notifications with auto-dismiss, stacking, and animations
// ═══════════════════════════════════════════════════════════

import {
  Injectable,
  ComponentRef,
  ApplicationRef,
  createComponent,
  EnvironmentInjector,
  inject
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { ToastRef } from './toast-ref';
import { ToastContainerComponent } from '../components/toast-container/toast-container';

/**
 * Toast type for different notification styles
 */
export type ToastType = 'success' | 'error' | 'warning' | 'info';

/**
 * Toast position on screen
 */
export type ToastPosition = 'top-right' | 'top-center' | 'bottom-right' | 'bottom-center';

/**
 * Configuration options for displaying a toast
 */
export interface ToastConfig {
  /** The message to display */
  message: string;

  /** Type of toast (affects styling) */
  type: ToastType;

  /** Duration in milliseconds before auto-dismiss (0 = no auto-dismiss) */
  duration?: number;

  /** Optional action button configuration */
  action?: {
    /** Text for the action button */
    label: string;
    /** Callback when action is clicked */
    callback?: () => void;
  };

  /** Position on screen */
  position?: ToastPosition;

  /** Custom CSS class */
  customClass?: string;

  /** Whether the toast can be dismissed by clicking */
  dismissible?: boolean;

  /** Whether to show progress bar for auto-dismiss */
  showProgress?: boolean;
}

/**
 * Default toast configuration
 */
const DEFAULT_CONFIG: Partial<ToastConfig> = {
  duration: 3000,
  position: 'top-right',
  dismissible: true,
  showProgress: true
};

/**
 * Internal toast data passed to container
 */
export interface ToastData {
  id: string;
  config: ToastConfig;
  toastRef: ToastRef;
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private readonly document = inject(DOCUMENT);
  private readonly appRef = inject(ApplicationRef);
  private readonly injector = inject(EnvironmentInjector);

  /** Map of toast containers by position */
  private containers = new Map<ToastPosition, ComponentRef<ToastContainerComponent>>();

  /** Map of all active toasts */
  private activeToasts = new Map<string, ToastData>();

  /** Counter for generating unique toast IDs */
  private toastIdCounter = 0;

  /**
   * Displays a success toast.
   * @param message The message to display
   * @param duration Optional duration in milliseconds (default: 3000)
   */
  success(message: string, duration?: number): ToastRef {
    return this.show({
      message,
      type: 'success',
      duration: duration ?? DEFAULT_CONFIG.duration
    });
  }

  /**
   * Displays an error toast.
   * @param message The message to display
   * @param duration Optional duration in milliseconds (default: 3000)
   */
  error(message: string, duration?: number): ToastRef {
    return this.show({
      message,
      type: 'error',
      duration: duration ?? DEFAULT_CONFIG.duration
    });
  }

  /**
   * Displays a warning toast.
   * @param message The message to display
   * @param duration Optional duration in milliseconds (default: 3000)
   */
  warning(message: string, duration?: number): ToastRef {
    return this.show({
      message,
      type: 'warning',
      duration: duration ?? DEFAULT_CONFIG.duration
    });
  }

  /**
   * Displays an info toast.
   * @param message The message to display
   * @param duration Optional duration in milliseconds (default: 3000)
   */
  info(message: string, duration?: number): ToastRef {
    return this.show({
      message,
      type: 'info',
      duration: duration ?? DEFAULT_CONFIG.duration
    });
  }

  /**
   * Displays a toast with custom configuration.
   * @param config Toast configuration
   * @returns Reference to the toast
   */
  show(config: ToastConfig): ToastRef {
    // Merge with defaults
    const toastConfig: ToastConfig = { ...DEFAULT_CONFIG, ...config };
    const position = toastConfig.position || 'top-right';

    // Generate unique ID
    const toastId = `toast-${this.toastIdCounter++}`;

    // Create toast reference
    const toastRef = new ToastRef(toastId, toastConfig.duration);

    // Create toast data
    const toastData: ToastData = {
      id: toastId,
      config: toastConfig,
      toastRef
    };

    // Store in active toasts
    this.activeToasts.set(toastId, toastData);

    // Get or create container for this position
    const container = this.getOrCreateContainer(position);

    // Add toast to container
    container.instance.addToast(toastData);

    // Setup auto-dismiss
    toastRef._setupAutoDismiss(() => {
      this.dismiss(toastRef);
    });

    // Handle dismissal
    toastRef.afterDismissed().subscribe(() => {
      this.removeToast(toastRef);
    });

    // Handle action
    if (toastConfig.action?.callback) {
      toastRef.onAction().subscribe(() => {
        toastConfig.action!.callback!();
      });
    }

    return toastRef;
  }

  /**
   * Dismisses a specific toast.
   * @param toastRef The toast reference to dismiss
   */
  dismiss(toastRef: ToastRef): void {
    if (!toastRef) {
      return;
    }

    toastRef.dismiss();
  }

  /**
   * Dismisses all active toasts.
   */
  dismissAll(): void {
    const toasts = Array.from(this.activeToasts.values());
    toasts.forEach(toast => this.dismiss(toast.toastRef));
  }

  /**
   * Gets all active toasts.
   */
  getActiveToasts(): ToastRef[] {
    return Array.from(this.activeToasts.values()).map(data => data.toastRef);
  }

  /**
   * Gets or creates a container for the specified position.
   */
  private getOrCreateContainer(position: ToastPosition): ComponentRef<ToastContainerComponent> {
    let container = this.containers.get(position);

    if (!container) {
      // Create new container
      container = createComponent(ToastContainerComponent, {
        environmentInjector: this.injector
      });

      // Set position
      container.instance.position = position;

      // Attach to application
      this.appRef.attachView(container.hostView);

      // Append to body
      const domElem = (container.hostView as any).rootNodes[0] as HTMLElement;
      this.document.body.appendChild(domElem);

      // Store container
      this.containers.set(position, container);
    }

    return container;
  }

  /**
   * Removes a toast from the active toasts and container.
   */
  private removeToast(toastRef: ToastRef): void {
    const toastData = this.activeToasts.get(toastRef.id);
    if (!toastData) {
      return;
    }

    // Remove from active toasts
    this.activeToasts.delete(toastRef.id);

    // Remove from container
    const position = toastData.config.position || 'top-right';
    const container = this.containers.get(position);
    if (container) {
      container.instance.removeToast(toastRef.id);

      // If container is empty, destroy it
      if (container.instance.toasts.length === 0) {
        this.destroyContainer(position);
      }
    }
  }

  /**
   * Destroys a container and removes it from the DOM.
   */
  private destroyContainer(position: ToastPosition): void {
    const container = this.containers.get(position);
    if (container) {
      this.appRef.detachView(container.hostView);
      container.destroy();
      this.containers.delete(position);
    }
  }
}
