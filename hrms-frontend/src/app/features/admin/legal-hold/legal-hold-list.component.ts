import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Chip, ChipColor } from '@app/shared/ui';
import { UiModule } from '../../../shared/ui/ui.module';
import { TableComponent, TableColumn, TableColumnDirective, TooltipDirective } from '../../../shared/ui';
import { LegalHold, LegalHoldStatus } from '../../../models/legal-hold.model';
import { LegalHoldService } from '../../../services/legal-hold.service';
import { NotificationService } from '../../../services/notification.service';

@Component({
  selector: 'app-legal-hold-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    Chip,
    UiModule,
    TableComponent,
    TableColumnDirective,
    TooltipDirective
  ],
  templateUrl: './legal-hold-list.component.html',
  styleUrls: ['./legal-hold-list.component.css']
})
export class LegalHoldListComponent implements OnInit {
  legalHolds: LegalHold[] = [];
  loading = false;

  columns: TableColumn[] = [
    { key: 'caseNumber', label: 'Case Number' },
    { key: 'description', label: 'Description' },
    { key: 'startDate', label: 'Start Date' },
    { key: 'endDate', label: 'End Date' },
    { key: 'status', label: 'Status' },
    { key: 'affectedCount', label: 'Affected Records' },
    { key: 'actions', label: 'Actions' }
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

  getStatusColor(status: LegalHoldStatus): ChipColor {
    switch (status) {
      case LegalHoldStatus.ACTIVE:
        return 'warning';
      case LegalHoldStatus.RELEASED:
        return 'success';
      case LegalHoldStatus.EXPIRED:
        return 'neutral';
      default:
        return 'neutral';
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
