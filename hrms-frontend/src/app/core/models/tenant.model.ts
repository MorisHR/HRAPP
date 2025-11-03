export interface Tenant {
  id: string;
  name: string;
  domain: string;
  contactEmail: string;
  contactPhone: string;
  industry: IndustrySector;
  status: TenantStatus;
  subscriptionPlan: SubscriptionPlan;
  employeeCount: number;
  createdAt: string;
  updatedAt: string;
  settings?: TenantSettings;
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

export enum SubscriptionPlan {
  Basic = 'Basic',
  Professional = 'Professional',
  Enterprise = 'Enterprise'
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
  name: string;
  domain: string;
  contactEmail: string;
  contactPhone: string;
  industry: IndustrySector;
  subscriptionPlan: SubscriptionPlan;
}
