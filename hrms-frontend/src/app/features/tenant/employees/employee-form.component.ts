import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EmployeeService } from '../../../core/services/employee.service';
import { CreateEmployeeRequest } from '../../../core/models/employee.model';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="employee-form-container">
      <mat-card>
        <mat-card-header>
          <button mat-icon-button routerLink="/tenant/employees" class="back-button">
            <mat-icon>arrow_back</mat-icon>
          </button>
          <mat-card-title>
            <h1>{{ isEditMode() ? 'Edit Employee' : 'Add New Employee' }}</h1>
          </mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="employeeForm" (ngSubmit)="onSubmit()">
            <div class="form-grid">
              <!-- Basic Information -->
              <h3 class="section-title">Basic Information</h3>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Full Name</mat-label>
                <input matInput formControlName="fullName" placeholder="Enter full name">
                <mat-error *ngIf="employeeForm.get('fullName')?.hasError('required')">
                  Full name is required
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Email</mat-label>
                <input matInput type="email" formControlName="email" placeholder="employee@company.com">
                <mat-error *ngIf="employeeForm.get('email')?.hasError('required')">
                  Email is required
                </mat-error>
                <mat-error *ngIf="employeeForm.get('email')?.hasError('email')">
                  Please enter a valid email
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Employee Code</mat-label>
                <input matInput formControlName="employeeCode" placeholder="EMP001">
                <mat-error *ngIf="employeeForm.get('employeeCode')?.hasError('required')">
                  Employee code is required
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Employee Type</mat-label>
                <mat-select formControlName="employeeType">
                  <mat-option value="Local">Local</mat-option>
                  <mat-option value="Expatriate">Expatriate</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Phone Number</mat-label>
                <input matInput formControlName="phoneNumber" placeholder="+1234567890">
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Join Date</mat-label>
                <input matInput [matDatepicker]="joinPicker" formControlName="joinDate">
                <mat-datepicker-toggle matIconSuffix [for]="joinPicker"></mat-datepicker-toggle>
                <mat-datepicker #joinPicker></mat-datepicker>
              </mat-form-field>

              <!-- Department & Position -->
              <h3 class="section-title full-width">Department & Position</h3>

              <mat-form-field appearance="outline">
                <mat-label>Department</mat-label>
                <input matInput formControlName="department" placeholder="e.g., Engineering">
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Designation</mat-label>
                <input matInput formControlName="designation" placeholder="e.g., Software Engineer">
              </mat-form-field>

              @if (error()) {
                <div class="error-message full-width">
                  <mat-icon>error</mat-icon>
                  <span>{{ error() }}</span>
                </div>
              }

              <div class="form-actions full-width">
                <button mat-button type="button" routerLink="/tenant/employees">
                  Cancel
                </button>
                <button mat-raised-button color="primary" type="submit" [disabled]="loading() || !employeeForm.valid">
                  @if (loading()) {
                    <mat-spinner diameter="20"></mat-spinner>
                  }
                  {{ isEditMode() ? 'Update Employee' : 'Create Employee' }}
                </button>
              </div>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .employee-form-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }

    mat-card-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 24px;
    }

    .back-button {
      margin-right: 8px;
    }

    .form-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 16px;
    }

    .full-width {
      grid-column: 1 / -1;
    }

    .section-title {
      grid-column: 1 / -1;
      margin: 24px 0 8px 0;
      color: #666;
      font-size: 14px;
      font-weight: 500;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .section-title:first-child {
      margin-top: 0;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      background-color: #ffebee;
      color: #c62828;
      border-radius: 4px;

      mat-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
      }
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      margin-top: 24px;
      padding-top: 24px;
      border-top: 1px solid #e0e0e0;
    }

    mat-spinner {
      display: inline-block;
      margin-right: 8px;
    }
  `]
})
export class EmployeeFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private employeeService = inject(EmployeeService);

  employeeForm: FormGroup;
  isEditMode = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);

  constructor() {
    console.log('📝 Employee Form Component initialized');

    this.employeeForm = this.fb.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      employeeCode: ['', Validators.required],
      employeeType: ['Local', Validators.required],
      phoneNumber: [''],
      joinDate: [new Date()],
      department: [''],
      designation: ['']
    });
  }

  ngOnInit(): void {
    const employeeId = this.route.snapshot.paramMap.get('id');
    if (employeeId) {
      this.isEditMode.set(true);
      this.loadEmployee(employeeId);
    }
  }

  loadEmployee(id: string): void {
    this.loading.set(true);
    this.employeeService.getEmployeeById(id).subscribe({
      next: (data: any) => {
        const employee = data.data || data;
        this.employeeForm.patchValue(employee);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading employee:', error);
        this.error.set('Failed to load employee data');
        this.loading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.employeeForm.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const employeeData: CreateEmployeeRequest = this.employeeForm.value;

    this.employeeService.createEmployee(employeeData).subscribe({
      next: (response: any) => {
        console.log('✅ Employee created successfully:', response);
        this.router.navigate(['/tenant/employees']);
      },
      error: (error) => {
        console.error('❌ Error creating employee:', error);
        this.error.set(error.error?.message || 'Failed to create employee');
        this.loading.set(false);
      }
    });
  }
}
