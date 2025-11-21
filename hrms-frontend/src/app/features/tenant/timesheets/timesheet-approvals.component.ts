import { Component, OnInit, signal, inject, computed } from '@angular/core';

import { RouterModule } from '@angular/router';
import { SelectionModel } from '@angular/cdk/collections';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Chip, ChipColor, CheckboxComponent } from '@app/shared/ui';
import { UiModule } from '../../../shared/ui/ui.module';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule, ReactiveFormsModule, FormControl } from '@angular/forms';
import { TableComponent, TableColumn, TableColumnDirective, TooltipDirective } from '../../../shared/ui';
import { TimesheetService } from '../../../core/services/timesheet.service';
import {
  Timesheet,
  getStatusLabel,
  getStatusColor
} from '../../../core/models/timesheet.model';

@Component({
  selector: 'app-timesheet-approvals',
  standalone: true,
  imports: [
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    CheckboxComponent,
    UiModule,
    MatFormFieldModule,
    MatInputModule,
    TableComponent,
    TableColumnDirective,
    TooltipDirective
],
  templateUrl: './timesheet-approvals.component.html',
  styleUrl: './timesheet-approvals.component.scss'
})
export class TimesheetApprovalsComponent implements OnInit {
  private timesheetService = inject(TimesheetService);

  pendingTimesheets = this.timesheetService.pendingApprovals;
  loading = this.timesheetService.loading;

  selection = new SelectionModel<Timesheet>(true, []);

  columns: TableColumn[] = [
    { key: 'select', label: '' },
    { key: 'employee', label: 'Employee' },
    { key: 'period', label: 'Period' },
    { key: 'totalHours', label: 'Total Hours' },
    { key: 'overtime', label: 'Overtime' },
    { key: 'submittedAt', label: 'Submitted' },
    { key: 'actions', label: 'Actions' }
  ];

  // Computed stats
  stats = computed(() => {
    const timesheets = this.pendingTimesheets();
    const totalRegular = timesheets.reduce((sum, t) => sum + t.totalRegularHours, 0);
    const totalOvertime = timesheets.reduce((sum, t) => sum + t.totalOvertimeHours, 0);

    return {
      totalPending: timesheets.length,
      totalRegularHours: totalRegular,
      totalOvertimeHours: totalOvertime,
      totalPayableHours: timesheets.reduce((sum, t) => sum + t.totalPayableHours, 0)
    };
  });

  ngOnInit(): void {
    this.loadPendingApprovals();
  }

  loadPendingApprovals(): void {
    this.timesheetService.getPendingApprovals().subscribe({
      error: (err) => {
        console.error('Failed to load pending approvals:', err);
      }
    });
  }

  isAllSelected(): boolean {
    const numSelected = this.selection.selected.length;
    const numRows = this.pendingTimesheets().length;
    return numSelected === numRows && numRows > 0;
  }

  masterToggle(): void {
    if (this.isAllSelected()) {
      this.selection.clear();
    } else {
      this.pendingTimesheets().forEach(row => this.selection.select(row));
    }
  }

  getStatusLabel(status: number): string {
    return getStatusLabel(status);
  }

  getStatusColor(status: number): ChipColor {
    const originalColor = getStatusColor(status);
    // Map Material colors to custom Chip colors
    switch (originalColor) {
      case 'primary':
        return 'primary';
      case 'accent':
        return 'success';
      case 'warn':
        return 'error';
      default:
        return 'neutral';
    }
  }

  formatPeriod(timesheet: Timesheet): string {
    const start = new Date(timesheet.periodStart);
    const end = new Date(timesheet.periodEnd);
    return `${start.toLocaleDateString()} - ${end.toLocaleDateString()}`;
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString();
  }

  formatHours(hours: number): string {
    return this.timesheetService.formatHours(hours);
  }

  getTotalHours(timesheet: Timesheet): number {
    return timesheet.totalRegularHours + timesheet.totalOvertimeHours;
  }

  approveSelected(): void {
    const selected = this.selection.selected;
    if (selected.length === 0) {
      alert('Please select at least one timesheet to approve.');
      return;
    }

    const count = selected.length;
    if (!confirm(`Approve ${count} selected timesheet(s)?`)) {
      return;
    }

    const ids = selected.map(t => t.id);
    this.timesheetService.bulkApproveTimesheets({ timesheetIds: ids }).subscribe({
      next: () => {
        alert(`${count} timesheet(s) approved successfully!`);
        this.selection.clear();
        this.loadPendingApprovals();
      },
      error: (err) => {
        console.error('Failed to approve timesheets:', err);
        alert('Failed to approve timesheets. Please try again.');
      }
    });
  }

  approveSingle(timesheet: Timesheet): void {
    if (!confirm(`Approve timesheet for ${timesheet.employeeName}?`)) {
      return;
    }

    this.timesheetService.approveTimesheet(timesheet.id).subscribe({
      next: () => {
        alert('Timesheet approved successfully!');
        this.loadPendingApprovals();
      },
      error: (err) => {
        console.error('Failed to approve timesheet:', err);
        alert('Failed to approve timesheet. Please try again.');
      }
    });
  }

  rejectSingle(timesheet: Timesheet): void {
    const reason = prompt(`Reject timesheet for ${timesheet.employeeName}?\n\nPlease provide a reason:`);

    if (!reason) {
      return;
    }

    this.timesheetService.rejectTimesheet(timesheet.id, { reason }).subscribe({
      next: () => {
        alert('Timesheet rejected.');
        this.loadPendingApprovals();
      },
      error: (err) => {
        console.error('Failed to reject timesheet:', err);
        alert('Failed to reject timesheet. Please try again.');
      }
    });
  }

  viewDetails(timesheet: Timesheet): void {
    // Navigate to detail view
    // This will need to be updated based on routing structure
    console.log('View details for timesheet:', timesheet.id);
  }
}
