-- ═══════════════════════════════════════════════════════════════
-- SEED TENANT DATA FOR ADMIN DASHBOARD TESTING
-- Fortune 500-grade realistic test data with diverse scenarios
-- ═══════════════════════════════════════════════════════════════

-- IMPORTANT: UUIDs must be generated fresh each time
-- TenantStatus: Pending=0, Active=1, Suspended=2, SoftDeleted=3, Expired=4, Trial=5, ExpiringSoon=6
-- EmployeeTier: Tier1=1, Tier2=2, Tier3=3, Tier4=4, Tier5=5, Custom=6

-- ───────────────────────────────────────────────────────────────
-- TENANT 1: Large Active Enterprise (Tier 5)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Mauritius Commercial Bank',
    'mcb',
    'tenant_mcb',
    'hr@mcb.mu',
    '+230-208-5000',
    1, -- Active
    5, -- Tier5 (501-1000 employees)
    701460.00, -- $1,299/mo × 12 × 45 MUR/USD
    1000, -- MaxUsers
    300, -- MaxStorageGB
    1000000, -- ApiCallsPerMonth
    847, -- CurrentUserCount (84.7% capacity)
    245, -- CurrentStorageGB
    NOW() - INTERVAL '11 months',
    NOW() + INTERVAL '1 month',
    NULL,
    'admin_mcb',
    'admin@mcb.mu',
    'Jean',
    'Baptiste',
    FALSE, -- IsGovernmentEntity
    30, -- GracePeriodDays
    FALSE, -- IsDeleted
    NOW() - INTERVAL '11 months',
    NOW() - INTERVAL '2 days'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 2: Medium Active Company (Tier 3)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Rogers Capital',
    'rogers',
    'tenant_rogers',
    'contact@rogers.mu',
    '+230-203-7000',
    1, -- Active
    3, -- Tier3 (101-200 employees)
    188460.00, -- $349/mo × 12 × 45 MUR/USD
    200, -- MaxUsers
    50, -- MaxStorageGB
    250000, -- ApiCallsPerMonth
    156, -- CurrentUserCount (78% capacity)
    38, -- CurrentStorageGB
    NOW() - INTERVAL '8 months',
    NOW() + INTERVAL '4 months',
    NULL,
    'admin_rogers',
    'admin@rogers.mu',
    'Marie',
    'Leblanc',
    FALSE, -- IsGovernmentEntity
    30, -- GracePeriodDays
    FALSE, -- IsDeleted
    NOW() - INTERVAL '8 months',
    NOW() - INTERVAL '5 days'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 3: Small Active Company (Tier 1)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Island Tech Solutions',
    'islandtech',
    'tenant_islandtech',
    'hr@islandtech.mu',
    '+230-460-1200',
    1, -- Active
    1, -- Tier1 (1-50 employees)
    53460.00, -- $99/mo × 12 × 45 MUR/USD
    50, -- MaxUsers
    10, -- MaxStorageGB
    50000, -- ApiCallsPerMonth
    32, -- CurrentUserCount (64% capacity)
    6, -- CurrentStorageGB
    NOW() - INTERVAL '5 months',
    NOW() + INTERVAL '7 months',
    NULL,
    'admin_island',
    'admin@islandtech.mu',
    'Raj',
    'Patel',
    FALSE,
    30,
    FALSE, -- IsDeleted
    NOW() - INTERVAL '5 months',
    NOW() - INTERVAL '10 days'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 4: Trial Company (Tier 2)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Coastal Logistics Ltd',
    'coastal',
    'tenant_coastal',
    'info@coastal.mu',
    '+230-234-5600',
    5, -- Trial
    2, -- Tier2 (51-100 employees)
    107460.00, -- $199/mo × 12 × 45 MUR/USD
    100, -- MaxUsers
    25, -- MaxStorageGB
    100000, -- ApiCallsPerMonth
    68, -- CurrentUserCount
    12, -- CurrentStorageGB
    NOW() - INTERVAL '20 days',
    NULL,
    NOW() + INTERVAL '10 days', -- Trial ending in 10 days
    'admin_coastal',
    'admin@coastal.mu',
    'Sophie',
    'Dubois',
    FALSE,
    30,
    FALSE, -- IsDeleted
    NOW() - INTERVAL '20 days',
    NOW() - INTERVAL '1 day'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 5: Suspended Company (Payment Failed)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "SuspensionReason", "SuspensionDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Port Louis Retail Co',
    'plretail',
    'tenant_plretail',
    'finance@plretail.mu',
    '+230-211-8900',
    2, -- Suspended
    2, -- Tier2
    107460.00,
    100,
    25,
    100000,
    72,
    18,
    NOW() - INTERVAL '9 months',
    NOW() - INTERVAL '15 days', -- Expired 15 days ago
    NULL,
    'Payment failure - card declined',
    NOW() - INTERVAL '5 days',
    'admin_plretail',
    'admin@plretail.mu',
    'David',
    'Wong',
    FALSE,
    30,
    FALSE, -- IsDeleted
    NOW() - INTERVAL '9 months',
    NOW() - INTERVAL '5 days'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 6: Large Active Enterprise Near Capacity (Tier 4)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Mauritius Telecom',
    'mtelecom',
    'tenant_mtelecom',
    'hr@mtelecom.mu',
    '+230-203-7000',
    1, -- Active
    4, -- Tier4 (201-500 employees)
    377460.00, -- $699/mo × 12 × 45 MUR/USD
    500, -- MaxUsers
    150, -- MaxStorageGB
    500000, -- ApiCallsPerMonth
    467, -- CurrentUserCount (93.4% capacity - should trigger alert!)
    138, -- CurrentStorageGB
    NOW() - INTERVAL '14 months',
    NOW() + INTERVAL '10 months',
    NULL,
    'admin_mtelecom',
    'admin@mtelecom.mu',
    'Anand',
    'Sharma',
    FALSE,
    30,
    FALSE, -- IsDeleted
    NOW() - INTERVAL '14 months',
    NOW() - INTERVAL '3 days'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 7: Trial Expiring Soon (Will convert?)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Phoenix Healthcare Group',
    'phoenix',
    'tenant_phoenix',
    'it@phoenix-health.mu',
    '+230-601-5000',
    5, -- Trial
    3, -- Tier3
    188460.00,
    200,
    50,
    250000,
    123,
    28,
    NOW() - INTERVAL '25 days',
    NULL,
    NOW() + INTERVAL '5 days', -- Trial ending in 5 days - should trigger alert!
    'admin_phoenix',
    'admin@phoenix-health.mu',
    'Priya',
    'Ramgoolam',
    FALSE,
    30,
    FALSE, -- IsDeleted
    NOW() - INTERVAL '25 days',
    NOW() - INTERVAL '2 hours'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 8: Government Entity (Active, Tier 3)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Ministry of Health & Wellness',
    'mohw',
    'tenant_mohw',
    'ict@govmu.org',
    '+230-403-3000',
    1, -- Active
    3, -- Tier3
    188460.00,
    200,
    50,
    250000,
    178,
    42,
    NOW() - INTERVAL '6 months',
    NOW() + INTERVAL '6 months',
    NULL,
    'admin_mohw',
    'admin@govmu.org',
    'Nisha',
    'Gokhool',
    TRUE, -- Government entity
    30,
    FALSE, -- IsDeleted
    NOW() - INTERVAL '6 months',
    NOW() - INTERVAL '7 days'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 9: Recently Created Active (Tier 1)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Ebene Digital Services',
    'ebene',
    'tenant_ebene',
    'contact@ebene.mu',
    '+230-467-9900',
    1, -- Active
    1, -- Tier1
    53460.00,
    50,
    10,
    50000,
    18,
    3,
    NOW() - INTERVAL '3 weeks',
    NOW() + INTERVAL '49 weeks',
    NULL,
    'admin_ebene',
    'admin@ebene.mu',
    'Kevin',
    'Li',
    FALSE,
    30,
    FALSE, -- IsDeleted
    NOW() - INTERVAL '3 weeks',
    NOW() - INTERVAL '4 hours'
);

