export interface Tenant {
  id: string;
  companyName: string;
  subdomain: string;
  schemaName: string;
  contactEmail: string;
  contactPhone: string;
  status: TenantStatus;
  statusDisplay: string;
  employeeTier: string;
  employeeTierDisplay: string;
  monthlyPrice: number;
  maxUsers: number;
  currentUserCount: number;
  maxStorageGB: number;
  currentStorageGB: number;
  apiCallsPerMonth: number;
  employeeCount: number; // derived from currentUserCount
  createdAt: string;
  subscriptionStartDate: string;
  subscriptionEndDate?: string;
  trialEndDate?: string;
  suspensionReason?: string;
  suspensionDate?: string;
  softDeleteDate?: string;
  deletionReason?: string;
  daysUntilHardDelete?: number;
  adminUserName: string;
  adminEmail: string;
}

export enum IndustrySector {
  Technology = 'Technology',
  Manufacturing = 'Manufacturing',
  Healthcare = 'Healthcare',
  Finance = 'Finance',
  Retail = 'Retail',
  Education = 'Education',
  Hospitality = 'Hospitality',
  Construction = 'Construction',
  RealEstate = 'RealEstate',
  Transportation = 'Transportation',
  Energy = 'Energy',
  Telecommunications = 'Telecommunications',
  Agriculture = 'Agriculture',
  Media = 'Media',
  Consulting = 'Consulting',
  Legal = 'Legal',
  Automotive = 'Automotive',
  Aerospace = 'Aerospace',
  Pharmaceuticals = 'Pharmaceuticals',
  Insurance = 'Insurance',
  FoodBeverage = 'FoodBeverage',
  Fashion = 'Fashion',
  Sports = 'Sports',
  Entertainment = 'Entertainment',
  GovernmentPublicSector = 'GovernmentPublicSector',
  NonProfitCharity = 'NonProfitCharity',
  Logistics = 'Logistics',
  Mining = 'Mining',
  Utilities = 'Utilities',
  Other = 'Other'
}

export enum TenantStatus {
  Active = 'Active',
  Suspended = 'Suspended',
  Trial = 'Trial',
  Expired = 'Expired'
}

export interface TenantSettings {
  timezone: string;
  dateFormat: string;
  currency: string;
  workingDays: number[];
  workingHoursStart: string;
  workingHoursEnd: string;
}

export interface CreateTenantRequest {
  companyName: string;
  subdomain: string;
  contactEmail: string;
  contactPhone: string;
  employeeTier: string;
  maxUsers: number;
  maxStorageGB: number;
  apiCallsPerMonth: number;
  monthlyPrice: number;
  adminUserName: string;
  adminEmail: string;
  adminPassword: string;
}
