export interface Employee {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  joiningDate: string;
  status: EmployeeStatus;
  avatarUrl?: string;
  managerId?: string;
  managerName?: string;
}

export enum EmployeeStatus {
  Active = 'Active',
  OnLeave = 'OnLeave',
  Suspended = 'Suspended',
  Terminated = 'Terminated'
}

export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  joiningDate: string;
  managerId?: string;
}
