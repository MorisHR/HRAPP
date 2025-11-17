#!/usr/bin/env node

/**
 * Bundle Size Tracker
 * Purpose: Track and analyze bundle sizes over time for migration progress
 * Usage: node scripts/track-bundle-size.js [--baseline] [--compare]
 */

const fs = require('fs');
const path = require('path');

// Configuration
const FRONTEND_DIR = path.join(__dirname, '../hrms-frontend');
const DIST_DIR = path.join(FRONTEND_DIR, 'dist');
const HISTORY_FILE = path.join(__dirname, '../bundle-history.json');
const REPORT_FILE = path.join(__dirname, '../bundle-size-report.txt');

// Color codes for terminal output
const colors = {
  reset: '\x1b[0m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  cyan: '\x1b[36m',
};

/**
 * Utility function to format bytes to human-readable format
 */
function formatBytes(bytes, decimals = 2) {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const dm = decimals < 0 ? 0 : decimals;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

/**
 * Print colored output
 */
function print(message, color = 'reset') {
  console.log(`${colors[color]}${message}${colors.reset}`);
}

/**
 * Print section header
 */
function printHeader(title) {
  console.log('');
  print('═'.repeat(70), 'blue');
  print(title, 'blue');
  print('═'.repeat(70), 'blue');
}

/**
 * Get file size
 */
function getFileSize(filePath) {
  try {
    const stats = fs.statSync(filePath);
    return stats.size;
  } catch (error) {
    return 0;
  }
}

/**
 * Find all JavaScript files in dist directory
 */
function findDistFiles(dir, fileList = []) {
  const files = fs.readdirSync(dir);

  files.forEach((file) => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);

    if (stat.isDirectory()) {
      findDistFiles(filePath, fileList);
    } else if (file.endsWith('.js')) {
      fileList.push({
        name: file,
        path: filePath,
        size: stat.size,
        type: categorizeFile(file),
      });
    }
  });

  return fileList;
}

/**
 * Categorize file by name
 */
function categorizeFile(filename) {
  if (filename.includes('main')) return 'main';
  if (filename.includes('polyfills')) return 'polyfills';
  if (filename.includes('runtime')) return 'runtime';
  if (filename.includes('vendor')) return 'vendor';
  if (filename.match(/^\d+\./)) return 'lazy';
  return 'other';
}

/**
 * Analyze bundle sizes
 */
function analyzeBundleSize() {
  printHeader('Bundle Size Analysis');

  // Check if dist directory exists
  if (!fs.existsSync(DIST_DIR)) {
    print('✗ Error: dist directory not found. Run build first.', 'red');
    print(`  Expected location: ${DIST_DIR}`, 'yellow');
    process.exit(1);
  }

  print('✓ Found dist directory', 'green');

  // Find all JavaScript files
  const files = findDistFiles(DIST_DIR);

  if (files.length === 0) {
    print('✗ No JavaScript files found in dist directory', 'red');
    process.exit(1);
  }

  // Calculate totals by category
  const categories = {
    main: { files: [], total: 0 },
    polyfills: { files: [], total: 0 },
    runtime: { files: [], total: 0 },
    vendor: { files: [], total: 0 },
    lazy: { files: [], total: 0 },
    other: { files: [], total: 0 },
  };

  files.forEach((file) => {
    categories[file.type].files.push(file);
    categories[file.type].total += file.size;
  });

  // Calculate total size
  const totalSize = files.reduce((sum, file) => sum + file.size, 0);

  // Create bundle data object
  const bundleData = {
    timestamp: new Date().toISOString(),
    date: new Date().toLocaleDateString(),
    time: new Date().toLocaleTimeString(),
    totalSize,
    totalSizeFormatted: formatBytes(totalSize),
    fileCount: files.length,
    categories: {},
    files: files.map((f) => ({
      name: f.name,
      size: f.size,
      sizeFormatted: formatBytes(f.size),
      type: f.type,
    })),
  };

  // Add category summaries
  Object.keys(categories).forEach((cat) => {
    bundleData.categories[cat] = {
      count: categories[cat].files.length,
      total: categories[cat].total,
      totalFormatted: formatBytes(categories[cat].total),
    };
  });

  return bundleData;
}

/**
 * Display bundle analysis
 */
