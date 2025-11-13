import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  email = signal('');
  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  isValidEmail(email: string): boolean {
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailPattern.test(email);
  }

  isValidForm(): boolean {
    const emailValue = this.email();
    return emailValue.trim() !== '' && this.isValidEmail(emailValue);
  }

  onBackToLogin(): void {
    this.router.navigate(['/auth/subdomain']);
  }

  async onSubmit(): Promise<void> {
    if (!this.isValidForm()) {
      this.errorMessage.set('Please enter a valid email address');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const request = {
      email: this.email().trim()
    };

    this.authService.forgotPassword(request).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.successMessage.set(
          'Password reset instructions have been sent to your email address. Please check your inbox.'
        );
        this.email.set('');
      },
      error: (error) => {
        console.error('Forgot password error:', error);
        this.isLoading.set(false);

        if (error.status === 404) {
          // For security reasons, we show success even if email doesn't exist
          // This prevents email enumeration attacks
          this.successMessage.set(
            'If an account exists with this email, you will receive password reset instructions.'
          );
          this.email.set('');
        } else if (error.status >= 500) {
          this.errorMessage.set('Server error. Please try again later.');
        } else {
          this.errorMessage.set(error.error?.message || 'Failed to send reset email. Please try again.');
        }
      }
    });
  }
}
