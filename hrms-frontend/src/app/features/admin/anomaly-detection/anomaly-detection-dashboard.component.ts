import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { DetectedAnomaly, AnomalyStatus, AnomalyStatistics, AnomalyRiskLevel } from '../../../models/anomaly.model';
import { AnomalyDetectionService } from '../../../services/anomaly-detection.service';
import { NotificationService } from '../../../services/notification.service';

@Component({
  selector: 'app-anomaly-detection-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    MatSelectModule,
    MatFormFieldModule,
    MatDialogModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './anomaly-detection-dashboard.component.html',
  styleUrls: ['./anomaly-detection-dashboard.component.css']
})
export class AnomalyDetectionDashboardComponent implements OnInit {
  anomalies: DetectedAnomaly[] = [];
  statistics?: AnomalyStatistics;
  loading = false;
  totalCount = 0;
  pageSize = 20;
  pageNumber = 1;
  selectedStatus?: AnomalyStatus;

  displayedColumns: string[] = [
    'detectedAt',
    'anomalyType',
    'riskLevel',
    'userEmail',
    'description',
    'status',
    'actions'
  ];

  statusOptions = Object.values(AnomalyStatus);

  constructor(
    private anomalyService: AnomalyDetectionService,
    private notificationService: NotificationService,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadStatistics();
    this.loadAnomalies();
  }

  loadStatistics(): void {
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 30);
    const endDate = new Date();

    this.anomalyService.getStatistics(undefined, startDate, endDate).subscribe({
      next: (stats) => {
        this.statistics = stats;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load statistics:', error);
        this.notificationService.error('Failed to load anomaly statistics');
        this.cdr.detectChanges();
      }
    });
  }

  loadAnomalies(): void {
    this.loading = true;
    this.cdr.detectChanges();
    this.anomalyService.getAnomalies(undefined, this.selectedStatus, this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        this.anomalies = response.anomalies;
        this.totalCount = response.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load anomalies:', error);
        this.notificationService.error('Failed to load anomalies');
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadAnomalies();
  }

  onStatusFilterChange(): void {
    this.pageNumber = 1;
    this.loadAnomalies();
  }

  getRiskLevelColor(riskLevel: AnomalyRiskLevel): string {
    switch (riskLevel) {
      case AnomalyRiskLevel.CRITICAL:
      case AnomalyRiskLevel.EMERGENCY:
        return 'warn';
      case AnomalyRiskLevel.HIGH:
        return 'accent';
      case AnomalyRiskLevel.MEDIUM:
        return 'primary';
      default:
        return '';
    }
  }

  getStatusColor(status: AnomalyStatus): string {
    switch (status) {
      case AnomalyStatus.NEW:
        return 'warn';
      case AnomalyStatus.INVESTIGATING:
        return 'accent';
      case AnomalyStatus.CONFIRMED_THREAT:
        return 'warn';
      case AnomalyStatus.RESOLVED:
        return 'primary';
      case AnomalyStatus.FALSE_POSITIVE:
        return '';
      default:
        return '';
    }
  }

  viewDetails(anomaly: DetectedAnomaly): void {
    // Open details dialog
    console.log('View details for anomaly:', anomaly.id);
    this.notificationService.info('Anomaly details dialog not yet implemented');
  }

  updateStatus(anomaly: DetectedAnomaly, newStatus: AnomalyStatus): void {
    this.anomalyService.updateAnomalyStatus(anomaly.id, newStatus).subscribe({
      next: () => {
        this.notificationService.success(`Anomaly status updated to ${newStatus}`);
        this.loadAnomalies();
        this.loadStatistics();
      },
      error: (error) => {
        console.error('Failed to update anomaly status:', error);
        this.notificationService.error('Failed to update anomaly status');
      }
    });
  }

  markAsFalsePositive(anomaly: DetectedAnomaly): void {
    this.updateStatus(anomaly, AnomalyStatus.FALSE_POSITIVE);
  }

  startInvestigation(anomaly: DetectedAnomaly): void {
    this.updateStatus(anomaly, AnomalyStatus.INVESTIGATING);
  }

  resolveAnomaly(anomaly: DetectedAnomaly): void {
    this.updateStatus(anomaly, AnomalyStatus.RESOLVED);
  }
}
