# Phase 2 Migration - Component Priority Matrix

**Project:** HRMS Frontend Angular Material to Custom UI Migration
**Phase:** Phase 2 (26 Remaining Components)
**Analysis Date:** 2025-11-17
**Analyst:** Senior Frontend Migration Architect

---

## EXECUTIVE SUMMARY

### Current State Analysis
- **Custom UI Components:** 29 components available
- **Existing Test Coverage:** 5 test files (17% coverage)
- **Total Application Components:** 59 feature components
- **Phase 1 Complete:** 5 components migrated (Datepicker, Pagination, Subdomain, Organization, Sidebar)
- **Remaining Material Usage:** 26 component types across 39+ files

### Key Findings
1. **High Material Dependency:** 757 mat-icon occurrences across 69 files
2. **Table Component Critical:** 40 files using MatTableModule (most business-critical)
3. **Dialog Usage Moderate:** 14 files using MatDialogModule
4. **Expansion Panel Limited:** 11 files using MatExpansionModule
5. **Low Test Coverage:** Only 5 test files for 29 custom components (17%)

---

## COMPONENT PRIORITY MATRIX

### Scoring Methodology
- **Business Impact:** High (3), Medium (2), Low (1)
- **Usage Frequency:** High (3), Medium (2), Low (1)
- **Migration Complexity:** Simple (3), Medium (2), Complex (1)
- **Dependencies:** Independent (3), Some (2), Many (1)
- **Priority Score:** Sum of all factors (Max: 12, Min: 4)

---

## WAVE 1: QUICK WINS (Priority Score: 10-12)

### 1. Icon Component ⭐ HIGHEST PRIORITY
**Priority Score:** 12/12
- **Business Impact:** High (3) - Used everywhere for visual consistency
- **Usage Frequency:** High (3) - 757 occurrences across 69 files
- **Complexity:** Simple (3) - Direct 1:1 replacement, already built
- **Dependencies:** Independent (3) - No component dependencies
- **Estimated LOC:** 150 LOC (simple wrapper)
- **Estimated Hours:** 8 hours (1 day)
- **Test Requirements:** 25 tests
- **Risk:** LOW - Already has custom component built

**Migration Impact:**
- Files affected: 69 files
- Material imports removed: MatIconModule (69 instances)
- Breaking changes: None (same API)

**Week 1 Target:** ✅ YES - Immediate start

---

### 2. Progress Spinner ⭐ HIGH PRIORITY
**Priority Score:** 11/12
- **Business Impact:** High (3) - Critical for loading states
- **Usage Frequency:** High (3) - 70 occurrences across 41 files
- **Complexity:** Simple (3) - Already built
- **Dependencies:** Independent (3) - No dependencies
- **Estimated LOC:** 120 LOC
- **Estimated Hours:** 6 hours (0.75 day)
- **Test Requirements:** 22 tests
- **Risk:** LOW - Component exists

**Migration Impact:**
- Files affected: 41 files
- Material imports removed: MatProgressSpinnerModule
- Breaking changes: None

**Week 1 Target:** ✅ YES

---

### 3. Snackbar/Toast ⭐ HIGH PRIORITY
**Priority Score:** 11/12
- **Business Impact:** High (3) - User feedback mechanism
- **Usage Frequency:** Medium (2) - 32 occurrences
- **Complexity:** Simple (3) - Toast component exists
- **Dependencies:** Independent (3)
- **Estimated LOC:** 180 LOC
- **Estimated Hours:** 10 hours (1.25 days)
- **Test Requirements:** 30 tests
- **Risk:** LOW - Direct replacement

**Migration Impact:**
- Files affected: ~20 files
- Material imports removed: MatSnackBarModule
- Breaking changes: API differences (mat-snack-bar → toast service)

**Week 1 Target:** ✅ YES

---

