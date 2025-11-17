#!/usr/bin/env node

/**
 * GCP COST CALCULATOR FOR FORTUNE 500 HRMS
 *
 * Calculates monthly GCP costs for hosting the Angular application
 * based on bundle size, user traffic, and Cloud CDN/Cloud Storage pricing.
 *
 * Pricing as of 2025 (subject to change):
 * - Cloud Storage: $0.020/GB/month (Standard)
 * - Cloud CDN egress: $0.08/GB (first 10TB)
 * - Origin fetch: $0.01/GB
 */

// GCP Pricing Constants (USD)
const PRICING = {
  // Cloud Storage costs
  storage: {
    perGBMonth: 0.020,           // Standard storage
    classAOps: 0.05 / 10000,     // Per 10,000 operations
    classBOps: 0.004 / 10000     // Per 10,000 operations
  },

  // Cloud CDN costs
  cdn: {
    egressTier1: 0.08,           // 0-10 TB per month
    egressTier2: 0.06,           // 10-150 TB per month
    cacheHitRatio: 0.90          // Assume 90% cache hit ratio
  },

  // Origin costs (Cloud Storage to CDN)
  origin: {
    egressPerGB: 0.01            // Internal Google Cloud egress
  }
};

/**
 * Calculate GCP costs based on application metrics
 * @param {Object} config - Configuration object
 * @param {number} config.bundleSizeBytes - Total bundle size in bytes
 * @param {number} config.monthlyUsers - Number of unique users per month
 * @param {number} config.avgSessionsPerUser - Average sessions per user per month
 * @param {number} config.cacheHitRatio - CDN cache hit ratio (0-1)
 * @returns {Object} Cost breakdown
 */
function calculateGCPCosts({
  bundleSizeBytes,
  monthlyUsers,
  avgSessionsPerUser = 20,
  cacheHitRatio = PRICING.cdn.cacheHitRatio
}) {
  // Convert bundle size to GB
  const bundleSizeGB = bundleSizeBytes / (1024 * 1024 * 1024);

  // Calculate total sessions
  const totalSessions = monthlyUsers * avgSessionsPerUser;

  // Calculate total data transfer (all sessions)
  const totalDataTransferGB = (bundleSizeBytes * totalSessions) / (1024 * 1024 * 1024);

  // CDN cache hits vs misses
  const cdnCacheHits = totalSessions * cacheHitRatio;
  const cdnCacheMisses = totalSessions * (1 - cacheHitRatio);

  // Egress from CDN to users (all sessions)
  const cdnEgressGB = totalDataTransferGB;
  const cdnEgressCost = calculateEgressCost(cdnEgressGB);

  // Origin fetches (only cache misses)
  const originFetchGB = (bundleSizeBytes * cdnCacheMisses) / (1024 * 1024 * 1024);
  const originFetchCost = originFetchGB * PRICING.origin.egressPerGB;

  // Storage costs (minimal - just for hosting static files)
  const storageCost = bundleSizeGB * PRICING.storage.perGBMonth;

  // API operations (rough estimate: 2 per session - list + get)
  const apiOperations = totalSessions * 2;
  const apiCost = (apiOperations * PRICING.storage.classBOps);

  // Total monthly cost
  const totalMonthlyCost = cdnEgressCost + originFetchCost + storageCost + apiCost;

  // Calculate cost per user
  const costPerUser = totalMonthlyCost / monthlyUsers;

  return {
    bundleSize: {
      bytes: bundleSizeBytes,
      mb: (bundleSizeBytes / (1024 * 1024)).toFixed(2),
      gb: bundleSizeGB.toFixed(4)
    },
    traffic: {
      monthlyUsers,
      avgSessionsPerUser,
      totalSessions,
      totalDataTransferGB: totalDataTransferGB.toFixed(2),
      cacheHitRatio: (cacheHitRatio * 100).toFixed(1) + '%'
    },
    costs: {
      cdn: {
        egressGB: cdnEgressGB.toFixed(2),
        cost: cdnEgressCost.toFixed(2)
      },
      origin: {
        fetchGB: originFetchGB.toFixed(2),
        cost: originFetchCost.toFixed(2)
      },
      storage: {
        sizeGB: bundleSizeGB.toFixed(4),
        cost: storageCost.toFixed(4)
      },
      api: {
        operations: apiOperations,
        cost: apiCost.toFixed(4)
      },
      total: {
        monthly: totalMonthlyCost.toFixed(2),
        yearly: (totalMonthlyCost * 12).toFixed(2),
        perUser: costPerUser.toFixed(4)
      }
    }
  };
}

/**
 * Calculate CDN egress cost with tiered pricing
 * @param {number} egressGB - Total egress in GB
 * @returns {number} Total cost
 */
function calculateEgressCost(egressGB) {
  if (egressGB <= 10 * 1024) {
    // Tier 1: 0-10 TB
    return egressGB * PRICING.cdn.egressTier1;
  } else if (egressGB <= 150 * 1024) {
    // Tier 2: 10-150 TB
    const tier1Cost = 10 * 1024 * PRICING.cdn.egressTier1;
    const tier2GB = egressGB - (10 * 1024);
    const tier2Cost = tier2GB * PRICING.cdn.egressTier2;
    return tier1Cost + tier2Cost;
  } else {
    // For simplicity, cap at tier 2
    const tier1Cost = 10 * 1024 * PRICING.cdn.egressTier1;
    const tier2GB = egressGB - (10 * 1024);
    const tier2Cost = tier2GB * PRICING.cdn.egressTier2;
    return tier1Cost + tier2Cost;
  }
}

