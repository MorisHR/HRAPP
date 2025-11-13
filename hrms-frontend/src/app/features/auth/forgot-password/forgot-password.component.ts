import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';


@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  form: FormGroup;
  loading = signal(false);
  submitted = signal(false);
  successMessage = signal('');
  errorMessage = signal('');
  returnUrl = signal('/auth/subdomain'); // ✅ FORTUNE 500 PATTERN: Defensive default

  constructor() {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  ngOnInit(): void {
    // ✅ FORTUNE 500 PATTERN: Context Preservation via Query Parameters
    // Read returnUrl from query params, fallback to subdomain selection if missing
    const returnUrlParam = this.route.snapshot.queryParamMap.get('returnUrl');
    if (returnUrlParam) {
      this.returnUrl.set(returnUrlParam);
      console.log(`✅ Forgot password: returnUrl set to ${returnUrlParam}`);
    } else {
      console.log('⚠️ Forgot password: No returnUrl provided, using default /auth/subdomain');
    }
  }

  get f() { return this.form.controls; }

  get isValidForm(): boolean {
    return this.form.valid;
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    if (this.form.invalid) return;

    this.loading.set(true);
    this.authService.forgotPassword(this.form.value.email).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.successMessage.set(response.message ||
          'If this email is registered, you will receive a password reset link.');
        this.form.reset();
        this.submitted.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.errorMessage.set(error.error?.message || 'An error occurred. Please try again.');
      }
    });
  }
}
