import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  // Signals for component state
  hidePassword = signal(true);
  error = signal<string | null>(null);
  loading = this.authService.loading;

  loginForm: FormGroup;

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      subdomain: [''] // Optional: for tenant employee login
    });
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      this.error.set(null);

      // Prepare login credentials, excluding empty subdomain
      const credentials = {
        email: this.loginForm.value.email,
        password: this.loginForm.value.password,
        ...(this.loginForm.value.subdomain && { subdomain: this.loginForm.value.subdomain.trim() })
      };

      this.authService.login(credentials).subscribe({
        error: (err) => {
          this.error.set(err.error?.message || 'Login failed. Please try again.');
        }
      });
    }
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update(value => !value);
  }
}
