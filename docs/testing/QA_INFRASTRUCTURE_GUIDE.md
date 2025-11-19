# QA Infrastructure Guide

## Fortune 500 Migration - Automated Testing & Validation

This guide provides comprehensive documentation for the automated QA infrastructure built to support the Material UI to Custom Components migration.

---

## Table of Contents

1. [Overview](#overview)
2. [Scripts](#scripts)
3. [Documentation](#documentation)
4. [Usage Examples](#usage-examples)
5. [CI/CD Integration](#cicd-integration)
6. [Troubleshooting](#troubleshooting)

---

## Overview

The QA infrastructure consists of:

- **4 Automated Scripts** - Validation, bundle analysis, rollback verification, and size tracking
- **2 Documentation Files** - Migration checklist and feature flag test scenarios
- **Automated Testing** - TypeScript compilation, unit tests, and build verification
- **Reporting** - Detailed reports with actionable insights

### Key Features

- Comprehensive migration validation
- Bundle size tracking over time
- Rollback safety verification
- Feature flag testing scenarios
- CI/CD ready
- Detailed error reporting
- Proper exit codes for automation

---

## Scripts

### 1. Migration Validation Script

**Location:** `/workspaces/HRAPP/scripts/validate-migration.sh`

**Purpose:** Comprehensive validation for the entire migration process

**Features:**
- Environment validation (Node, npm, dependencies)
- TypeScript compilation check
- Material UI import analysis
- Unit test execution
- Production build verification
- Bundle size analysis
- Feature flag configuration check
- Migration status summary
- Automated reporting

**Usage:**

```bash
# Full validation (recommended)
./scripts/validate-migration.sh

# Skip build (faster for quick checks)
./scripts/validate-migration.sh --skip-build

# Verbose output
./scripts/validate-migration.sh --verbose

# Both options
./scripts/validate-migration.sh --skip-build --verbose
```

**Exit Codes:**
- `0` - All validations passed
- `1` - One or more validations failed

**Output Files:**
- `validation-report-[timestamp].txt` - Detailed validation report

**Example Output:**

```
╔════════════════════════════════════════════════════════════╗
║  Fortune 500 Migration Validation Script                  ║
║  Comprehensive validation for Material UI migration       ║
╚════════════════════════════════════════════════════════════╝

========================================
1. Environment Validation
========================================
✓ Frontend directory exists
✓ Dependencies installed
✓ Required commands available

========================================
2. TypeScript Compilation Check
========================================
✓ TypeScript compilation successful

========================================
3. Material UI Import Analysis
========================================
Material UI Import Statistics:
  - @angular/material imports: 380
  - @angular/cdk imports: 2
  - Total: 382

Custom Component Usage:
  - Custom inputs: 202
  - Custom buttons: 354

⚠ Found 380 Material UI imports (migration in progress)

[... continues with all 9 validation steps ...]

╔════════════════════════════════════════════════════════════╗
║  ✓ ALL VALIDATIONS PASSED                                  ║
║  Migration validation successful!                          ║
╚════════════════════════════════════════════════════════════╝
```

---

### 2. Bundle Size Tracker

**Location:** `/workspaces/HRAPP/scripts/track-bundle-size.js`

**Purpose:** Track and analyze bundle sizes over time to measure migration progress

**Features:**
- Analyzes all JavaScript and CSS files in dist folder
- Categorizes files (main, vendor, polyfills, lazy-loaded)
- Tracks size history in `bundle-history.json`
- Compares with previous builds
- Shows historical trends
- Budget warnings
- Detailed file-by-file breakdown

**Usage:**

```bash
# Analyze current build (must build first)
cd hrms-frontend && npm run build
cd ..
node scripts/track-bundle-size.js

# Set as baseline measurement
node scripts/track-bundle-size.js --baseline

# Force comparison with previous
node scripts/track-bundle-size.js --compare
```

**Output Files:**
- `bundle-history.json` - Historical size data (last 100 builds)
- `bundle-size-report.txt` - Latest analysis report

**Example Output:**

```
╔════════════════════════════════════════════════════════════════════╗
║  Bundle Size Tracker - Fortune 500 Migration                      ║
╚════════════════════════════════════════════════════════════════════╝

═══════════════════════════════════════════════════════════════
Bundle Size Analysis
═══════════════════════════════════════════════════════════════
✓ Found dist directory

Bundle Summary:
  Total Size: 2.34 MB (2456064 bytes)
  File Count: 12
  Analyzed: 11/15/2025 06:00:00

Size by Category:
  main        : 456.23 KB    (18.5%) - 1 file(s)
  vendor      : 1.45 MB      (60.1%) - 1 file(s)
  polyfills   : 234.12 KB    (9.5%) - 1 file(s)
  lazy        : 287.45 KB    (11.7%) - 8 file(s)

Largest Files:
   1. vendor-ABCD1234.js                        1.45 MB
   2. main-EFGH5678.js                          456.23 KB
   3. polyfills-IJKL9012.js                     234.12 KB
   ...

Budget Analysis:
  ✓ Main bundle within budget: 456.23 KB < 500 KB
  ⚠ Total bundle approaching budget: 2.34 MB (limit: 1 MB)

═══════════════════════════════════════════════════════════════
Comparison with Previous Build
═══════════════════════════════════════════════════════════════
Previous: 2.56 MB (11/14/2025 14:30:00)
Current:  2.34 MB (11/15/2025 06:00:00)
Change:   -220 KB (-8.59%)

Category Changes:
  main        : -45 KB
  vendor      : -175 KB

═══════════════════════════════════════════════════════════════
✓ Bundle size tracking complete
═══════════════════════════════════════════════════════════════
```

---

### 3. Rollback Verification Script

**Location:** `/workspaces/HRAPP/scripts/verify-rollback.sh`

**Purpose:** Verify that Material UI components still work and rollback is safe

**Features:**
- Checks feature flag status
- Verifies Material UI imports still exist
- Validates Material module configuration
- Tests TypeScript compilation with Material UI
- Detects broken custom component imports
- Verifies Material component availability
- Tests development build
- Checks for dual implementations (feature flagged components)
- Generates rollback safety report

**Usage:**

```bash
# Basic rollback verification
./scripts/verify-rollback.sh

# Verbose output with detailed checks
./scripts/verify-rollback.sh --verbose

# Attempt to fix broken imports (future feature)
./scripts/verify-rollback.sh --fix-imports
```

**Exit Codes:**
- `0` - Rollback is safe, Material UI functional
- `1` - Issues detected, rollback may fail

**Output Files:**
- `rollback-verification-[timestamp].txt` - Rollback safety report

**Example Output:**

```
╔════════════════════════════════════════════════════════════╗
║  Rollback Verification Script                              ║
║  Verify Material UI still works after migration attempt   ║
╚════════════════════════════════════════════════════════════╝

========================================
1. Feature Flag Status Check
========================================
✓ Feature flag service found

Feature Flag Configuration:
  - Enabled flags: 2
  - Disabled flags: 3

⚠ 2 feature flag(s) still enabled

========================================
2. Material UI Import Verification
========================================
✓ Material UI package found

Material UI Import Statistics:
  - Files with Material imports: 45

✓ Found 45 file(s) with Material imports

========================================
3. Material Module Configuration
========================================
✓ Material modules configured
✓ Material UI in package.json (version: ^20.2.13)

[... continues with all 8 verification steps ...]

╔════════════════════════════════════════════════════════════╗
║  ✓ ROLLBACK VERIFICATION PASSED                            ║
║  Material UI components are functional                     ║
║  Safe to rollback by disabling feature flags               ║
╚════════════════════════════════════════════════════════════╝
```

---

### 4. Bundle Analysis Script

**Location:** `/workspaces/HRAPP/scripts/analyze-bundle.sh`

**Purpose:** Deep analysis of Angular bundle composition and recommendations

**Features:**
- Builds with production stats (if available)
- Analyzes all JavaScript and CSS files
- Attempts webpack-bundle-analyzer integration
- Dependency analysis
- Source code composition analysis
- Material vs Custom component usage tracking
- Optimization recommendations
- Generates detailed analysis report

**Usage:**

```bash
# Full analysis (builds and analyzes)
./scripts/analyze-bundle.sh

# Skip build (use existing dist folder)
./scripts/analyze-bundle.sh --skip-build

# Open visual report in browser
./scripts/analyze-bundle.sh --open-browser

# Both options
./scripts/analyze-bundle.sh --skip-build --open-browser
```

**Output Files:**
- `bundle-analysis-[timestamp].txt` - Text analysis report
- `dist/bundle-report.html` - Visual bundle analyzer report (if webpack-bundle-analyzer installed)

**Example Output:**

```
╔════════════════════════════════════════════════════════════╗
║  Bundle Analysis Script                                    ║
║  Analyze Angular bundle composition and size              ║
╚════════════════════════════════════════════════════════════╝

========================================
1. Building with Stats
========================================
ℹ Building production bundle with stats...
✓ Production build completed

========================================
2. Bundle File Analysis
========================================
✓ Found build output: dist/hrms-frontend/browser

JavaScript Bundle Files:

  main-ABCD1234.js - 456 KB
  polyfills-EFGH5678.js - 234 KB
  vendor-IJKL9012.js - 1450 KB
  ...

ℹ Total JavaScript size: 2340 KB

CSS Bundle Files:

  styles-MNOP3456.css - 72 KB

ℹ Total CSS size: 72 KB

========================================
Bundle Size Summary
========================================
  JavaScript: 2340 KB
  CSS: 72 KB
  Total: 2412 KB

⚠ Bundle size acceptable (500-1024 KB)

========================================
5. Dependency Analysis
========================================
Production Dependencies:
  - Production: 10 packages
  - Development: 13 packages
  - Total: 23 packages

⚠ Angular Material is installed (consider removing if fully migrated)

========================================
6. Source Code Analysis
========================================
Source File Statistics:
  - TypeScript files: 234
  - Test files: 45
  - HTML templates: 123
  - SCSS files: 156

Application Structure:
  - Components: 78
  - Services: 34

UI Component Usage:
  - Material UI imports: 380
  - Custom UI imports: 156

⚠ More Material imports than custom components

========================================
7. Optimization Recommendations
========================================

ℹ Bundle Optimization Tips:
✓ Lazy loading in use (12 routes)
✓ Build optimization enabled

ℹ Material UI Removal Checklist:
  1. Verify all components migrated to custom UI
  2. Run: grep -r '@angular/material' src/
  3. If no imports found, remove package:
     npm uninstall @angular/material @angular/cdk
  4. Expected bundle size reduction: ~300-500 KB

ℹ General Recommendations:
  - Implement component lazy loading
  - Use OnPush change detection strategy
  - Optimize images (WebP format, lazy loading)
  - Remove unused dependencies
  - Enable gzip/brotli compression
  - Consider code splitting for large features

╔════════════════════════════════════════════════════════════╗
║  ✓ BUNDLE ANALYSIS COMPLETE                                ║
╚════════════════════════════════════════════════════════════╝
```

---

## Documentation

### 1. Migration Checklist

**Location:** `/workspaces/HRAPP/MIGRATION_CHECKLIST.md`

**Purpose:** Step-by-step checklist for migrating each component

**Sections:**
- Pre-Migration Phase (Assessment, Preparation)
- Migration Phase (Code changes, Module updates)
- Testing Phase (Unit, Integration, Accessibility, Performance)
- Deployment Phase (Code review, Gradual rollout, Monitoring)
- Completion Phase (Cleanup, Final verification)
- Rollback Procedure
- Sign-Off Requirements
- Metrics Summary

**Usage:**
1. Copy checklist for each component being migrated
2. Fill in component-specific details
3. Follow steps in order
4. Check off each item as completed
5. Collect sign-offs from stakeholders
6. Archive completed checklists

**Key Features:**
- 12 major phases with 80+ checkpoints
- Feature flag integration guide
- Testing requirements (Unit, Visual, Accessibility, Performance, Browser)
- Gradual rollout strategy (0% → 5% → 25% → 50% → 100%)
- Rollback procedure
- Sign-off templates

---

### 2. Feature Flag Test Scenarios

**Location:** `/workspaces/HRAPP/test-scenarios/feature-flag-tests.md`

**Purpose:** Comprehensive test cases for feature flag system

**Sections:**
1. Feature Flag System Tests (3 tests)
2. Rollout Percentage Scenarios (5 tests - 0%, 5%, 25%, 50%, 100%)
3. Error Handling Scenarios (4 tests)
4. Rollback Scenarios (5 tests)
5. Performance Tests (3 tests)
6. Edge Cases (5 tests)
7. Integration Tests (3 tests)

**Total:** 28 comprehensive test scenarios

**Usage:**
1. Execute tests before each rollout phase
2. Document results in provided tables
3. Record pass/fail status
4. Track critical issues
5. Generate test summary report

**Key Test Scenarios:**

**Rollout Testing:**
- 0% - Feature completely disabled
- 5% - Canary deployment (50±20 out of 1000 users)
- 25% - Limited rollout (250±50 out of 1000 users)
- 50% - A/B testing (500±50 out of 1000 users)
- 100% - Full release (all users)

**Rollback Testing:**
- Immediate rollback (100% → 0%)
- Gradual rollback (100% → 50% → 0%)
- Rollback with active users
- Multiple rollback cycles
- Rollback verification

**Performance Testing:**
- Flag evaluation performance (< 1ms per check)
- Bundle size impact (< 5KB)
- Runtime performance impact (< 5%)

---

## Usage Examples

### Daily Development Workflow

```bash
# 1. Before starting work - verify environment
./scripts/validate-migration.sh --skip-build

# 2. After making changes - full validation
./scripts/validate-migration.sh

# 3. Check bundle size impact
cd hrms-frontend && npm run build && cd ..
node scripts/track-bundle-size.js --compare

# 4. Before committing - ensure rollback works
./scripts/verify-rollback.sh
```

### Pre-Release Validation

```bash
# 1. Full validation with verbose output
./scripts/validate-migration.sh --verbose

# 2. Deep bundle analysis
./scripts/analyze-bundle.sh

# 3. Bundle size tracking
node scripts/track-bundle-size.js

# 4. Rollback safety check
./scripts/verify-rollback.sh --verbose

# 5. Review all reports
ls -lt *.txt | head -5
```

### Migration Progress Tracking

```bash
# Weekly - track bundle size trend
node scripts/track-bundle-size.js

# Review historical data
cat bundle-history.json | jq '.[-10:]' | jq -r '.[] | "\(.date) \(.time) - \(.totalSizeFormatted)"'

# Count remaining Material imports
cd hrms-frontend
grep -r "from '@angular/material" src/ | wc -l
cd ..
```

### Rollback Testing

```bash
# 1. Disable feature flags in environment.ts
# (Set enabled: false or rolloutPercentage: 0)

# 2. Verify rollback works
./scripts/verify-rollback.sh --verbose

# 3. Run validation with Material UI
cd hrms-frontend
npm run test
npm run build
cd ..

# 4. Re-enable feature flags if rollback successful
```

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Migration Validation

on:
  pull_request:
    paths:
      - 'hrms-frontend/**'
  push:
    branches:
      - main
      - 'migrate/**'

jobs:
  validate:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: hrms-frontend/package-lock.json

      - name: Install Dependencies
        working-directory: hrms-frontend
        run: npm ci

      - name: Run Migration Validation
        run: |
          chmod +x scripts/validate-migration.sh
          ./scripts/validate-migration.sh --verbose

      - name: Track Bundle Size
        run: |
          chmod +x scripts/track-bundle-size.js
          node scripts/track-bundle-size.js

      - name: Verify Rollback Safety
        run: |
          chmod +x scripts/verify-rollback.sh
          ./scripts/verify-rollback.sh

      - name: Upload Reports
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: validation-reports
          path: |
            validation-report-*.txt
            bundle-size-report.txt
            rollback-verification-*.txt
            bundle-history.json

      - name: Comment on PR
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v6
        with:
          script: |
            const fs = require('fs');
            const report = fs.readFileSync('bundle-size-report.txt', 'utf8');
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: `## Bundle Size Report\n\`\`\`\n${report}\n\`\`\``
            });
```

### GitLab CI Example

```yaml
stages:
  - validate
  - analyze

migration_validation:
  stage: validate
  image: node:20
  script:
    - cd hrms-frontend
    - npm ci
    - cd ..
    - chmod +x scripts/validate-migration.sh
    - ./scripts/validate-migration.sh --verbose
  artifacts:
    paths:
      - validation-report-*.txt
    expire_in: 1 week

bundle_analysis:
  stage: analyze
  image: node:20
  script:
    - cd hrms-frontend
    - npm ci
    - npm run build
    - cd ..
    - chmod +x scripts/track-bundle-size.js
    - node scripts/track-bundle-size.js
  artifacts:
    paths:
      - bundle-size-report.txt
      - bundle-history.json
    expire_in: 1 month

rollback_verification:
  stage: validate
  image: node:20
  script:
    - cd hrms-frontend
    - npm ci
    - cd ..
    - chmod +x scripts/verify-rollback.sh
    - ./scripts/verify-rollback.sh
  artifacts:
    paths:
      - rollback-verification-*.txt
    expire_in: 1 week
```

---

## Troubleshooting

### Script Execution Issues

**Problem:** `permission denied` when running scripts

```bash
# Solution: Make scripts executable
chmod +x scripts/validate-migration.sh
chmod +x scripts/verify-rollback.sh
chmod +x scripts/analyze-bundle.sh
chmod +x scripts/track-bundle-size.js
```

**Problem:** `required file not found` or `\r: command not found`

```bash
# Solution: Fix line endings (Windows CRLF → Unix LF)
sed -i 's/\r$//' scripts/validate-migration.sh
sed -i 's/\r$//' scripts/verify-rollback.sh
sed -i 's/\r$//' scripts/analyze-bundle.sh
sed -i 's/\r$//' scripts/track-bundle-size.js
```

**Problem:** Scripts run but don't find dist folder

```bash
# Solution: Build first
cd hrms-frontend
npm run build -- --configuration production
cd ..
```

### Validation Failures

**Problem:** TypeScript compilation fails

```bash
# Check specific errors
cd hrms-frontend
npx tsc --noEmit --project tsconfig.app.json

# Common fixes:
# - Update imports
# - Fix type errors
# - Ensure all dependencies installed
```

**Problem:** Tests fail

```bash
# Run tests directly to see detailed output
cd hrms-frontend
npm run test

# Fix test issues:
# - Update test imports
# - Mock feature flag service
# - Fix test harnesses
```

**Problem:** Build fails

```bash
# Check build errors
cd hrms-frontend
npm run build -- --configuration production --verbose

# Common fixes:
# - Clear node_modules and reinstall
# - Clear Angular cache: rm -rf .angular/
# - Check for circular dependencies
```

### Bundle Size Issues

**Problem:** Bundle size exceeds budget

```bash
# Analyze what's taking space
./scripts/analyze-bundle.sh --open-browser

# Check for:
# - Unused dependencies
# - Material UI still installed
# - Large images/assets
# - Missing lazy loading
```

**Problem:** No size reduction after migration

```bash
# Verify Material UI removal
cd hrms-frontend
grep -r "@angular/material" src/

# If no imports found:
npm uninstall @angular/material @angular/cdk
npm run build
cd ..
node scripts/track-bundle-size.js --compare
```

### Rollback Issues

**Problem:** Material UI imports missing

```bash
# Check if Material UI was removed
cd hrms-frontend
npm list @angular/material

# If not installed:
npm install @angular/material@^20.2.13 @angular/cdk@^20.2.13

# Restore Material imports in components
# (Should be preserved with feature flag implementation)
```

**Problem:** Feature flags not working

```bash
# Check feature flag service exists
test -f hrms-frontend/src/app/core/services/feature-flag.service.ts && echo "Found" || echo "Missing"

# Check environment configuration
cat hrms-frontend/src/environments/environment.ts | grep -A 10 "featureFlags"

# Verify service is provided in app.config.ts
```

---

## Best Practices

### 1. Run Validations Frequently

- Before starting work (quick check with `--skip-build`)
- After each component migration
- Before committing code
- Before creating pull requests
- Before deploying to staging/production

### 2. Track Bundle Size Consistently

- After every build
- Before and after removing dependencies
- Weekly to track trends
- Document significant changes

### 3. Test Rollback Safety

- After implementing feature flags
- Before each rollout phase
- After completing migration
- Before removing Material UI

### 4. Document Everything

- Fill out migration checklist for each component
- Record test results in feature flag test scenarios
- Save all generated reports
- Track metrics over time

### 5. Gradual Rollout

- Start with 0-5% (internal testing)
- Increase to 25% (limited users)
- Move to 50% (A/B testing)
- Full rollout at 100%
- Monitor at each stage for 24-72 hours

---

## Metrics to Track

### Bundle Size Metrics

- Total bundle size (goal: < 1 MB)
- Main bundle size (goal: < 500 KB)
- Size reduction from Material UI removal (expect: 300-500 KB)
- Historical trend (weekly tracking)

### Migration Progress Metrics

- Total components: [count]
- Migrated components: [count]
- Migration percentage: [%]
- Material UI imports remaining: [count]
- Custom component usage: [count]

### Quality Metrics

- TypeScript compilation: Pass/Fail
- Unit test coverage: [%]
- Test pass rate: [%]
- Build success rate: [%]
- Rollback safety: Verified/Not Verified

### Performance Metrics

- Page load time: [ms]
- Time to Interactive: [ms]
- First Contentful Paint: [ms]
- Bundle load time: [ms]

---

## Support and Resources

### Documentation

- Migration Checklist: `/MIGRATION_CHECKLIST.md`
- Feature Flag Tests: `/test-scenarios/feature-flag-tests.md`
- This Guide: `/QA_INFRASTRUCTURE_GUIDE.md`

### Scripts

- Validation: `/scripts/validate-migration.sh`
- Bundle Tracker: `/scripts/track-bundle-size.js`
- Rollback Verify: `/scripts/verify-rollback.sh`
- Bundle Analyzer: `/scripts/analyze-bundle.sh`

### Generated Reports

- Validation: `validation-report-[timestamp].txt`
- Bundle Size: `bundle-size-report.txt`
- Bundle History: `bundle-history.json`
- Rollback: `rollback-verification-[timestamp].txt`
- Bundle Analysis: `bundle-analysis-[timestamp].txt`

---

## Version History

- **v1.0** (2025-11-15) - Initial QA infrastructure release
  - 4 automated scripts
  - 2 documentation files
  - Comprehensive testing scenarios
  - CI/CD integration examples

---

**Last Updated:** 2025-11-15

**Maintained By:** QA Engineering Team

**Questions?** Contact the Tech Lead or review the documentation files.
