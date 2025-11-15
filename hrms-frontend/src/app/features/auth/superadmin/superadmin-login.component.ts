import { Component, signal, effect, OnInit } from '@angular/core';

import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SessionManagementService } from '../../../core/services/session-management.service';
import { UiModule } from '../../../shared/ui/ui.module';

type LoginStage = 'credentials' | 'mfa-setup' | 'mfa-verify';

@Component({
  selector: 'app-superadmin-login',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, UiModule],
  templateUrl: './superadmin-login.component.html',
  styleUrls: ['./superadmin-login.component.scss']
})
export class SuperAdminLoginComponent implements OnInit {
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

  // TOTP verification (legacy signal for mfa-verify stage)
  totpCode = signal('');

  // ✅ PRODUCTION-GRADE REACTIVE FORMS
  mfaSetupForm!: FormGroup;
  mfaVerifyForm!: FormGroup;

  // UI state
  isLoading = signal(false);
  errorMessage = signal('');
  showBackupCodes = signal(false);
  backupCodesDownloaded = signal(false);

  constructor(
    private router: Router,
    private authService: AuthService,
    private sessionManagement: SessionManagementService,
    private fb: FormBuilder
  ) {
    // ✅ Initialize reactive forms with proper validators
    this.initializeForms();

    // Debug button state on every signal change
    effect(() => {
      const backupDownloaded = this.backupCodesDownloaded();
      const loading = this.isLoading();
      const stage = this.loginStage();

      if (stage === 'mfa-setup' && this.mfaSetupForm) {
        const totpControl = this.mfaSetupForm.get('totpCode');
        const totpValue = totpControl?.value || '';

        console.log('🔍 [BUTTON STATE DEBUG - MFA SETUP]');
        console.log('  - Login Stage:', stage);
        console.log('  - TOTP Code Value:', `"${totpValue}" (length: ${totpValue.length})`);
        console.log('  - TOTP Control Valid:', totpControl?.valid);
        console.log('  - TOTP Control Errors:', totpControl?.errors);
        console.log('  - Backup Codes Downloaded:', backupDownloaded);
        console.log('  - Is Loading:', loading);
        console.log('  - Form Valid:', this.mfaSetupForm.valid);
        console.log('  - Button Disabled:', this.isMfaSetupButtonDisabled);
      }
    });
  }

  ngOnInit(): void {
    // ✅ FORTUNE 500 PATTERN: Auto-redirect authenticated users with valid tokens
    // Prevents users from viewing login page while logged in
    // Matches Fortune 500 behavior (Google, Microsoft, Salesforce)
    if (this.authService.isAuthenticated() && !this.sessionManagement.isTokenExpired()) {
      const user = this.authService.user();

      // ✅ Check if user is SuperAdmin - only redirect SuperAdmins to admin dashboard
      if (user?.role === 'SuperAdmin') {
        console.log('✅ SuperAdmin already authenticated - redirecting to admin dashboard');
        this.router.navigate(['/admin/dashboard'], { replaceUrl: true });
        return;
      } else {
        // Non-SuperAdmin user trying to access SuperAdmin login
        // Clear their session silently so they can login as SuperAdmin
        console.log('⚠️ Non-SuperAdmin user on SuperAdmin login page - clearing session');
        this.authService.clearAuthStateSilently();
        // User can now login as SuperAdmin
      }
    }

    // ✅ FORTUNE 500 PATTERN: Silent clearing of expired tokens without navigation
    // If user has expired/invalid tokens, clear them silently since we're already on login page
    // This prevents unwanted redirects when accessing /auth/superadmin directly
    if (this.authService.isAuthenticated() && this.sessionManagement.isTokenExpired()) {
      console.log('⚠️ Token expired - clearing silently (already on login page)');
      this.authService.clearAuthStateSilently();
    }
  }

