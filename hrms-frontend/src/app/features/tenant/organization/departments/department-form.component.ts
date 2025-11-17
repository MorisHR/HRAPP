import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';

import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DepartmentService, DepartmentDropdownDto } from './services/department.service';
import { EmployeeService } from '../../../../core/services/employee.service';

interface Employee {
  id: string;
  fullName: string;
}

@Component({
  selector: 'app-department-form',
  standalone: true,
  imports: [
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatProgressSpinnerModule
],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="department-form-container">
      <div class="page-header">
        <button mat-icon-button routerLink="/tenant/settings/organization/departments">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <div>
          <h1>{{ isEditMode ? 'Edit Department' : 'Add Department' }}</h1>
          <p class="subtitle">{{ isEditMode ? 'Update department information' : 'Create a new department' }}</p>
        </div>
      </div>

      @if (loading()) {
        <mat-card>
          <mat-card-content class="loading-container">
            <mat-spinner diameter="50"></mat-spinner>
            <p>Loading...</p>
          </mat-card-content>
        </mat-card>
      }

      @if (!loading()) {
        <form [formGroup]="departmentForm" (ngSubmit)="onSubmit()">
          <!-- Basic Information -->
          <mat-card>
            <mat-card-header>
              <mat-card-title>
                <mat-icon>info</mat-icon>
                Basic Information
              </mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Department Code *</mat-label>
                  <input matInput
                         formControlName="code"
                         placeholder="e.g., HR, FIN, IT"
                         maxlength="20">
                  <mat-hint>Unique code (uppercase recommended)</mat-hint>
                  @if (departmentForm.get('code')?.hasError('required') && departmentForm.get('code')?.touched) {
                    <mat-error>Department code is required</mat-error>
                  }
                  @if (departmentForm.get('code')?.hasError('maxlength')) {
                    <mat-error>Code cannot exceed 20 characters</mat-error>
                  }
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Department Name *</mat-label>
                  <input matInput
                         formControlName="name"
                         placeholder="e.g., Human Resources"
                         maxlength="100">
                  @if (departmentForm.get('name')?.hasError('required') && departmentForm.get('name')?.touched) {
                    <mat-error>Department name is required</mat-error>
                  }
                  @if (departmentForm.get('name')?.hasError('maxlength')) {
                    <mat-error>Name cannot exceed 100 characters</mat-error>
                  }
                </mat-form-field>

                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Description</mat-label>
                  <textarea matInput
                            formControlName="description"
                            placeholder="Describe the department's role and responsibilities"
                            rows="3"
                            maxlength="500"></textarea>
                  <mat-hint>Optional (max 500 characters)</mat-hint>
                </mat-form-field>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Hierarchy & Management -->
          <mat-card>
            <mat-card-header>
              <mat-card-title>
                <mat-icon>account_tree</mat-icon>
                Hierarchy & Management
              </mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Parent Department</mat-label>
                  <mat-select formControlName="parentDepartmentId">
                    <mat-option [value]="null">None (Root Department)</mat-option>
                    @for (dept of availableDepartments(); track dept.id) {
                      <mat-option [value]="dept.id">
                        {{ dept.name }} ({{ dept.code }})
                      </mat-option>
                    }
                  </mat-select>
                  <mat-hint>Select parent if this is a sub-department</mat-hint>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Department Head</mat-label>
                  <mat-select formControlName="departmentHeadId">
                    <mat-option [value]="null">Not Assigned</mat-option>
                    @for (emp of employees(); track emp.id) {
                      <mat-option [value]="emp.id">
                        {{ emp.fullName }}
                      </mat-option>
                    }
                  </mat-select>
                  <mat-hint>Assign an employee as department head</mat-hint>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Cost Center Code</mat-label>
                  <input matInput
                         formControlName="costCenterCode"
                         placeholder="e.g., CC-001"
                         maxlength="50">
                  <mat-hint>Optional cost center identifier</mat-hint>
                  @if (departmentForm.get('costCenterCode')?.hasError('maxlength')) {
                    <mat-error>Cost center code cannot exceed 50 characters</mat-error>
                  }
                </mat-form-field>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Settings -->
          <mat-card>
            <mat-card-header>
              <mat-card-title>
                <mat-icon>settings</mat-icon>
                Settings
              </mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <mat-checkbox formControlName="isActive">
                Active Department
              </mat-checkbox>
              <p class="checkbox-hint">Inactive departments are hidden from dropdowns and reports</p>
            </mat-card-content>
          </mat-card>

          <!-- Actions -->
          <div class="form-actions">
            <button type="button"
                    mat-stroked-button
                    routerLink="/tenant/settings/organization/departments">
              Cancel
            </button>
            <button type="submit"
                    mat-raised-button
                    color="primary"
                    [disabled]="!departmentForm.valid || submitting()">
              @if (submitting()) {
                <mat-spinner diameter="20"></mat-spinner>
              }
              {{ isEditMode ? 'Update Department' : 'Create Department' }}
            </button>
          </div>
        </form>
      }
    </div>
  `,
  styles: [`
    .department-form-container {
      padding: 24px;
      max-width: 1000px;
      margin: 0 auto;
    }

    .page-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 24px;

      h1 {
        margin: 0;
        font-size: 28px;
        font-weight: 600;
        color: #000000;
      }

      .subtitle {
        margin: 4px 0 0 0;
        color: #666;
        font-size: 14px;
      }
    }

    mat-card {
      margin-bottom: 24px;

      mat-card-header {
        margin-bottom: 16px;

        mat-card-title {
          display: flex;
          align-items: center;
          gap: 8px;
          font-size: 18px;
          font-weight: 600;

          mat-icon {
            color: #1976d2;
          }
        }
      }
    }

    .form-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 16px;
      margin-top: 16px;

      .full-width {
        grid-column: 1 / -1;
      }
    }

    .checkbox-hint {
      margin: 8px 0 0 32px;
      font-size: 12px;
      color: #666;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 16px;
      margin-top: 24px;

      button {
        min-width: 120px;
      }
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;

      p {
        margin-top: 16px;
        color: #666;
      }
    }
  `]
})
export class DepartmentFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private departmentService = inject(DepartmentService);
  private employeeService = inject(EmployeeService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  departmentForm!: FormGroup;
  isEditMode = false;
  departmentId: string | null = null;
  loading = signal(false);
  submitting = signal(false);
  availableDepartments = signal<DepartmentDropdownDto[]>([]);
  employees = signal<Employee[]>([]);

  ngOnInit(): void {
    this.initializeForm();
    this.checkEditMode();
    this.loadDepartments();
    this.loadEmployees();
  }

  initializeForm(): void {
    this.departmentForm = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(20)]],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      parentDepartmentId: [null],
      departmentHeadId: [null],
      costCenterCode: ['', Validators.maxLength(50)],
      isActive: [true]
    });
  }

  checkEditMode(): void {
    this.departmentId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.departmentId;

    if (this.isEditMode && this.departmentId) {
      this.loadDepartmentData(this.departmentId);
    }
  }

  loadDepartmentData(id: string): void {
    this.loading.set(true);
    this.departmentService.getById(id).subscribe({
      next: (dept) => {
        this.departmentForm.patchValue({
          code: dept.code,
          name: dept.name,
          description: dept.description,
          parentDepartmentId: dept.parentDepartmentId,
          departmentHeadId: dept.departmentHeadId,
          costCenterCode: dept.costCenterCode,
          isActive: dept.isActive
        });
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading department:', err);
        alert('Failed to load department data');
        this.router.navigate(['/tenant/settings/organization/departments']);
      }
    });
  }

  loadDepartments(): void {
    this.departmentService.getDropdown().subscribe({
      next: (depts) => {
        // Filter out current department if in edit mode
        const filtered = this.isEditMode && this.departmentId
          ? depts.filter(d => d.id !== this.departmentId)
          : depts;
        this.availableDepartments.set(filtered);
      },
      error: (err) => {
        console.error('Error loading departments:', err);
      }
    });
  }

  loadEmployees(): void {
    // Load employees for department head dropdown
    this.employeeService.getEmployees().subscribe({
      next: (response: any) => {
        // Handle the API response format: { success: true, data: [...] }
        const employeeData = response.data || response;
        const employees = employeeData.map((emp: any) => ({
          id: emp.id,
          fullName: emp.fullName || `${emp.firstName || ''} ${emp.lastName || ''}`.trim()
        }));
        this.employees.set(employees);
      },
      error: (err) => {
        console.error('Error loading employees:', err);
        // Keep empty array on error
        this.employees.set([]);
      }
    });
  }

  onSubmit(): void {
    if (!this.departmentForm.valid) {
      Object.keys(this.departmentForm.controls).forEach(key => {
        this.departmentForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.submitting.set(true);
    const formValue = this.departmentForm.value;

    // Convert empty strings to null for optional fields
    const payload = {
      ...formValue,
      description: formValue.description || null,
      parentDepartmentId: formValue.parentDepartmentId || null,
      departmentHeadId: formValue.departmentHeadId || null,
      costCenterCode: formValue.costCenterCode || null
    };

    const request = this.isEditMode && this.departmentId
      ? this.departmentService.update(this.departmentId, payload)
      : this.departmentService.create(payload);

    request.subscribe({
      next: () => {
        const message = this.isEditMode
          ? 'Department updated successfully'
          : 'Department created successfully';
        alert(message);
        this.router.navigate(['/tenant/settings/organization/departments']);
      },
      error: (err) => {
        console.error('Error saving department:', err);
        alert(err.error?.error || 'Failed to save department');
        this.submitting.set(false);
      }
    });
  }
}
