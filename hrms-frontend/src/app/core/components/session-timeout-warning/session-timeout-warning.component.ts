import { Component, inject } from '@angular/core';

import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SessionManagementService } from '../../services/session-management.service';

/**
 * Session Timeout Warning Modal
 *
 * Displays 1 minute before automatic logout
 * Allows user to extend session or logout immediately
 */
@Component({
  selector: 'app-session-timeout-warning',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="session-warning-modal">
      <div class="modal-header">
        <mat-icon class="warning-icon">access_time</mat-icon>
        <h2>Session Expiring Soon</h2>
      </div>

      <div class="modal-content">
        <p class="message">
          Your session will expire in <strong>{{ sessionService.remainingTimeFormatted() }}</strong> due to inactivity.
        </p>
        <p class="sub-message">
          You will be automatically logged out for security reasons.
        </p>
      </div>

      <div class="modal-actions">
        <button mat-raised-button color="primary" (click)="extendSession()" class="extend-btn">
          <mat-icon>check_circle</mat-icon>
          Stay Logged In
        </button>
        <button mat-stroked-button (click)="logoutNow()" class="logout-btn">
          <mat-icon>exit_to_app</mat-icon>
          Logout Now
        </button>
      </div>

      <div class="countdown-bar">
        <div
          class="countdown-fill"
          [style.width.%]="getCountdownPercentage()">
        </div>
      </div>
    </div>
  `,
  styles: [`
    .session-warning-modal {
      padding: 24px;
      max-width: 450px;
      font-family: 'Roboto', 'Helvetica Neue', sans-serif;
    }

    .modal-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 20px;
    }

    .warning-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #ff9800;
    }

    h2 {
      margin: 0;
      font-size: 24px;
      font-weight: 500;
      color: #333;
    }

    .modal-content {
      margin-bottom: 24px;
    }

    .message {
      font-size: 16px;
      line-height: 1.5;
      color: #555;
      margin-bottom: 8px;
    }

    .message strong {
      color: #ff9800;
      font-weight: 600;
    }

    .sub-message {
      font-size: 14px;
      color: #777;
      margin: 0;
    }

    .modal-actions {
      display: flex;
      gap: 12px;
      margin-bottom: 20px;
    }

    .extend-btn {
      flex: 1;
      height: 44px;
      font-size: 15px;
      font-weight: 500;
    }

    .logout-btn {
      flex: 1;
      height: 44px;
      font-size: 15px;
      font-weight: 500;
    }

    .modal-actions button mat-icon {
      margin-right: 8px;
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    .countdown-bar {
      height: 6px;
      background-color: #e0e0e0;
      border-radius: 3px;
      overflow: hidden;
    }

    .countdown-fill {
      height: 100%;
      background: linear-gradient(90deg, #ff9800 0%, #f44336 100%);
      transition: width 1s linear;
    }

    /* Responsive */
    @media (max-width: 600px) {
      .session-warning-modal {
        padding: 16px;
      }

      .modal-actions {
        flex-direction: column;
      }

      .extend-btn,
      .logout-btn {
        width: 100%;
      }
    }
  `]
})
export class SessionTimeoutWarningComponent {
  sessionService = inject(SessionManagementService);

  extendSession(): void {
    this.sessionService.extendSession();
  }

  logoutNow(): void {
    // This will trigger immediate logout
    this.sessionService.stopSession();
    // The session service will handle the actual logout
  }

  getCountdownPercentage(): number {
    const total = 60 * 1000; // 1 minute in milliseconds
    const remaining = this.sessionService.remainingTime();
    return (remaining / total) * 100;
  }
}