-- ───────────────────────────────────────────────────────────────
-- TENANT 10: Mid-Size Active Growing Fast (Tier 2)
-- ───────────────────────────────────────────────────────────────
INSERT INTO master."Tenants" (
    "Id", "CompanyName", "Subdomain", "SchemaName", "ContactEmail", "ContactPhone",
    "Status", "EmployeeTier", "YearlyPriceMUR",
    "MaxUsers", "MaxStorageGB", "ApiCallsPerMonth",
    "CurrentUserCount", "CurrentStorageGB",
    "SubscriptionStartDate", "SubscriptionEndDate", "TrialEndDate",
    "AdminUserName", "AdminEmail", "AdminFirstName", "AdminLastName",
    "IsGovernmentEntity", "GracePeriodDays", "IsDeleted",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'Mauritius Investment Corp',
    'micinvest',
    'tenant_micinvest',
    'hr@micinvest.mu',
    '+230-210-1500',
    1, -- Active
    2, -- Tier2
    107460.00,
    100,
    25,
    100000,
    89, -- CurrentUserCount (89% capacity)
    20,
    NOW() - INTERVAL '7 months',
    NOW() + INTERVAL '5 months',
    NULL,
    'admin_mic',
    'admin@micinvest.mu',
    'Sarah',
    'Chen',
    FALSE,
    30,
    FALSE, -- IsDeleted
    NOW() - INTERVAL '7 months',
    NOW() - INTERVAL '1 day'
);

-- ═══════════════════════════════════════════════════════════════
-- VERIFICATION QUERIES
-- ═══════════════════════════════════════════════════════════════

-- Show summary statistics
SELECT
    COUNT(*) as total_tenants,
    COUNT(CASE WHEN "Status" = 1 THEN 1 END) as active_tenants,
    COUNT(CASE WHEN "Status" = 5 THEN 1 END) as trial_tenants,
    COUNT(CASE WHEN "Status" = 2 THEN 1 END) as suspended_tenants,
    SUM("CurrentUserCount") as total_employees,
    ROUND(AVG("YearlyPriceMUR"::numeric / 12), 2) as avg_monthly_revenue_mur,
    SUM("YearlyPriceMUR" / 12) as total_monthly_revenue_mur
FROM master."Tenants";

-- Show capacity warnings (>90% full)
SELECT
    "CompanyName",
    "CurrentUserCount",
    "MaxUsers",
    ROUND(("CurrentUserCount"::numeric / "MaxUsers") * 100, 1) as capacity_percent
FROM master."Tenants"
WHERE "Status" = 1
  AND "CurrentUserCount"::numeric / "MaxUsers" >= 0.90
ORDER BY capacity_percent DESC;

-- Show trials expiring soon (next 7 days)
SELECT
    "CompanyName",
    "TrialEndDate",
    ("TrialEndDate" - NOW())::text as time_remaining
FROM master."Tenants"
WHERE "Status" = 5
  AND "TrialEndDate" IS NOT NULL
  AND "TrialEndDate" >= NOW()
  AND "TrialEndDate" <= NOW() + INTERVAL '7 days'
ORDER BY "TrialEndDate";
