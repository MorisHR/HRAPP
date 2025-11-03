export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  tenantId?: string;
  tenantName?: string;
  avatarUrl?: string;
}

export enum UserRole {
  SuperAdmin = 'SuperAdmin',
  TenantAdmin = 'TenantAdmin',
  HR = 'HR',
  Manager = 'Manager',
  Employee = 'Employee'
}

export interface LoginRequest {
  email: string;
  password: string;
  tenantId?: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
  expiresIn: number;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  token: string | null;
  loading: boolean;
}
