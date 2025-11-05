import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-superadmin-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './superadmin-login.component.html',
  styleUrls: ['./superadmin-login.component.scss']
})
export class SuperAdminLoginComponent {
  email = signal('');
  password = signal('');
  isLoading = signal(false);
  errorMessage = signal('');
  hidePassword = signal(true);

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

  isValidForm(): boolean {
    const emailValue = this.email();
    const passwordValue = this.password();
    return emailValue.trim() !== '' &&
           this.isValidEmail(emailValue) &&
           passwordValue.trim() !== '' &&
           passwordValue.length >= 6;
  }

  async onSubmit(): Promise<void> {
    if (!this.isValidForm()) {
      this.errorMessage.set('Please enter a valid email and password (minimum 6 characters)');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const credentials = {
      email: this.email().trim(),
      password: this.password()
    };

    this.authService.login(credentials).subscribe({
      next: () => {
        // Login successful, navigation handled by auth service
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('SuperAdmin login error:', error);
        this.isLoading.set(false);

        if (error.status === 401) {
          this.errorMessage.set('Invalid credentials. SuperAdmin access denied.');
        } else if (error.status >= 500) {
          this.errorMessage.set('Server error. Please try again later.');
        } else {
          this.errorMessage.set(error.error?.message || 'Login failed. Please try again.');
        }
      }
    });
  }
}
