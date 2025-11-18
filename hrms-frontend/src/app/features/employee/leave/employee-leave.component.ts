import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UiModule } from '../../../shared/ui/ui.module';
import { MatFormFieldModule } from '@angular/material/form-field';
import { Chip, ChipColor } from '../../../shared/ui';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ToastService } from '../../../shared/ui';
import { TableComponent, TableColumn, TableColumnDirective, TooltipDirective } from '../../../shared/ui';
import { LeaveService } from '../../../core/services/leave.service';
import { AuthService } from '../../../core/services/auth.service';
import { Leave, LeaveBalance as ApiLeaveBalance, LeaveType, LeaveStatus, ApplyLeaveRequest } from '../../../core/models/leave.model';

interface LeaveRequest {
  id: string;
  leaveType: string;
  startDate: Date;
  endDate: Date;
  days: number;
  reason: string;
  status: 'pending' | 'approved' | 'rejected' | 'cancelled';
  appliedDate: Date;
  approvedBy?: string;
  approvedDate?: Date;
  rejectionReason?: string;
}

interface LeaveBalance {
  type: string;
  total: number;
  used: number;
  remaining: number;
}

@Component({
  selector: 'app-employee-leave',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    Chip,
    UiModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    TableComponent,
    TableColumnDirective,
    TooltipDirective
  ],
  templateUrl: './employee-leave.component.html',
  styleUrl: './employee-leave.component.scss'
})
export class EmployeeLeaveComponent implements OnInit {
  private fb = inject(FormBuilder);
  private toastService = inject(ToastService);
  private leaveService = inject(LeaveService);
  private authService = inject(AuthService);

  loading = signal(false);
  submitting = signal(false);
  showForm = signal(false);
  leaveRequests = signal<LeaveRequest[]>([]);
  leaveBalances = signal<LeaveBalance[]>([]);
  employeeId = signal<string>('');

  leaveForm: FormGroup;

  columns: TableColumn[] = [
    { key: 'leaveType', label: 'Leave Type' },
    { key: 'dates', label: 'Period' },
    { key: 'days', label: 'Days' },
    { key: 'status', label: 'Status' },
    { key: 'appliedDate', label: 'Applied' },
    { key: 'actions', label: 'Actions' }
  ];

  leaveTypes = [
    { value: 'annual', label: 'Annual Leave' },
    { value: 'sick', label: 'Sick Leave' },
    { value: 'casual', label: 'Casual Leave' },
    { value: 'unpaid', label: 'Unpaid Leave' },
    { value: 'maternity', label: 'Maternity Leave' },
    { value: 'paternity', label: 'Paternity Leave' }
  ];

  // ✅ FORTUNE 500 PATTERN: Computed statistics
  stats = computed(() => {
    const requests = this.leaveRequests();
    return {
      total: requests.length,
      pending: requests.filter(r => r.status === 'pending').length,
      approved: requests.filter(r => r.status === 'approved').length,
      rejected: requests.filter(r => r.status === 'rejected').length
    };
  });