### 4. Menu Component
**Priority Score:** 10/12
- **Business Impact:** Medium (2) - Navigation and actions
- **Usage Frequency:** Medium (2) - 16 occurrences
- **Complexity:** Simple (3) - Component exists
- **Dependencies:** Independent (3)
- **Estimated LOC:** 250 LOC
- **Estimated Hours:** 12 hours (1.5 days)
- **Test Requirements:** 35 tests
- **Risk:** MEDIUM - Keyboard navigation complexity

**Migration Impact:**
- Files affected: ~10 files
- Material imports removed: MatMenuModule
- Breaking changes: Minor (trigger positioning)

**Week 1 Target:** ✅ YES

---

### 5. Divider Component
**Priority Score:** 10/12
- **Business Impact:** Low (1) - Visual separation
- **Usage Frequency:** Medium (2) - Common in layouts
- **Complexity:** Simple (3) - Trivial component
- **Dependencies:** Independent (3)
- **Estimated LOC:** 80 LOC
- **Estimated Hours:** 4 hours (0.5 day)
- **Test Requirements:** 15 tests
- **Risk:** LOW - Simplest migration

**Migration Impact:**
- Files affected: ~15 files
- Material imports removed: MatDividerModule
- Breaking changes: None

**Week 1 Target:** ✅ YES

---

## WAVE 2: CORE COMPONENTS (Priority Score: 7-9)

### 6. Table Component 🚨 BUSINESS CRITICAL
**Priority Score:** 9/12
- **Business Impact:** High (3) - All list pages depend on this
- **Usage Frequency:** High (3) - 40 files
- **Complexity:** Medium (2) - Sorting, pagination, selection
- **Dependencies:** Some (2) - Pagination component
- **Estimated LOC:** 450 LOC
- **Estimated Hours:** 24 hours (3 days)
- **Test Requirements:** 70 tests
- **Risk:** HIGH - Complex component, many features

**Migration Impact:**
- Files affected: 40 files (HIGHEST)
- Material imports removed: MatTableModule, MatSortModule
- Breaking changes: Column definition API changes

**Critical Files:**
- employee-list.component.ts
- tenant-list.component.ts
- audit-logs.component.ts
- All dashboard tables
- All report tables

**Week 2 Target:** ✅ YES - Start immediately after Wave 1

---

### 7. Dialog Component 🚨 HIGH USAGE
**Priority Score:** 9/12
- **Business Impact:** High (3) - Confirmations and forms
- **Usage Frequency:** Medium (2) - 14 files
- **Complexity:** Medium (2) - Focus trap, backdrop, stacking
- **Dependencies:** Some (2) - Dialog container
- **Estimated LOC:** 380 LOC
- **Estimated Hours:** 20 hours (2.5 days)
- **Test Requirements:** 50 tests
- **Risk:** MEDIUM - Focus management complexity

**Migration Impact:**
- Files affected: 14 files
- Material imports removed: MatDialogModule
- Breaking changes: Service API changes

**Critical Files:**
- payment-detail-dialog.component.ts
- tier-upgrade-dialog.component.ts
- session-timeout-warning.component.ts
- Confirmation dialogs across app

**Week 2 Target:** ✅ YES

---

### 8. Tabs Component
**Priority Score:** 8/12
- **Business Impact:** Medium (2) - Navigation
- **Usage Frequency:** High (3) - 33 files
- **Complexity:** Medium (2) - Dynamic tabs, lazy loading
- **Dependencies:** Some (2) - Tab content projection
- **Estimated LOC:** 320 LOC
- **Estimated Hours:** 18 hours (2.25 days)
- **Test Requirements:** 45 tests
- **Risk:** MEDIUM - Lazy loading complexity

**Migration Impact:**
- Files affected: 33 files
- Material imports removed: MatTabsModule
- Breaking changes: Tab panel structure

**Week 2 Target:** ✅ YES

---

### 9. Expansion Panel/Accordion
**Priority Score:** 8/12
- **Business Impact:** Medium (2) - Progressive disclosure
- **Usage Frequency:** Medium (2) - 11 files
- **Complexity:** Medium (2) - Animation, multi-expand
- **Dependencies:** Some (2) - Header/content projection
- **Estimated LOC:** 280 LOC
- **Estimated Hours:** 16 hours (2 days)
- **Test Requirements:** 38 tests
- **Risk:** MEDIUM - Animation states

