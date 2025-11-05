import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PayrollService } from '../../../core/services/payroll.service';
import { Payslip, formatCurrency } from '../../../core/models/payroll.model';

@Component({
  selector: 'app-payslip-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './payslip-list.component.html',
  styleUrl: './payslip-list.component.scss'
})
export class PayslipListComponent implements OnInit {
  private payrollService = inject(PayrollService);

  payslips = signal<Payslip[]>([]);
  loading = this.payrollService.loading;

  displayedColumns = ['payPeriod', 'payDate', 'grossPay', 'totalDeductions', 'netPay', 'actions'];

  // Stats computed from payslips
  stats = computed(() => {
    const allPayslips = this.payslips();
    const currentYear = new Date().getFullYear();
    const currentYearPayslips = allPayslips.filter(p =>
      new Date(p.payDate).getFullYear() === currentYear
    );

    return {
      total: allPayslips.length,
      currentYear: currentYearPayslips.length,
      totalEarnedThisYear: currentYearPayslips.reduce((sum, p) => sum + p.netPay, 0),
      latestPayslip: allPayslips.length > 0 ? allPayslips[0] : null
    };
  });

  ngOnInit(): void {
    this.loadPayslips();
  }

  loadPayslips(): void {
    // Get current user's employee ID from auth service or local storage
    const employeeId = localStorage.getItem('employeeId') || '';

    if (!employeeId) {
      console.error('Employee ID not found');
      return;
    }

    this.payrollService.getEmployeePayslips(employeeId).subscribe({
      next: (payslips) => {
        // Sort by pay date descending (most recent first)
        this.payslips.set(payslips.sort((a, b) =>
          new Date(b.payDate).getTime() - new Date(a.payDate).getTime()
        ));
      },
      error: (err) => {
        console.error('Failed to load payslips:', err);
      }
    });
  }

  downloadPayslip(payslip: Payslip): void {
    this.payrollService.downloadPayslip(payslip.id).subscribe({
      next: (blob) => {
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Payslip-${payslip.payPeriod}-${payslip.employeeCode}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Failed to download payslip:', err);
        alert('Failed to download payslip. Please try again.');
      }
    });
  }

  formatCurrency(amount: number): string {
    return formatCurrency(amount);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-MU', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
