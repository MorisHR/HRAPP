export interface SubscriptionPayment {
  id: number;
  tenantId: number;
  tenantName: string;
  tenantSubdomain: string;
  amount: number;
  dueDate: string;
  paymentDate?: string;
  status: PaymentStatus;
  subscriptionTier: SubscriptionTier;
  reminderSentDate?: string;
  createdAt: string;
  updatedAt: string;
}

export enum PaymentStatus {
  Pending = 'Pending',
  Paid = 'Paid',
  Overdue = 'Overdue',
  Failed = 'Failed',
  Cancelled = 'Cancelled'
}

export enum SubscriptionTier {
  Free = 'Free',
  Basic = 'Basic',
  Professional = 'Professional',
  Enterprise = 'Enterprise'
}

export interface RecordPaymentRequest {
  paymentDate: string;
  amount: number;
  paymentMethod?: string;
  transactionId?: string;
  notes?: string;
}

export interface RevenueAnalytics {
  arr: number; // Annual Recurring Revenue
  mrr: number; // Monthly Recurring Revenue
  churnRate: number;
  ltv: number; // Customer Lifetime Value
  totalActiveSubscriptions: number;
  totalRevenue: number;
  monthlyRevenueData: MonthlyRevenueData[];
  tierDistribution: TierDistribution[];
}

export interface MonthlyRevenueData {
  month: string;
  year: number;
  revenue: number;
  subscriptionCount: number;
}

export interface TierDistribution {
  tier: SubscriptionTier;
  count: number;
  revenue: number;
}

export interface UpcomingRenewal {
  tenantId: number;
  tenantName: string;
  renewalDate: string;
  amount: number;
  tier: SubscriptionTier;
  daysUntilRenewal: number;
}

export interface TenantSubscriptionHistory {
  tenantId: number;
  tenantName: string;
  payments: SubscriptionPayment[];
  totalPaid: number;
  totalOverdue: number;
  subscriptionStartDate: string;
  currentTier: SubscriptionTier;
}

export interface SubscriptionOverview {
  totalPendingPayments: number;
  totalOverduePayments: number;
  pendingAmount: number;
  overdueAmount: number;
  recentlySuspendedTenants: SuspendedTenant[];
  upcomingRenewals: UpcomingRenewal[];
}

export interface SuspendedTenant {
  tenantId: number;
  tenantName: string;
  suspendedDate: string;
  overdueAmount: number;
  daysSuspended: number;
}

export interface PaymentReminderResponse {
  success: boolean;
  message: string;
  reminderSentDate: string;
}