**Migration Impact:**
- Files affected: 11 files
- Material imports removed: MatExpansionModule
- Breaking changes: Minor (header structure)

**Critical Files:**
- comprehensive-employee-form.component.ts
- audit-logs.component.ts

**Week 3 Target:** ✅ YES

---

### 10. Sidenav Component
**Priority Score:** 7/12
- **Business Impact:** High (3) - Main navigation
- **Usage Frequency:** Low (1) - 16 occurrences (2 layouts)
- **Complexity:** Medium (2) - Responsive, overlay, push modes
- **Dependencies:** Some (2) - Layout integration
- **Estimated LOC:** 350 LOC
- **Estimated Hours:** 20 hours (2.5 days)
- **Test Requirements:** 42 tests
- **Risk:** HIGH - Layout dependencies

**Migration Impact:**
- Files affected: 2 files (tenant-layout, admin-layout)
- Material imports removed: MatSidenavModule
- Breaking changes: Layout structure changes

**Week 3 Target:** ⚠️ Maybe - After testing layout impact

---

## WAVE 3: SPECIALIZED COMPONENTS (Priority Score: 5-6)

### 11. Stepper Component
**Priority Score:** 6/12
- **Business Impact:** Medium (2) - Multi-step forms
- **Usage Frequency:** Low (1) - Few files
- **Complexity:** Complex (1) - Step validation, navigation
- **Dependencies:** Some (2) - Form integration
- **Estimated LOC:** 420 LOC
- **Estimated Hours:** 28 hours (3.5 days)
- **Test Requirements:** 55 tests
- **Risk:** HIGH - Complex state management

**Migration Impact:**
- Files affected: ~5 files
- Material imports removed: MatStepperModule
- Breaking changes: Step API changes

**Week 4-5 Target:** ✅ YES

---

### 12. Autocomplete Component
**Priority Score:** 6/12
- **Business Impact:** Medium (2) - Search functionality
- **Usage Frequency:** Low (1) - Limited usage
- **Complexity:** Medium (2) - Filtering, virtual scroll
- **Dependencies:** Some (2) - Input, dropdown
- **Estimated LOC:** 380 LOC
- **Estimated Hours:** 22 hours (2.75 days)
- **Test Requirements:** 48 tests
- **Risk:** MEDIUM - Filter performance

**Migration Impact:**
- Files affected: ~8 files
- Material imports removed: MatAutocompleteModule
- Breaking changes: Option filtering API

**Week 5-6 Target:** ✅ YES

---

### 13. Paginator Component (Legacy)
**Priority Score:** 5/12
- **Business Impact:** High (3) - Data navigation
- **Usage Frequency:** Low (1) - Replaced by pagination
- **Complexity:** Simple (3) - Already have pagination
- **Dependencies:** Many (1) - Table integration
- **Estimated LOC:** 200 LOC
- **Estimated Hours:** 8 hours (1 day)
- **Test Requirements:** 28 tests
- **Risk:** LOW - Migration to pagination

**Note:** Migrate to existing pagination component, not paginator

**Week 6 Target:** ✅ YES

---

## WAVE 4: REMAINING COMPONENTS (Priority Score: 4-5)

### 14. List Component
**Priority Score:** 5/12
- **Business Impact:** Low (1) - Display lists
- **Usage Frequency:** Medium (2) - Various uses
- **Complexity:** Simple (3) - Basic rendering
- **Dependencies:** Independent (3)
- **Estimated LOC:** 180 LOC
- **Estimated Hours:** 10 hours (1.25 days)
- **Test Requirements:** 25 tests
- **Risk:** LOW

**Week 7 Target:** ✅ YES

---

