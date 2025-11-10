import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LegalHold, LegalHoldStatus } from '../../../models/legal-hold.model';
import { LegalHoldService } from '../../../services/legal-hold.service';
import { NotificationService } from '../../../services/notification.service';

@Component({
  selector: 'app-legal-hold-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    MatDialogModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './legal-hold-list.component.html',
  styleUrls: ['./legal-hold-list.component.css']
})
export class LegalHoldListComponent implements OnInit {
  legalHolds: LegalHold[] = [];
  loading = false;

  displayedColumns: string[] = [
    'caseNumber',
    'description',
    'startDate',
    'endDate',
    'status',
    'affectedCount',
    'actions'
  ];

  constructor(
    private legalHoldService: LegalHoldService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadLegalHolds();
  }

  loadLegalHolds(): void {
    this.loading = true;
    this.legalHoldService.getLegalHolds().subscribe({
      next: (holds) => {
        this.legalHolds = holds;
        this.loading = false;
      },
      error: (error) => {
        console.error('Failed to load legal holds:', error);
        this.notificationService.error('Failed to load legal holds');
        this.loading = false;
      }
    });
  }

  getStatusColor(status: LegalHoldStatus): string {
    switch (status) {
      case LegalHoldStatus.ACTIVE:
        return 'warn';
      case LegalHoldStatus.RELEASED:
        return 'primary';
      case LegalHoldStatus.EXPIRED:
        return '';
      default:
        return '';
    }
  }

  viewDetails(hold: LegalHold): void {
    console.log('View details for legal hold:', hold.id);
    this.notificationService.info('Legal hold details dialog not yet implemented');
  }

  exportEDiscovery(hold: LegalHold): void {
    console.log('Export eDiscovery for legal hold:', hold.id);
    this.notificationService.info('eDiscovery export not yet implemented');
  }

  releaseLegalHold(hold: LegalHold): void {
    if (confirm(`Are you sure you want to release legal hold "${hold.caseNumber}"?`)) {
      this.legalHoldService.releaseLegalHold(hold.id, 'Released by admin').subscribe({
        next: () => {
          this.notificationService.success('Legal hold released successfully');
          this.loadLegalHolds();
        },
        error: (error) => {
          console.error('Failed to release legal hold:', error);
          this.notificationService.error('Failed to release legal hold');
        }
      });
    }
  }
}
