import { Component, OnInit, signal, inject, computed } from '@angular/core';

import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UiModule } from '../../../shared/ui/ui.module';
import { Chip, ChipColor, TooltipDirective } from '../../../shared/ui';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { TableComponent, TableColumn, TableColumnDirective } from '../../../shared/ui';
import { TimesheetService } from '../../../core/services/timesheet.service';
import {
  Timesheet,
  TimesheetStatus,
  getStatusLabel,
  getStatusColor,
  canSubmitTimesheet
} from '../../../core/models/timesheet.model';

@Component({
  selector: 'app-timesheet-list',
  standalone: true,
  imports: [
    RouterModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    Chip,
    UiModule,
    MatSelectModule,
    MatFormFieldModule,
    TableComponent,
    TableColumnDirective,
    TooltipDirective
],
  templateUrl: './timesheet-list.component.html',
  styleUrl: './timesheet-list.component.scss'
})
export class TimesheetListComponent implements OnInit {
  private timesheetService = inject(TimesheetService);

  timesheets = this.timesheetService.timesheets;
  loading = this.timesheetService.loading;

  selectedStatus = signal<number | null>(null);

  columns: TableColumn[] = [
    { key: 'period', label: 'Period' },
    { key: 'status', label: 'Status' },
    { key: 'totalHours', label: 'Total Hours' },
    { key: 'overtime', label: 'Overtime' },
    { key: 'submittedAt', label: 'Submitted' },
    { key: 'actions', label: 'Actions' }
  ];

  // Computed filtered timesheets
  filteredTimesheets = computed(() => {
    const allTimesheets = this.timesheets();
    const statusFilter = this.selectedStatus();

    if (!statusFilter) {
      return allTimesheets;
    }

    return allTimesheets.filter(t => t.status === statusFilter);
  });

  // Stats computed from timesheets
  stats = computed(() => {
    const timesheets = this.timesheets();
    return {
      total: timesheets.length,
      draft: timesheets.filter(t => t.status === TimesheetStatus.Draft).length,
      submitted: timesheets.filter(t => t.status === TimesheetStatus.Submitted).length,
      approved: timesheets.filter(t => t.status === TimesheetStatus.Approved).length,
      rejected: timesheets.filter(t => t.status === TimesheetStatus.Rejected).length
    };
  });

  statusOptions = [
    { value: null, label: 'All Status' },
    { value: TimesheetStatus.Draft, label: 'Draft' },
    { value: TimesheetStatus.Submitted, label: 'Submitted' },
    { value: TimesheetStatus.Approved, label: 'Approved' },
    { value: TimesheetStatus.Rejected, label: 'Rejected' }
  ];

  ngOnInit(): void {
    this.loadTimesheets();
  }

  loadTimesheets(): void {
    this.timesheetService.getMyTimesheets().subscribe({
      error: (err) => {
        console.error('Failed to load timesheets:', err);
      }
    });
  }

  getStatusLabel(status: TimesheetStatus): string {
    return getStatusLabel(status);
  }

  getStatusColor(status: TimesheetStatus): ChipColor {
    const statusColorString = getStatusColor(status);
    // Map Material color names to custom chip colors
    const colorMap: Record<string, ChipColor> = {
      'primary': 'primary',
      'accent': 'warning',
      'warn': 'error',
      'success': 'success',
      'draft': 'neutral',
      '': 'neutral'
    };
    return colorMap[statusColorString] || 'neutral';
  }

  canSubmit(timesheet: Timesheet): boolean {
    return canSubmitTimesheet(timesheet);
  }

  submitTimesheet(timesheet: Timesheet): void {
    if (!this.canSubmit(timesheet)) {
      return;
    }

    if (!confirm(`Submit timesheet for period ${this.formatPeriod(timesheet)}?`)) {
      return;
    }

    this.timesheetService.submitTimesheet(timesheet.id).subscribe({
      next: () => {
        this.loadTimesheets();
      },
      error: (err) => {
        console.error('Failed to submit timesheet:', err);
        alert('Failed to submit timesheet. Please try again.');
      }
    });
  }

  formatPeriod(timesheet: Timesheet): string {
    const start = new Date(timesheet.periodStart);
    const end = new Date(timesheet.periodEnd);
    return `${start.toLocaleDateString()} - ${end.toLocaleDateString()}`;
  }

  formatDate(date: string | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString();
  }

  formatHours(hours: number): string {
    return this.timesheetService.formatHours(hours);
  }

  getTotalHours(timesheet: Timesheet): number {
    return timesheet.totalRegularHours + timesheet.totalOvertimeHours;
  }

  onStatusFilterChange(status: number | null): void {
    this.selectedStatus.set(status);
  }
}