### 15. Badge Component
**Priority Score:** 5/12
- **Business Impact:** Low (1) - Notifications
- **Usage Frequency:** Medium (2) - Status indicators
- **Complexity:** Simple (3)
- **Dependencies:** Independent (3)
- **Estimated LOC:** 120 LOC
- **Estimated Hours:** 6 hours (0.75 day)
- **Test Requirements:** 18 tests
- **Risk:** LOW

**Week 7 Target:** ✅ YES

---

### 16. Chip Component
**Priority Score:** 5/12
- **Business Impact:** Low (1) - Tags, filters
- **Usage Frequency:** Medium (2) - Common in filters
- **Complexity:** Simple (3)
- **Dependencies:** Independent (3)
- **Estimated LOC:** 160 LOC
- **Estimated Hours:** 8 hours (1 day)
- **Test Requirements:** 22 tests
- **Risk:** LOW

**Week 7 Target:** ✅ YES

---

### 17. Toolbar Component
**Priority Score:** 5/12
- **Business Impact:** Low (1) - Page headers
- **Usage Frequency:** Low (1) - Limited use
- **Complexity:** Simple (3)
- **Dependencies:** Independent (3)
- **Estimated LOC:** 140 LOC
- **Estimated Hours:** 6 hours (0.75 day)
- **Test Requirements:** 20 tests
- **Risk:** LOW

**Week 8 Target:** ✅ YES

---

### 18. Progress Bar Component
**Priority Score:** 5/12
- **Business Impact:** Low (1) - Upload progress
- **Usage Frequency:** Low (1) - Rare usage
- **Complexity:** Simple (3)
- **Dependencies:** Independent (3)
- **Estimated LOC:** 130 LOC
- **Estimated Hours:** 6 hours (0.75 day)
- **Test Requirements:** 20 tests
- **Risk:** LOW

**Week 8 Target:** ✅ YES

---

## ALREADY MIGRATED (Phase 1)

### ✅ Completed Components
1. **Datepicker** - 384 LOC, 100+ tests
2. **Pagination** - 340 LOC, 60+ tests
3. **Input** - Migrated in Phase 1
4. **Button** - Migrated in Phase 1
5. **Card** - Migrated in Phase 1
6. **Select** - Migrated in Phase 1
7. **Checkbox** - Migrated in Phase 1
8. **Radio/Radio Group** - Migrated in Phase 1
9. **Toggle** - Migrated in Phase 1

---

## SUMMARY BY PRIORITY

### Week 1 Components (5 components)
1. Icon Component (12/12) - 8 hours
2. Progress Spinner (11/12) - 6 hours
3. Snackbar/Toast (11/12) - 10 hours
4. Menu (10/12) - 12 hours
5. Divider (10/12) - 4 hours

**Week 1 Total:** 40 hours (1 developer-week)

---

### Week 2 Components (3 components)
6. Table Component (9/12) - 24 hours
7. Dialog Component (9/12) - 20 hours
8. Tabs Component (8/12) - 18 hours

**Week 2 Total:** 62 hours (1.5 developer-weeks)

---

### Week 3 Components (2 components)
9. Expansion Panel (8/12) - 16 hours
10. Sidenav Component (7/12) - 20 hours

**Week 3 Total:** 36 hours (1 developer-week)

---

### Weeks 4-8 Components (8 components)
11. Stepper (6/12) - 28 hours
12. Autocomplete (6/12) - 22 hours
13. Paginator (5/12) - 8 hours
14. List (5/12) - 10 hours
15. Badge (5/12) - 6 hours
16. Chip (5/12) - 8 hours
17. Toolbar (5/12) - 6 hours
18. Progress Bar (5/12) - 6 hours

**Weeks 4-8 Total:** 94 hours (2.5 developer-weeks)

---

## TOTAL EFFORT ESTIMATION

### Development Hours
- **Wave 1 (Week 1):** 40 hours
- **Wave 2 (Weeks 2-3):** 98 hours
- **Wave 3 (Weeks 4-6):** 50 hours
- **Wave 4 (Weeks 7-8):** 44 hours
- **Total Development:** 232 hours (29 developer-days, ~6 weeks)

