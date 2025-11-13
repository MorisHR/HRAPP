import { Component, Input, OnInit, signal, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Clipboard } from '@angular/cdk/clipboard';
import { DeviceApiKeyService, DeviceApiKeyDto, GenerateApiKeyResponse } from '../../../../core/services/device-api-key.service';

@Component({
  selector: 'app-device-api-keys',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './device-api-keys.component.html',
  styleUrls: ['./device-api-keys.component.scss']
})
export class DeviceApiKeysComponent implements OnInit {
  @Input() deviceId!: string;

  // Using signals for reactive state
  apiKeys = signal<DeviceApiKeyDto[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  displayedColumns: string[] = ['description', 'status', 'createdAt', 'expiresAt', 'lastUsedAt', 'usageCount', 'actions'];

  constructor(
    private apiKeyService: DeviceApiKeyService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private clipboard: Clipboard,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (!this.deviceId) {
      console.error('DeviceApiKeysComponent: deviceId is required');
      return;
    }
    this.loadApiKeys();
  }

  /**
   * Load all API keys for the device
   */
  private loadApiKeys(): void {
    this.loading.set(true);
    this.error.set(null);

    this.apiKeyService.getDeviceApiKeys(this.deviceId).subscribe({
      next: (keys) => {
        this.apiKeys.set(keys);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading API keys:', error);
        this.error.set('Failed to load API keys');
        this.loading.set(false);
        this.snackBar.open('Failed to load API keys', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Open dialog to generate a new API key
   */
  onGenerateNewKey(): void {
    const dialogRef = this.dialog.open(GenerateApiKeyDialogComponent, {
      width: '500px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe((description: string | null) => {
      if (description) {
        this.generateApiKey(description);
      }
    });
  }

  /**
   * Generate a new API key
   */
  private generateApiKey(description: string): void {
    this.apiKeyService.generateApiKey(this.deviceId, description).subscribe({
      next: (response) => {
        // Show the API key in a dialog
        this.showApiKeyDialog(response);
        // Reload the list
        this.loadApiKeys();
        this.snackBar.open('API key generated successfully', 'Close', { duration: 3000 });
      },
      error: (error) => {
        console.error('Error generating API key:', error);
        this.snackBar.open('Failed to generate API key', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Show the generated API key in a dialog
   */
  private showApiKeyDialog(response: GenerateApiKeyResponse): void {
    this.dialog.open(ShowApiKeyDialogComponent, {
      width: '600px',
      disableClose: true,
      data: response
    });
  }

  /**
   * Revoke an API key
   */
  onRevoke(apiKey: DeviceApiKeyDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Revoke API Key',
        message: `Are you sure you want to revoke the API key "${apiKey.description}"? This action cannot be undone.`,
        confirmText: 'Revoke',
        cancelText: 'Cancel'
      }
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.revokeApiKey(apiKey.id);
      }
    });
  }

  /**
   * Revoke an API key
   */
  private revokeApiKey(apiKeyId: string): void {
    this.apiKeyService.revokeApiKey(this.deviceId, apiKeyId).subscribe({
      next: () => {
        this.snackBar.open('API key revoked successfully', 'Close', { duration: 3000 });
        this.loadApiKeys();
      },
      error: (error) => {
        console.error('Error revoking API key:', error);
        this.snackBar.open('Failed to revoke API key', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Rotate an API key
   */
  onRotate(apiKey: DeviceApiKeyDto): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Rotate API Key',
        message: `Are you sure you want to rotate the API key "${apiKey.description}"? The old key will be revoked and a new one will be generated.`,
        confirmText: 'Rotate',
        cancelText: 'Cancel'
      }
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.rotateApiKey(apiKey.id);
      }
    });
  }

  /**
   * Rotate an API key
   */
  private rotateApiKey(apiKeyId: string): void {
    this.apiKeyService.rotateApiKey(this.deviceId, apiKeyId).subscribe({
      next: (response) => {
        // Show the new API key in a dialog
        this.showApiKeyDialog(response);
        // Reload the list
        this.loadApiKeys();
        this.snackBar.open('API key rotated successfully', 'Close', { duration: 3000 });
      },
      error: (error) => {
        console.error('Error rotating API key:', error);
        this.snackBar.open('Failed to rotate API key', 'Close', { duration: 3000 });
      }
    });
  }

  /**
   * Get status chip class based on key state
   */
  getStatusClass(apiKey: DeviceApiKeyDto): string {
    if (!apiKey.isActive) {
      return 'status-revoked';
    }
    if (apiKey.expiresAt && new Date(apiKey.expiresAt) < new Date()) {
      return 'status-expired';
    }
    if (apiKey.daysUntilExpiration && apiKey.daysUntilExpiration < 30) {
      return 'status-expiring';
    }
    return 'status-active';
  }

  /**
   * Get status text
   */
  getStatusText(apiKey: DeviceApiKeyDto): string {
    if (!apiKey.isActive) {
      return 'Revoked';
    }
    if (apiKey.expiresAt && new Date(apiKey.expiresAt) < new Date()) {
      return 'Expired';
    }
    if (apiKey.daysUntilExpiration && apiKey.daysUntilExpiration < 30) {
      return `Expiring Soon (${apiKey.daysUntilExpiration}d)`;
    }
    return 'Active';
  }

  /**
   * Format date for display
   */
  formatDate(dateString?: string): string {
    if (!dateString) {
      return 'Never';
    }
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }

  /**
   * Refresh the API keys list
   */
  onRefresh(): void {
    this.loadApiKeys();
  }
}

/**
 * Dialog for generating a new API key
 */
@Component({
  selector: 'app-generate-api-key-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>
      <mat-icon>key</mat-icon>
      Generate New API Key
    </h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description</mat-label>
          <input matInput formControlName="description" placeholder="e.g., Production sync service">
          <mat-icon matPrefix>description</mat-icon>
          <mat-hint>Describe the purpose of this API key</mat-hint>
          @if (form.get('description')?.hasError('required') && form.get('description')?.touched) {
            <mat-error>Description is required</mat-error>
          }
          @if (form.get('description')?.hasError('maxlength')) {
            <mat-error>Maximum 200 characters</mat-error>
          }
        </mat-form-field>

        <div class="warning-box">
          <mat-icon>warning</mat-icon>
          <div>
            <strong>Important:</strong> The API key will only be shown once after generation.
            Make sure to copy and store it securely.
          </div>
        </div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button
        mat-raised-button
        color="primary"
        (click)="onGenerate()"
        [disabled]="form.invalid">
        <mat-icon>vpn_key</mat-icon>
        Generate
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width {
      width: 100%;
    }

    .warning-box {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 16px;
      background-color: #fff3cd;
      border: 1px solid #ffc107;
      border-radius: 4px;
      margin-top: 16px;
    }

    .warning-box mat-icon {
      color: #ff9800;
      margin-top: 2px;
    }

    h2 {
      display: flex;
      align-items: center;
      gap: 12px;
    }
  `]
})
export class GenerateApiKeyDialogComponent {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<GenerateApiKeyDialogComponent>
  ) {
    this.form = this.fb.group({
      description: ['', [Validators.required, Validators.maxLength(200)]]
    });
  }

  onGenerate(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value.description);
    }
  }
}

/**
 * Dialog for showing the generated API key
 */
@Component({
  selector: 'app-show-api-key-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule
  ],
  template: `
    <h2 mat-dialog-title class="success-title">
      <mat-icon>check_circle</mat-icon>
      API Key Generated Successfully
    </h2>
    <mat-dialog-content>
      <div class="warning-section">
        <mat-icon>warning</mat-icon>
        <div>
          <strong>IMPORTANT: Save this key securely!</strong>
          <p>This is the only time you will see this API key. It cannot be retrieved later.</p>
        </div>
      </div>

      <div class="api-key-section">
        <label>Your API Key:</label>
        <div class="api-key-display">
          <code>{{ data.plaintextKey }}</code>
          <button
            mat-icon-button
            (click)="copyToClipboard()"
            [class.copied]="copied"
            matTooltip="Copy to clipboard">
            <mat-icon>{{ copied ? 'check' : 'content_copy' }}</mat-icon>
          </button>
        </div>
      </div>

      <div class="info-section">
        <div class="info-item">
          <mat-icon>event</mat-icon>
          <div>
            <strong>Expires:</strong>
            <span>{{ formatDate(data.expiresAt) }}</span>
          </div>
        </div>
      </div>

      @if (!hidden) {
        <div class="countdown-timer">
          <mat-icon>timer</mat-icon>
          This dialog will close automatically in {{ countdown }} seconds
        </div>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button
        mat-raised-button
        color="primary"
        (click)="onClose()"
        [disabled]="!copied">
        <mat-icon>check</mat-icon>
        I've Saved the Key
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .success-title {
      display: flex;
      align-items: center;
      gap: 12px;
      color: #4caf50;
    }

    .warning-section {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 16px;
      background-color: #fff3cd;
      border: 1px solid #ffc107;
      border-radius: 4px;
      margin-bottom: 24px;
    }

    .warning-section mat-icon {
      color: #ff9800;
      margin-top: 2px;
    }

    .warning-section p {
      margin: 8px 0 0 0;
      color: #666;
    }

    .api-key-section {
      margin-bottom: 24px;
    }

    .api-key-section label {
      display: block;
      font-weight: 500;
      margin-bottom: 8px;
      color: #333;
    }

    .api-key-display {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      background-color: #f5f5f5;
      border: 2px solid #2196f3;
      border-radius: 4px;
    }

    .api-key-display code {
      flex: 1;
      font-family: 'Courier New', monospace;
      font-size: 13px;
      word-break: break-all;
      color: #1976d2;
    }

    .api-key-display button {
      flex-shrink: 0;
    }

    .api-key-display button.copied {
      color: #4caf50;
    }

    .info-section {
      margin-bottom: 16px;
    }

    .info-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 8px;
    }

    .info-item mat-icon {
      color: #666;
    }

    .info-item div {
      display: flex;
      gap: 8px;
    }

    .countdown-timer {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      background-color: #e3f2fd;
      border-radius: 4px;
      color: #1976d2;
      font-size: 14px;
    }

    mat-dialog-actions button {
      min-width: 200px;
    }
  `]
})
export class ShowApiKeyDialogComponent implements OnInit {
  copied = false;
  hidden = false;
  countdown = 60;
  private countdownInterval: any;

  constructor(
    private dialogRef: MatDialogRef<ShowApiKeyDialogComponent>,
    private clipboard: Clipboard,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: GenerateApiKeyResponse
  ) {}

  ngOnInit(): void {
    // Auto-copy to clipboard on open
    this.copyToClipboard();

    // Start countdown
    this.startCountdown();
  }

  private startCountdown(): void {
    this.countdownInterval = setInterval(() => {
      this.countdown--;
      if (this.countdown <= 0) {
        this.hidden = true;
        clearInterval(this.countdownInterval);
        // Auto-close after 60 seconds if user hasn't closed it
        setTimeout(() => {
          if (this.dialogRef.close) {
            this.dialogRef.close();
          }
        }, 100);
      }
    }, 1000);
  }

  copyToClipboard(): void {
    const success = this.clipboard.copy(this.data.plaintextKey);
    if (success) {
      this.copied = true;
      this.snackBar.open('API key copied to clipboard', 'Close', { duration: 2000 });
    } else {
      this.snackBar.open('Failed to copy API key', 'Close', { duration: 3000 });
    }
  }

  onClose(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
    this.dialogRef.close();
  }

  formatDate(dateString?: string): string {
    if (!dateString) {
      return 'Never';
    }
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }
}

/**
 * Confirmation dialog
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ data.cancelText }}</button>
      <button mat-raised-button color="warn" [mat-dialog-close]="true">{{ data.confirmText }}</button>
    </mat-dialog-actions>
  `
})
export class ConfirmDialogComponent {
  constructor(
    @Inject(MAT_DIALOG_DATA) public data: {
      title: string;
      message: string;
      confirmText: string;
      cancelText: string;
    }
  ) {}
}
