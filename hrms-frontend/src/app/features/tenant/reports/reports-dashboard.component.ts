import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-reports-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <div class="reports-dashboard">
      <div class="header">
        <h1><mat-icon>assessment</mat-icon> Reports & Analytics</h1>
        <p class="subtitle">Generate insights and reports from your HR data</p>
      </div>

      <div class="dashboard-grid">
        <mat-card class="report-card">
          <mat-card-header>
            <mat-icon class="card-icon attendance">event_available</mat-icon>
            <div>
              <h3>Attendance Reports</h3>
              <p>Daily, weekly, and monthly attendance</p>
            </div>
          </mat-card-header>
        </mat-card>

        <mat-card class="report-card">
          <mat-card-header>
            <mat-icon class="card-icon leave">beach_access</mat-icon>
            <div>
              <h3>Leave Reports</h3>
              <p>Leave balances and history</p>
            </div>
          </mat-card-header>
        </mat-card>

        <mat-card class="report-card">
          <mat-card-header>
            <mat-icon class="card-icon payroll">payments</mat-icon>
            <div>
              <h3>Payroll Reports</h3>
              <p>Salary analysis and tax reports</p>
            </div>
          </mat-card-header>
        </mat-card>

        <mat-card class="report-card">
          <mat-card-header>
            <mat-icon class="card-icon employee">people</mat-icon>
            <div>
              <h3>Employee Reports</h3>
              <p>Headcount and demographics</p>
            </div>
          </mat-card-header>
        </mat-card>

        <mat-card class="report-card">
          <mat-card-header>
            <mat-icon class="card-icon performance">trending_up</mat-icon>
            <div>
              <h3>Performance Reports</h3>
              <p>Reviews and KPI tracking</p>
            </div>
          </mat-card-header>
        </mat-card>

        <mat-card class="report-card">
          <mat-card-header>
            <mat-icon class="card-icon compliance">verified_user</mat-icon>
            <div>
              <h3>Compliance Reports</h3>
              <p>Statutory and regulatory reports</p>
            </div>
          </mat-card-header>
        </mat-card>
      </div>

      <mat-card class="coming-soon-card">
        <mat-card-content>
          <mat-icon class="construction-icon">construction</mat-icon>
          <h2>Reports & Analytics Dashboard</h2>
          <p>This feature is under development and will be available soon.</p>
          <p class="feature-list">Upcoming features include:</p>
          <ul>
            <li>Pre-built report templates</li>
            <li>Custom report builder</li>
            <li>Data visualization and charts</li>
            <li>Export to PDF, Excel, CSV</li>
            <li>Scheduled report delivery</li>
            <li>Real-time analytics dashboards</li>
            <li>Comparative analysis tools</li>
          </ul>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .reports-dashboard {
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
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 20px;
      margin-bottom: 32px;
    }

    .report-card {
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .report-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 6px 12px rgba(0,0,0,0.15);
    }

    .report-card mat-card-header {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 20px;
    }

    .card-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
    }

    .card-icon.attendance { color: #4caf50; }
    .card-icon.leave { color: #2196f3; }
    .card-icon.payroll { color: #ff9800; }
    .card-icon.employee { color: #9c27b0; }
    .card-icon.performance { color: #00bcd4; }
    .card-icon.compliance { color: #f44336; }

    .report-card h3 {
      margin: 0 0 4px 0;
      font-size: 16px;
      font-weight: 600;
      color: #333;
    }

    .report-card p {
      margin: 0;
      font-size: 13px;
      color: #666;
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
export class ReportsDashboardComponent {}
