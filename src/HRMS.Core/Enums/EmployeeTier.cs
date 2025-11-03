namespace HRMS.Core.Enums;

/// <summary>
/// Employee-based pricing tiers
/// Simplified pricing model based on company size
/// All tiers include full Enterprise features
/// </summary>
public enum EmployeeTier
{
    /// <summary>
    /// Tier 1: 1-50 Employees ($99/mo)
    /// 50 users, 10GB storage, 50K API calls/month
    /// </summary>
    Tier1 = 1,

    /// <summary>
    /// Tier 2: 51-100 Employees ($199/mo)
    /// 100 users, 25GB storage, 100K API calls/month
    /// </summary>
    Tier2 = 2,

    /// <summary>
    /// Tier 3: 101-200 Employees ($349/mo)
    /// 200 users, 50GB storage, 250K API calls/month
    /// </summary>
    Tier3 = 3,

    /// <summary>
    /// Tier 4: 201-500 Employees ($699/mo)
    /// 500 users, 150GB storage, 500K API calls/month
    /// </summary>
    Tier4 = 4,

    /// <summary>
    /// Tier 5: 501-1000 Employees ($1,299/mo)
    /// 1000 users, 300GB storage, 1M API calls/month
    /// </summary>
    Tier5 = 5,

    /// <summary>
    /// Custom: 1000+ Employees (Custom pricing)
    /// Negotiable limits and pricing
    /// </summary>
    Custom = 6
}
