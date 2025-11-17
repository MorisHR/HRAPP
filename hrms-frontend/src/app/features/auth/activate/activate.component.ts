import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ButtonComponent } from '../../../shared/ui/components/button/button';

@Component({
  selector: 'app-activate',
  standalone: true,
  imports: [ButtonComponent],
  templateUrl: './activate.component.html',
  styleUrls: ['./activate.component.scss']
})
export class ActivateComponent implements OnInit {
  loading = true;
  success = false;
  error: string | null = null;
  message = '';
  loginUrl = '';
  subdomain = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token) {
      this.loading = false;
      this.error = 'Invalid activation link. No token provided.';
      return;
    }

    this.activateTenant(token);
  }

  activateTenant(token: string) {
    const apiUrl = environment.apiUrl || 'http://localhost:5090';

    this.http.post<any>(`${apiUrl}/api/tenants/activate`, { activationToken: token })
      .subscribe({
        next: (response) => {
          this.loading = false;
          this.success = true;
          this.message = response.message;
          this.loginUrl = response.loginUrl || '/login';
          this.subdomain = response.tenantSubdomain || '';
        },
        error: (err) => {
          this.loading = false;
          this.success = false;
          this.error = err.error?.message || 'Activation failed. Please try again or contact support.';
        }
      });
  }

  goToLogin() {
    this.router.navigate(['/auth/subdomain']);
  }

  goToResendActivation() {
    this.router.navigate(['/auth/resend-activation']);
  }

  contactSupport() {
    window.location.href = 'mailto:support@morishr.com';
  }
}
