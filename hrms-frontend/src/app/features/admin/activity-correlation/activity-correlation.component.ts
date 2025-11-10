import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ActivityCorrelation, TimelineEvent } from '../../../models/compliance-report.model';
import { ComplianceReportService } from '../../../services/compliance-report.service';
import { NotificationService } from '../../../services/notification.service';

@Component({
  selector: 'app-activity-correlation',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  templateUrl: './activity-correlation.component.html',
  styleUrls: ['./activity-correlation.component.css']
})
export class ActivityCorrelationComponent implements OnInit {
  loading = false;
  correlation?: ActivityCorrelation;

  userId: string = '';
  startDate: Date = new Date(new Date().setDate(new Date().getDate() - 7));
  endDate: Date = new Date();

  displayedColumns: string[] = ['timestamp', 'actionType', 'category', 'severity', 'entityType', 'success'];

  constructor(
    private complianceReportService: ComplianceReportService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {}

  generateCorrelation(): void {
    if (!this.userId) {
      this.notificationService.warning('Please enter a user ID');
      return;
    }

    this.loading = true;
    this.complianceReportService.getUserActivityCorrelation(
      this.userId,
      this.startDate,
      this.endDate
    ).subscribe({
      next: (correlation) => {
        this.correlation = correlation;
        this.loading = false;
        this.notificationService.success('Activity correlation generated successfully');
      },
      error: (error) => {
        console.error('Failed to generate activity correlation:', error);
        this.notificationService.error('Failed to generate activity correlation');
        this.loading = false;
      }
    });
  }

  getSeverityColor(severity: string): string {
    switch (severity) {
      case 'CRITICAL':
      case 'EMERGENCY':
        return 'error';
      case 'WARNING':
        return 'warn';
      default:
        return '';
    }
  }
}
