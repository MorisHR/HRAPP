import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-tenant-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tenant-login.component.html',
  styleUrls: ['./tenant-login.component.scss']
})
export class TenantLoginComponent implements OnInit {
  email = signal('');
  password = signal('');
  subdomain = signal('');
  companyName = signal('');
  isLoading = signal(false);
  errorMessage = signal('');
  hidePassword = signal(true);

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Check if subdomain exists in localStorage
    const storedSubdomain = localStorage.getItem('hrms_subdomain');
    const storedCompanyName = localStorage.getItem('hrms_company_name');

    if (!storedSubdomain) {
      // No subdomain, redirect to subdomain entry page
      this.router.navigate(['/auth/subdomain']);
      return;
    }

    this.subdomain.set(storedSubdomain);
    this.companyName.set(storedCompanyName || storedSubdomain);
  }

  onChangeCompany(): void {
    // Clear stored subdomain and redirect back to subdomain entry
    localStorage.removeItem('hrms_subdomain');
    localStorage.removeItem('hrms_company_name');
    this.router.navigate(['/auth/subdomain']);
  }

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
      password: this.password(),
      subdomain: this.subdomain()
    };

    this.authService.login(credentials).subscribe({
      next: () => {
        // Login successful, navigation handled by auth service
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Login error:', error);
        this.isLoading.set(false);

        if (error.status === 401) {
          this.errorMessage.set('Invalid email or password. Please try again.');
        } else if (error.status === 403) {
          this.errorMessage.set('Your account has been deactivated. Please contact your administrator.');
        } else if (error.status >= 500) {
          this.errorMessage.set('Server error. Please try again later.');
        } else {
          this.errorMessage.set(error.error?.message || 'Login failed. Please try again.');
        }
      }
    });
  }
}