/**
 * Compare costs between current and optimized bundle
 */
function compareBundles(currentBytes, optimizedBytes, monthlyUsers, avgSessions) {
  console.log('\n=== GCP COST COMPARISON ===\n');

  const current = calculateGCPCosts({
    bundleSizeBytes: currentBytes,
    monthlyUsers,
    avgSessionsPerUser: avgSessions
  });

  const optimized = calculateGCPCosts({
    bundleSizeBytes: optimizedBytes,
    monthlyUsers,
    avgSessionsPerUser: avgSessions
  });

  console.log('CURRENT BUNDLE (with Material Design):');
  console.log(`  Bundle Size: ${current.bundleSize.mb} MB (${current.bundleSize.gb} GB)`);
  console.log(`  Monthly Egress: ${current.costs.cdn.egressGB} GB`);
  console.log(`  Monthly Cost: $${current.costs.total.monthly}`);
  console.log(`  Yearly Cost: $${current.costs.total.yearly}`);
  console.log(`  Cost per User: $${current.costs.total.perUser}`);

  console.log('\nOPTIMIZED BUNDLE (custom UI components):');
  console.log(`  Bundle Size: ${optimized.bundleSize.mb} MB (${optimized.bundleSize.gb} GB)`);
  console.log(`  Monthly Egress: ${optimized.costs.cdn.egressGB} GB`);
  console.log(`  Monthly Cost: $${optimized.costs.total.monthly}`);
  console.log(`  Yearly Cost: $${optimized.costs.total.yearly}`);
  console.log(`  Cost per User: $${optimized.costs.total.perUser}`);

  const savingsMonthly = parseFloat(current.costs.total.monthly) - parseFloat(optimized.costs.total.monthly);
  const savingsYearly = parseFloat(current.costs.total.yearly) - parseFloat(optimized.costs.total.yearly);
  const savingsPercent = (savingsMonthly / parseFloat(current.costs.total.monthly)) * 100;

  console.log('\nSAVINGS:');
  console.log(`  Monthly: $${savingsMonthly.toFixed(2)} (${savingsPercent.toFixed(1)}%)`);
  console.log(`  Yearly: $${savingsYearly.toFixed(2)}`);
  console.log(`  5-Year TCO Reduction: $${(savingsYearly * 5).toFixed(2)}`);

  return { current, optimized, savingsMonthly, savingsYearly };
}

/**
 * Main execution
 */
if (require.main === module) {
  const args = process.argv.slice(2);

  if (args.includes('--help') || args.includes('-h')) {
    console.log(`
GCP Cost Calculator for Fortune 500 HRMS

Usage:
  node calculate-gcp-costs.js [options]

Options:
  --current <bytes>      Current bundle size in bytes
  --optimized <bytes>    Optimized bundle size in bytes
  --users <number>       Monthly active users (default: 10000)
  --sessions <number>    Avg sessions per user per month (default: 20)
  --compare              Run comparison mode
  --help, -h             Show this help message

Examples:
  # Calculate cost for current bundle
  node calculate-gcp-costs.js --current 8575923 --users 10000

  # Compare current vs optimized
  node calculate-gcp-costs.js --compare --current 8575923 --optimized 5000000 --users 10000

  # Calculate with custom session count
  node calculate-gcp-costs.js --current 8575923 --users 50000 --sessions 30
`);
    process.exit(0);
  }

  const currentBytes = parseInt(args[args.indexOf('--current') + 1] || 8575923);
  const optimizedBytes = parseInt(args[args.indexOf('--optimized') + 1] || 5000000);
  const users = parseInt(args[args.indexOf('--users') + 1] || 10000);
  const sessions = parseInt(args[args.indexOf('--sessions') + 1] || 20);

  if (args.includes('--compare')) {
    compareBundles(currentBytes, optimizedBytes, users, sessions);
  } else {
    const result = calculateGCPCosts({
      bundleSizeBytes: currentBytes,
      monthlyUsers: users,
      avgSessionsPerUser: sessions
    });

    console.log('\n=== GCP COST ANALYSIS ===\n');
    console.log('Bundle Size:', result.bundleSize.mb, 'MB');
    console.log('Monthly Users:', result.traffic.monthlyUsers.toLocaleString());
    console.log('Total Sessions:', result.traffic.totalSessions.toLocaleString());
    console.log('Cache Hit Ratio:', result.traffic.cacheHitRatio);
    console.log('\nCost Breakdown:');
    console.log('  CDN Egress:', result.costs.cdn.egressGB, 'GB → $' + result.costs.cdn.cost);
    console.log('  Origin Fetch:', result.costs.origin.fetchGB, 'GB → $' + result.costs.origin.cost);
    console.log('  Storage:', result.costs.storage.sizeGB, 'GB → $' + result.costs.storage.cost);
    console.log('  API Operations:', result.costs.api.operations.toLocaleString(), '→ $' + result.costs.api.cost);
    console.log('\nTotal Costs:');
    console.log('  Monthly: $' + result.costs.total.monthly);
    console.log('  Yearly: $' + result.costs.total.yearly);
    console.log('  Per User: $' + result.costs.total.perUser);
  }
}

module.exports = { calculateGCPCosts, compareBundles };
