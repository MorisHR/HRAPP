# QA Infrastructure - Quick Start Guide

## 5-Minute Setup and Usage

### What Was Created

#### Automated Scripts (4)

1. **validate-migration.sh** - Comprehensive migration validation
2. **track-bundle-size.js** - Bundle size tracking over time
3. **verify-rollback.sh** - Rollback safety verification
4. **analyze-bundle.sh** - Deep bundle analysis

#### Documentation (2)

1. **MIGRATION_CHECKLIST.md** - Step-by-step component migration guide
2. **test-scenarios/feature-flag-tests.md** - 28 feature flag test scenarios

#### Guides (2)

1. **QA_INFRASTRUCTURE_GUIDE.md** - Complete reference documentation
2. **QA_QUICK_START.md** - This file

---

## Quick Start

### 1. Make Scripts Executable (One-Time Setup)

```bash
chmod +x scripts/validate-migration.sh
chmod +x scripts/verify-rollback.sh
chmod +x scripts/analyze-bundle.sh
chmod +x scripts/track-bundle-size.js
```

### 2. Run Your First Validation

```bash
# Quick validation (no build)
./scripts/validate-migration.sh --skip-build

# Full validation
./scripts/validate-migration.sh
```

### 3. Track Bundle Size

```bash
# Build first
cd hrms-frontend && npm run build && cd ..

# Track size
node scripts/track-bundle-size.js
```

### 4. Verify Rollback Safety

```bash
./scripts/verify-rollback.sh
```

---

## Common Commands

### Daily Development

```bash
# Morning check
./scripts/validate-migration.sh --skip-build

# After changes
./scripts/validate-migration.sh
```

### Pre-Commit

```bash
# Ensure everything works
./scripts/validate-migration.sh --verbose
./scripts/verify-rollback.sh
```

### Pre-Release

```bash
# Full validation suite
./scripts/validate-migration.sh --verbose
./scripts/analyze-bundle.sh
node scripts/track-bundle-size.js --compare
./scripts/verify-rollback.sh --verbose
```

---

## Expected Output Examples

### Validation Script Success

```
╔════════════════════════════════════════════════════════════╗
║  Fortune 500 Migration Validation Script                  ║
╚════════════════════════════════════════════════════════════╝

✓ Frontend directory exists
✓ Dependencies installed
✓ Required commands available
✓ TypeScript compilation successful

Material UI Import Statistics:
  - @angular/material imports: 380
  - Custom inputs: 202
  - Custom buttons: 354

╔════════════════════════════════════════════════════════════╗
║  ✓ ALL VALIDATIONS PASSED                                  ║
╚════════════════════════════════════════════════════════════╝
```

### Bundle Size Tracker Output

```
Bundle Summary:
  Total Size: 2.34 MB
  File Count: 12

Size by Category:
  main        : 456 KB    (18.5%)
  vendor      : 1.45 MB   (60.1%)

Comparison with Previous Build:
  Previous: 2.56 MB
  Current:  2.34 MB
  Change:   -220 KB (-8.59%)

✓ Bundle size tracking complete
```

### Rollback Verification Success

```
╔════════════════════════════════════════════════════════════╗
║  Rollback Verification Script                              ║
╚════════════════════════════════════════════════════════════╝

✓ Feature flag service found
✓ Material UI package found
✓ Found 45 file(s) with Material imports
✓ Material UI in package.json
✓ TypeScript compilation successful
✓ Development build successful

╔════════════════════════════════════════════════════════════╗
║  ✓ ROLLBACK VERIFICATION PASSED                            ║
║  Safe to rollback by disabling feature flags               ║
╚════════════════════════════════════════════════════════════╝
```

---

## Files Created

### Scripts Directory

```
scripts/
├── validate-migration.sh       (14 KB) - Main validation script
├── verify-rollback.sh          (16 KB) - Rollback safety check
├── analyze-bundle.sh           (16 KB) - Bundle analysis
└── track-bundle-size.js        (13 KB) - Size tracking
```

### Documentation

```
/workspaces/HRAPP/
├── MIGRATION_CHECKLIST.md           (12 KB) - Component migration guide
├── QA_INFRASTRUCTURE_GUIDE.md       (30 KB) - Complete documentation
├── QA_QUICK_START.md                (This file)
└── test-scenarios/
    └── feature-flag-tests.md        (19 KB) - 28 test scenarios
```

### Generated Reports (Created when scripts run)

```
validation-report-[timestamp].txt
bundle-size-report.txt
bundle-history.json
rollback-verification-[timestamp].txt
bundle-analysis-[timestamp].txt
```

---

## Integration with CI/CD

### GitHub Actions

Add to `.github/workflows/migration-validation.yml`:

```yaml
- name: Run Migration Validation
  run: ./scripts/validate-migration.sh --verbose

- name: Track Bundle Size
  run: node scripts/track-bundle-size.js
```

### Pre-Commit Hook

Add to `.git/hooks/pre-commit`:

```bash
#!/bin/bash
./scripts/validate-migration.sh --skip-build || exit 1
```

---

## Troubleshooting

### Permission Denied

```bash
chmod +x scripts/*.sh scripts/*.js
```

### Line Ending Issues

```bash
sed -i 's/\r$//' scripts/*.sh scripts/*.js
```

### Build Not Found

```bash
cd hrms-frontend
npm run build
cd ..
```

---

## Next Steps

1. **Review the comprehensive guide:** `/workspaces/HRAPP/QA_INFRASTRUCTURE_GUIDE.md`
2. **Start migrating components:** Use `/workspaces/HRAPP/MIGRATION_CHECKLIST.md`
3. **Test feature flags:** Follow `/workspaces/HRAPP/test-scenarios/feature-flag-tests.md`
4. **Integrate with CI/CD:** See examples in QA_INFRASTRUCTURE_GUIDE.md

---

## Key Features

- **Automated Validation:** 9-step comprehensive validation
- **Bundle Tracking:** Historical size tracking with trends
- **Rollback Safety:** Verify Material UI still works
- **Detailed Reports:** Text reports for all validations
- **CI/CD Ready:** Proper exit codes and automation support
- **Migration Guidance:** Step-by-step checklists
- **Test Scenarios:** 28 comprehensive test cases

---

## Current Migration Status

Run this to see current status:

```bash
./scripts/validate-migration.sh --skip-build | grep -A 10 "Migration Progress"
```

---

**Quick Help:**

- Full docs: `cat QA_INFRASTRUCTURE_GUIDE.md`
- Script help: `./scripts/validate-migration.sh --help`
- List reports: `ls -lt *.txt *.json | head -10`

**All scripts are production-ready and can be integrated into your CI/CD pipeline immediately.**
