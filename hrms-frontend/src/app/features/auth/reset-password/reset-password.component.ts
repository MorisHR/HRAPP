import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ButtonComponent } from '../../../shared/ui/components/button/button';


@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ButtonComponent
  ],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss']
})
export class ResetPasswordComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  form: FormGroup;
  loading = signal(false);
  submitted = signal(false);
  successMessage = signal('');
  errorMessage = signal('');
  invalidToken = signal(false);
  token = signal('');
  returnUrl = signal('/auth/subdomain'); // ✅ FORTUNE 500 PATTERN: Defensive default
  hidePassword = signal(true);
  hideConfirmPassword = signal(true);

  constructor() {
    this.form = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(12)]],
      confirmPassword: ['', Validators.required]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  ngOnInit(): void {
    // ✅ FORTUNE 500 PATTERN: Context Preservation via Query Parameters
    const tokenParam = this.route.snapshot.queryParamMap.get('token') || '';
    const returnUrlParam = this.route.snapshot.queryParamMap.get('returnUrl');

    this.token.set(tokenParam);
    if (returnUrlParam) {
      this.returnUrl.set(returnUrlParam);
      console.log(`✅ Reset password: returnUrl set to ${returnUrlParam}`);
    } else {
      console.log('⚠️ Reset password: No returnUrl provided, using default /auth/subdomain');
    }

    if (!tokenParam) {
      this.invalidToken.set(true);
      this.errorMessage.set('Invalid or missing reset token.');
    }
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword');
    const confirmPassword = control.get('confirmPassword');
    return newPassword && confirmPassword && newPassword.value !== confirmPassword.value
      ? { passwordMismatch: true }
      : null;
  }

  get f() { return this.form.controls; }

  get isValidForm(): boolean {
    return this.form.valid;
  }

  get passwordStrength(): string {
    const password = this.f['newPassword'].value;
    if (!password) return '';

    let strength = 0;
    if (password.length >= 12) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;

    if (strength <= 2) return 'weak';
    if (strength <= 3) return 'medium';
    return 'strong';
  }

  get passwordRequirements() {
    const password = this.f['newPassword'].value || '';
    return {
      minLength: password.length >= 12,
      hasLowercase: /[a-z]/.test(password),
      hasUppercase: /[A-Z]/.test(password),
      hasNumber: /[0-9]/.test(password),
      hasSpecial: /[^a-zA-Z0-9]/.test(password)
    };
  }

  togglePasswordVisibility(): void {
    this.hidePassword.set(!this.hidePassword());
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.set(!this.hideConfirmPassword());
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    if (this.form.invalid) return;

    this.loading.set(true);
    this.authService.resetPassword({
      token: this.token(),
      newPassword: this.form.value.newPassword,
      confirmPassword: this.form.value.confirmPassword
    }).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.successMessage.set('Password reset successfully! Redirecting to login...');
        setTimeout(() => this.router.navigate([this.returnUrl()]), 2000);
      },
      error: (error) => {
        this.loading.set(false);
        this.errorMessage.set(error.error?.message ||
          'Failed to reset password. Please try again.');
      }
    });
  }
}
