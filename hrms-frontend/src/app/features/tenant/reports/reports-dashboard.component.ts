import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UiModule } from '../../../shared/ui/ui.module';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { ToastService, Divider, TooltipDirective, ExpansionPanel, ExpansionPanelGroup } from '../../../shared/ui';
import { ReportsService, DashboardSummaryDto } from '../../../core/services/reports.service';

@Component({
  selector: 'app-reports-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    UiModule,
    MatSelectModule,
    MatInputModule,
    ExpansionPanel,
    ExpansionPanelGroup,
    Divider,
    TooltipDirective
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="reports-dashboard">
      <div class="header">
        <h1><mat-icon>assessment</mat-icon> Reports & Analytics</h1>
        <p class="subtitle">Generate insights and reports from your HR data</p>
      </div>
    
      <!-- Dashboard KPIs Section -->
      @if (dashboardData()) {
        <mat-card class="kpi-card">
          <mat-card-header>
            <mat-icon class="header-icon">dashboard</mat-icon>
            <div>
              <mat-card-title>Dashboard Overview</mat-card-title>
              <mat-card-subtitle>Real-time HR metrics</mat-card-subtitle>
            </div>
          </mat-card-header>
          <mat-card-content>
            <div class="kpi-grid">
              <div class="kpi-item">
                <mat-icon class="kpi-icon employee">people</mat-icon>
                <div class="kpi-content">
                  <div class="kpi-value">{{ dashboardData()?.totalEmployees }}</div>
                  <div class="kpi-label">Total Employees</div>
                </div>
              </div>
              <div class="kpi-item">
                <mat-icon class="kpi-icon success">verified_user</mat-icon>
                <div class="kpi-content">
                  <div class="kpi-value">{{ dashboardData()?.activeEmployees }}</div>
                  <div class="kpi-label">Active Employees</div>
                </div>
              </div>
              <div class="kpi-item">
                <mat-icon class="kpi-icon department">business</mat-icon>
                <div class="kpi-content">
                  <div class="kpi-value">{{ dashboardData()?.totalDepartments }}</div>
                  <div class="kpi-label">Departments</div>
                </div>
              </div>
              <div class="kpi-item">
                <mat-icon class="kpi-icon warning">pending_actions</mat-icon>
                <div class="kpi-content">
                  <div class="kpi-value">{{ dashboardData()?.pendingLeaveRequests }}</div>
                  <div class="kpi-label">Pending Leaves</div>
                </div>
              </div>
              <div class="kpi-item">
                <mat-icon class="kpi-icon attendance">event_available</mat-icon>
                <div class="kpi-content">
                  <div class="kpi-value">{{ dashboardData()?.todayAttendance }}%</div>
                  <div class="kpi-label">Today's Attendance</div>
                </div>
              </div>
              <div class="kpi-item">
                <mat-icon class="kpi-icon payroll">payments</mat-icon>
                <div class="kpi-content">
                  <div class="kpi-value">Rs {{ (dashboardData()?.monthlyPayroll || 0) | number:'1.0-0' }}</div>
                  <div class="kpi-label">Monthly Payroll</div>
                </div>
              </div>
            </div>
          </mat-card-content>
          <mat-card-actions>
            <button mat-button (click)="loadDashboardData()" [disabled]="isLoadingDashboard()">
              <mat-icon>refresh</mat-icon> Refresh
            </button>
          </mat-card-actions>
        </mat-card>
      }
    
      <!-- Loading State for Dashboard -->
      @if (isLoadingDashboard() && !dashboardData()) {
        <mat-card class="loading-card">
          <mat-card-content>
            <app-progress-spinner size="medium" color="primary"></app-progress-spinner>
            <p>Loading dashboard data...</p>
          </mat-card-content>
        </mat-card>
      }
    
      <app-divider class="section-divider" />
    
      <!-- Report Generation Section -->
      <h2 class="section-title">Generate Reports</h2>

      <app-expansion-panel-group class="reports-accordion">

        <!-- Payroll Reports -->
        <app-expansion-panel>
          <div panel-title>
            <mat-icon class="panel-icon payroll">payments</mat-icon>
            Payroll Reports - Salary summaries, deductions, and bank transfers
          </div>
    
          <div class="report-controls">
            <mat-form-field appearance="outline">
              <mat-label>Month</mat-label>
              <mat-select [(ngModel)]="payrollMonth">
                @for (month of months; track month) {
                  <mat-option [value]="month.value">
                    {{ month.label }}
                  </mat-option>
                }
              </mat-select>
            </mat-form-field>
    
            <mat-form-field appearance="outline">
              <mat-label>Year</mat-label>
              <mat-select [(ngModel)]="payrollYear">
                @for (year of years; track year) {
                  <mat-option [value]="year">
                    {{ year }}
                  </mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>
    
          <div class="report-actions">
            <button mat-raised-button color="primary"
              (click)="generatePayrollReport('excel')"
              [disabled]="isLoadingPayroll()"
              appTooltip="Export monthly payroll summary to Excel">
              <mat-icon>table_chart</mat-icon>
              @if (!isLoadingPayroll()) {
                <span>Monthly Payroll Summary (Excel)</span>
              }
              @if (isLoadingPayroll()) {
                <app-progress-spinner size="small" color="primary"></app-progress-spinner>
              }
            </button>
    
            <button mat-raised-button color="accent"
              (click)="generateStatutoryDeductions()"
              [disabled]="isLoadingPayroll()"
              appTooltip="Export statutory deductions report">
              <mat-icon>account_balance</mat-icon>
              Statutory Deductions (Excel)
            </button>
    
            <button mat-raised-button
              (click)="generateBankTransferList()"
              [disabled]="isLoadingPayroll()"
              appTooltip="Export bank transfer list for payroll">
              <mat-icon>account_balance_wallet</mat-icon>
              Bank Transfer List (Excel)
            </button>
          </div>
        </app-expansion-panel>

        <!-- Attendance Reports -->
        <app-expansion-panel>
          <div panel-title>
            <mat-icon class="panel-icon attendance">event_available</mat-icon>
            Attendance Reports - Monthly registers, overtime, and attendance analysis
          </div>
    
          <div class="report-controls">
            <mat-form-field appearance="outline">
              <mat-label>Month</mat-label>
              <mat-select [(ngModel)]="attendanceMonth">
                @for (month of months; track month) {
                  <mat-option [value]="month.value">
                    {{ month.label }}
                  </mat-option>
                }
              </mat-select>
            </mat-form-field>
    
            <mat-form-field appearance="outline">
              <mat-label>Year</mat-label>
              <mat-select [(ngModel)]="attendanceYear">
                @for (year of years; track year) {
                  <mat-option [value]="year">
                    {{ year }}
                  </mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>
    
          <div class="report-actions">
            <button mat-raised-button color="primary"
              (click)="generateAttendanceReport('excel')"
              [disabled]="isLoadingAttendance()"
              appTooltip="Export monthly attendance register">
              <mat-icon>table_chart</mat-icon>
              @if (!isLoadingAttendance()) {
                <span>Monthly Attendance Register (Excel)</span>
              }
              @if (isLoadingAttendance()) {
                <app-progress-spinner size="small" color="primary"></app-progress-spinner>
              }
            </button>
    
            <button mat-raised-button color="accent"
              (click)="generateOvertimeReport()"
              [disabled]="isLoadingAttendance()"
              appTooltip="Export overtime report">
              <mat-icon>schedule</mat-icon>
              Overtime Report (Excel)
            </button>
          </div>
        </app-expansion-panel>

        <!-- Leave Reports -->
        <app-expansion-panel>
          <div panel-title>
            <mat-icon class="panel-icon leave">beach_access</mat-icon>
            Leave Reports - Leave balances and utilization reports
          </div>
    
          <div class="report-controls">
            <mat-form-field appearance="outline">
              <mat-label>Year</mat-label>
              <mat-select [(ngModel)]="leaveYear">
                @for (year of years; track year) {
                  <mat-option [value]="year">
                    {{ year }}
                  </mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>
    
          <div class="report-actions">
            <button mat-raised-button color="primary"
              (click)="generateLeaveReport('excel')"
              [disabled]="isLoadingLeave()"
              appTooltip="Export leave balance report">
              <mat-icon>table_chart</mat-icon>
              @if (!isLoadingLeave()) {
                <span>Leave Balance Report (Excel)</span>
              }
              @if (isLoadingLeave()) {
                <app-progress-spinner size="small" color="primary"></app-progress-spinner>
              }
            </button>
          </div>
        </app-expansion-panel>

        <!-- Employee Reports -->
        <app-expansion-panel>
          <div panel-title>
            <mat-icon class="panel-icon employee">people</mat-icon>
            Employee Reports - Headcount, demographics, and expatriate tracking
          </div>
    
          <div class="report-actions">
            <button mat-raised-button color="primary"
              (click)="generateEmployeeReport('headcount')"
              [disabled]="isLoadingEmployee()"
              appTooltip="Export headcount and demographics report">
              <mat-icon>table_chart</mat-icon>
              @if (!isLoadingEmployee()) {
                <span>Headcount Report (Excel)</span>
              }
              @if (isLoadingEmployee()) {
                <app-progress-spinner size="small" color="primary"></app-progress-spinner>
              }
            </button>
    
            <button mat-raised-button color="accent"
              (click)="generateEmployeeReport('expatriates')"
              [disabled]="isLoadingEmployee()"
              appTooltip="Export expatriate tracking report">
              <mat-icon>flight_takeoff</mat-icon>
              Expatriate Report (Excel)
            </button>
          </div>
        </app-expansion-panel>

      </app-expansion-panel-group>
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

    /* KPI Card Styles */
    .kpi-card {
      margin-bottom: 32px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .kpi-card .header-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      margin-right: 12px;
      color: white;
    }

    .kpi-card mat-card-title {
      color: white;
      font-size: 20px;
      font-weight: 600;
    }

    .kpi-card mat-card-subtitle {
      color: rgba(255, 255, 255, 0.8);
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 20px;
      margin-top: 20px;
    }

    .kpi-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      background: rgba(255, 255, 255, 0.1);
      border-radius: 8px;
      backdrop-filter: blur(10px);
      transition: all 0.3s ease;
    }

    .kpi-item:hover {
      background: rgba(255, 255, 255, 0.15);
      transform: translateY(-2px);
    }

    .kpi-icon {
      font-size: 40px;
      width: 40px;
      height: 40px;
      opacity: 0.9;
    }

    .kpi-icon.employee { color: #fff59d; }
    .kpi-icon.success { color: #81c784; }
    .kpi-icon.department { color: #90caf9; }
    .kpi-icon.warning { color: #ffcc80; }
    .kpi-icon.attendance { color: #a5d6a7; }
    .kpi-icon.payroll { color: #ffab91; }

    .kpi-content {
      flex: 1;
    }

    .kpi-value {
      font-size: 28px;
      font-weight: 700;
      line-height: 1;
      margin-bottom: 4px;
      color: white;
    }

    .kpi-label {
      font-size: 12px;
      opacity: 0.9;
      color: rgba(255, 255, 255, 0.9);
    }

    .loading-card {
      text-align: center;
      padding: 40px;
      margin-bottom: 32px;
    }

    .loading-card app-progress-spinner {
      margin: 0 auto 16px;
    }

    .section-divider {
      margin: 40px 0;
    }

    .section-title {
      font-size: 24px;
      font-weight: 500;
      color: #1a237e;
      margin: 0 0 24px 0;
    }

    /* Accordion Styles */
    .reports-accordion {
      margin-bottom: 32px;
    }

    .reports-accordion .mat-expansion-panel {
      margin-bottom: 16px;
    }

    .panel-icon {
      margin-right: 12px;
      font-size: 24px;
      width: 24px;
      height: 24px;
    }

    .panel-icon.payroll { color: #ff9800; }
    .panel-icon.attendance { color: #4caf50; }
    .panel-icon.leave { color: #2196f3; }
    .panel-icon.employee { color: #9c27b0; }

    /* Report Controls */
    .report-controls {
      display: flex;
      gap: 16px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }

    .report-controls mat-form-field {
      min-width: 150px;
    }

    /* Report Actions */
    .report-actions {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }

    .report-actions button {
      min-width: 200px;
    }

    .report-actions button mat-icon {
      margin-right: 8px;
    }

    .report-actions button app-progress-spinner {
      display: inline-block;
      margin-right: 8px;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .reports-dashboard {
        padding: 16px;
      }

      .kpi-grid {
        grid-template-columns: 1fr;
      }

      .report-controls {
        flex-direction: column;
      }

      .report-controls mat-form-field {
        width: 100%;
      }

      .report-actions {
        flex-direction: column;
      }

      .report-actions button {
        width: 100%;
      }
    }
  `]
})
export class ReportsDashboardComponent implements OnInit {
  // Signals for reactive state management
  dashboardData = signal<DashboardSummaryDto | null>(null);
  isLoadingDashboard = signal(false);
  isLoadingPayroll = signal(false);
  isLoadingAttendance = signal(false);
  isLoadingLeave = signal(false);
  isLoadingEmployee = signal(false);

  // Date selection properties
  currentDate = new Date();
  payrollMonth = this.currentDate.getMonth() + 1;
  payrollYear = this.currentDate.getFullYear();
  attendanceMonth = this.currentDate.getMonth() + 1;
  attendanceYear = this.currentDate.getFullYear();
  leaveYear = this.currentDate.getFullYear();

  // Month and year options
  months = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' }
  ];

  years: number[] = [];

  constructor(
    private reportsService: ReportsService,
    private toastService: ToastService
  ) {
    // Generate year options (current year and previous 5 years)
    const currentYear = this.currentDate.getFullYear();
    for (let i = 0; i <= 5; i++) {
      this.years.push(currentYear - i);
    }
  }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  // ==================== DASHBOARD KPIs ====================

  /**
   * Load dashboard summary data with KPIs
   */
  loadDashboardData(): void {
    this.isLoadingDashboard.set(true);
    this.reportsService.getDashboard().subscribe({
      next: (data: DashboardSummaryDto) => {
        this.dashboardData.set(data);
        this.isLoadingDashboard.set(false);
      },
      error: (error: any) => {
        console.error('Error loading dashboard data:', error);
        this.showError('Failed to load dashboard data');
        this.isLoadingDashboard.set(false);
      }
    });
  }

  // ==================== PAYROLL REPORTS ====================

  /**
   * Generate payroll report (Excel/PDF)
   */
  generatePayrollReport(format: 'excel' | 'pdf'): void {
    if (format === 'excel') {
      this.isLoadingPayroll.set(true);
      this.reportsService.exportMonthlyPayroll(this.payrollMonth, this.payrollYear).subscribe({
        next: (blob: Blob) => {
          const filename = `Payroll_Report_${this.getMonthName(this.payrollMonth)}_${this.payrollYear}.xlsx`;
          this.reportsService.downloadFile(blob, filename);
          this.showSuccess(`Payroll report downloaded successfully`);
          this.isLoadingPayroll.set(false);
        },
        error: (error: any) => {
          console.error('Error generating payroll report:', error);
          this.showError('Failed to generate payroll report');
          this.isLoadingPayroll.set(false);
        }
      });
    }
  }

  /**
   * Generate statutory deductions report
   */
  generateStatutoryDeductions(): void {
    this.isLoadingPayroll.set(true);
    this.reportsService.exportStatutoryDeductions(this.payrollMonth, this.payrollYear).subscribe({
      next: (blob: Blob) => {
        const filename = `Statutory_Deductions_${this.getMonthName(this.payrollMonth)}_${this.payrollYear}.xlsx`;
        this.reportsService.downloadFile(blob, filename);
        this.showSuccess(`Statutory deductions report downloaded successfully`);
        this.isLoadingPayroll.set(false);
      },
      error: (error: any) => {
        console.error('Error generating statutory deductions report:', error);
        this.showError('Failed to generate statutory deductions report');
        this.isLoadingPayroll.set(false);
      }
    });
  }

  /**
   * Generate bank transfer list
   */
  generateBankTransferList(): void {
    this.isLoadingPayroll.set(true);
    this.reportsService.exportBankTransferList(this.payrollMonth, this.payrollYear).subscribe({
      next: (blob: Blob) => {
        const filename = `Bank_Transfer_List_${this.getMonthName(this.payrollMonth)}_${this.payrollYear}.xlsx`;
        this.reportsService.downloadFile(blob, filename);
        this.showSuccess(`Bank transfer list downloaded successfully`);
        this.isLoadingPayroll.set(false);
      },
      error: (error: any) => {
        console.error('Error generating bank transfer list:', error);
        this.showError('Failed to generate bank transfer list');
        this.isLoadingPayroll.set(false);
      }
    });
  }

  // ==================== ATTENDANCE REPORTS ====================

  /**
   * Generate attendance report (Excel/PDF)
   */
  generateAttendanceReport(format: 'excel' | 'pdf'): void {
    if (format === 'excel') {
      this.isLoadingAttendance.set(true);
      this.reportsService.exportAttendanceRegister(this.attendanceMonth, this.attendanceYear).subscribe({
        next: (blob: Blob) => {
          const filename = `Attendance_Register_${this.getMonthName(this.attendanceMonth)}_${this.attendanceYear}.xlsx`;
          this.reportsService.downloadFile(blob, filename);
          this.showSuccess(`Attendance report downloaded successfully`);
          this.isLoadingAttendance.set(false);
        },
        error: (error: any) => {
          console.error('Error generating attendance report:', error);
          this.showError('Failed to generate attendance report');
          this.isLoadingAttendance.set(false);
        }
      });
    }
  }

  /**
   * Generate overtime report
   */
  generateOvertimeReport(): void {
    this.isLoadingAttendance.set(true);
    this.reportsService.exportOvertimeReport(this.attendanceMonth, this.attendanceYear).subscribe({
      next: (blob: Blob) => {
        const filename = `Overtime_Report_${this.getMonthName(this.attendanceMonth)}_${this.attendanceYear}.xlsx`;
        this.reportsService.downloadFile(blob, filename);
        this.showSuccess(`Overtime report downloaded successfully`);
        this.isLoadingAttendance.set(false);
      },
      error: (error: any) => {
        console.error('Error generating overtime report:', error);
        this.showError('Failed to generate overtime report');
        this.isLoadingAttendance.set(false);
      }
    });
  }

  // ==================== LEAVE REPORTS ====================

  /**
   * Generate leave report (Excel/PDF)
   */
  generateLeaveReport(format: 'excel' | 'pdf'): void {
    if (format === 'excel') {
      this.isLoadingLeave.set(true);
      this.reportsService.exportLeaveBalance(this.leaveYear).subscribe({
        next: (blob: Blob) => {
          const filename = `Leave_Balance_Report_${this.leaveYear}.xlsx`;
          this.reportsService.downloadFile(blob, filename);
          this.showSuccess(`Leave report downloaded successfully`);
          this.isLoadingLeave.set(false);
        },
        error: (error: any) => {
          console.error('Error generating leave report:', error);
          this.showError('Failed to generate leave report');
          this.isLoadingLeave.set(false);
        }
      });
    }
  }

  // ==================== EMPLOYEE REPORTS ====================

  /**
   * Generate employee report
   */
  generateEmployeeReport(type: 'headcount' | 'expatriates'): void {
    this.isLoadingEmployee.set(true);

    if (type === 'headcount') {
      this.reportsService.exportHeadcount().subscribe({
        next: (blob: Blob) => {
          const filename = `Headcount_Report_${new Date().toISOString().split('T')[0]}.xlsx`;
          this.reportsService.downloadFile(blob, filename);
          this.showSuccess(`Headcount report downloaded successfully`);
          this.isLoadingEmployee.set(false);
        },
        error: (error: any) => {
          console.error('Error generating headcount report:', error);
          this.showError('Failed to generate headcount report');
          this.isLoadingEmployee.set(false);
        }
      });
    } else if (type === 'expatriates') {
      this.reportsService.exportExpatriates().subscribe({
        next: (blob: Blob) => {
          const filename = `Expatriate_Report_${new Date().toISOString().split('T')[0]}.xlsx`;
          this.reportsService.downloadFile(blob, filename);
          this.showSuccess(`Expatriate report downloaded successfully`);
          this.isLoadingEmployee.set(false);
        },
        error: (error: any) => {
          console.error('Error generating expatriate report:', error);
          this.showError('Failed to generate expatriate report');
          this.isLoadingEmployee.set(false);
        }
      });
    }
  }

  // ==================== HELPER METHODS ====================

  /**
   * Get month name from month number
   */
  private getMonthName(month: number): string {
    const monthObj = this.months.find(m => m.value === month);
    return monthObj ? monthObj.label : 'Unknown';
  }

  /**
   * Show success message
   */
  private showSuccess(message: string): void {
    this.toastService.success(message, 3000);
  }

  /**
   * Show error message
   */
  private showError(message: string): void {
    this.toastService.error(message, 5000);
  }
}