  constructor() {
    this.leaveForm = this.fb.group({
      leaveType: ['', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]]
    });
  }

  ngOnInit(): void {
    // ✅ FORTUNE 500 PATTERN: Get authenticated user
    const user = this.authService.user();
    if (user?.id) {
      this.employeeId.set(user.id);
      this.loadLeaveData();
    } else {
      this.toastService.error('Unable to load employee information', 3000);
    }
  }

  // ✅ FORTUNE 500 PATTERN: Load all data with real APIs and error handling
  loadLeaveData(): void {
    this.loading.set(true);
    const empId = this.employeeId();

    // Load leave balance
    this.leaveService.getLeaveBalance(empId).subscribe({
      next: (balances) => {
        this.leaveBalances.set(balances.map(b => ({
          type: this.mapLeaveTypeToString(b.leaveType),
          total: b.total,
          used: b.used,
          remaining: b.remaining
        })));
      },
      error: (err) => {
        console.error('Failed to load leave balance:', err);
        // Fallback to mock data
        this.leaveBalances.set(this.generateMockBalances());
        this.toastService.warning('Using demo leave balance. API issue.', 3000);
      }
    });

    // Load leave requests
    this.leaveService.getEmployeeLeaves(empId).subscribe({
      next: (leaves) => {
        this.leaveRequests.set(leaves.map(l => this.mapToLeaveRequest(l)));
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load leave requests:', err);
        this.loading.set(false);
        // Fallback to mock data
        this.leaveRequests.set(this.generateMockRequests());
        this.toastService.warning('Using demo leave data. API issue.', 3000);
      }
    });
  }

  // ✅ FORTUNE 500 PATTERN: Map backend model to UI model
  private mapToLeaveRequest(leave: Leave): LeaveRequest {
    return {
      id: leave.id,
      leaveType: leave.leaveType,
      startDate: new Date(leave.startDate),
      endDate: new Date(leave.endDate),
      days: leave.days,
      reason: leave.reason,
      status: this.mapLeaveStatus(leave.status),
      appliedDate: new Date(leave.appliedDate),
      approvedBy: leave.approvedBy,
      approvedDate: leave.approvedDate ? new Date(leave.approvedDate) : undefined,
      rejectionReason: leave.rejectionReason
    };
  }

  private mapLeaveStatus(status: LeaveStatus): 'pending' | 'approved' | 'rejected' | 'cancelled' {
    switch (status) {
      case LeaveStatus.Pending: return 'pending';
      case LeaveStatus.Approved: return 'approved';
      case LeaveStatus.Rejected: return 'rejected';
      case LeaveStatus.Cancelled: return 'cancelled';
      default: return 'pending';
    }
  }

  private mapLeaveTypeToString(type: LeaveType): string {
    switch (type) {
      case LeaveType.Annual: return 'annual';
      case LeaveType.Sick: return 'sick';
      case LeaveType.Casual: return 'casual';
      case LeaveType.Maternity: return 'maternity';
      case LeaveType.Paternity: return 'paternity';
      case LeaveType.Unpaid: return 'unpaid';
      default: return 'annual';
    }
  }

  // ✅ FORTUNE 500 PATTERN: Form submission with real API
  onSubmit(): void {
    if (this.leaveForm.invalid) {
      this.markFormGroupTouched(this.leaveForm);
      this.toastService.warning('Please fill all required fields correctly', 3000);
      return;
    }

    const formValue = this.leaveForm.value;
    const startDate = new Date(formValue.startDate);
    const endDate = new Date(formValue.endDate);

    // Validation: End date must be after start date
    if (endDate <= startDate) {
      this.toastService.warning('End date must be after start date', 3000);
      return;
    }

    // Calculate days
    const days = this.calculateDays(startDate, endDate);

    // Check if leave balance is sufficient
    const selectedType = formValue.leaveType;
    const balance = this.leaveBalances().find(b => b.type === selectedType);
    if (balance && balance.remaining < days) {
      this.toastService.warning(
        `Insufficient leave balance. You have ${balance.remaining} days remaining.`,
        4000
      );
      return;
    }

    this.submitting.set(true);

    const request: ApplyLeaveRequest = {
      leaveType: this.stringToLeaveType(selectedType),
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      reason: formValue.reason
    };

    this.leaveService.applyLeave(request).subscribe({
      next: (leave) => {
        this.leaveRequests.update(requests => [this.mapToLeaveRequest(leave), ...requests]);
        this.submitting.set(false);
        this.showForm.set(false);
        this.leaveForm.reset();

        this.toastService.success('Leave request submitted successfully', 5000);

        // Refresh balance
        this.loadLeaveData();
      },
      error: (err) => {
        console.error('Leave application failed:', err);
        this.submitting.set(false);
        this.toastService.error(
          err.error?.message || 'Failed to submit leave request. Please try again.',
          3000
        );
      }
    });
  }

  private stringToLeaveType(type: string): LeaveType {
    switch (type) {
      case 'annual': return LeaveType.Annual;
      case 'sick': return LeaveType.Sick;
      case 'casual': return LeaveType.Casual;
      case 'maternity': return LeaveType.Maternity;
      case 'paternity': return LeaveType.Paternity;
      case 'unpaid': return LeaveType.Unpaid;
      default: return LeaveType.Annual;
    }
  }

  toggleForm(): void {
    this.showForm.update(val => !val);
    if (!this.showForm()) {
      this.leaveForm.reset();
    }
  }

  cancelRequest(request: LeaveRequest): void {
    if (request.status !== 'pending') {
      this.toastService.warning('Only pending requests can be cancelled', 3000);
      return;
    }

    if (!confirm(`Cancel leave request for ${this.formatDateRange(request.startDate, request.endDate)}?`)) {
      return;
    }

    this.loading.set(true);
    this.leaveService.cancelLeave(request.id).subscribe({
      next: (updatedLeave) => {
        this.leaveRequests.update(requests =>
          requests.map(r => r.id === request.id ? this.mapToLeaveRequest(updatedLeave) : r)
        );
        this.loading.set(false);
        this.toastService.success('Leave request cancelled successfully', 3000);

        // Refresh balance
        this.loadLeaveData();
      },
      error: (err) => {
        console.error('Failed to cancel leave:', err);
        this.loading.set(false);
        this.toastService.error(
          err.error?.message || 'Failed to cancel leave request.',
          3000
        );
      }
    });
  }

  getStatusColor(status: string): ChipColor {
    const colors: Record<string, ChipColor> = {
      'pending': 'warning',
      'approved': 'success',
      'rejected': 'error',
      'cancelled': 'neutral'
    };
    return colors[status] || 'neutral';
  }

  getStatusLabel(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }

  getLeaveTypeLabel(type: string): string {
    const leaveType = this.leaveTypes.find(t => t.value === type);
    return leaveType ? leaveType.label : type;
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatDateRange(startDate: Date, endDate: Date): string {
    return `${this.formatDate(startDate)} - ${this.formatDate(endDate)}`;
  }

  calculateDays(startDate: Date, endDate: Date): number {
    const diffTime = Math.abs(endDate.getTime() - startDate.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1; // Include both start and end dates
    return diffDays;
  }

  getBalancePercentage(balance: LeaveBalance): number {
    return (balance.remaining / balance.total) * 100;
  }

  getBalanceColor(percentage: number): ChipColor {
    if (percentage > 50) return 'success';
    if (percentage > 25) return 'warning';
    return 'error';
  }

  private markFormGroupTouched(formGroup: FormGroup) {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  // ✅ FORTUNE 500 PATTERN: Generate mock data for demo
  private generateMockBalances(): LeaveBalance[] {
    return [
      { type: 'annual', total: 20, used: 8, remaining: 12 },
      { type: 'sick', total: 12, used: 3, remaining: 9 },
      { type: 'casual', total: 10, used: 5, remaining: 5 }
    ];
  }

  private generateMockRequests(): LeaveRequest[] {
    const requests: LeaveRequest[] = [];
    const statuses: Array<'pending' | 'approved' | 'rejected'> = ['pending', 'approved', 'rejected'];

    for (let i = 0; i < 10; i++) {
      const startDate = new Date();
      startDate.setDate(startDate.getDate() + Math.floor(Math.random() * 60) - 30);

      const endDate = new Date(startDate);
      endDate.setDate(endDate.getDate() + Math.floor(Math.random() * 5) + 1);

      requests.push({
        id: `leave-${i}`,
        leaveType: this.leaveTypes[Math.floor(Math.random() * 3)].label,
        startDate,
        endDate,
        days: this.calculateDays(startDate, endDate),
        reason: 'Personal reasons',
        status: statuses[Math.floor(Math.random() * statuses.length)],
        appliedDate: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000)
      });
    }

    return requests.sort((a, b) => b.appliedDate.getTime() - a.appliedDate.getTime());
  }
}
