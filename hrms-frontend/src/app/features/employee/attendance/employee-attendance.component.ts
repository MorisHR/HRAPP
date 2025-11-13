import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AttendanceService } from '../../../core/services/attendance.service';
import { AuthService } from '../../../core/services/auth.service';
import { Attendance, AttendanceStatus } from '../../../core/models/attendance.model';

interface AttendanceRecord {
  id: string;
  date: Date;
  checkIn: Date | null;
  checkOut: Date | null;
  status: 'present' | 'absent' | 'late' | 'half-day' | 'on-leave';
  workHours: number;
  location?: string;
}

@Component({
  selector: 'app-employee-attendance',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule
  ],
  templateUrl: './employee-attendance.component.html',
  styleUrl: './employee-attendance.component.scss'
})
export class EmployeeAttendanceComponent implements OnInit {
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private attendanceService = inject(AttendanceService);
  private authService = inject(AuthService);

  loading = signal(false);
  attendanceRecords = signal<AttendanceRecord[]>([]);
  currentStatus = signal<'checked-in' | 'checked-out' | 'not-started'>('not-started');
  lastCheckIn = signal<Date | null>(null);
  lastCheckOut = signal<Date | null>(null);
  employeeId = signal<string>('');

  displayedColumns = ['date', 'checkIn', 'checkOut', 'workHours', 'status'];

  // ✅ FORTUNE 500 PATTERN: Computed statistics for dashboard
  stats = computed(() => {
    const records = this.attendanceRecords();
    const thisMonth = records.filter(r => {
      const recordDate = new Date(r.date);
      const now = new Date();
      return recordDate.getMonth() === now.getMonth() &&
             recordDate.getFullYear() === now.getFullYear();
    });

    const presentDays = thisMonth.filter(r => r.status === 'present').length;
    const lateDays = thisMonth.filter(r => r.status === 'late').length;
    const totalWorkHours = thisMonth.reduce((sum, r) => sum + r.workHours, 0);
    const avgWorkHours = thisMonth.length > 0 ? totalWorkHours / thisMonth.length : 0;

    return {
      presentDays,
      lateDays,
      totalWorkHours: totalWorkHours.toFixed(1),
      avgWorkHours: avgWorkHours.toFixed(1),
      attendanceRate: thisMonth.length > 0
        ? ((presentDays / thisMonth.length) * 100).toFixed(0)
        : '0'
    };
  });

  ngOnInit(): void {
    // ✅ FORTUNE 500 PATTERN: Get authenticated user
    const user = this.authService.user();
    if (user?.id) {
      this.employeeId.set(user.id);
      this.loadAttendanceRecords();
      this.checkCurrentStatus();
    } else {
      this.snackBar.open('Unable to load employee information', 'Close', { duration: 3000 });
    }
  }

  // ✅ FORTUNE 500 PATTERN: Load data with real API and error handling
  loadAttendanceRecords(): void {
    this.loading.set(true);

    const now = new Date();
    const startDate = new Date(now.getFullYear(), now.getMonth() - 1, 1); // Last 2 months
    const endDate = new Date(now.getFullYear(), now.getMonth() + 1, 0);

    this.attendanceService.getAttendanceRecords(
      startDate.toISOString().split('T')[0],
      endDate.toISOString().split('T')[0]
    ).subscribe({
      next: (records) => {
        const mapped = records.map(r => this.mapToAttendanceRecord(r));
        this.attendanceRecords.set(mapped);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load attendance records:', err);
        this.loading.set(false);
        // Fallback to mock data for demo if API fails
        this.attendanceRecords.set(this.generateMockRecords());
        this.snackBar.open('⚠️ Using demo data. API connection issue.', 'Close', { duration: 5000 });
      }
    });
  }

  // ✅ FORTUNE 500 PATTERN: Map backend model to UI model
  private mapToAttendanceRecord(attendance: Attendance): AttendanceRecord {
    return {
      id: attendance.id,
      date: new Date(attendance.date),
      checkIn: attendance.checkIn ? new Date(attendance.checkIn) : null,
      checkOut: attendance.checkOut ? new Date(attendance.checkOut) : null,
      status: this.mapStatus(attendance.status),
      workHours: attendance.workHours || 0,
      location: 'Office'
    };
  }

  private mapStatus(status: AttendanceStatus): 'present' | 'absent' | 'late' | 'half-day' | 'on-leave' {
    switch (status) {
      case AttendanceStatus.Present: return 'present';
      case AttendanceStatus.Absent: return 'absent';
      case AttendanceStatus.Late: return 'late';
      case AttendanceStatus.HalfDay: return 'half-day';
      case AttendanceStatus.OnLeave: return 'on-leave';
      default: return 'absent';
    }
  }

