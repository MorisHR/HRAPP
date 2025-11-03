export interface Leave {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  days: number;
  reason: string;
  status: LeaveStatus;
  appliedDate: string;
  approvedBy?: string;
  approvedDate?: string;
  rejectionReason?: string;
}

export enum LeaveType {
  Annual = 'Annual',
  Sick = 'Sick',
  Casual = 'Casual',
  Maternity = 'Maternity',
  Paternity = 'Paternity',
  Unpaid = 'Unpaid',
  CompensatoryOff = 'CompensatoryOff'
}

export enum LeaveStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Cancelled = 'Cancelled'
}

export interface LeaveBalance {
  leaveType: LeaveType;
  total: number;
  used: number;
  remaining: number;
}

export interface ApplyLeaveRequest {
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  reason: string;
}
