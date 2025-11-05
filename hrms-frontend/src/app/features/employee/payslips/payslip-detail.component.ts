import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { PayrollService } from '../../../core/services/payroll.service';
import { Payslip, formatCurrency } from '../../../core/models/payroll.model';

@Component({
  selector: 'app-payslip-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  templateUrl: './payslip-detail.component.html',
  styleUrl: './payslip-detail.component.scss'
})
export class PayslipDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private payrollService = inject(PayrollService);

  payslip = signal<Payslip | null>(null);
  loading = signal<boolean>(false);

  earningsColumns = ['name', 'amount'];
  deductionsColumns = ['name', 'amount'];

  ngOnInit(): void {
    const payslipId = this.route.snapshot.paramMap.get('id');
    if (payslipId) {
      this.loadPayslip(payslipId);
    }
  }

  loadPayslip(id: string): void {
    this.loading.set(true);
    this.payrollService.getPayslip(id).subscribe({
      next: (payslip) => {
        this.payslip.set(payslip);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load payslip:', err);
        this.loading.set(false);
        alert('Failed to load payslip details. Please try again.');
      }
    });
  }

  downloadPayslip(): void {
    const payslip = this.payslip();
    if (!payslip) return;

    this.payrollService.downloadPayslip(payslip.id).subscribe({
      next: (blob) => {
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

  goBack(): void {
    this.router.navigate(['/employee/payslips']);
  }

  formatCurrency(amount: number): string {
    return formatCurrency(amount);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-MU', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
}