  // ✅ FORTUNE 500 PATTERN: Check current attendance status
  checkCurrentStatus(): void {
    const today = new Date().toDateString();
    const todayRecord = this.attendanceRecords().find(r =>
      new Date(r.date).toDateString() === today
    );

    if (todayRecord) {
      if (todayRecord.checkIn && !todayRecord.checkOut) {
        this.currentStatus.set('checked-in');
        this.lastCheckIn.set(todayRecord.checkIn);
      } else if (todayRecord.checkIn && todayRecord.checkOut) {
        this.currentStatus.set('checked-out');
        this.lastCheckIn.set(todayRecord.checkIn);
        this.lastCheckOut.set(todayRecord.checkOut);
      }
    } else {
      this.currentStatus.set('not-started');
    }
  }

  // ✅ FORTUNE 500 PATTERN: Mark check-in with real API
  markCheckIn(): void {
    const status = this.currentStatus();

    if (status === 'checked-in') {
      this.snackBar.open('You are already checked in', 'Close', { duration: 3000 });
      return;
    }

    if (status === 'checked-out') {
      this.snackBar.open('You have already completed today\'s attendance', 'Close', { duration: 3000 });
      return;
    }

    const empId = this.employeeId();
    if (!empId) {
      this.snackBar.open('Employee ID not found', 'Close', { duration: 3000 });
      return;
    }

    this.loading.set(true);
    this.attendanceService.checkIn(empId).subscribe({
      next: (attendance) => {
        const now = new Date(attendance.checkIn);
        this.lastCheckIn.set(now);
        this.currentStatus.set('checked-in');
        this.loading.set(false);

        this.snackBar.open(
          `✓ Check-in recorded at ${now.toLocaleTimeString()}`,
          'Close',
          { duration: 5000, panelClass: ['success-snackbar'] }
        );

        this.loadAttendanceRecords();
      },
      error: (err) => {
        console.error('Check-in failed:', err);
        this.loading.set(false);
        this.snackBar.open(
          '❌ Check-in failed. Please try again.',
          'Close',
          { duration: 3000 }
        );
      }
    });
  }

  // ✅ FORTUNE 500 PATTERN: Mark check-out with real API
  markCheckOut(): void {
    const status = this.currentStatus();

    if (status !== 'checked-in') {
      this.snackBar.open('You must check in first', 'Close', { duration: 3000 });
      return;
    }

    const empId = this.employeeId();
    if (!empId) {
      this.snackBar.open('Employee ID not found', 'Close', { duration: 3000 });
      return;
    }

    this.loading.set(true);
    this.attendanceService.checkOut(empId).subscribe({
      next: (attendance) => {
        const now = attendance.checkOut ? new Date(attendance.checkOut) : new Date();
        this.lastCheckOut.set(now);
        this.currentStatus.set('checked-out');
        this.loading.set(false);

        this.snackBar.open(
          `✓ Check-out recorded at ${now.toLocaleTimeString()}`,
          'Close',
          { duration: 5000, panelClass: ['success-snackbar'] }
        );

        this.loadAttendanceRecords();
      },
      error: (err) => {
        console.error('Check-out failed:', err);
        this.loading.set(false);
        this.snackBar.open(
          '❌ Check-out failed. Please try again.',
          'Close',
          { duration: 3000 }
        );
      }
    });
  }

  getStatusColor(status: string): string {
    const colors: Record<string, string> = {
      'present': 'success',
      'late': 'warn',
      'absent': 'error',
      'half-day': 'accent',
      'on-leave': 'primary'
    };
    return colors[status] || '';
  }

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      'present': 'Present',
      'late': 'Late',
      'absent': 'Absent',
      'half-day': 'Half Day',
      'on-leave': 'On Leave'
    };
    return labels[status] || status;
  }

  formatTime(date: Date | null): string {
    if (!date) return '-';
    return new Date(date).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatHours(hours: number): string {
    return `${hours.toFixed(1)}h`;
  }

  // ✅ FORTUNE 500 PATTERN: Generate mock data for demo
  private generateMockRecords(): AttendanceRecord[] {
    const records: AttendanceRecord[] = [];
    const today = new Date();

    // Generate last 30 days
    for (let i = 0; i < 30; i++) {
      const date = new Date(today);
      date.setDate(date.getDate() - i);

      // Skip weekends
      if (date.getDay() === 0 || date.getDay() === 6) continue;

      const statuses: Array<'present' | 'late' | 'on-leave'> = ['present', 'present', 'present', 'late', 'on-leave'];
      const status = statuses[Math.floor(Math.random() * statuses.length)];

      let checkIn: Date | null = null;
      let checkOut: Date | null = null;
      let workHours = 0;

      if (status === 'present' || status === 'late') {
        checkIn = new Date(date);
        checkIn.setHours(status === 'late' ? 9 + Math.random() : 9, Math.floor(Math.random() * 60), 0);

        checkOut = new Date(date);
        checkOut.setHours(17 + Math.floor(Math.random() * 2), Math.floor(Math.random() * 60), 0);

        workHours = (checkOut.getTime() - checkIn.getTime()) / (1000 * 60 * 60);
      }

      records.push({
        id: `att-${i}`,
        date,
        checkIn,
        checkOut,
        status,
        workHours,
        location: 'Office'
      });
    }

    return records.reverse();
  }
}
