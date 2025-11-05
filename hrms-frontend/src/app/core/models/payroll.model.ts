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

// Timesheet-based Payroll Calculation Interfaces

export interface PayrollResult {
  // Employee Information
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  department: string;
  jobTitle: string;
  periodStart: string;
  periodEnd: string;

  // Basic Salary
  basicSalary: number;
  hourlyRate: number;

  // Hours Breakdown
  totalRegularHours: number;
  totalOvertimeHours: number;
  totalHolidayHours: number;
  totalLeaveHours: number;
  totalPayableHours: number;

  // Working Days
  workingDays: number;
  leaveDays: number;

  // Gross Pay Components
  regularPay: number;
  overtimePay: number;
  holidayPay: number;
  leavePay: number;

  // Allowances
  housingAllowance: number;
  transportAllowance: number;
  mealAllowance: number;
  mobileAllowance: number;
  otherAllowances: number;

  // Total Gross Salary
  totalGrossSalary: number;

  // Mauritius Statutory Deductions
  statutoryDeductions: MauritiusDeductions;

  // Other Deductions
  otherDeductions: number;
  loanDeduction: number;
  advanceDeduction: number;

  // Total Deductions
  totalDeductions: number;

  // Net Salary
  netSalary: number;

  // Timesheet References
  timesheetIds: string[];
  timesheetsProcessed: number;

  // Calculation Metadata
  calculatedAt: string;
  calculationNotes?: string;
  hasWarnings: boolean;
  warnings: string[];
}

export interface MauritiusDeductions {
  // Employee Contributions (deducted from salary)
  npF_Employee: number;
  nsF_Employee: number;
  csG_Employee: number;
  payE_Tax: number;
  totalEmployeeContributions: number;

  // Employer Contributions (recorded but not deducted from employee salary)
  npF_Employer: number;
  nsF_Employer: number;
  csG_Employer: number;
  prgF_Contribution: number;
  trainingLevy: number;
  totalEmployerContributions: number;

  // Calculation Details
  isBelowCSG_Threshold: boolean;
  csG_EmployeeRate: number;
  csG_EmployerRate: number;
  prgF_Rate: number;
  taxBracket: string;
}

export interface CalculateFromTimesheetsRequest {
  employeeId: string;
  periodStart: string;
  periodEnd: string;
}

export interface BatchCalculateFromTimesheetsRequest {
  employeeIds: string[];
  periodStart: string;
  periodEnd: string;
}

export interface BatchPayrollResult {
  results: PayrollResult[];
  totalProcessed: number;
  totalFailed: number;
  errors: string[];
  processedAt: string;
}

// Helper Functions

export function formatCurrency(amount: number): string {
  return `MUR ${amount.toLocaleString('en-MU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function formatHours(hours: number): string {
  return `${hours.toFixed(2)} hrs`;
}

export function formatPercentage(rate: number): string {
  return `${(rate * 100).toFixed(2)}%`;
}

export function calculateTaxBracketLabel(annualIncome: number): string {
  if (annualIncome <= 390000) return '0% (≤ MUR 390k)';
  if (annualIncome <= 550000) return '10% (MUR 390k - 550k)';
  if (annualIncome <= 650000) return '12% (MUR 550k - 650k)';
  return '20% (> MUR 650k)';
}
