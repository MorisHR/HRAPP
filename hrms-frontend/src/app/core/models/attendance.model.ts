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

// Real-time punch data models
export interface PunchRecord {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeCode?: string;
  deviceId: string;
  deviceName: string;
  deviceLocation?: string;
  timestamp: string;
  punchType: PunchType;
  verificationMethod: VerificationMethod;
  verificationQuality: number; // 0-100 percentage
  temperature?: number;
  maskDetected?: boolean;
  photoUrl?: string;
  latitude?: number;
  longitude?: number;
  status: PunchStatus;
  isNew?: boolean; // Client-side flag for animation
}

export enum PunchType {
  CheckIn = 'CheckIn',
  CheckOut = 'CheckOut',
  Break = 'Break',
  BreakReturn = 'BreakReturn'
}

export enum VerificationMethod {
  Fingerprint = 'Fingerprint',
  Face = 'Face',
  Card = 'Card',
  PIN = 'PIN',
  Mobile = 'Mobile',
  Web = 'Web'
}

export enum PunchStatus {
  Valid = 'Valid',
  Late = 'Late',
  Early = 'Early',
  Invalid = 'Invalid',
  Duplicate = 'Duplicate'
}

export interface BiometricDevice {
  id: string;
  deviceId: string;
  name: string;
  location: string;
  ipAddress?: string;
  serialNumber?: string;
  model?: string;
  isOnline: boolean;
  lastSyncTime: string;
  punchCountToday: number;
  batteryLevel?: number;
  firmware?: string;
  status: DeviceStatus;
}

export enum DeviceStatus {
  Online = 'Online',
  Offline = 'Offline',
  Error = 'Error',
  Maintenance = 'Maintenance'
}

export interface LiveAttendanceStats {
  presentToday: number;
  absentToday: number;
  lateArrivals: number;
  onLeave: number;
  totalPunchesToday: number;
  averageVerificationQuality: number;
  lastUpdated: string;
}

export interface DashboardFilters {
  searchQuery?: string;
  deviceId?: string;
  verificationMethod?: VerificationMethod;
  punchType?: PunchType;
  startDate?: string;
  endDate?: string;
}
