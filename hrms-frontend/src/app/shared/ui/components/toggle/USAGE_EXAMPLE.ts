/**
 * TOGGLE COMPONENT - USAGE EXAMPLES
 *
 * A production-ready iOS-style toggle switch component that replaces mat-slide-toggle.
 * Features smooth animations, keyboard accessibility, and multiple color variants.
 */

import { Component } from '@angular/core';
import { Toggle } from './toggle';

@Component({
  selector: 'app-toggle-examples',
  standalone: true,
  imports: [Toggle],
  template: `
    <div class="examples-container">
      <h2>Toggle Component Examples</h2>

      <!-- Basic Toggle -->
      <section>
        <h3>Basic Toggle</h3>
        <app-toggle
          [checked]="isBasicChecked"
          (checkedChange)="isBasicChecked = $event"
        />
        <p>Status: {{ isBasicChecked ? 'ON' : 'OFF' }}</p>
      </section>

      <!-- Toggle with Label (After) -->
      <section>
        <h3>Toggle with Label (After Position)</h3>
        <app-toggle
          [checked]="isLabelChecked"
          label="Enable notifications"
          labelPosition="after"
          (checkedChange)="isLabelChecked = $event"
        />
      </section>

      <!-- Toggle with Label (Before) -->
      <section>
        <h3>Toggle with Label (Before Position)</h3>
        <app-toggle
          [checked]="isLabelBeforeChecked"
          label="Dark mode"
          labelPosition="before"
          (checkedChange)="isLabelBeforeChecked = $event"
        />
      </section>

      <!-- Primary Color (Default) -->
      <section>
        <h3>Primary Color</h3>
        <app-toggle
          [checked]="isPrimaryChecked"
          label="Primary toggle"
          color="primary"
          (checkedChange)="isPrimaryChecked = $event"
        />
      </section>

      <!-- Success Color -->
      <section>
        <h3>Success Color</h3>
        <app-toggle
          [checked]="isSuccessChecked"
          label="Success toggle"
          color="success"
          (checkedChange)="isSuccessChecked = $event"
        />
      </section>

      <!-- Warning Color -->
      <section>
        <h3>Warning Color</h3>
        <app-toggle
          [checked]="isWarningChecked"
          label="Warning toggle"
          color="warning"
          (checkedChange)="isWarningChecked = $event"
        />
      </section>

      <!-- Disabled State (Unchecked) -->
      <section>
        <h3>Disabled State (Unchecked)</h3>
        <app-toggle
          [checked]="false"
          [disabled]="true"
          label="Disabled unchecked"
        />
      </section>

      <!-- Disabled State (Checked) -->
      <section>
        <h3>Disabled State (Checked)</h3>
        <app-toggle
          [checked]="true"
          [disabled]="true"
          label="Disabled checked"
        />
      </section>

      <!-- Real-world Example: Settings Form -->
      <section>
        <h3>Real-world Example: Settings Form</h3>
        <form class="settings-form">
          <div class="setting-item">
            <app-toggle
              [checked]="settings.emailNotifications"
              label="Email notifications"
              labelPosition="after"
              color="primary"
              (checkedChange)="settings.emailNotifications = $event"
            />
          </div>

          <div class="setting-item">
            <app-toggle
              [checked]="settings.pushNotifications"
              label="Push notifications"
              labelPosition="after"
              color="primary"
              (checkedChange)="settings.pushNotifications = $event"
            />
          </div>

          <div class="setting-item">
            <app-toggle
              [checked]="settings.autoSave"
              label="Auto-save drafts"
              labelPosition="after"
              color="success"
              (checkedChange)="settings.autoSave = $event"
            />
          </div>

          <div class="setting-item">
            <app-toggle
              [checked]="settings.betaFeatures"
              label="Enable beta features"
              labelPosition="after"
              color="warning"
              (checkedChange)="settings.betaFeatures = $event"
            />
          </div>

          <div class="setting-item">
            <app-toggle
              [checked]="settings.analytics"
              label="Share analytics"
              labelPosition="after"
              color="primary"
              [disabled]="settings.privacyMode"
              (checkedChange)="settings.analytics = $event"
            />
          </div>

          <div class="setting-item">
            <app-toggle
              [checked]="settings.privacyMode"
              label="Privacy mode (disables analytics)"
              labelPosition="after"
              color="warning"
              (checkedChange)="onPrivacyModeChange($event)"
            />
          </div>
        </form>
      </section>

      <!-- Keyboard Navigation Demo -->
      <section>
        <h3>Keyboard Navigation</h3>
        <p>Use Tab to navigate between toggles, Space to toggle.</p>
        <div class="keyboard-demo">
          <app-toggle label="Toggle 1" [checked]="false" />
          <app-toggle label="Toggle 2" [checked]="false" />
          <app-toggle label="Toggle 3" [checked]="false" />
        </div>
      </section>
    </div>
  `,
  styles: [`
    .examples-container {
      padding: 24px;
      max-width: 800px;
    }

    h2 {
      font-size: 24px;
      font-weight: 700;
      margin-bottom: 32px;
    }

    h3 {
      font-size: 16px;
      font-weight: 600;
      margin-bottom: 16px;
      color: #374151;
    }

    section {
      margin-bottom: 32px;
      padding: 24px;
      background: #f9fafb;
      border-radius: 8px;
    }

    p {
      margin-top: 12px;
      font-size: 14px;
      color: #6b7280;
    }

    .settings-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .setting-item {
      padding: 12px 0;
      border-bottom: 1px solid #e5e7eb;

      &:last-child {
        border-bottom: none;
      }
    }

    .keyboard-demo {
      display: flex;
      flex-direction: column;
      gap: 16px;
      margin-top: 16px;
    }
  `]
})
export class ToggleExamplesComponent {
  // Basic examples
  isBasicChecked = false;
  isLabelChecked = true;
  isLabelBeforeChecked = false;
  isPrimaryChecked = true;
  isSuccessChecked = true;
  isWarningChecked = false;

  // Settings form
  settings = {
    emailNotifications: true,
    pushNotifications: false,
    autoSave: true,
    betaFeatures: false,
    analytics: true,
    privacyMode: false
  };

  onPrivacyModeChange(enabled: boolean): void {
    this.settings.privacyMode = enabled;
    if (enabled) {
      this.settings.analytics = false;
    }
  }
}

/**
 * MIGRATION GUIDE: mat-slide-toggle to app-toggle
 *
 * Before:
 * ```html
 * <mat-slide-toggle
 *   [(ngModel)]="isEnabled"
 *   [disabled]="isDisabled"
 *   color="primary"
 *   labelPosition="after"
 * >
 *   Enable feature
 * </mat-slide-toggle>
 * ```
 *
 * After:
 * ```html
 * <app-toggle
 *   [checked]="isEnabled"
 *   (checkedChange)="isEnabled = $event"
 *   [disabled]="isDisabled"
 *   color="primary"
 *   labelPosition="after"
 *   label="Enable feature"
 * />
 * ```
 *
 * Key differences:
 * 1. Use [checked] instead of [(ngModel)]
 * 2. Use (checkedChange) event instead of (ngModelChange)
 * 3. Pass label as an @Input() prop instead of content projection
 * 4. Available colors: 'primary' | 'success' | 'warning'
 */
