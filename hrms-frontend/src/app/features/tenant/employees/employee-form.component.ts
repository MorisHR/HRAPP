import { Component, signal, inject, OnInit } from '@angular/core';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { EmployeeService } from '../../../core/services/employee.service';
import { CreateEmployeeRequest } from '../../../core/models/employee.model';
import { UiModule } from '../../../shared/ui/ui.module';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterModule,
    UiModule
  ],
  template: `
    <div class="employee-form-container">
      <app-card>
        <div class="card-header">
          <app-button
            variant="ghost"
            size="small"
            routerLink="/tenant/employees"
            class="back-button">
            <app-icon name="arrow_back"></app-icon>
          </app-button>
          <h1>{{ isEditMode() ? 'Edit Employee' : 'Add New Employee' }}</h1>
        </div>

        <div class="card-content">
          <form [formGroup]="employeeForm" (ngSubmit)="onSubmit()">
            <div class="form-grid">
              <!-- Basic Information -->
              <h3 class="section-title">Basic Information</h3>

              <app-input
                class="full-width"
                label="Full Name"
                placeholder="Enter full name"
                formControlName="fullName"
                [required]="true"
                [error]="getFieldError('fullName')">
              </app-input>

              <app-input
                class="full-width"
                label="Email"
                type="email"
                placeholder="employee@company.com"
                formControlName="email"
                [required]="true"
                [error]="getFieldError('email')">
              </app-input>

              <app-input
                label="Employee Code"
                placeholder="EMP001"
                formControlName="employeeCode"
                [required]="true"
                [error]="getFieldError('employeeCode')">
              </app-input>

              <app-select
                label="Employee Type"
                placeholder="Select employee type"
                formControlName="employeeType"
                [options]="employeeTypeOptions"
                [required]="true">
              </app-select>

              <app-input
                label="Phone Number"
                type="tel"
                placeholder="+1234567890"
                formControlName="phoneNumber">
              </app-input>

              <app-datepicker
                label="Join Date"
                formControlName="joinDate">
              </app-datepicker>

              <!-- Department & Position -->
              <h3 class="section-title full-width">Department & Position</h3>

              <app-input
                label="Department"
                placeholder="e.g., Engineering"
                formControlName="department">
              </app-input>

              <app-input
                label="Designation"
                placeholder="e.g., Software Engineer"
                formControlName="designation">
              </app-input>

              @if (error()) {
                <div class="error-message full-width">
                  <app-icon name="error"></app-icon>
                  <span>{{ error() }}</span>
                </div>
              }

              <div class="form-actions full-width">
                <app-button
                  type="button"
                  variant="ghost"
                  (click)="navigateToList()">
                  Cancel
                </app-button>
                <app-button
                  type="submit"
                  variant="primary"
                  [disabled]="loading() || !employeeForm.valid"
                  [loading]="loading()">
                  {{ isEditMode() ? 'Update Employee' : 'Create Employee' }}
                </app-button>
              </div>
            </div>
          </form>
        </div>
      </app-card>
    </div>
    `,
  styles: [`
    .employee-form-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .card-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 24px;
      padding-bottom: 16px;
      border-bottom: 1px solid #e0e0e0;
    }

    .card-header h1 {
      margin: 0;
      font-size: 24px;
      font-weight: 500;
    }

    .back-button {
      margin-right: 8px;
    }

    .card-content {
      padding-top: 24px;
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

    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      margin-top: 24px;
      padding-top: 24px;
      border-top: 1px solid #e0e0e0;
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

  // Options for select components
  employeeTypeOptions = [
    { value: 'Local', label: 'Local' },
    { value: 'Expatriate', label: 'Expatriate' }
  ];

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

  getFieldError(fieldName: string): string | null {
    const control = this.employeeForm.get(fieldName);
    if (control && control.invalid && (control.dirty || control.touched)) {
      if (control.hasError('required')) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (control.hasError('email')) {
        return 'Please enter a valid email';
      }
      if (control.hasError('minlength')) {
        const minLength = control.getError('minlength').requiredLength;
        return `Must be at least ${minLength} characters`;
      }
    }
    return null;
  }

  private getFieldLabel(fieldName: string): string {
    const labels: Record<string, string> = {
      'fullName': 'Full name',
      'email': 'Email',
      'employeeCode': 'Employee code',
      'employeeType': 'Employee type',
      'phoneNumber': 'Phone number',
      'joinDate': 'Join date',
      'department': 'Department',
      'designation': 'Designation'
    };
    return labels[fieldName] || fieldName;
  }

  navigateToList(): void {
    this.router.navigate(['/tenant/employees']);
  }
}
