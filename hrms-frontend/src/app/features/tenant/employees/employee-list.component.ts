import { Component, signal, inject, OnInit, computed, ChangeDetectionStrategy } from '@angular/core';

import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EmployeeService } from '../../../core/services/employee.service';
import { Employee, EmployeeStatus } from '../../../core/models/employee.model';

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
    FormsModule,
    // Material Components (fallback)
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTooltipModule,
    // Custom UI Components
    CardComponent,
    ButtonComponent,
    IconComponent,
    ProgressSpinner
],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="employee-list-container">
      <!-- Enterprise Employee Management -->
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
              <!-- Stats Bar -->
              <div class="stats-bar">
                <div class="stat-card">
                  <span class="stat-value">{{ totalEmployees() }}</span>
                  <span class="stat-label">Total Employees</span>
                </div>
                <div class="stat-card stat-card--success">
                  <span class="stat-value">{{ activeEmployees() }}</span>
                  <span class="stat-label">Active</span>
                </div>
                <div class="stat-card stat-card--warning">
                  <span class="stat-value">{{ onLeaveEmployees() }}</span>
                  <span class="stat-label">On Leave</span>
                </div>
              </div>

              <!-- Search & Filters Bar -->
              <div class="controls-bar">
                <div class="search-wrapper">
                  <app-icon name="search" class="search-icon"></app-icon>
                  <input
                    type="text"
                    placeholder="Search by name, email, department..."
                    [ngModel]="searchTerm()"
                    (ngModelChange)="onSearchChange($event)"
                    class="search-input">
                </div>

                <mat-form-field appearance="outline" class="filter-field">
                  <mat-label>Status</mat-label>
                  <mat-select [value]="statusFilter()" (selectionChange)="onStatusFilterChange($event.value)">
                    <mat-option value="all">All Status</mat-option>
                    <mat-option value="Active">Active</mat-option>
                    <mat-option value="OnLeave">On Leave</mat-option>
                    <mat-option value="Suspended">Suspended</mat-option>
                    <mat-option value="Terminated">Terminated</mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline" class="filter-field">
                  <mat-label>Department</mat-label>
                  <mat-select [value]="departmentFilter()" (selectionChange)="onDepartmentFilterChange($event.value)">
                    <mat-option value="all">All Departments</mat-option>
                    @for (dept of availableDepartments(); track dept) {
                      <mat-option [value]="dept">{{ dept }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              </div>

              <!-- Enterprise Table with Row Actions -->
              <div class="table-wrapper">
                <table class="enterprise-table">
                  <thead>
                    <tr>
                      <th>Employee Code</th>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Department</th>
                      <th>Status</th>
                      <th class="actions-header">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (emp of paginatedEmployees(); track emp.id) {
                      <tr class="table-row">
                        <td class="employee-code">{{ emp.employeeCode }}</td>
                        <td class="employee-name">{{ emp.firstName }} {{ emp.lastName }}</td>
                        <td class="employee-email">{{ emp.email }}</td>
                        <td>{{ emp.department }}</td>
                        <td>
                          <span class="status-badge" [class]="'status-' + emp.status">
                            {{ emp.status === 'OnLeave' ? 'On Leave' : emp.status }}
                          </span>
                        </td>
                        <td class="actions-cell">
                          <button mat-icon-button matTooltip="View Details" [routerLink]="['/tenant/employees', emp.id]">
                            <mat-icon>visibility</mat-icon>
                          </button>
                          <button mat-icon-button matTooltip="Edit" [routerLink]="['/tenant/employees', emp.id, 'edit']">
                            <mat-icon>edit</mat-icon>
                          </button>
                          <button mat-icon-button matTooltip="Delete" color="warn" (click)="deleteEmployee(emp.id)">
                            <mat-icon>delete</mat-icon>
                          </button>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>

              <!-- Pagination -->
              <div class="pagination">
                <div class="pagination-info">{{ paginationInfo }}</div>
                <div class="pagination-controls">
                  <button mat-icon-button [disabled]="currentPage() === 1" (click)="prevPage()" matTooltip="Previous Page">
                    <mat-icon>chevron_left</mat-icon>
                  </button>
                  <span class="page-indicator">Page {{ currentPage() }} of {{ totalPages() }}</span>
                  <button mat-icon-button [disabled]="currentPage() === totalPages()" (click)="nextPage()" matTooltip="Next Page">
                    <mat-icon>chevron_right</mat-icon>
                  </button>
                </div>
              </div>
            }
        </div>
      </app-card>
    </div>
  `,
  styles: [`
    .employee-list-container {
      padding: 24px;
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

    /* ========================================
       STATS BAR - Fortune 500 KPI Cards
       ======================================== */
    .stats-bar {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 16px;
      margin-bottom: 24px;
    }

    .stat-card {
      background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%);
      border: 1px solid #e0e0e0;
      border-radius: 12px;
      padding: 20px;
      text-align: center;
      transition: all 250ms cubic-bezier(0.4, 0.0, 0.2, 1);
    }

    .stat-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    }

    .stat-card--success {
      background: linear-gradient(135deg, #e8f5e9 0%, #f1f8f4 100%);
      border-color: #a5d6a7;
    }

    .stat-card--warning {
      background: linear-gradient(135deg, #fff3e0 0%, #fff9f0 100%);
      border-color: #ffcc80;
    }

    .stat-value {
      display: block;
      font-size: 32px;
      font-weight: 700;
      color: #161616;
      line-height: 1.2;
      margin-bottom: 4px;
    }

    .stat-label {
      display: block;
      font-size: 13px;
      font-weight: 500;
      color: #6f6f6f;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    /* ========================================
       SEARCH & FILTERS BAR - Enterprise Controls
       ======================================== */
    .controls-bar {
      display: flex;
      gap: 16px;
      margin-bottom: 24px;
      align-items: center;
    }

    .search-wrapper {
      flex: 1;
      position: relative;
      max-width: 400px;
    }

    .search-icon {
      position: absolute;
      left: 12px;
      top: 50%;
      transform: translateY(-50%);
      color: #6f6f6f;
      pointer-events: none;
    }

    .search-input {
      width: 100%;
      height: 48px;
      padding: 12px 12px 12px 44px;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      font-size: 14px;
      color: #161616;
      background: #ffffff;
      transition: all 200ms cubic-bezier(0.4, 0.0, 0.2, 1);
    }

    .search-input:hover {
      border-color: #0F62FE;
    }

    .search-input:focus {
      outline: none;
      border-color: #0F62FE;
      box-shadow: 0 0 0 3px rgba(15, 98, 254, 0.1);
    }

    .search-input::placeholder {
      color: #8d8d8d;
    }

    .filter-field {
      width: 180px;
    }

    /* ========================================
       ENTERPRISE TABLE - Fortune 500 Grade
       ======================================== */
    .table-wrapper {
      overflow-x: auto;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      margin-bottom: 16px;
    }

    .enterprise-table {
      width: 100%;
      border-collapse: collapse;
      background: #ffffff;
    }

    .enterprise-table thead {
      background: linear-gradient(180deg, #f8f9fa 0%, #f1f3f5 100%);
      border-bottom: 2px solid #e0e0e0;
    }

    .enterprise-table th {
      padding: 16px;
      text-align: left;
      font-size: 12px;
      font-weight: 700;
      color: #525252;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      border-bottom: 2px solid #e0e0e0;
    }

    .enterprise-table th.actions-header {
      text-align: center;
      width: 150px;
    }

    .enterprise-table tbody tr {
      border-bottom: 1px solid #f1f3f5;
      transition: all 150ms cubic-bezier(0.4, 0.0, 0.2, 1);
    }

    .enterprise-table tbody tr:hover {
      background-color: #f8f9fa;
      box-shadow: inset 0 0 0 1px #e0e0e0;
    }

    .enterprise-table td {
      padding: 16px;
      font-size: 14px;
      color: #161616;
      vertical-align: middle;
    }

    .employee-code {
      font-weight: 600;
      color: #0F62FE;
      font-family: 'Courier New', monospace;
    }

    .employee-name {
      font-weight: 500;
    }

    .employee-email {
      color: #525252;
      font-size: 13px;
    }

    .status-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }

    .status-Active {
      background: #e8f5e9;
      color: #2e7d32;
      border: 1px solid #a5d6a7;
    }

    .status-OnLeave {
      background: #fff3e0;
      color: #f57c00;
      border: 1px solid #ffcc80;
    }

    .status-Suspended {
      background: #ffebee;
      color: #c62828;
      border: 1px solid #ef9a9a;
    }

    .status-Terminated {
      background: #f3e5f5;
      color: #6a1b9a;
      border: 1px solid #ce93d8;
    }

    .actions-cell {
      text-align: center;
      white-space: nowrap;
    }

    .actions-cell button {
      opacity: 0.7;
      transition: opacity 150ms cubic-bezier(0.4, 0.0, 0.2, 1);
    }

    .actions-cell button:hover {
      opacity: 1;
    }

    /* ========================================
       PAGINATION - Enterprise Grade
       ======================================== */
    .pagination {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 0;
    }

    .pagination-info {
      font-size: 14px;
      color: #525252;
      font-weight: 500;
    }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .page-indicator {
      font-size: 14px;
      color: #161616;
      font-weight: 500;
      min-width: 100px;
      text-align: center;
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

  // Search and filtering
  searchTerm = signal<string>('');
  statusFilter = signal<string>('all');
  departmentFilter = signal<string>('all');

  // Pagination
  currentPage = signal<number>(1);
  pageSize = signal<number>(10);

  // Stats
  totalEmployees = signal<number>(0);
  activeEmployees = signal<number>(0);
  onLeaveEmployees = signal<number>(0);

  // Computed filtered & paginated employees
  filteredEmployees = computed(() => {
    let result = this.employees();

    // Apply search
    const search = this.searchTerm().toLowerCase();
    if (search) {
      result = result.filter(emp =>
        emp.firstName?.toLowerCase().includes(search) ||
        emp.lastName?.toLowerCase().includes(search) ||
        emp.email?.toLowerCase().includes(search) ||
        emp.employeeCode?.toLowerCase().includes(search) ||
        emp.department?.toLowerCase().includes(search)
      );
    }

    // Apply status filter
    if (this.statusFilter() !== 'all') {
      result = result.filter(emp => emp.status.toString() === this.statusFilter());
    }

    // Apply department filter
    if (this.departmentFilter() !== 'all') {
      result = result.filter(emp => emp.department === this.departmentFilter());
    }

    return result;
  });

  paginatedEmployees = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize();
    const end = start + this.pageSize();
    return this.filteredEmployees().slice(start, end);
  });

  totalPages = computed(() =>
    Math.ceil(this.filteredEmployees().length / this.pageSize())
  );

  // Available departments for filter
  availableDepartments = computed(() => {
    const depts = new Set(this.employees().map(e => e.department).filter(Boolean));
    return Array.from(depts).sort();
  });

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

        // Calculate stats
        this.totalEmployees.set(employees.length);
        this.activeEmployees.set(employees.filter((e: Employee) => e.status === EmployeeStatus.Active).length);
        this.onLeaveEmployees.set(employees.filter((e: Employee) => e.status === EmployeeStatus.OnLeave).length);

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

  // Search and filter methods
  onSearchChange(value: string): void {
    this.searchTerm.set(value);
    this.currentPage.set(1); // Reset to first page on search
  }

  onStatusFilterChange(value: string): void {
    this.statusFilter.set(value);
    this.currentPage.set(1);
  }

  onDepartmentFilterChange(value: string): void {
    this.departmentFilter.set(value);
    this.currentPage.set(1);
  }

  // Pagination methods
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  nextPage(): void {
    this.goToPage(this.currentPage() + 1);
  }

  prevPage(): void {
    this.goToPage(this.currentPage() - 1);
  }

  get paginationInfo(): string {
    const start = (this.currentPage() - 1) * this.pageSize() + 1;
    const end = Math.min(this.currentPage() * this.pageSize(), this.filteredEmployees().length);
    const total = this.filteredEmployees().length;
    return `Showing ${start}-${end} of ${total} employees`;
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
