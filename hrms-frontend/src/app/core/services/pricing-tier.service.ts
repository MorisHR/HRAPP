import { Injectable } from '@angular/core';

export type TierType = 'tier1' | 'tier2' | 'tier3' | 'tier4' | 'tier5' | 'custom';

export interface PricingTier {
  id: TierType;
  name: string;
  employeeRange: string;
  price: number | string;
  maxUsers: number | string;
  storageGB: number | string;
  apiCallsMonth: number | string;
  features: string[];
}

@Injectable({
  providedIn: 'root'
})
export class PricingTierService {

  private readonly tiers: Record<TierType, PricingTier> = {
    tier1: {
      id: 'tier1',
      name: '1-50 Employees',
      employeeRange: '1-50',
      price: 99,
      maxUsers: 50,
      storageGB: 10,
      apiCallsMonth: 50000,
      features: ['All Enterprise Features']
    },
    tier2: {
      id: 'tier2',
      name: '51-100 Employees',
      employeeRange: '51-100',
      price: 199,
      maxUsers: 100,
      storageGB: 25,
      apiCallsMonth: 100000,
      features: ['All Enterprise Features']
    },
    tier3: {
      id: 'tier3',
      name: '101-200 Employees',
      employeeRange: '101-200',
      price: 349,
      maxUsers: 200,
      storageGB: 50,
      apiCallsMonth: 250000,
      features: ['All Enterprise Features']
    },
    tier4: {
      id: 'tier4',
      name: '201-500 Employees',
      employeeRange: '201-500',
      price: 699,
      maxUsers: 500,
      storageGB: 150,
      apiCallsMonth: 500000,
      features: ['All Enterprise Features']
    },
    tier5: {
      id: 'tier5',
      name: '501-1000 Employees',
      employeeRange: '501-1000',
      price: 1299,
      maxUsers: 1000,
      storageGB: 300,
      apiCallsMonth: 1000000,
      features: ['All Enterprise Features']
    },
    custom: {
      id: 'custom',
      name: '1000+ Employees',
      employeeRange: '1000+',
      price: 'Custom',
      maxUsers: 'Custom',
      storageGB: 'Custom',
      apiCallsMonth: 'Custom',
      features: ['All Enterprise Features', 'Dedicated Support', 'Custom SLA']
    }
  };

  private readonly enterpriseFeatures = [
    'Complete HR Management',
    'Attendance & Leave Tracking',
    'Payroll Processing',
    'Mauritius Tax Compliance',
    '30+ Industry Sectors',
    'Multi-user Access',
    'Email Support',
    '99.9% Uptime SLA'
  ];

  getTier(tierId: TierType): PricingTier {
    return this.tiers[tierId];
  }

  getAllTiers(): PricingTier[] {
    return Object.values(this.tiers);
  }

  getEnterpriseFeatures(): string[] {
    return this.enterpriseFeatures;
  }

  formatPrice(price: number | string): string {
    if (typeof price === 'number') {
      return `$${price}`;
    }
    return price;
  }

  formatStorage(storageGB: number | string): string {
    if (typeof storageGB === 'number') {
      return `${storageGB} GB`;
    }
    return storageGB;
  }

  formatApiCalls(apiCalls: number | string): string {
    if (typeof apiCalls === 'number') {
      if (apiCalls >= 1000000) {
        return `${apiCalls / 1000000}M`;
      }
      return `${apiCalls / 1000}K`;
    }
    return apiCalls;
  }
}
