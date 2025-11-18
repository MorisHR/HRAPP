import { Component, OnInit, signal, inject, computed } from '@angular/core';

import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Chip, ChipColor, TooltipDirective } from '@app/shared/ui';
import { UiModule } from '../../../shared/ui/ui.module';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TableComponent, TableColumn, TableColumnDirective } from '../../../shared/ui';
import { TimesheetService } from '../../../core/services/timesheet.service';
import {
  Timesheet,
  TimesheetEntry,
  TimesheetComment,
  getStatusLabel,
  getStatusColor,
  getDayTypeLabel,
  canEditTimesheet,
  canSubmitTimesheet
} from '../../../core/models/timesheet.model';

@Component({
  selector: 'app-timesheet-detail',
  standalone: true,
  imports: [
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    Chip,
    TooltipDirective,
    UiModule,
    MatFormFieldModule,
    MatInputModule,
    TableComponent,
    TableColumnDirective
],
  templateUrl: './timesheet-detail.component.html',
  styleUrl: './timesheet-detail.component.scss'
})
export class TimesheetDetailComponent implements OnInit {
  private timesheetService = inject(TimesheetService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  timesheet = this.timesheetService.currentTimesheet;
  loading = this.timesheetService.loading;

  columns: TableColumn[] = [
    { key: 'date', label: 'Date' },
    { key: 'dayType', label: 'Type' },
    { key: 'clockIn', label: 'Clock In' },
    { key: 'clockOut', label: 'Clock Out' },
    { key: 'regular', label: 'Regular' },
    { key: 'overtime', label: 'Overtime' },
    { key: 'total', label: 'Total' },
    { key: 'notes', label: 'Notes' }
  ];

  // Computed values
  canEdit = computed(() => {
    const ts = this.timesheet();
    return ts ? canEditTimesheet(ts) : false;
  });

  canSubmit = computed(() => {
    const ts = this.timesheet();
    return ts ? canSubmitTimesheet(ts) : false;
  });

  weekdays = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadTimesheet(id);
    }
  }

  loadTimesheet(id: string): void {
    this.timesheetService.getTimesheetById(id).subscribe({
      error: (err) => {
        console.error('Failed to load timesheet:', err);
        alert('Failed to load timesheet details.');
        this.router.navigate(['/employee/timesheets']);
      }
    });
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

  getDayTypeLabel(dayType: number): string {
    return getDayTypeLabel(dayType);
  }

  getDayTypeColor(dayType: number): string {
    const colors: { [key: number]: string } = {
      1: 'regular',      // Regular
      2: 'weekend',      // Weekend
      3: 'holiday',      // Holiday
      4: 'leave',        // Sick Leave
      5: 'leave',        // Annual Leave
      6: 'leave',        // Casual Leave
      7: 'unpaid',       // Unpaid Leave
      8: 'absent'        // Absent
    };
    return colors[dayType] || 'regular';
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });
  }

  formatTime(timeStr: string | undefined): string {
    if (!timeStr) return '-';
    const date = new Date(timeStr);
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  }

  formatHours(hours: number): string {
    return this.timesheetService.formatHours(hours);
  }

  getTotalHours(entry: TimesheetEntry): number {
    return entry.regularHours + entry.overtimeHours + entry.holidayHours;
  }

  submitTimesheet(): void {
    const ts = this.timesheet();
    if (!ts || !this.canSubmit()) return;

    if (!confirm('Submit this timesheet for approval?')) return;

    this.timesheetService.submitTimesheet(ts.id).subscribe({
      next: () => {
        alert('Timesheet submitted successfully!');
        this.loadTimesheet(ts.id);
      },
      error: (err) => {
        console.error('Failed to submit timesheet:', err);
        alert('Failed to submit timesheet. Please try again.');
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/employee/timesheets']);
  }

  getTotalRegularHours(): number {
    return this.timesheet()?.totalRegularHours || 0;
  }

  getTotalOvertimeHours(): number {
    return this.timesheet()?.totalOvertimeHours || 0;
  }

  getTotalHolidayHours(): number {
    return this.timesheet()?.totalHolidayHours || 0;
  }

  getTotalLeaveHours(): number {
    const ts = this.timesheet();
    if (!ts) return 0;
    return ts.totalAnnualLeaveHours + ts.totalSickLeaveHours;
  }

  getTotalPayableHours(): number {
    return this.timesheet()?.totalPayableHours || 0;
  }

  getPeriodDisplay(): string {
    const ts = this.timesheet();
    if (!ts) return '';
    const start = new Date(ts.periodStart);
    const end = new Date(ts.periodEnd);
    return `${start.toLocaleDateString()} - ${end.toLocaleDateString()}`;
  }
}
