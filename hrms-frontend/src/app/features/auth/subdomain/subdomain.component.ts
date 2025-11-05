import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface TenantCheckResponse {
  success: boolean;
  data?: {
    exists: boolean;
    companyName?: string;
    subdomain?: string;
    logoUrl?: string | null;
    isActive?: boolean;
  };
  message?: string;
}

@Component({
  selector: 'app-subdomain',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subdomain.component.html',
  styleUrls: ['./subdomain.component.scss']
})
export class SubdomainComponent {
  subdomain = signal('');
  isLoading = signal(false);
  errorMessage = signal('');
  showModal = signal(false);

  private readonly subdomainPattern = /^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$/;

  constructor(
    private router: Router,
    private http: HttpClient
  ) {}

  onSubdomainInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value.toLowerCase().trim();

    // Remove any non-allowed characters
    value = value.replace(/[^a-z0-9-]/g, '');

    // Ensure it doesn't start or end with hyphen
    if (value.startsWith('-')) {
      value = value.substring(1);
    }
    if (value.endsWith('-')) {
      value = value.substring(0, value.length - 1);
    }

    this.subdomain.set(value);
    this.errorMessage.set('');
  }

  isValidSubdomain(): boolean {
    const value = this.subdomain();
    if (!value || value.length < 2 || value.length > 63) {
      return false;
    }
    return this.subdomainPattern.test(value);
  }

  async onContinue(): Promise<void> {
    if (!this.isValidSubdomain()) {
      this.errorMessage.set('Please enter a valid subdomain (2-63 characters, letters, numbers, and hyphens only)');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    try {
      const response = await this.http.get<TenantCheckResponse>(
        `${environment.apiUrl}/tenants/check/${this.subdomain()}`
      ).toPromise();

      if (!response) {
        this.errorMessage.set('Unable to verify domain. Please try again.');
        return;
      }

      if (response.success === false) {
        this.errorMessage.set(response.message || 'This company account is not active. Please contact support.');
        return;
      }

      if (!response.data?.exists) {
        this.errorMessage.set('Company not found. Please check your domain and try again.');
        return;
      }

      if (response.data.isActive === false) {
        this.errorMessage.set('This company account is not active. Please contact support.');
        return;
      }

      // Store subdomain in localStorage
      localStorage.setItem('hrms_subdomain', this.subdomain());
      if (response.data.companyName) {
        localStorage.setItem('hrms_company_name', response.data.companyName);
      }

      // Navigate to login page
      await this.router.navigate(['/auth/login']);
    } catch (error: any) {
      console.error('Subdomain check error:', error);
      if (error.status === 404) {
        this.errorMessage.set('Company not found. Please check your domain and try again.');
      } else if (error.status >= 500) {
        this.errorMessage.set('Server error. Please try again later.');
      } else {
        this.errorMessage.set('Unable to verify domain. Please try again.');
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  openModal(): void {
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
  }
}