function displayBundleAnalysis(data) {
  console.log('');
  print('Bundle Summary:', 'cyan');
  print(`  Total Size: ${data.totalSizeFormatted} (${data.totalSize} bytes)`, 'white');
  print(`  File Count: ${data.fileCount}`, 'white');
  print(`  Analyzed: ${data.date} ${data.time}`, 'white');

  console.log('');
  print('Size by Category:', 'cyan');

  Object.keys(data.categories).forEach((cat) => {
    const catData = data.categories[cat];
    if (catData.count > 0) {
      const percentage = ((catData.total / data.totalSize) * 100).toFixed(1);
      print(`  ${cat.padEnd(12)}: ${catData.totalFormatted.padEnd(12)} (${percentage}%) - ${catData.count} file(s)`, 'white');
    }
  });

  // Find largest files
  console.log('');
  print('Largest Files:', 'cyan');
  const largestFiles = [...data.files].sort((a, b) => b.size - a.size).slice(0, 10);

  largestFiles.forEach((file, index) => {
    print(`  ${(index + 1).toString().padStart(2)}. ${file.name.padEnd(40)} ${file.sizeFormatted.padStart(10)}`, 'white');
  });

  // Budget warnings
  console.log('');
  print('Budget Analysis:', 'cyan');

  const budgets = {
    initial: { limit: 500 * 1024, warning: 400 * 1024 }, // 500 KB limit, 400 KB warning
    total: { limit: 1024 * 1024, warning: 800 * 1024 }, // 1 MB limit, 800 KB warning
  };

  const mainSize = data.categories.main.total;

  if (mainSize > budgets.initial.limit) {
    print(`  ✗ Main bundle exceeds budget: ${formatBytes(mainSize)} > ${formatBytes(budgets.initial.limit)}`, 'red');
  } else if (mainSize > budgets.initial.warning) {
    print(`  ⚠ Main bundle approaching budget: ${formatBytes(mainSize)} (limit: ${formatBytes(budgets.initial.limit)})`, 'yellow');
  } else {
    print(`  ✓ Main bundle within budget: ${formatBytes(mainSize)} < ${formatBytes(budgets.initial.limit)}`, 'green');
  }

  if (data.totalSize > budgets.total.limit) {
    print(`  ✗ Total bundle exceeds budget: ${data.totalSizeFormatted} > ${formatBytes(budgets.total.limit)}`, 'red');
  } else if (data.totalSize > budgets.total.warning) {
    print(`  ⚠ Total bundle approaching budget: ${data.totalSizeFormatted} (limit: ${formatBytes(budgets.total.limit)})`, 'yellow');
  } else {
    print(`  ✓ Total bundle within budget: ${data.totalSizeFormatted} < ${formatBytes(budgets.total.limit)}`, 'green');
  }
}

/**
 * Load bundle history
 */
function loadHistory() {
  if (!fs.existsSync(HISTORY_FILE)) {
    return [];
  }

  try {
    const content = fs.readFileSync(HISTORY_FILE, 'utf-8');
    return JSON.parse(content);
  } catch (error) {
    print(`⚠ Warning: Could not load history file: ${error.message}`, 'yellow');
    return [];
  }
}

/**
 * Save to history
 */
function saveToHistory(data) {
  const history = loadHistory();
  history.push(data);

  // Keep only last 100 entries
  if (history.length > 100) {
    history.splice(0, history.length - 100);
  }

  fs.writeFileSync(HISTORY_FILE, JSON.stringify(history, null, 2));
  print(`✓ Saved to history: ${HISTORY_FILE}`, 'green');
}

/**
 * Compare with previous build
 */
function compareWithPrevious(currentData) {
  const history = loadHistory();

  if (history.length === 0) {
    print('ℹ No previous data for comparison', 'yellow');
    return;
  }

  const previousData = history[history.length - 1];

  printHeader('Comparison with Previous Build');

  const sizeDiff = currentData.totalSize - previousData.totalSize;
  const percentChange = ((sizeDiff / previousData.totalSize) * 100).toFixed(2);

  print(`Previous: ${previousData.totalSizeFormatted} (${previousData.date} ${previousData.time})`, 'white');
  print(`Current:  ${currentData.totalSizeFormatted} (${currentData.date} ${currentData.time})`, 'white');

  if (sizeDiff > 0) {
    print(`Change:   +${formatBytes(sizeDiff)} (+${percentChange}%)`, 'red');
  } else if (sizeDiff < 0) {
    print(`Change:   ${formatBytes(sizeDiff)} (${percentChange}%)`, 'green');
  } else {
    print(`Change:   No change`, 'white');
  }

  // Compare by category
  console.log('');
  print('Category Changes:', 'cyan');

  Object.keys(currentData.categories).forEach((cat) => {
    const current = currentData.categories[cat].total;
    const previous = previousData.categories[cat]?.total || 0;
    const diff = current - previous;

    if (diff !== 0) {
      const sign = diff > 0 ? '+' : '';
      const color = diff > 0 ? 'red' : 'green';
      print(`  ${cat.padEnd(12)}: ${sign}${formatBytes(diff)}`, color);
    }
  });
}

