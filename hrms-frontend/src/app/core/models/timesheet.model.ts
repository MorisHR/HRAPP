// ChipColor type for UI components
export type ChipColor = 'primary' | 'success' | 'warning' | 'error' | 'neutral';

export interface Timesheet {
  id: string;
  employeeId: string;
  employeeName?: string;
  periodType: PeriodType;
  periodStart: string;
  periodEnd: string;
  totalRegularHours: number;
  totalOvertimeHours: number;
  totalHolidayHours: number;
  totalSickLeaveHours: number;
  totalAnnualLeaveHours: number;
  totalAbsentHours: number;
  totalPayableHours: number;
  status: TimesheetStatus;
  submittedAt?: string;
  submittedBy?: string;
  approvedAt?: string;
  approvedBy?: string;
  approvedByName?: string;
  rejectedAt?: string;
  rejectedBy?: string;
  rejectionReason?: string;
  isLocked: boolean;
  lockedAt?: string;
  lockedBy?: string;
  notes?: string;
  entries?: TimesheetEntry[];
  comments?: TimesheetComment[];
  createdAt: string;
  updatedAt?: string;
}

export interface TimesheetEntry {
  id: string;
  timesheetId: string;
  date: string;
  attendanceId?: string;
  clockInTime?: string;
  clockOutTime?: string;
  breakDuration: number;
  actualHours: number;
  regularHours: number;
  overtimeHours: number;
  holidayHours: number;
  sickLeaveHours: number;
  annualLeaveHours: number;
  isAbsent: boolean;
  isHoliday: boolean;
  isWeekend: boolean;
  isOnLeave: boolean;
  dayType: DayType;
  notes?: string;
}

export interface TimesheetAdjustment {
  id: string;
  timesheetEntryId: string;
  adjustmentType: AdjustmentType;
  fieldName: string;
  oldValue?: string;
  newValue?: string;
  reason: string;
  adjustedBy: string;
  adjustedByName?: string;
  adjustedAt: string;
  status: AdjustmentStatus;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: string;
  rejectionReason?: string;
}

export interface TimesheetComment {
  id: string;
  timesheetId: string;
  userId: string;
  userName: string;
  comment: string;
  commentedAt: string;
}

export enum TimesheetStatus {
  Draft = 1,
  Submitted = 2,
  Approved = 3,
  Rejected = 4,
  Locked = 5
}

export enum PeriodType {
  Weekly = 1,
  BiWeekly = 2,
  Monthly = 3
}

export enum DayType {
  Regular = 1,
  Weekend = 2,
  Holiday = 3,
  SickLeave = 4,
  AnnualLeave = 5,
  CasualLeave = 6,
  UnpaidLeave = 7,
  Absent = 8
}

export enum AdjustmentType {
  ManualCorrection = 1,
  SystemCorrection = 2,
  AttendanceUpdate = 3,
  LeaveUpdate = 4
}

export enum AdjustmentStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3
}

// DTOs for API requests
export interface GenerateTimesheetRequest {
  employeeId: string;
  periodStart: string;
  periodEnd: string;
  periodType: PeriodType;
}

export interface UpdateTimesheetEntryRequest {
  clockInTime?: string;
  clockOutTime?: string;
  breakDuration?: number;
  notes?: string;
}

export interface CreateAdjustmentRequest {
  adjustmentType: AdjustmentType;
  fieldName: string;
  oldValue?: string;
  newValue?: string;
  reason: string;
}

export interface BulkApproveRequest {
  timesheetIds: string[];
}

export interface RejectTimesheetRequest {
  reason: string;
}

export interface AddCommentRequest {
  comment: string;
}

// Stats for dashboard
export interface TimesheetStats {
  totalTimesheets: number;
  draftTimesheets: number;
  submittedTimesheets: number;
  approvedTimesheets: number;
  rejectedTimesheets: number;
  totalRegularHours: number;
  totalOvertimeHours: number;
  averageWorkHours: number;
}

// Helper functions
export function getStatusLabel(status: TimesheetStatus): string {
  switch (status) {
    case TimesheetStatus.Draft: return 'Draft';
    case TimesheetStatus.Submitted: return 'Submitted';
    case TimesheetStatus.Approved: return 'Approved';
    case TimesheetStatus.Rejected: return 'Rejected';
    case TimesheetStatus.Locked: return 'Locked';
    default: return 'Unknown';
  }
}

export function getStatusColor(status: TimesheetStatus): string {
  switch (status) {
    case TimesheetStatus.Draft: return 'gray';
    case TimesheetStatus.Submitted: return 'blue';
    case TimesheetStatus.Approved: return 'green';
    case TimesheetStatus.Rejected: return 'red';
    case TimesheetStatus.Locked: return 'purple';
    default: return 'gray';
  }
}

export function getStatusChipColor(status: TimesheetStatus): ChipColor {
  switch (status) {
    case TimesheetStatus.Draft: return 'neutral';
    case TimesheetStatus.Submitted: return 'primary';
    case TimesheetStatus.Approved: return 'success';
    case TimesheetStatus.Rejected: return 'error';
    case TimesheetStatus.Locked: return 'warning';
    default: return 'neutral';
  }
}

export function getDayTypeLabel(dayType: DayType): string {
  switch (dayType) {
    case DayType.Regular: return 'Regular';
    case DayType.Weekend: return 'Weekend';
    case DayType.Holiday: return 'Holiday';
    case DayType.SickLeave: return 'Sick Leave';
    case DayType.AnnualLeave: return 'Annual Leave';
    case DayType.CasualLeave: return 'Casual Leave';
    case DayType.UnpaidLeave: return 'Unpaid Leave';
    case DayType.Absent: return 'Absent';
    default: return 'Unknown';
  }
}

export function canEditTimesheet(timesheet: Timesheet): boolean {
  return timesheet.status === TimesheetStatus.Draft && !timesheet.isLocked;
}

export function canSubmitTimesheet(timesheet: Timesheet): boolean {
  return timesheet.status === TimesheetStatus.Draft && !timesheet.isLocked;
}

export function canApproveTimesheet(timesheet: Timesheet): boolean {
  return timesheet.status === TimesheetStatus.Submitted && !timesheet.isLocked;
}
