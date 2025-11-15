import { Component, signal, OnInit } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TenantContextService } from '../../../core/services/tenant-context.service';
import { SessionManagementService } from '../../../core/services/session-management.service';
import { UiModule } from '../../../shared/ui/ui.module';

@Component({
  selector: 'app-tenant-login',
  standalone: true,
  imports: [FormsModule, UiModule],
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
    private authService: AuthService,
    private tenantContext: TenantContextService,
    private sessionManagement: SessionManagementService
  ) {}

  ngOnInit(): void {
    // ✅ FORTUNE 500 PATTERN: Auto-redirect authenticated users with valid tokens
    // Prevents users from viewing login page while logged in
    // Matches Fortune 500 behavior (Google, Microsoft, Salesforce)
    if (this.authService.isAuthenticated() && !this.sessionManagement.isTokenExpired()) {
      console.log('✅ User already authenticated - redirecting to dashboard');
      this.router.navigate(['/tenant/dashboard'], { replaceUrl: true });
      return;
    }

    // ✅ FORTUNE 500 PATTERN: Silent clearing of expired tokens without navigation
    // If user has expired/invalid tokens, clear them silently since we're already on login page
    // This prevents unwanted redirects when accessing /auth/login directly
    if (this.authService.isAuthenticated() && this.sessionManagement.isTokenExpired()) {
      console.log('⚠️ Token expired - clearing silently (already on login page)');
      this.authService.clearAuthStateSilently();
    }

    // ✅ ENVIRONMENT-AWARE TENANT DETECTION
    // Automatically gets tenant from URL (production/localhost) or localStorage (Codespaces)
    const currentTenant = this.tenantContext.getCurrentTenant();

    if (!currentTenant) {
      // No tenant context, redirect to subdomain selection
      console.log('❌ No tenant context, redirecting to subdomain selection');
      this.tenantContext.navigateToSubdomainSelection('/auth/subdomain');
      return;
    }

    // Set subdomain from tenant context
    this.subdomain.set(currentTenant);
    this.companyName.set(currentTenant); // You can fetch company name from API if needed
    console.log(`✅ Tenant login page loaded for tenant: ${currentTenant}`);
    console.log(`📍 Routing mode: ${this.tenantContext.getRoutingMode()}`);
  }

  onChangeCompany(): void {
    // Navigate back to subdomain selection (environment-aware)
    console.log('🔄 Changing company, redirecting to subdomain selection');
    this.tenantContext.navigateToSubdomainSelection('/auth/subdomain');
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update(value => !value);
  }

  isValidEmail(email: string): boolean {
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailPattern.test(email);
  }

  asString(value: string | number): string {
    return String(value);
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