  // ✅ Initialize forms with proper validation
  private initializeForms(): void {
    this.mfaSetupForm = this.fb.group({
      totpCode: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(6),
        Validators.pattern(/^\d{6}$/)
      ]]
    });

    this.mfaVerifyForm = this.fb.group({
      totpCode: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(6),
        Validators.pattern(/^\d{6}$/)
      ]]
    });

    // ✅ Force validation update on value changes
    this.mfaSetupForm.get('totpCode')?.valueChanges.subscribe(value => {
      if (value && value.length === 6 && /^\d{6}$/.test(value)) {
        this.mfaSetupForm.get('totpCode')?.setErrors(null);
        this.mfaSetupForm.updateValueAndValidity({ emitEvent: false });
      }
    });

    this.mfaVerifyForm.get('totpCode')?.valueChanges.subscribe(value => {
      if (value && value.length === 6 && /^\d{6}$/.test(value)) {
        this.mfaVerifyForm.get('totpCode')?.setErrors(null);
        this.mfaVerifyForm.updateValueAndValidity({ emitEvent: false });
      }
    });
  }

  // ✅ Reset MFA setup form when entering setup stage
  private resetMfaSetupForm(): void {
    this.mfaSetupForm.reset({
      totpCode: ''
    });
    this.mfaSetupForm.markAsUntouched();
    this.mfaSetupForm.markAsPristine();
    this.errorMessage.set('');
  }

  // ✅ Reset MFA verify form when entering verify stage
  private resetMfaVerifyForm(): void {
    this.mfaVerifyForm.reset({
      totpCode: ''
    });
    this.mfaVerifyForm.markAsUntouched();
    this.mfaVerifyForm.markAsPristine();
    this.errorMessage.set('');
  }

  // ✅ Computed property for button disabled state
  get isMfaSetupButtonDisabled(): boolean {
    const code = this.mfaSetupForm.get('totpCode')?.value || '';
    const backupDownloaded = this.backupCodesDownloaded();
    const loading = this.isLoading();

    // Explicit validation: button enabled only when ALL conditions met
    return loading ||
           !backupDownloaded ||
           code.length !== 6 ||
           !/^\d{6}$/.test(code);
  }

  // ✅ Computed property for verify button disabled state
  get isMfaVerifyButtonDisabled(): boolean {
    const code = this.mfaVerifyForm.get('totpCode')?.value || '';
    const loading = this.isLoading();

    return loading ||
           code.length !== 6 ||
           !/^\d{6}$/.test(code);
  }

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

  // ✅ LEGACY: Keep for backward compatibility with mfa-verify stage
  isValidTotpCode(): boolean {
    const code = this.totpCode();
    return /^\d{6}$/.test(code);
  }

  // ✅ LEGACY: Keep for backward compatibility
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

          // ✅ Reset form before transitioning to setup stage
          this.resetMfaSetupForm();
          this.loginStage.set('mfa-setup');
        }
        // Check if MFA verification is required (subsequent login)
        else if ('userId' in response && !('qrCode' in response)) {
          this.userId.set(response.userId);

          // ✅ Reset form before transitioning to verify stage
          this.resetMfaVerifyForm();
          this.loginStage.set('mfa-verify');
        }
        // Direct login (no MFA) - MFA disabled for development
        else {
          // Save auth state with token and user data
          const loginData = response as any;

          if (loginData.token && loginData.adminUser) {
            // Create user object matching User interface
            const user = {
              id: loginData.adminUser.id,
              email: loginData.adminUser.email,
              firstName: loginData.adminUser.userName?.split(' ')[0] || 'Admin',
              lastName: loginData.adminUser.userName?.split(' ').slice(1).join(' ') || 'User',
              role: 'SuperAdmin' as any,
              avatarUrl: undefined
            };

            // Create LoginResponse object
            const loginResponse = {
              token: loginData.token,
              refreshToken: loginData.refreshToken || '',
              user: user,
              expiresAt: loginData.expiresAt || new Date(Date.now() + 60 * 60 * 1000).toISOString(),
              expiresIn: 3600
            };

            // Set auth state using the proper method
            this.authService.setAuthStatePublic(loginResponse);

            console.log('✅ Direct login successful - navigating to dashboard');
            this.router.navigate(['/admin/dashboard']);
          } else {
            this.errorMessage.set('Invalid login response. Please try again.');
          }
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

  // ✅ PRODUCTION-GRADE: MFA Setup with reactive forms
  async onMfaSetupSubmit(): Promise<void> {
    console.log('🔵 [MFA SETUP] onMfaSetupSubmit() called');

    // ✅ Get code from reactive form control
    const code = this.mfaSetupForm.get('totpCode')?.value || '';

    // ✅ Explicit validation before submitting
    if (code.length !== 6 || !/^\d{6}$/.test(code)) {
      this.errorMessage.set('Please enter a valid 6-digit TOTP code.');
      console.log('❌ [MFA SETUP] Invalid TOTP code:', code);
      return;
    }

    if (!this.backupCodesDownloaded()) {
      this.errorMessage.set('Please download or copy your backup codes first.');
      console.log('❌ [MFA SETUP] Backup codes not downloaded');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const setupRequest = {
      userId: this.userId(),
      totpCode: code,
      secret: this.mfaSecret(),
      backupCodes: this.backupCodes()
    };

    console.log('📤 [MFA SETUP] Sending request to completeMfaSetup()');
    console.log('📋 [MFA SETUP] Request data:', {
      userId: setupRequest.userId,
      totpCode: setupRequest.totpCode,
      secret: setupRequest.secret,
      backupCodesCount: setupRequest.backupCodes.length
    });

    this.authService.completeMfaSetup(setupRequest).subscribe({
      next: () => {
        console.log('✅ [MFA SETUP] Setup completed successfully');
        this.isLoading.set(false);
        // Auth service handles navigation to /admin/dashboard
      },
      error: (error) => {
        console.error('❌ [MFA SETUP] Setup failed:', error);
        this.isLoading.set(false);

        // ✅ CRITICAL FIX: Show user-friendly errors, NOT Angular technical errors!
        if (error.status === 400 || error.status === 401) {
          this.errorMessage.set('Invalid code. Please check your authenticator app and try again.');
        } else if (error.status === 0) {
          this.errorMessage.set('Network error. Please check your connection and try again.');
        } else if (error.status >= 500) {
          this.errorMessage.set('Server error. Please try again later.');
        } else {
          this.errorMessage.set('Setup failed. Please try again or contact support.');
        }

        // ✅ Mark form as touched to show validation errors
        this.mfaSetupForm.markAllAsTouched();
      }
    });
  }

  // ✅ PRODUCTION-GRADE: MFA Verify with reactive forms
  async onMfaVerifySubmit(): Promise<void> {
    // ✅ Get code from reactive form control
    const code = this.mfaVerifyForm.get('totpCode')?.value || '';

    // ✅ Explicit validation before submitting
    if (code.length !== 6 || !/^\d{6}$/.test(code)) {
      this.errorMessage.set('Please enter a valid 6-digit code.');
      console.log('❌ [MFA VERIFY] Invalid TOTP code:', code);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const verifyRequest = {
      userId: this.userId(),
      code: code
    };

    this.authService.verifyMfa(verifyRequest).subscribe({
      next: () => {
        this.isLoading.set(false);
        // Auth service handles navigation to /admin/dashboard
      },
      error: (error) => {
        console.error('MFA verification error:', error);
        this.isLoading.set(false);

        // ✅ CRITICAL FIX: Show user-friendly errors, NOT Angular technical errors!
        if (error.status === 400 || error.status === 401) {
          this.errorMessage.set('Invalid verification code. Please check your authenticator app and try again, or use a backup code.');
        } else if (error.status === 0) {
          this.errorMessage.set('Network error. Please check your connection and try again.');
        } else if (error.status >= 500) {
          this.errorMessage.set('Server error. Please try again later.');
        } else {
          this.errorMessage.set('Verification failed. Please try again or contact support.');
        }

        // ✅ Mark form as touched to show validation errors
        this.mfaVerifyForm.markAllAsTouched();
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

  // ✅ PRODUCTION-GRADE: Auto-format TOTP input for reactive forms
  onTotpInput(event: Event, formType: 'setup' | 'verify'): void {
    const input = (event.target as HTMLInputElement).value.replace(/\s/g, '');

    // Only allow 0-6 digits
    if (/^\d{0,6}$/.test(input)) {
      if (formType === 'setup') {
        this.mfaSetupForm.patchValue({ totpCode: input }, { emitEvent: true });
      } else {
        this.mfaVerifyForm.patchValue({ totpCode: input }, { emitEvent: true });
      }
    }
  }

  // Navigate back to credentials stage
  backToCredentials(): void {
    this.loginStage.set('credentials');
    this.errorMessage.set('');

    // ✅ Reset both MFA forms
    this.resetMfaSetupForm();
    this.resetMfaVerifyForm();
  }
}
