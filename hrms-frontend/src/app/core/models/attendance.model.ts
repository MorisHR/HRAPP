export interface Attendance {
  id: string;
  employeeId: string;
  employeeName: string;
  date: string;
  checkIn: string;
  checkOut?: string;
  status: AttendanceStatus;
  workHours?: number;
  overtimeHours?: number;
  notes?: string;
}

export enum AttendanceStatus {
  Present = 'Present',
  Absent = 'Absent',
  Late = 'Late',
  HalfDay = 'HalfDay',
  OnLeave = 'OnLeave',
  Holiday = 'Holiday'
}

export interface AttendanceStats {
  totalPresent: number;
  totalAbsent: number;
  totalLate: number;
  totalOvertimeHours: number;
  attendanceRate: number;
}
