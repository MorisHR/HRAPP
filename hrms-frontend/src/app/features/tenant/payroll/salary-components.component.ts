import { Component, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Subject, takeUntil, finalize } from 'rxjs';

// Material imports
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTabsModule } from '@angular/material/tabs';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';

// Services
import {
  SalaryComponentsService,
  SalaryComponentDto,
  CreateSalaryComponentDto,
  UpdateSalaryComponentDto,
  BulkCreateComponentsDto
} from '../../../core/services/salary-components.service';
import { EmployeeService } from '../../../core/services/employee.service';

interface EmployeeOption {
  id: string;
  fullName: string;
  employeeCode: string;
  department: string;
}

@Component({
  selector: 'app-salary-components',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatTooltipModule,
    MatTabsModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule
  ],
  templateUrl: './salary-components.component.html',
  styleUrls: ['./salary-components.component.scss']
})
export class SalaryComponentsComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private salaryComponentsService = inject(SalaryComponentsService);
  private employeeService = inject(EmployeeService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private destroy$ = new Subject<void>();

  // State signals
  loading = signal(false);
  saving = signal(false);
  components = signal<SalaryComponentDto[]>([]);
  filteredComponents = signal<SalaryComponentDto[]>([]);
  employees = signal<EmployeeOption[]>([]);
  selectedEmployee = signal<string | undefined>(undefined);
  showForm = signal(false);
  editingComponent = signal<SalaryComponentDto | null>(null);
  selectedTab = signal(0);
  bulkMode = signal(false);

  // Form
  componentForm!: FormGroup;
  bulkForm!: FormGroup;

  // Table columns
  displayedColumns = [
    'componentName',
    'componentType',
    'amount',
    'isRecurring',
    'effectiveDate',
    'endDate',
    'isActive',
    'requiresApproval',
    'isApproved',
    'actions'
  ];

  // Filter options
  filterType = signal<'all' | 'Allowance' | 'Deduction'>('all');
  filterActive = signal<'all' | 'active' | 'inactive'>('all');
  filterApproval = signal<'all' | 'approved' | 'pending'>('all');

  constructor() {
    this.initializeForms();
  }

  ngOnInit(): void {
    this.loadEmployees();
    this.loadAllComponents();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForms(): void {
    this.componentForm = this.fb.group({
      employeeId: ['', Validators.required],
      componentName: ['', [Validators.required, Validators.minLength(2)]],
      componentType: ['Allowance', Validators.required],
      amount: [0, [Validators.required, Validators.min(0)]],
      isRecurring: [true],
      effectiveDate: [new Date(), Validators.required],
      endDate: [''],
      description: [''],
      requiresApproval: [false]
    });

    this.bulkForm = this.fb.group({
      employeeIds: [[], Validators.required],
      componentName: ['', [Validators.required, Validators.minLength(2)]],
      componentType: ['Allowance', Validators.required],
      amount: [0, [Validators.required, Validators.min(0)]],
      isRecurring: [true],
      effectiveDate: [new Date(), Validators.required],
      endDate: [''],
      description: [''],
      requiresApproval: [false]
    });
  }

  private loadEmployees(): void {
    this.loading.set(true);
    this.employeeService.getEmployees()
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (response: any) => {
          const data = response.data || response;
          const employeeList = Array.isArray(data) ? data : [];

          const options: EmployeeOption[] = employeeList.map((emp: any) => ({
            id: emp.id,
            fullName: `${emp.firstName} ${emp.lastName}`,
            employeeCode: emp.employeeCode || '',
            department: emp.department || 'N/A'
          }));

          this.employees.set(options);
        },
        error: (error: any) => {
          console.error('Error loading employees:', error);
          this.snackBar.open('Failed to load employees', 'Close', { duration: 3000 });
        }
      });
  }

  private loadAllComponents(): void {
    this.loading.set(true);
    this.salaryComponentsService.getAllComponents(false)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (components) => {
          this.components.set(components);
          this.applyFilters();
        },
        error: (error) => {
          console.error('Error loading components:', error);
          this.snackBar.open('Failed to load salary components', 'Close', { duration: 3000 });
        }
      });
  }

  loadEmployeeComponents(employeeId: string): void {
    if (!employeeId) {
      this.loadAllComponents();
      return;
    }

    this.loading.set(true);
    this.selectedEmployee.set(employeeId);

    this.salaryComponentsService.getEmployeeComponents(employeeId, false)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (components) => {
          this.components.set(components);
          this.applyFilters();
        },
        error: (error) => {
          console.error('Error loading employee components:', error);
          this.snackBar.open('Failed to load employee components', 'Close', { duration: 3000 });
        }
      });
  }

  applyFilters(): void {
    let filtered = [...this.components()];

    // Filter by type
    if (this.filterType() !== 'all') {
      filtered = filtered.filter(c => c.componentType === this.filterType());
    }

    // Filter by active status
    if (this.filterActive() === 'active') {
      filtered = filtered.filter(c => c.isActive);
    } else if (this.filterActive() === 'inactive') {
      filtered = filtered.filter(c => !c.isActive);
    }

    // Filter by approval status
    if (this.filterApproval() === 'approved') {
      filtered = filtered.filter(c => c.isApproved);
    } else if (this.filterApproval() === 'pending') {
      filtered = filtered.filter(c => c.requiresApproval && !c.isApproved);
    }

    this.filteredComponents.set(filtered);
  }

  setFilterType(type: 'all' | 'Allowance' | 'Deduction'): void {
    this.filterType.set(type);
    this.applyFilters();
  }

  setFilterActive(status: 'all' | 'active' | 'inactive'): void {
    this.filterActive.set(status);
    this.applyFilters();
  }

  setFilterApproval(status: 'all' | 'approved' | 'pending'): void {
    this.filterApproval.set(status);
    this.applyFilters();
  }

  showCreateForm(): void {
    this.editingComponent.set(null);
    this.componentForm.reset({
      componentType: 'Allowance',
      amount: 0,
      isRecurring: true,
      effectiveDate: new Date(),
      requiresApproval: false
    });
    this.showForm.set(true);
  }

  editComponent(component: SalaryComponentDto): void {
    this.editingComponent.set(component);
    this.componentForm.patchValue({
      employeeId: component.employeeId,
      componentName: component.componentName,
      componentType: component.componentType,
      amount: component.amount,
      isRecurring: component.isRecurring,
      effectiveDate: new Date(component.effectiveDate),
      endDate: component.endDate ? new Date(component.endDate) : null,
      description: component.description || '',
      requiresApproval: component.requiresApproval
    });
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingComponent.set(null);
    this.componentForm.reset();
  }

  onSubmit(): void {
    if (this.componentForm.invalid) {
      this.snackBar.open('Please fill all required fields', 'Close', { duration: 3000 });
      return;
    }

    const editing = this.editingComponent();
    if (editing) {
      this.updateComponent(editing.id);
    } else {
      this.createComponent();
    }
  }

  private createComponent(): void {
    this.saving.set(true);
    const formValue = this.componentForm.value;

    const dto: CreateSalaryComponentDto = {
      employeeId: formValue.employeeId,
      componentName: formValue.componentName,
      componentType: formValue.componentType,
      amount: formValue.amount,
      isRecurring: formValue.isRecurring,
      effectiveDate: new Date(formValue.effectiveDate).toISOString(),
      endDate: formValue.endDate ? new Date(formValue.endDate).toISOString() : undefined,
      description: formValue.description || undefined,
      requiresApproval: formValue.requiresApproval
    };

    this.salaryComponentsService.createComponent(dto)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: () => {
          this.snackBar.open('Salary component created successfully', 'Close', { duration: 3000 });
          this.cancelForm();
          this.loadAllComponents();
        },
        error: (error) => {
          console.error('Error creating component:', error);
          this.snackBar.open(
            error.error?.message || 'Failed to create salary component',
            'Close',
            { duration: 5000 }
          );
        }
      });
  }

  private updateComponent(id: string): void {
    this.saving.set(true);
    const formValue = this.componentForm.value;

    const dto: UpdateSalaryComponentDto = {
      componentName: formValue.componentName,
      amount: formValue.amount,
      isRecurring: formValue.isRecurring,
      effectiveDate: new Date(formValue.effectiveDate).toISOString(),
      endDate: formValue.endDate ? new Date(formValue.endDate).toISOString() : undefined,
      description: formValue.description || undefined
    };

    this.salaryComponentsService.updateComponent(id, dto)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: () => {
          this.snackBar.open('Salary component updated successfully', 'Close', { duration: 3000 });
          this.cancelForm();
          this.loadAllComponents();
        },
        error: (error) => {
          console.error('Error updating component:', error);
          this.snackBar.open(
            error.error?.message || 'Failed to update salary component',
            'Close',
            { duration: 5000 }
          );
        }
      });
  }

  deactivateComponent(component: SalaryComponentDto): void {
    if (!confirm(`Are you sure you want to deactivate "${component.componentName}"?`)) {
      return;
    }

    this.salaryComponentsService.deactivateComponent(component.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open('Component deactivated successfully', 'Close', { duration: 3000 });
          this.loadAllComponents();
        },
        error: (error) => {
          console.error('Error deactivating component:', error);
          this.snackBar.open('Failed to deactivate component', 'Close', { duration: 3000 });
        }
      });
  }

  deleteComponent(component: SalaryComponentDto): void {
    if (!confirm(`Are you sure you want to permanently delete "${component.componentName}"? This action cannot be undone.`)) {
      return;
    }

    this.salaryComponentsService.deleteComponent(component.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open('Component deleted successfully', 'Close', { duration: 3000 });
          this.loadAllComponents();
        },
        error: (error) => {
          console.error('Error deleting component:', error);
          this.snackBar.open('Failed to delete component', 'Close', { duration: 3000 });
        }
      });
  }

  approveComponent(component: SalaryComponentDto): void {
    if (component.isApproved) {
      this.snackBar.open('Component is already approved', 'Close', { duration: 2000 });
      return;
    }

    this.salaryComponentsService.approveComponent(component.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open('Component approved successfully', 'Close', { duration: 3000 });
          this.loadAllComponents();
        },
        error: (error) => {
          console.error('Error approving component:', error);
          this.snackBar.open('Failed to approve component', 'Close', { duration: 3000 });
        }
      });
  }

  toggleBulkMode(): void {
    this.bulkMode.set(!this.bulkMode());
    if (this.bulkMode()) {
      this.bulkForm.reset({
        employeeIds: [],
        componentType: 'Allowance',
        amount: 0,
        isRecurring: true,
        effectiveDate: new Date(),
        requiresApproval: false
      });
    }
  }

  onBulkSubmit(): void {
    if (this.bulkForm.invalid) {
      this.snackBar.open('Please fill all required fields', 'Close', { duration: 3000 });
      return;
    }

    const formValue = this.bulkForm.value;

    const dto: BulkCreateComponentsDto = {
      employeeIds: formValue.employeeIds,
      componentDetails: {
        employeeId: '', // Will be replaced on backend
        componentName: formValue.componentName,
        componentType: formValue.componentType,
        amount: formValue.amount,
        isRecurring: formValue.isRecurring,
        effectiveDate: new Date(formValue.effectiveDate).toISOString(),
        endDate: formValue.endDate ? new Date(formValue.endDate).toISOString() : undefined,
        description: formValue.description || undefined,
        requiresApproval: formValue.requiresApproval
      }
    };

    this.saving.set(true);
    this.salaryComponentsService.bulkCreateComponents(dto)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: (response) => {
          this.snackBar.open(
            `Successfully created ${response.componentIds.length} salary components`,
            'Close',
            { duration: 3000 }
          );
          this.toggleBulkMode();
          this.loadAllComponents();
        },
        error: (error) => {
          console.error('Error bulk creating components:', error);
          this.snackBar.open(
            error.error?.message || 'Failed to create salary components',
            'Close',
            { duration: 5000 }
          );
        }
      });
  }

  // Helper methods
  getComponentTypeBadgeClass(type: string): string {
    return type === 'Allowance' ? 'badge-allowance' : 'badge-deduction';
  }

  getStatusBadgeClass(isActive: boolean): string {
    return isActive ? 'badge-active' : 'badge-inactive';
  }

  getApprovalBadgeClass(component: SalaryComponentDto): string {
    if (!component.requiresApproval) return 'badge-no-approval';
    return component.isApproved ? 'badge-approved' : 'badge-pending';
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }

  formatCurrency(amount: number): string {
    return `MUR ${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  getEmployeeName(employeeId: string): string {
    const employee = this.employees().find(e => e.id === employeeId);
    return employee ? employee.fullName : 'Unknown';
  }

  // Calculate totals for selected employee
  calculateTotals(month: number, year: number, employeeId?: string): void {
    if (!employeeId) {
      this.snackBar.open('Please select an employee first', 'Close', { duration: 2000 });
      return;
    }

    // Get both allowances and deductions
    this.salaryComponentsService.getTotalAllowances(employeeId, month, year)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (allowances) => {
          this.salaryComponentsService.getTotalDeductions(employeeId, month, year)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: (deductions) => {
                const message = `
                  Total Allowances: ${this.formatCurrency(allowances.totalAllowances || 0)}
                  Total Deductions: ${this.formatCurrency(deductions.totalDeductions || 0)}
                  Net: ${this.formatCurrency((allowances.totalAllowances || 0) - (deductions.totalDeductions || 0))}
                `;
                this.snackBar.open(message, 'Close', { duration: 5000 });
              },
              error: (error) => {
                console.error('Error getting deductions:', error);
              }
            });
        },
        error: (error) => {
          console.error('Error getting allowances:', error);
          this.snackBar.open('Failed to calculate totals', 'Close', { duration: 3000 });
        }
      });
  }

  getCurrentMonth(): number {
    return new Date().getMonth() + 1;
  }

  getCurrentYear(): number {
    return new Date().getFullYear();
  }

  // Count methods for statistics
  getTotalComponents(): number {
    return this.components().length;
  }

  getActiveComponents(): number {
    return this.components().filter(c => c.isActive).length;
  }

  getPendingApprovals(): number {
    return this.components().filter(c => c.requiresApproval && !c.isApproved).length;
  }

  getTotalAllowances(): number {
    return this.components()
      .filter(c => c.componentType === 'Allowance' && c.isActive)
      .reduce((sum, c) => sum + c.amount, 0);
  }

  getTotalDeductions(): number {
    return this.components()
      .filter(c => c.componentType === 'Deduction' && c.isActive)
      .reduce((sum, c) => sum + c.amount, 0);
  }
}
