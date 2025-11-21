import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { UiModule } from '../../../shared/ui/ui.module';
import { ActivityCorrelation, TimelineEvent } from '../../../models/compliance-report.model';
import { ComplianceReportService } from '../../../services/compliance-report.service';
import { NotificationService } from '../../../services/notification.service';
import { TableComponent, TableColumn } from '../../../shared/ui/components/table/table';

@Component({
  selector: 'app-security-analytics',
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
    UiModule,
    TableComponent
  ],
  templateUrl: './security-analytics.component.html',
  styleUrls: ['./security-analytics.component.scss']
})
export class SecurityAnalyticsComponent implements OnInit {
  loading = signal(false);
  correlation = signal<ActivityCorrelation | undefined>(undefined);

  userId: string = '';
  startDate: Date = new Date(new Date().setDate(new Date().getDate() - 7));
  endDate: Date = new Date();

  // Table columns
  tableColumns: TableColumn[] = [
    { key: 'timestamp', label: 'Timestamp', sortable: true },
    { key: 'actionType', label: 'Action', sortable: true },
    { key: 'category', label: 'Category', sortable: true },
    { key: 'severity', label: 'Severity', sortable: true },
    { key: 'entityType', label: 'Entity Type', sortable: true },
    { key: 'success', label: 'Status', sortable: true }
  ];

  constructor(
    private complianceReportService: ComplianceReportService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {}

  generateAnalytics(): void {
    if (!this.userId) {
      this.notificationService.warning('Please enter a user ID');
      return;
    }

    this.loading.set(true);
    this.complianceReportService.getUserActivityCorrelation(
      this.userId,
      this.startDate,
      this.endDate
    ).subscribe({
      next: (correlation) => {
        this.correlation.set(correlation);
        this.loading.set(false);
        this.notificationService.success('Security analytics generated successfully');
      },
      error: (error) => {
        console.error('Failed to generate security analytics:', error);
        this.notificationService.error('Failed to generate security analytics');
        this.loading.set(false);
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
