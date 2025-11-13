import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss']
})
export class ResetPasswordComponent implements OnInit {
  token = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');
  hideNewPassword = signal(true);
  hideConfirmPassword = signal(true);

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Get token from query parameters
    this.route.queryParams.subscribe(params => {
      const token = params['token'];
      if (token) {
        this.token.set(token);
      } else {
        this.errorMessage.set('Invalid or missing reset token. Please request a new password reset.');
      }
    });
  }

  toggleNewPasswordVisibility(): void {
    this.hideNewPassword.update(value => !value);
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.update(value => !value);
  }

  isValidPassword(password: string): boolean {
    // Password must be at least 8 characters and contain at least one uppercase, lowercase, number, and special character
    const minLength = password.length >= 8;
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumber = /[0-9]/.test(password);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);
    return minLength && hasUpperCase && hasLowerCase && hasNumber && hasSpecialChar;
  }

  passwordsMatch(): boolean {
    return this.newPassword() === this.confirmPassword() && this.newPassword().length > 0;
  }

  isValidForm(): boolean {
    const newPasswordValue = this.newPassword();
    const confirmPasswordValue = this.confirmPassword();
    return this.token().trim() !== '' &&
           newPasswordValue.trim() !== '' &&
           confirmPasswordValue.trim() !== '' &&
           this.isValidPassword(newPasswordValue) &&
           this.passwordsMatch();
  }

  onBackToLogin(): void {
    this.router.navigate(['/auth/subdomain']);
  }

  async onSubmit(): Promise<void> {
    if (!this.isValidForm()) {
      if (!this.isValidPassword(this.newPassword())) {
        this.errorMessage.set('Password must be at least 8 characters and contain uppercase, lowercase, number, and special character.');
      } else if (!this.passwordsMatch()) {
        this.errorMessage.set('Passwords do not match.');
      } else {
        this.errorMessage.set('Please fill in all fields correctly.');
      }
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const request = {
      token: this.token(),
      newPassword: this.newPassword()
    };

    this.authService.resetPassword(request).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.successMessage.set('Your password has been reset successfully. Redirecting to login...');
        
        // Redirect to login after 3 seconds
        setTimeout(() => {
          this.router.navigate(['/auth/subdomain']);
        }, 3000);
      },
      error: (error) => {
        console.error('Reset password error:', error);
        this.isLoading.set(false);

        if (error.status === 400) {
          this.errorMessage.set('Invalid or expired reset token. Please request a new password reset.');
        } else if (error.status === 404) {
          this.errorMessage.set('User not found. Please request a new password reset.');
        } else if (error.status >= 500) {
          this.errorMessage.set('Server error. Please try again later.');
        } else {
          this.errorMessage.set(error.error?.message || 'Failed to reset password. Please try again.');
        }
      }
    });
  }
}