### Testing Hours (60+ tests per component minimum)
- **Unit Tests:** 580 tests × 0.5 hours = 290 hours
- **Integration Tests:** 40 tests × 1 hour = 40 hours
- **E2E Tests:** 30 scenarios × 2 hours = 60 hours
- **Total Testing:** 390 hours (49 developer-days, ~10 weeks)

### QA & Documentation
- **QA Testing:** 80 hours (2 weeks)
- **Documentation:** 40 hours (1 week)
- **Code Review:** 40 hours (1 week)
- **Total QA:** 160 hours (4 weeks)

### GRAND TOTAL
- **Development:** 232 hours
- **Testing:** 390 hours
- **QA/Docs:** 160 hours
- **Total:** 782 hours (98 developer-days, ~20 weeks with 1 developer)

**With 2 developers:** ~10 weeks
**With 3 developers:** ~7 weeks

---

## DEPENDENCY GRAPH

```
Independent (Can start immediately):
├── Icon Component ✅
├── Progress Spinner ✅
├── Snackbar/Toast ✅
├── Menu ✅
├── Divider ✅
├── Badge
├── Chip
├── Toolbar
└── Progress Bar

Depends on Icon:
├── Table Component (needs icon for sort)
├── Dialog Component (needs icon for close)
├── Expansion Panel (needs icon for expand)
└── Sidenav (needs icon for menu)

Depends on Table:
└── Paginator (integrates with table)

Depends on Input:
├── Autocomplete (extends input)
└── Stepper (form steps)

Complex Dependencies:
├── Tabs (lazy loading, routing)
└── Stepper (multi-step validation)
```

---

## RISK ASSESSMENT

### HIGH RISK (Requires careful planning)
1. **Table Component** - Most business-critical, 40 files affected
2. **Sidenav Component** - Layout restructuring required
3. **Stepper Component** - Complex state management

### MEDIUM RISK (Standard migration)
1. **Dialog Component** - Focus management
2. **Tabs Component** - Lazy loading
3. **Expansion Panel** - Animation states
4. **Menu Component** - Positioning logic
5. **Autocomplete** - Filter performance

### LOW RISK (Quick wins)
1. **Icon Component** - Simple replacement
2. **Progress Spinner** - Direct swap
3. **Snackbar/Toast** - Service replacement
4. **Divider** - Trivial component
5. **Badge, Chip, Toolbar, Progress Bar** - Simple components

---

## BREAKING CHANGES ANALYSIS

### API Changes Required
1. **MatSnackBar → ToastService** - Service API different
2. **MatDialog → DialogService** - Data passing changes
3. **MatTable → app-table** - Column definition API
4. **MatPaginator → app-pagination** - Event structure

### Template Changes Required
1. **mat-icon → app-icon** - Name attribute
2. **mat-menu → app-menu** - Trigger syntax
3. **mat-tab-group → app-tabs** - Tab panel structure
4. **mat-expansion-panel → app-expansion-panel** - Header structure

### No Breaking Changes
1. **mat-progress-spinner → app-progress-spinner** - Same API
2. **mat-divider → app-divider** - Same API
3. **mat-badge → app-badge** - Same API
4. **mat-chip → app-chip** - Same API

---

## RECOMMENDATIONS

### Immediate Actions (Week 1)
1. ✅ Migrate Icon component first (highest impact, lowest risk)
2. ✅ Migrate Progress Spinner (quick win)
3. ✅ Create comprehensive test suite for each component
4. ✅ Document breaking changes in migration guide

### Strategic Planning (Weeks 2-3)
1. ⚠️ Table component requires dedicated sprint
2. ⚠️ Dialog component needs focus management testing
3. ⚠️ Consider feature flags for gradual rollout

### Long-term Goals (Weeks 4-8)
1. 📊 Achieve 80% test coverage minimum
2. 📊 Document all API changes
3. 📊 Performance benchmarking
4. 📊 Accessibility audit (WCAG 2.1 AA)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-17
**Next Review:** Week 2 (after Wave 1 completion)