/**
 * Show historical trend
 */
function showHistoricalTrend() {
  const history = loadHistory();

  if (history.length < 2) {
    print('ℹ Not enough data for trend analysis (need at least 2 builds)', 'yellow');
    return;
  }

  printHeader('Historical Trend (Last 10 Builds)');

  const recentHistory = history.slice(-10);

  console.log('');
  print('Date                 Time         Total Size    Change', 'cyan');
  print('─'.repeat(70), 'cyan');

  recentHistory.forEach((entry, index) => {
    let changeStr = '         ';
    if (index > 0) {
      const diff = entry.totalSize - recentHistory[index - 1].totalSize;
      const sign = diff > 0 ? '+' : '';
      changeStr = `${sign}${formatBytes(diff)}`.padEnd(9);
    }

    print(
      `${entry.date.padEnd(20)} ${entry.time.padEnd(12)} ${entry.totalSizeFormatted.padEnd(13)} ${changeStr}`,
      'white'
    );
  });

  // Calculate overall trend
  const firstBuild = recentHistory[0];
  const lastBuild = recentHistory[recentHistory.length - 1];
  const overallDiff = lastBuild.totalSize - firstBuild.totalSize;
  const overallPercent = ((overallDiff / firstBuild.totalSize) * 100).toFixed(2);

  console.log('');
  print(`Overall Trend: ${overallDiff > 0 ? '+' : ''}${formatBytes(overallDiff)} (${overallPercent}%)`, overallDiff > 0 ? 'red' : 'green');
}

/**
 * Save report to file
 */
function saveReport(data) {
  const report = [];

  report.push('═'.repeat(70));
  report.push('Bundle Size Report');
  report.push('═'.repeat(70));
  report.push(`Generated: ${data.date} ${data.time}`);
  report.push('');
  report.push('Summary:');
  report.push(`  Total Size: ${data.totalSizeFormatted}`);
  report.push(`  File Count: ${data.fileCount}`);
  report.push('');
  report.push('Category Breakdown:');

  Object.keys(data.categories).forEach((cat) => {
    const catData = data.categories[cat];
    if (catData.count > 0) {
      const percentage = ((catData.total / data.totalSize) * 100).toFixed(1);
      report.push(`  ${cat.padEnd(12)}: ${catData.totalFormatted.padEnd(12)} (${percentage}%)`);
    }
  });

  report.push('');
  report.push('All Files:');
  data.files.forEach((file, index) => {
    report.push(`  ${(index + 1).toString().padStart(3)}. ${file.name.padEnd(45)} ${file.sizeFormatted.padStart(10)}`);
  });

  fs.writeFileSync(REPORT_FILE, report.join('\n'));
  print(`✓ Report saved: ${REPORT_FILE}`, 'green');
}

/**
 * Main execution
 */
function main() {
  const args = process.argv.slice(2);
  const isBaseline = args.includes('--baseline');
  const isCompare = args.includes('--compare');

  print('╔════════════════════════════════════════════════════════════════════╗', 'blue');
  print('║  Bundle Size Tracker - Fortune 500 Migration                      ║', 'blue');
  print('╚════════════════════════════════════════════════════════════════════╝', 'blue');

  // Analyze current bundle
  const bundleData = analyzeBundleSize();
  displayBundleAnalysis(bundleData);

  // Compare with previous if requested
  if (isCompare || (!isBaseline && loadHistory().length > 0)) {
    compareWithPrevious(bundleData);
  }

  // Show historical trend
  if (loadHistory().length >= 2) {
    showHistoricalTrend();
  }

  // Save to history
  saveToHistory(bundleData);

  // Save report
  saveReport(bundleData);

  console.log('');
  print('═'.repeat(70), 'blue');
  print('✓ Bundle size tracking complete', 'green');
  print('═'.repeat(70), 'blue');
}

// Run main function
try {
  main();
} catch (error) {
  print(`✗ Error: ${error.message}`, 'red');
  if (process.env.VERBOSE) {
    console.error(error.stack);
  }
  process.exit(1);
}
