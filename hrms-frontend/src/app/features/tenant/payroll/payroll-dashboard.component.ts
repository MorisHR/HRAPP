import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PayrollService } from '../../../core/services/payroll.service';
import { EmployeeService } from '../../../core/services/employee.service';
import { SalaryComponentsService } from '../../../core/services/salary-components.service';

@Component({
  selector: 'app-payroll-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="payroll-dashboard">
      <div class="header">
        <h1><mat-icon>payments</mat-icon> Payroll Management</h1>
        <p class="subtitle">Process payroll and manage employee compensation</p>
      </div>

      @if (loading()) {
        <div class="loading-container">
          <mat-spinner diameter="50"></mat-spinner>
          <p>Loading payroll data...</p>
        </div>
      }

      @if (!loading()) {
        <div class="dashboard-grid">
          <mat-card class="stat-card clickable" (click)="navigateToSalaryComponents()">
            <mat-card-header>
              <mat-icon class="card-icon components">account_balance_wallet</mat-icon>
              <div>
                <h3>Salary Components</h3>
                <p class="stat-value">{{ salaryComponentStats().totalComponents }}</p>
                <p class="stat-subtext">Click to manage</p>
              </div>
            </mat-card-header>
          </mat-card>

          <mat-card class="stat-card">
            <mat-card-header>
              <mat-icon class="card-icon allowances">add_circle</mat-icon>
              <div>
                <h3>Total Allowances</h3>
                <p class="stat-value">MUR {{ formatCurrency(salaryComponentStats().totalAllowances) }}</p>
                <p class="stat-subtext">Active recurring</p>
              </div>
            </mat-card-header>
          </mat-card>

          <mat-card class="stat-card">
            <mat-card-header>
              <mat-icon class="card-icon deductions">remove_circle</mat-icon>
              <div>
                <h3>Total Deductions</h3>
                <p class="stat-value">MUR {{ formatCurrency(salaryComponentStats().totalDeductions) }}</p>
                <p class="stat-subtext">Active recurring</p>
              </div>
            </mat-card-header>
          </mat-card>

          <mat-card class="stat-card">
            <mat-card-header>
              <mat-icon class="card-icon employees">groups</mat-icon>
              <div>
                <h3>Employees</h3>
                <p class="stat-value">{{ stats().totalEmployees || 0 }}</p>
                <p class="stat-subtext">Active employees</p>
              </div>
            </mat-card-header>
          </mat-card>
        </div>

        <!-- Quick Actions -->
        <div class="quick-actions">
          <h2>Quick Actions</h2>
          <div class="actions-grid">
            <mat-card class="action-card clickable" (click)="navigateToSalaryComponents()">
              <mat-card-content>
                <mat-icon>account_balance_wallet</mat-icon>
                <h3>Manage Salary Components</h3>
                <p>Create and manage allowances and deductions</p>
                <button mat-button color="primary">Open</button>
              </mat-card-content>
            </mat-card>

            <mat-card class="action-card coming-soon">
              <mat-card-content>
                <mat-icon>receipt</mat-icon>
                <h3>Process Payroll</h3>
                <p>Calculate and process monthly payroll</p>
                <span class="coming-soon-badge">Coming Soon</span>
              </mat-card-content>
            </mat-card>

            <mat-card class="action-card coming-soon">
              <mat-card-content>
                <mat-icon>description</mat-icon>
                <h3>Generate Payslips</h3>
                <p>Create and distribute employee payslips</p>
                <span class="coming-soon-badge">Coming Soon</span>
              </mat-card-content>
            </mat-card>

            <mat-card class="action-card coming-soon">
              <mat-card-content>
                <mat-icon>analytics</mat-icon>
                <h3>Payroll Reports</h3>
                <p>View payroll summaries and statutory reports</p>
                <span class="coming-soon-badge">Coming Soon</span>
              </mat-card-content>
            </mat-card>
          </div>
        </div>
      }

      <!-- Coming Soon Features -->
      <mat-card class="features-card">
        <mat-card-content>
          <mat-icon class="construction-icon">construction</mat-icon>
          <h2>Additional Features in Development</h2>
          <p>The following features are under development and will be available soon:</p>
          <div class="features-grid">
            <div class="feature-item">
              <mat-icon>calculate</mat-icon>
              <span>Tax calculations (Mauritius tax rules)</span>
            </div>
            <div class="feature-item">
              <mat-icon>account_balance</mat-icon>
              <span>Bank file generation for payments</span>
            </div>
            <div class="feature-item">
              <mat-icon>assignment</mat-icon>
              <span>Statutory reporting (NPF, NSF, etc.)</span>
            </div>
            <div class="feature-item">
              <mat-icon>event_note</mat-icon>
              <span>Year-end tax forms</span>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .payroll-dashboard {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .header {
      margin-bottom: 32px;
    }

    .header h1 {
      display: flex;
      align-items: center;
      gap: 12px;
      font-size: 32px;
      font-weight: 500;
      color: #1a237e;
      margin: 0 0 8px 0;
    }

    .header h1 mat-icon {
      font-size: 36px;
      width: 36px;
      height: 36px;
      color: #3f51b5;
    }

    .subtitle {
      color: #666;
      font-size: 16px;
      margin: 0;
    }

    .dashboard-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 20px;
      margin-bottom: 32px;
    }

    .stat-card {
      cursor: default;
      transition: all 0.3s ease;
    }

    .stat-card.clickable {
      cursor: pointer;
    }

    .stat-card.clickable:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 16px rgba(0,0,0,0.15);
    }

    .stat-card mat-card-header {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px;
    }

    .card-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
    }

    .card-icon.components { color: #673ab7; }
    .card-icon.allowances { color: #4caf50; }
    .card-icon.deductions { color: #f44336; }
    .card-icon.employees { color: #9c27b0; }

    .stat-subtext {
      font-size: 12px;
      color: #999;
      margin: 4px 0 0 0;
    }

    .stat-card h3 {
      margin: 0;
      font-size: 14px;
      font-weight: 500;
      color: #666;
    }

    .stat-value {
      font-size: 32px;
      font-weight: 600;
      color: #333;
      margin: 4px 0 0 0;
    }

    // Quick Actions
    .quick-actions {
      margin: 32px 0;

      h2 {
        font-size: 24px;
        font-weight: 500;
        color: #1a237e;
        margin: 0 0 16px 0;
      }
    }

    .actions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 20px;
    }

    .action-card {
      transition: all 0.3s ease;

      &.clickable {
        cursor: pointer;
      }

      &.clickable:hover {
        transform: translateY(-4px);
        box-shadow: 0 8px 16px rgba(0,0,0,0.15);
      }

      &.coming-soon {
        opacity: 0.7;
        position: relative;
      }

      mat-card-content {
        text-align: center;
        padding: 24px !important;

        mat-icon {
          font-size: 48px;
          width: 48px;
          height: 48px;
          color: #3f51b5;
          margin-bottom: 12px;
        }

        h3 {
          margin: 0 0 8px 0;
          font-size: 18px;
          color: #333;
        }

        p {
          margin: 0 0 16px 0;
          font-size: 14px;
          color: #666;
        }

        .coming-soon-badge {
          display: inline-block;
          padding: 4px 12px;
          background: #ff9800;
          color: white;
          border-radius: 12px;
          font-size: 12px;
          font-weight: 500;
        }
      }
    }

    .features-card {
      text-align: center;
      padding: 32px;
      background: linear-gradient(135deg, #f5f7fa 0%, #e8eaf6 100%);

      mat-card-content {
        padding: 0 !important;
      }

      .construction-icon {
        font-size: 56px;
        width: 56px;
        height: 56px;
        color: #ff9800;
        margin-bottom: 16px;
      }

      h2 {
        color: #1a237e;
        margin: 0 0 12px 0;
        font-size: 24px;
      }

      p {
        color: #555;
        font-size: 14px;
        margin: 0 0 24px 0;
      }
    }

    .features-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      max-width: 800px;
      margin: 0 auto;

      .feature-item {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 12px;
        background: white;
        border-radius: 8px;
        text-align: left;

        mat-icon {
          font-size: 24px;
          width: 24px;
          height: 24px;
          color: #3f51b5;
        }

        span {
          font-size: 14px;
          color: #333;
        }
      }
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      gap: 16px;
      color: #666;
    }
  `]
})
export class PayrollDashboardComponent implements OnInit {
  private payrollService = inject(PayrollService);
  private employeeService = inject(EmployeeService);
  private salaryComponentsService = inject(SalaryComponentsService);
  private router = inject(Router);

  loading = signal<boolean>(false);
  stats = signal<{
    pendingPayslips: number;
    processedPayslips: number;
    totalPayroll: number;
    totalEmployees: number;
  }>({
    pendingPayslips: 0,
    processedPayslips: 0,
    totalPayroll: 0,
    totalEmployees: 0
  });

  salaryComponentStats = signal<{
    totalComponents: number;
    totalAllowances: number;
    totalDeductions: number;
  }>({
    totalComponents: 0,
    totalAllowances: 0,
    totalDeductions: 0
  });

  ngOnInit(): void {
    this.loadPayrollStats();
    this.loadSalaryComponentStats();
  }

  private loadPayrollStats(): void {
    this.loading.set(true);

    const currentMonth = new Date().getMonth() + 1;
    const currentYear = new Date().getFullYear();

    // Load employee count
    this.employeeService.getEmployees().subscribe({
      next: (response: any) => {
        const employeeData = response.data || response;
        const totalEmployees = Array.isArray(employeeData) ? employeeData.length : 0;

        // TODO: Load payslips for current month when backend endpoint is available
        // Backend needs to implement: GET /api/payroll/payslips?month={month}&year={year}
        // For now, setting default values
        this.stats.set({
          pendingPayslips: 0,
          processedPayslips: 0,
          totalPayroll: 0,
          totalEmployees
        });
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading employees:', err);
        this.loading.set(false);
      }
    });
  }

  private loadSalaryComponentStats(): void {
    // Load all salary components
    this.salaryComponentsService.getAllComponents(true).subscribe({
      next: (components) => {
        const totalAllowances = components
          .filter(c => c.componentType === 'Allowance' && c.isActive && c.isRecurring)
          .reduce((sum, c) => sum + c.amount, 0);

        const totalDeductions = components
          .filter(c => c.componentType === 'Deduction' && c.isActive && c.isRecurring)
          .reduce((sum, c) => sum + c.amount, 0);

        this.salaryComponentStats.set({
          totalComponents: components.filter(c => c.isActive).length,
          totalAllowances,
          totalDeductions
        });
      },
      error: (err) => {
        console.error('Error loading salary components:', err);
        // Keep default values on error
      }
    });
  }

  navigateToSalaryComponents(): void {
    this.router.navigate(['/tenant/payroll/salary-components']);
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-MU', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(amount);
  }
}
