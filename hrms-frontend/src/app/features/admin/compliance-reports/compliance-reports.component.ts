import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SoxComplianceReport, GdprComplianceReport } from '../../../models/compliance-report.model';
import { ComplianceReportService } from '../../../services/compliance-report.service';
import { NotificationService } from '../../../services/notification.service';

@Component({
  selector: 'app-compliance-reports',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './compliance-reports.component.html',
  styleUrls: ['./compliance-reports.component.css']
})
export class ComplianceReportsComponent implements OnInit {
  loading = false;
  soxReport?: SoxComplianceReport;
  gdprReport?: GdprComplianceReport;

  // SOX Report Parameters
  soxStartDate: Date = new Date(new Date().setDate(new Date().getDate() - 90));
  soxEndDate: Date = new Date();

  // GDPR Report Parameters
  gdprUserId: string = '';

  constructor(
    private complianceReportService: ComplianceReportService,
    private notificationService: NotificationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {}

  generateSoxReport(): void {
    this.loading = true;
    this.cdr.detectChanges();
    this.complianceReportService.generateSoxFullReport(this.soxStartDate, this.soxEndDate).subscribe({
      next: (report) => {
        this.soxReport = report;
        this.loading = false;
        this.cdr.detectChanges();
        this.notificationService.success('SOX compliance report generated successfully');
      },
      error: (error) => {
        console.error('Failed to generate SOX report:', error);
        this.notificationService.error('Failed to generate SOX report');
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  generateGdprReport(): void {
    if (!this.gdprUserId) {
      this.notificationService.warning('Please enter a user ID');
      return;
    }

    this.loading = true;
    this.cdr.detectChanges();
    this.complianceReportService.generateRightToAccessReport(this.gdprUserId).subscribe({
      next: (report) => {
        this.gdprReport = report;
        this.loading = false;
        this.cdr.detectChanges();
        this.notificationService.success('GDPR compliance report generated successfully');
      },
      error: (error) => {
        console.error('Failed to generate GDPR report:', error);
        this.notificationService.error('Failed to generate GDPR report');
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  exportSoxReport(): void {
    this.notificationService.info('Export functionality not yet implemented');
  }

  exportGdprReport(): void {
    this.notificationService.info('Export functionality not yet implemented');
  }
}
