import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

type LoginStage = 'credentials' | 'mfa-setup' | 'mfa-verify';

@Component({
  selector: 'app-superadmin-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './superadmin-login.component.html',
  styleUrls: ['./superadmin-login.component.scss']
})
export class SuperAdminLoginComponent {
  // Login stage tracking
  loginStage = signal<LoginStage>('credentials');

  // Credentials form
  email = signal('');
  password = signal('');
  hidePassword = signal(true);

  // MFA data
  userId = signal('');
  mfaSecret = signal('');
  qrCodeBase64 = signal('');
  backupCodes = signal<string[]>([]);

  // TOTP verification
  totpCode = signal('');

  // UI state
  isLoading = signal(false);
  errorMessage = signal('');
  showBackupCodes = signal(false);
  backupCodesDownloaded = signal(false);

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  togglePasswordVisibility(): void {
    this.hidePassword.update(value => !value);
  }

  isValidEmail(email: string): boolean {
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailPattern.test(email);
  }

  isValidCredentialsForm(): boolean {
    const emailValue = this.email();
    const passwordValue = this.password();
    return emailValue.trim() !== '' &&
           this.isValidEmail(emailValue) &&
           passwordValue.trim() !== '' &&
           passwordValue.length >= 6;
  }

  isValidTotpCode(): boolean {
    const code = this.totpCode();
    return /^\d{6}$/.test(code);
  }

  isValidMfaSetupForm(): boolean {
    return this.isValidTotpCode() && this.backupCodesDownloaded();
  }

  async onCredentialsSubmit(): Promise<void> {
    if (!this.isValidCredentialsForm()) {
      this.errorMessage.set('Please enter a valid email and password (minimum 6 characters)');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const credentials = {
      email: this.email().trim(),
      password: this.password()
    };

    this.authService.superAdminSecretLogin(credentials).subscribe({
      next: (response) => {
        this.isLoading.set(false);

        // Check if MFA setup is required (first login)
        if ('qrCode' in response && 'secret' in response) {
          // MFA setup required
          this.userId.set(response.userId);
          this.qrCodeBase64.set(response.qrCode);
          this.mfaSecret.set(response.secret);
          this.backupCodes.set(response.backupCodes);
          this.loginStage.set('mfa-setup');
        }
        // Check if MFA verification is required (subsequent login)
        else if ('userId' in response && !('qrCode' in response)) {
          this.userId.set(response.userId);
          this.loginStage.set('mfa-verify');
        }
        // Direct login (no MFA) - should not happen for SuperAdmin
        else {
          this.router.navigate(['/admin/dashboard']);
        }
      },
      error: (error) => {
        console.error('SuperAdmin login error:', error);
        this.isLoading.set(false);

        if (error.status === 401) {
          this.errorMessage.set('Invalid credentials. SuperAdmin access denied.');
        } else if (error.status >= 500) {
          this.errorMessage.set('Server error. Please try again later.');
        } else {
          this.errorMessage.set(error.message || 'Login failed. Please try again.');
        }
      }
    });
  }

  async onMfaSetupSubmit(): Promise<void> {
    if (!this.isValidMfaSetupForm()) {
      this.errorMessage.set('Please enter a valid 6-digit TOTP code and download backup codes.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const setupRequest = {
      userId: this.userId(),
      totpCode: this.totpCode(),
      secret: this.mfaSecret(),
      backupCodes: this.backupCodes()
    };

    this.authService.completeMfaSetup(setupRequest).subscribe({
      next: () => {
        this.isLoading.set(false);
        // Auth service handles navigation to /admin/dashboard
      },
      error: (error) => {
        console.error('MFA setup error:', error);
        this.isLoading.set(false);
        this.errorMessage.set(error.message || 'Invalid TOTP code. Please try again.');
      }
    });
  }

  async onMfaVerifySubmit(): Promise<void> {
    if (!this.isValidTotpCode()) {
      this.errorMessage.set('Please enter a valid 6-digit code.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const verifyRequest = {
      userId: this.userId(),
      code: this.totpCode()
    };

    this.authService.verifyMfa(verifyRequest).subscribe({
      next: () => {
        this.isLoading.set(false);
        // Auth service handles navigation to /admin/dashboard
      },
      error: (error) => {
        console.error('MFA verification error:', error);
        this.isLoading.set(false);
        this.errorMessage.set(error.message || 'Invalid verification code. Please try again.');
      }
    });
  }

  downloadBackupCodes(): void {
    const codes = this.backupCodes().join('\n');
    const blob = new Blob([
      'MorisHR SuperAdmin Backup Codes\n',
      '================================\n',
      `Email: ${this.email()}\n`,
      `Generated: ${new Date().toLocaleString()}\n\n`,
      'IMPORTANT: Store these codes securely!\n',
      'Each code can only be used once.\n\n',
      codes
    ], { type: 'text/plain' });

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `morishr-backup-codes-${Date.now()}.txt`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);

    this.backupCodesDownloaded.set(true);
  }

  copyBackupCodes(): void {
    const codes = this.backupCodes().join('\n');
    navigator.clipboard.writeText(codes).then(() => {
      alert('Backup codes copied to clipboard!');
      this.backupCodesDownloaded.set(true);
    });
  }

  // Auto-format TOTP input (add space after 3 digits)
  onTotpInput(event: Event): void {
    const input = (event.target as HTMLInputElement).value.replace(/\s/g, '');
    if (/^\d{0,6}$/.test(input)) {
      this.totpCode.set(input);
    }
  }

  // Navigate back to credentials stage
  backToCredentials(): void {
    this.loginStage.set('credentials');
    this.errorMessage.set('');
    this.totpCode.set('');
  }
}
