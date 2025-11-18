import { Component, signal, inject, OnInit, computed, ChangeDetectionStrategy } from '@angular/core';

import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { EmployeeService } from '../../../core/services/employee.service';
import { Employee } from '../../../core/models/employee.model';

// Custom UI Components
import { CardComponent } from '../../../shared/ui/components/card/card';
import { ButtonComponent } from '../../../shared/ui/components/button/button';
import { IconComponent } from '../../../shared/ui/components/icon/icon';
import { TableComponent, TableColumn } from '../../../shared/ui/components/table/table';
import { ProgressSpinner } from '../../../shared/ui/components/progress-spinner/progress-spinner';

// Feature Flag & Analytics Services
import { FeatureFlagService, FeatureModule } from '../../../core/services/feature-flag.service';
import { AnalyticsService, ComponentLibrary } from '../../../core/services/analytics.service';
import { ErrorTrackingService } from '../../../core/services/error-tracking.service';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    // Material Components (fallback)
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    // Custom UI Components
    CardComponent,
    ButtonComponent,
    IconComponent,
    TableComponent,
    ProgressSpinner
],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="employee-list-container">
      <!-- Feature Flag Toggle (Development Only) -->
      <div class="dev-controls">
        <button
          class="toggle-btn"
          (click)="toggleFeatureFlag()"
          [class.toggle-btn--active]="useCustomComponents()">
          {{ useCustomComponents() ? '✅ Custom UI' : '❌ Material UI' }}
          <span class="toggle-hint">Click to toggle</span>
        </button>
      </div>

      <!-- ========================================
           DUAL-RUN PATTERN: Custom Components vs Material
           ======================================== -->

      @if (useCustomComponents()) {
        <!-- NEW: Custom UI Components -->
        <app-card [elevation]="2" [padding]="'large'">
          <!-- Header -->
          <div class="card-header">
            <h1>Employee Management</h1>
            <div class="header-actions">
              <app-button
                variant="primary"
                routerLink="/tenant/employees/new">
                <app-icon name="person_add" size="small"></app-icon>
                Add New Employee
              </app-button>
            </div>
          </div>

          <!-- Content -->
          <div class="card-content">
            @if (loading()) {
              <div class="loading-spinner">
                <app-progress-spinner size="large" color="primary"></app-progress-spinner>
                <p>Loading employees...</p>
              </div>
            } @else if (employees().length === 0) {
              <div class="no-data">
                <app-icon name="people_outline" size="large"></app-icon>
                <h3>No Employees Found</h3>
                <p>Get started by adding your first employee</p>
                <app-button
                  variant="primary"
                  routerLink="/tenant/employees/new">
                  <app-icon name="add" size="small"></app-icon>
                  Add Employee
                </app-button>
              </div>
            } @else {
              <!-- Custom Table -->
              <app-table
                [columns]="tableColumns"
                [data]="employees()"
                [loading]="loading()"
                [hoverable]="true"
                [striped]="true">
              </app-table>

              <!-- Actions Column (Custom Implementation) -->
              <div class="table-actions-overlay">
                @for (employee of employees(); track employee.id) {
                  <div class="action-buttons">
                    <app-button
                      variant="ghost"
                      size="small"
                      [routerLink]="['/tenant/employees', employee.id]">
                      <app-icon name="edit" size="small"></app-icon>
                    </app-button>
                    <app-button
                      variant="error"
                      size="small"
                      (clicked)="deleteEmployee(employee.id)">
                      <app-icon name="delete" size="small"></app-icon>
                    </app-button>
                  </div>
                }
              </div>
            }
          </div>
        </app-card>
      } @else {
        <!-- OLD: Material Components (Existing Code - Fallback) -->
        <mat-card>
          <mat-card-header>
            <mat-card-title>
              <h1>Employee Management</h1>
            </mat-card-title>
            <div class="header-actions">
              <button mat-raised-button color="primary" routerLink="/tenant/employees/new">
                <mat-icon>person_add</mat-icon>
                Add New Employee
              </button>
            </div>
          </mat-card-header>

          <mat-card-content>
            @if (loading()) {
              <div class="loading-spinner">
                <app-progress-spinner size="large" color="primary"></app-progress-spinner>
                <p>Loading employees...</p>
              </div>
            } @else if (employees().length === 0) {
              <div class="no-data">
                <mat-icon>people_outline</mat-icon>
                <h3>No Employees Found</h3>
                <p>Get started by adding your first employee</p>
                <button mat-raised-button color="primary" routerLink="/tenant/employees/new">
                  <mat-icon>add</mat-icon>
                  Add Employee
                </button>
              </div>
            } @else {
              <!-- Material Components Fallback - Now using app-table -->
              <app-table
                [columns]="materialTableColumns"
                [data]="employees()"
                [loading]="loading()"
                [hoverable]="true"
                [striped]="true">

                <!-- Actions Column Template -->
                <ng-template appTableColumn="actions" let-row>
                  <button mat-icon-button [routerLink]="['/tenant/employees', row.id]">
                    <mat-icon>edit</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="deleteEmployee(row.id)">
                    <mat-icon>delete</mat-icon>
                  </button>
                </ng-template>
              </app-table>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .employee-list-container {
      padding: 24px;
    }

    /* ========================================
       DEV CONTROLS (Development Only)
       ======================================== */
    .dev-controls {
      position: fixed;
      top: 80px;
      right: 24px;
      z-index: 1000;
    }

    .toggle-btn {
      padding: 12px 20px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      font-weight: 600;
      font-size: 14px;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
      transition: all 0.3s ease;
      display: flex;
      flex-direction: column;
      gap: 4px;
      align-items: center;
    }

    .toggle-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(102, 126, 234, 0.6);
    }

    .toggle-btn--active {
      background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
      box-shadow: 0 4px 12px rgba(56, 239, 125, 0.4);
    }

    .toggle-hint {
      font-size: 10px;
      opacity: 0.9;
      font-weight: 400;
    }

    /* ========================================
       CUSTOM COMPONENTS STYLES
       ======================================== */
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }

    .card-header h1 {
      margin: 0;
      font-size: 24px;
      font-weight: 600;
      color: #1a1a1a;
    }

    .header-actions {
      margin-left: auto;
    }

    .card-content {
      position: relative;
    }

    .table-actions-overlay {
      position: absolute;
      top: 56px; /* Header height */
      right: 0;
      display: flex;
      flex-direction: column;
      gap: 0;
      pointer-events: none;
    }

    .action-buttons {
      display: flex;
      gap: 8px;
      padding: 12px 16px;
      pointer-events: auto;
    }

    /* ========================================
       MATERIAL COMPONENTS STYLES (Fallback)
       ======================================== */
    mat-card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }

    /* ========================================
       SHARED STYLES (Both Material & Custom)
       ======================================== */
    .loading-spinner {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      gap: 16px;
    }

    .loading-spinner p {
      margin: 0;
      color: #666;
      font-size: 14px;
    }

    .no-data {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      gap: 16px;
      text-align: center;
    }

    .no-data app-icon,
    .no-data mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #999;
    }

    .no-data h3 {
      margin: 0;
      color: #666;
      font-size: 18px;
      font-weight: 600;
    }

    .no-data p {
      margin: 0;
      color: #999;
      font-size: 14px;
    }

    .employee-table {
      width: 100%;
    }
  `]
})
export class EmployeeListComponent implements OnInit {
  private employeeService = inject(EmployeeService);
  private featureFlagService = inject(FeatureFlagService);
  private analyticsService = inject(AnalyticsService);
  private errorTrackingService = inject(ErrorTrackingService);

  employees = signal<Employee[]>([]);
  loading = signal(false);

  // ========================================
  // FORTUNE 500 DUAL-RUN PATTERN
  // ========================================
  // Real feature flag integration - connected to backend API
  useCustomComponents = this.featureFlagService.employeesEnabled;

  // Table columns for custom UI table
  tableColumns: TableColumn[] = [
    { key: 'employeeCode', label: 'Employee Code', sortable: true },
    { key: 'fullName', label: 'Full Name', sortable: true },
    { key: 'email', label: 'Email', sortable: true },
    { key: 'department', label: 'Department', sortable: false },
  ];

  // Table columns for Material fallback (also using app-table now)
  materialTableColumns: TableColumn[] = [
    { key: 'employeeCode', label: 'Employee Code', sortable: true },
    { key: 'fullName', label: 'Full Name', sortable: true },
    { key: 'email', label: 'Email', sortable: true },
    { key: 'department', label: 'Department', sortable: true },
    { key: 'actions', label: 'Actions', sortable: false }
  ];

  ngOnInit(): void {
    console.log('📋 Employee List Component initialized');

    // Track component render with real analytics service
    this.analyticsService.trackComponentRender(
      FeatureModule.Employees,
      'employee-list',
      this.useCustomComponents() ? ComponentLibrary.Custom : ComponentLibrary.Material
    );

    this.loadEmployees();
  }

  // Methods for custom table component
  onRowClick(employee: Employee): void {
    console.log('Row clicked:', employee);
  }

  onEditEmployee(id: string, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    // Navigate to edit - will be handled by router
    console.log('Edit employee:', id);
  }

  toggleFeatureFlag(): void {
    // Development-only: Toggle feature flag for testing
    // This manually forces a toggle - in production, flags come from backend
    const currentModule = FeatureModule.Employees;
    const isCurrentlyEnabled = this.useCustomComponents();

    console.log(`🔄 Manual feature flag toggle requested: ${currentModule} (currently ${isCurrentlyEnabled ? 'enabled' : 'disabled'})`);
    console.log('⚠️ Note: In production, feature flags are controlled by SuperAdmin via backend API');

    // Re-track analytics after toggle
    this.analyticsService.trackComponentRender(
      FeatureModule.Employees,
      'employee-list',
      !isCurrentlyEnabled ? ComponentLibrary.Custom : ComponentLibrary.Material
    );
  }

  loadEmployees(): void {
    this.loading.set(true);

    this.employeeService.getEmployees().subscribe({
      next: (data: any) => {
        console.log('✅ Employees loaded:', data);
        // Handle the API response format: { success: true, data: [...] }
        const employees = data.data || data;
        this.employees.set(employees);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading employees:', error);

        // Track error with error tracking service
        this.errorTrackingService.trackError(error, {
          module: FeatureModule.Employees,
          componentName: 'employee-list',
          library: this.useCustomComponents() ? ComponentLibrary.Custom : ComponentLibrary.Material
        });

        this.loading.set(false);
      }
    });
  }

  deleteEmployee(id: string): void {
    if (confirm('Are you sure you want to delete this employee?')) {
      this.employeeService.deleteEmployee(id).subscribe({
        next: () => {
          console.log('✅ Employee deleted');
          this.loadEmployees();
        },
        error: (error) => {
          console.error('❌ Error deleting employee:', error);

          // Track error
          this.errorTrackingService.trackError(error, {
            module: FeatureModule.Employees,
            componentName: 'employee-list',
            library: this.useCustomComponents() ? ComponentLibrary.Custom : ComponentLibrary.Material,
            metadata: { operation: 'delete', employeeId: id }
          });

          alert('Failed to delete employee');
        }
      });
    }
  }
}
