import { Injectable } from '@angular/core';

export interface Toast {
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private toasts: Toast[] = [];
  private toastContainer: HTMLElement | null = null;

  constructor() {
    this.initToastContainer();
  }

  private initToastContainer(): void {
    if (typeof document !== 'undefined') {
      this.toastContainer = document.createElement('div');
      this.toastContainer.className = 'toast-container';
      this.toastContainer.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        display: flex;
        flex-direction: column;
        gap: 10px;
      `;
      document.body.appendChild(this.toastContainer);
    }
  }

  success(message: string, duration = 3000): void {
    this.show({ message, type: 'success', duration });
  }

  error(message: string, duration = 5000): void {
    this.show({ message, type: 'error', duration });
  }

  info(message: string, duration = 3000): void {
    this.show({ message, type: 'info', duration });
  }

  warning(message: string, duration = 4000): void {
    this.show({ message, type: 'warning', duration });
  }

  private show(toast: Toast): void {
    if (!this.toastContainer) return;

    const toastElement = document.createElement('div');
    toastElement.className = `toast toast-${toast.type}`;
    toastElement.style.cssText = `
      padding: 16px 20px;
      border-radius: 8px;
      color: white;
      font-size: 14px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      min-width: 300px;
      max-width: 500px;
      animation: slideIn 0.3s ease-out;
      display: flex;
      align-items: center;
      gap: 12px;
    `;

    // Set background color based on type
    const colors = {
      success: '#28a745',
      error: '#dc3545',
      info: '#0dcaf0',
      warning: '#ffc107'
    };
    toastElement.style.backgroundColor = colors[toast.type];

    // Add icon
    const icons = {
      success: '✓',
      error: '✕',
      info: 'ℹ',
      warning: '⚠'
    };
    const icon = document.createElement('span');
    icon.textContent = icons[toast.type];
    icon.style.cssText = 'font-size: 20px; font-weight: bold;';
    toastElement.appendChild(icon);

    // Add message
    const messageElement = document.createElement('span');
    messageElement.textContent = toast.message;
    messageElement.style.flex = '1';
    toastElement.appendChild(messageElement);

    // Add close button
    const closeButton = document.createElement('button');
    closeButton.textContent = '×';
    closeButton.style.cssText = `
      background: none;
      border: none;
      color: white;
      font-size: 24px;
      cursor: pointer;
      padding: 0;
      line-height: 1;
      opacity: 0.8;
    `;
    closeButton.onclick = () => this.removeToast(toastElement);
    toastElement.appendChild(closeButton);

    this.toastContainer.appendChild(toastElement);

    // Auto remove after duration
    if (toast.duration) {
      setTimeout(() => this.removeToast(toastElement), toast.duration);
    }
  }

  private removeToast(element: HTMLElement): void {
    element.style.animation = 'slideOut 0.3s ease-in';
    setTimeout(() => {
      if (element.parentNode) {
        element.parentNode.removeChild(element);
      }
    }, 300);
  }
}

// Add CSS animations to document
if (typeof document !== 'undefined') {
  const style = document.createElement('style');
  style.textContent = `
    @keyframes slideIn {
      from {
        transform: translateX(400px);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    @keyframes slideOut {
      from {
        transform: translateX(0);
        opacity: 1;
      }
      to {
        transform: translateX(400px);
        opacity: 0;
      }
    }
  `;
  document.head.appendChild(style);
}
