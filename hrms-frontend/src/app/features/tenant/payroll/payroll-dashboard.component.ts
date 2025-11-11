import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-payroll-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <div class="payroll-dashboard">
      <div class="header">
        <h1><mat-icon>payments</mat-icon> Payroll Management</h1>
        <p class="subtitle">Process payroll and manage employee compensation</p>
      </div>

      <div class="dashboard-grid">
        <mat-card class="stat-card">
          <mat-card-header>
            <mat-icon class="card-icon pending">schedule</mat-icon>
            <div>
              <h3>Pending Payroll</h3>
              <p class="stat-value">--</p>
            </div>
          </mat-card-header>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-header>
            <mat-icon class="card-icon processed">check_circle</mat-icon>
            <div>
              <h3>Processed This Month</h3>
              <p class="stat-value">--</p>
            </div>
          </mat-card-header>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-header>
            <mat-icon class="card-icon total">account_balance</mat-icon>
            <div>
              <h3>Total Payroll (MTD)</h3>
              <p class="stat-value">MUR --</p>
            </div>
          </mat-card-header>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-header>
            <mat-icon class="card-icon employees">groups</mat-icon>
            <div>
              <h3>Employees</h3>
              <p class="stat-value">--</p>
            </div>
          </mat-card-header>
        </mat-card>
      </div>

      <mat-card class="coming-soon-card">
        <mat-card-content>
          <mat-icon class="construction-icon">construction</mat-icon>
          <h2>Payroll Management System</h2>
          <p>This feature is under development and will be available soon.</p>
          <p class="feature-list">Upcoming features include:</p>
          <ul>
            <li>Automated payroll processing</li>
            <li>Salary components and deductions</li>
            <li>Tax calculations (Mauritius tax rules)</li>
            <li>Payslip generation and distribution</li>
            <li>Bank file generation for payments</li>
            <li>Statutory reporting (NPF, NSF, etc.)</li>
            <li>Year-end tax forms</li>
          </ul>
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

    .card-icon.pending { color: #ff9800; }
    .card-icon.processed { color: #4caf50; }
    .card-icon.total { color: #2196f3; }
    .card-icon.employees { color: #9c27b0; }

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

    .coming-soon-card {
      text-align: center;
      padding: 48px 24px;
      background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
    }

    .construction-icon {
      font-size: 72px;
      width: 72px;
      height: 72px;
      color: #ff9800;
      margin-bottom: 16px;
    }

    .coming-soon-card h2 {
      color: #1a237e;
      margin: 0 0 16px 0;
      font-size: 28px;
    }

    .coming-soon-card p {
      color: #555;
      font-size: 16px;
      margin: 0 0 24px 0;
    }

    .feature-list {
      font-weight: 600;
      margin: 24px 0 12px 0;
    }

    .coming-soon-card ul {
      list-style: none;
      padding: 0;
      max-width: 500px;
      margin: 0 auto;
      text-align: left;
    }

    .coming-soon-card li {
      padding: 8px 0;
      color: #333;
      position: relative;
      padding-left: 28px;
    }

    .coming-soon-card li:before {
      content: '✓';
      position: absolute;
      left: 0;
      color: #4caf50;
      font-weight: bold;
      font-size: 18px;
    }
  `]
})
export class PayrollDashboardComponent {}
