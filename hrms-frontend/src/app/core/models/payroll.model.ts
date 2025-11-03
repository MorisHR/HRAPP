export interface Payroll {
  id: string;
  employeeId: string;
  employeeName: string;
  payPeriodStart: string;
  payPeriodEnd: string;
  basicSalary: number;
  allowances: number;
  deductions: number;
  netSalary: number;
  status: PayrollStatus;
  paymentDate?: string;
}

export enum PayrollStatus {
  Draft = 'Draft',
  Processed = 'Processed',
  Paid = 'Paid',
  Cancelled = 'Cancelled'
}

export interface Payslip {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeCode: string;
  department: string;
  designation: string;
  payPeriod: string;
  payDate: string;
  earnings: PayslipItem[];
  deductions: PayslipItem[];
  grossPay: number;
  totalDeductions: number;
  netPay: number;
}

export interface PayslipItem {
  name: string;
  amount: number;
}

export interface PayrollCycle {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: PayrollStatus;
  employeeCount: number;
  totalAmount: number;
}
