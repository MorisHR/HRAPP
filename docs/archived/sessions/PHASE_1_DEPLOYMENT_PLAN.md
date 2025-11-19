# Phase 1 Deployment Plan
## Quick Wins Migration - Fortune 500 Component Migration

**Phase:** 1 of 6
**Timeline:** 2 weeks (10 business days)
**Status:** 🟡 READY TO START
**Team:** 2-3 Frontend Engineers + 1 QA Engineer

---

## Executive Summary

Phase 1 targets **8 simple components** for migration to custom UI components. These components have minimal Material dependencies and provide quick wins to build team confidence and establish migration patterns.

### Goals

1. ✅ Migrate 8 components from Angular Material to custom components
2. ✅ Establish repeatable migration pattern
3. ✅ Build team confidence
4. ✅ Validate dual-run infrastructure
5. ✅ Set performance baselines
6. ✅ Prove zero-downtime deployment

### Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Components Migrated | 8/8 (100%) | Component count |
| Bundle Size Impact | <50KB increase | Webpack bundle analyzer |
| Performance | No degradation | Lighthouse score ≥90 |
| Test Coverage | ≥85% | Jest coverage report |
| Zero Production Issues | 0 incidents | Error tracking logs |
| User Complaints | 0 | Support tickets |

---

## Component Selection (Priority Order)

### 1. admin/login.component ⭐ HIGHEST PRIORITY

**File:** `src/app/features/admin/login/login.component.ts`

**Complexity:** 🟢 Low
**Material Dependencies:** 6 modules
- MatFormFieldModule
- MatInputModule
- MatButtonModule
- MatCardModule
- MatIconModule
- MatProgressSpinnerModule

**Migration Plan:**
- Replace MatCard → app-card
- Replace MatFormField → app-input
- Replace MatButton → app-button
- Replace MatIcon → app-icon
- Replace MatProgressSpinner → app-spinner (already exists)

**Estimated Effort:** 4 hours
**Test Effort:** 2 hours
**Total:** 6 hours

**Why First:**
- Simple form (2 fields: username, password)
- Already has custom input component
- Low risk (admin-only page)
- Good learning exercise

---

### 2. employee-form.component ⭐ HIGH PRIORITY

**File:** `src/app/features/tenant/employees/employee-form.component.ts`

**Complexity:** 🟢 Low
**Material Dependencies:** 2 modules
- MatFormFieldModule
- MatInputModule

**Migration Plan:**
- Replace MatFormField → app-input
- All fields already use reactive forms
- Minimal template changes

**Estimated Effort:** 3 hours
**Test Effort:** 2 hours
**Total:** 5 hours

**Why Second:**
- Already mostly custom
- Simple reactive form
- Proven pattern from admin/login

---

### 3. landing-page.component

**File:** `src/app/features/marketing/landing-page.component.ts`

**Complexity:** 🟢 Low
**Material Dependencies:** 3 modules
- MatButtonModule
- MatCardModule
- MatIconModule

**Migration Plan:**
- Replace MatCard → app-card (for feature showcase)
- Replace MatButton → app-button (for CTAs)
- Replace MatIcon → app-icon

**Estimated Effort:** 4 hours
**Test Effort:** 2 hours
**Total:** 6 hours

**Why Third:**
- Public-facing (good visual regression test)
- Marketing content (low business logic)
- Good performance benchmark

---

### 4. payslip-list.component

**File:** `src/app/features/employee/payslips/payslip-list.component.ts`

**Complexity:** 🟡 Medium
**Material Dependencies:** 5 modules
- MatTableModule
- MatCardModule
- MatButtonModule
- MatPaginatorModule
- MatSortModule

**Migration Plan:**
- Replace MatTable → app-table (already has sorting)
- Replace MatPaginator → app-pagination (need to build)
- Replace MatCard → app-card
- Replace MatButton → app-button

**Estimated Effort:** 6 hours
**Test Effort:** 3 hours
**Total:** 9 hours

**Why Fourth:**
- Tests table migration pattern
- Need to build pagination component
- Medium complexity

**Note:** Requires building app-pagination component first (3 hours)

---

### 5. payslip-detail.component

**File:** `src/app/features/employee/payslips/payslip-detail.component.ts`

**Complexity:** 🟢 Low
**Material Dependencies:** 4 modules
- MatCardModule
- MatButtonModule
- MatIconModule
- MatDividerModule

**Migration Plan:**
- Replace MatCard → app-card
- Replace MatButton → app-button
- Replace MatIcon → app-icon
- Replace MatDivider → app-divider (already exists)

**Estimated Effort:** 3 hours
**Test Effort:** 2 hours
**Total:** 5 hours

**Why Fifth:**
- Simple detail view
- Uses newly built divider component
- Good test of card layout

---

### 6. employee-attendance.component

**File:** `src/app/features/employee/attendance/employee-attendance.component.ts`

**Complexity:** 🟡 Medium
**Material Dependencies:** 6 modules
- MatTableModule
- MatCardModule
- MatButtonModule
- MatDatepickerModule
- MatFormFieldModule
- MatInputModule

**Migration Plan:**
- Replace MatTable → app-table
- Replace MatDatepicker → app-datepicker (need to build)
- Replace MatCard → app-card
- Replace MatFormField → app-input

**Estimated Effort:** 7 hours
**Test Effort:** 3 hours
**Total:** 10 hours

**Why Sixth:**
- Tests date picker migration
- Need to build datepicker component
- Medium complexity

**Note:** Requires building app-datepicker component first (5 hours)

---

### 7. subdomain.component

**File:** `src/app/features/auth/subdomain/subdomain.component.ts`

**Complexity:** 🟢 Low (ALREADY MIGRATED)
**Material Dependencies:** 0 modules

**Migration Plan:**
- ✅ Already 100% custom
- ✅ Uses app-input, app-button, app-card
- No work needed

**Estimated Effort:** 0 hours
**Test Effort:** 1 hour (verification only)
**Total:** 1 hour

**Why Seventh:**
- Already complete
- Serves as reference implementation
- Quick win

---

### 8. Auth Components (login, forgot-password, reset-password, activate)

**Files:**
- `src/app/features/auth/login/tenant-login.component.ts`
- `src/app/features/auth/forgot-password/forgot-password.component.ts`
- `src/app/features/auth/reset-password/reset-password.component.ts`
- `src/app/features/auth/activate/activate.component.ts`

**Complexity:** 🟢 Low (ALREADY MIGRATED)
**Material Dependencies:** 0 modules

**Migration Plan:**
- ✅ All 100% custom already
- ✅ Use app-input, app-button, app-card
- No work needed

**Estimated Effort:** 0 hours
**Test Effort:** 2 hours (verification only)
**Total:** 2 hours

**Why Eighth:**
- Already complete
- Multiple reference implementations
- Validation of existing work

---

## New Components to Build

### app-pagination Component

**Purpose:** Replace MatPaginatorModule
**Complexity:** 🟡 Medium
**Features Needed:**
- Page size selection (10, 25, 50, 100)
- Page navigation (first, prev, next, last)
- Page number input
- Total count display
- Accessibility (ARIA labels, keyboard nav)

**Estimated Effort:** 3 hours
**Test Effort:** 2 hours
**Total:** 5 hours

**Template:**
```typescript
@Component({
  selector: 'app-pagination',
  template: `
    <div class="pagination">
      <div class="page-size">
        <label>Items per page:</label>
        <select [(ngModel)]="pageSize" (change)="onPageSizeChange()">
          <option [value]="10">10</option>
          <option [value]="25">25</option>
          <option [value]="50">50</option>
          <option [value]="100">100</option>
        </select>
      </div>
      <div class="page-nav">
        <button (click)="firstPage()" [disabled]="currentPage === 1">First</button>
        <button (click)="prevPage()" [disabled]="currentPage === 1">Previous</button>
        <span>Page {{ currentPage }} of {{ totalPages }}</span>
        <button (click)="nextPage()" [disabled]="currentPage === totalPages">Next</button>
        <button (click)="lastPage()" [disabled]="currentPage === totalPages">Last</button>
      </div>
      <div class="total-count">
        {{ (currentPage - 1) * pageSize + 1 }} - {{ Math.min(currentPage * pageSize, totalItems) }} of {{ totalItems }}
      </div>
    </div>
  `
})
export class Pagination {
  pageSize = model<number>(25);
  currentPage = model<number>(1);
  totalItems = input.required<number>();
  pageChange = output<{ page: number; size: number }>();

  totalPages = computed(() => Math.ceil(this.totalItems() / this.pageSize()));

  // Methods...
}
```

---

### app-datepicker Component

**Purpose:** Replace MatDatepickerModule
**Complexity:** 🔴 High
**Features Needed:**
- Date selection calendar
- Input field with validation
- Min/max date constraints
- Keyboard navigation
- Accessibility (ARIA, focus management)
- Timezone handling

**Estimated Effort:** 5 hours
**Test Effort:** 3 hours
**Total:** 8 hours

**Alternative:** Consider using a lightweight library (date-fns + custom UI) to reduce effort

**Template:**
```typescript
@Component({
  selector: 'app-datepicker',
  template: `
    <div class="datepicker">
      <app-input
        [value]="formattedDate()"
        (valueChange)="onInputChange($event)"
        [placeholder]="placeholder()"
        [required]="required()"
        (focus)="openCalendar()"
      />
      @if (isOpen()) {
        <div class="calendar">
          <!-- Calendar grid -->
        </div>
      }
    </div>
  `
})
export class Datepicker {
  value = model<Date | null>(null);
  placeholder = input<string>('Select date');
  required = input<boolean>(false);
  min = input<Date | null>(null);
  max = input<Date | null>(null);

  // Methods...
}
```

---

## Week-by-Week Timeline

### Week 1 (Days 1-5)

**Day 1: Setup & Infrastructure**
- Morning: Team kickoff meeting
- Afternoon: Build app-pagination component
- Evening: Write pagination tests
- **Deliverable:** app-pagination component complete with tests

**Day 2: Admin Login + Employee Form**
- Morning: Migrate admin/login.component
- Afternoon: Write tests for admin/login
- Evening: Migrate employee-form.component
- **Deliverable:** 2 components migrated with tests

**Day 3: Landing Page + Payslip Detail**
- Morning: Migrate landing-page.component
- Afternoon: Migrate payslip-detail.component
- Evening: Write tests for both components
- **Deliverable:** 2 more components migrated

**Day 4: Build Datepicker**
- Morning: Build app-datepicker component
- Afternoon: Continue datepicker implementation
- Evening: Write datepicker tests
- **Deliverable:** app-datepicker component complete

**Day 5: Payslip List**
- Morning: Migrate payslip-list.component
- Afternoon: Write tests for payslip-list
- Evening: Code review for Week 1 components
- **Deliverable:** 1 more component migrated, code review complete

---

### Week 2 (Days 6-10)

**Day 6: Employee Attendance**
- Morning: Migrate employee-attendance.component
- Afternoon: Write tests for employee-attendance
- Evening: Fix any test failures
- **Deliverable:** 1 more component migrated

**Day 7: Verify Existing Migrations**
- Morning: Verify subdomain.component (already done)
- Afternoon: Verify auth components (already done)
- Evening: Integration testing
- **Deliverable:** 5 components verified

**Day 8: Performance Testing**
- Morning: Bundle size analysis
- Afternoon: Lighthouse performance testing
- Evening: Accessibility audits (axe DevTools)
- **Deliverable:** Performance baseline established

**Day 9: QA Testing**
- Morning: Manual QA testing (all 8 components)
- Afternoon: Visual regression testing
- Evening: Fix any bugs found
- **Deliverable:** QA sign-off

**Day 10: Production Deployment**
- Morning: Deploy with feature flags at 0%
- Afternoon: Gradual rollout (0% → 10% → 25%)
- Evening: Monitor analytics and error rates
- **Deliverable:** Phase 1 in production

---

## Deployment Strategy

### Pre-Deployment Checklist

- [ ] All 8 components migrated
- [ ] All tests passing (unit + integration)
- [ ] Bundle size analysis complete
- [ ] Performance baselines established
- [ ] Accessibility audits passed
- [ ] QA testing complete
- [ ] Feature flags configured
- [ ] Monitoring dashboards ready
- [ ] Rollback plan documented

### Rollout Plan (Day 10)

**Hour 1-2: Deploy to Production (0%)**
```bash
# Deploy code with feature flags disabled
git tag phase-1-v1.0.0
git push origin phase-1-v1.0.0
# Backend: Set all feature flags to 0%
curl -X POST /api/admin/feature-flags/phase-1 -d '{"enabled": false}'
```

**Hour 3-4: Enable for Internal Users (10%)**
```bash
# Enable for 10% of users (internal testing)
curl -X PATCH /api/admin/feature-flags/phase-1 -d '{"percentage": 10}'
# Monitor: Error rate, performance, user feedback
```

**Hour 5-6: Gradual Rollout (25%)**
```bash
# Increase to 25% if no issues
curl -X PATCH /api/admin/feature-flags/phase-1 -d '{"percentage": 25}'
```

**Day 11-12: Monitor at 25%**
- Monitor error rates, performance, user feedback
- Address any issues found
- Prepare for further rollout

**Day 13-14: Increase to 50%**
```bash
curl -X PATCH /api/admin/feature-flags/phase-1 -d '{"percentage": 50}'
```

**Day 15-17: Increase to 100%**
```bash
curl -X PATCH /api/admin/feature-flags/phase-1 -d '{"percentage": 100}'
```

### Monitoring Metrics

**Real-time Monitoring:**
- Error rate (target: <0.1%)
- Page load time (target: <2s)
- Lighthouse score (target: ≥90)
- User complaints (target: 0)

**Analytics Tracking:**
- Component render count (custom vs Material)
- Component error count
- User interaction time
- Conversion rates (for login, forms)

**Rollback Criteria:**
- Error rate >5% → Auto-rollback to 0%
- Lighthouse score drop >10 points → Manual review
- User complaints >5 → Manual review
- Performance degradation >20% → Manual review

---

## Testing Strategy

### Unit Testing

**Coverage Target:** ≥85%

**Test Categories:**
1. **Component Rendering**
   - Component mounts without errors
   - All inputs render correctly
   - All outputs emit correctly

2. **User Interactions**
   - Button clicks trigger correct handlers
   - Form inputs update model
   - Keyboard navigation works

3. **Edge Cases**
   - Empty state rendering
   - Loading state rendering
   - Error state rendering
   - Disabled state behavior

**Example Test:**
```typescript
describe('AdminLoginComponent', () => {
  it('should submit login form', async () => {
    const { fixture, component } = await setup();

    component.username.set('admin@test.com');
    component.password.set('password123');

    const submitSpy = jest.spyOn(component, 'onSubmit');
    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    button.click();

    expect(submitSpy).toHaveBeenCalled();
    expect(component.isLoading()).toBe(true);
  });
});
```

### Integration Testing

**Test Scenarios:**
1. **Login Flow**
   - User enters credentials → Success login → Redirect to dashboard
   - User enters wrong credentials → Error message displayed
   - User clicks forgot password → Redirect to forgot-password page

2. **Form Submission**
   - User fills form → Validates fields → Submits → Success message
   - User submits invalid form → Validation errors displayed

3. **Data Loading**
   - Component loads → API call triggered → Data displayed
   - API fails → Error state displayed

### Visual Regression Testing

**Tool:** Percy or Chromatic

**Test Cases:**
1. Default state
2. Hover state (buttons, links)
3. Focus state (form fields)
4. Error state (form validation)
5. Loading state (spinners)
6. Empty state (no data)
7. Responsive states (mobile, tablet, desktop)

**Example Config:**
```yaml
# .percy.yml
version: 2
snapshots:
  - name: Admin Login - Default
    url: /admin/login
    widths: [375, 768, 1280]

  - name: Admin Login - Error State
    url: /admin/login
    execute: |
      document.querySelector('input[name="username"]').value = 'invalid';
      document.querySelector('button[type="submit"]').click();
```

### Accessibility Testing

**Tool:** axe DevTools

**Test Categories:**
1. **Keyboard Navigation**
   - All interactive elements accessible via Tab
   - Enter/Space activate buttons
   - Escape closes dialogs

2. **Screen Reader**
   - All form fields have labels
   - Error messages announced
   - Loading states announced

3. **WCAG 2.1 AA Compliance**
   - Color contrast ≥4.5:1
   - Minimum touch target 44x44px
   - Focus indicators visible
   - No keyboard traps

**Example Test:**
```typescript
import { axe, toHaveNoViolations } from 'jest-axe';

it('should have no accessibility violations', async () => {
  const { fixture } = await setup();
  const results = await axe(fixture.nativeElement);
  expect(results).toHaveNoViolations();
});
```

---

## Performance Optimization

### Bundle Size Analysis

**Tool:** webpack-bundle-analyzer

**Target:** <50KB increase per component

**Analysis:**
```bash
npm run build -- --stats-json
npx webpack-bundle-analyzer dist/stats.json
```

**Optimization Strategies:**
1. **Lazy Loading**
   - Load components only when needed
   - Route-based code splitting

2. **Tree Shaking**
   - Import only used modules
   - Avoid barrel imports

3. **Compression**
   - Gzip/Brotli compression
   - Minification in production

### Performance Benchmarks

**Tool:** Lighthouse CI

**Targets:**
- Performance: ≥90
- Accessibility: 100
- Best Practices: ≥95
- SEO: ≥90

**Metrics to Track:**
- First Contentful Paint (FCP): <1.5s
- Largest Contentful Paint (LCP): <2.5s
- Time to Interactive (TTI): <3.5s
- Cumulative Layout Shift (CLS): <0.1
- Total Blocking Time (TBT): <300ms

**Benchmark Script:**
```bash
# Run Lighthouse CI
npm install -g @lhci/cli
lhci autorun --config=.lighthouserc.json
```

---

## Risk Mitigation

### Risk 1: Timeline Slip

**Probability:** Medium (40%)
**Impact:** Low
**Mitigation:**
- Build app-pagination and app-datepicker in advance (Day 1, Day 4)
- Prioritize simple components first
- Have backup plan: Skip employee-attendance if time runs out

### Risk 2: Performance Degradation

**Probability:** Low (20%)
**Impact:** Medium
**Mitigation:**
- Benchmark before and after migration
- Optimize bundle size proactively
- Use lazy loading where possible
- Have rollback plan ready

### Risk 3: Breaking Changes

**Probability:** Low (15%)
**Impact:** High
**Mitigation:**
- Comprehensive testing (unit + integration + visual)
- Dual-run pattern (Material as fallback)
- Gradual rollout (0% → 10% → 25% → 50% → 100%)
- Auto-rollback on >5% error rate

### Risk 4: Team Availability

**Probability:** Medium (30%)
**Impact:** Medium
**Mitigation:**
- Cross-train team members
- Document migration patterns
- Pair programming for complex components
- Have backup engineer on standby

### Risk 5: Dependency Issues

**Probability:** Low (10%)
**Impact:** Low
**Mitigation:**
- Lock package versions (package-lock.json)
- Test on multiple environments
- Have rollback to previous versions

---

## Team Structure

### Frontend Engineers (2-3)

**Engineer 1: Lead**
- Migrate admin/login, landing-page, employee-form
- Build app-pagination component
- Code reviews
- Performance testing

**Engineer 2: Developer**
- Migrate payslip-list, payslip-detail
- Build app-datepicker component
- Integration testing
- Accessibility testing

**Engineer 3: Developer (if available)**
- Migrate employee-attendance
- Verify existing migrations
- Visual regression testing
- Documentation

### QA Engineer (1)

**Responsibilities:**
- Manual testing of all components
- Automated test script development
- Bug reporting and tracking
- UAT coordination
- Sign-off on production readiness

### UI/UX Designer (0.5 FTE)

**Responsibilities:**
- Design review for all components
- Accessibility audit
- Visual consistency check
- User feedback collection

---

## Success Criteria

### Definition of Done (Per Component)

- [ ] Material dependencies removed from component
- [ ] Custom components implemented
- [ ] Unit tests written and passing (≥85% coverage)
- [ ] Integration tests passing
- [ ] Accessibility audit passed (axe DevTools)
- [ ] Performance benchmark met (Lighthouse ≥90)
- [ ] Visual regression tests passing
- [ ] QA testing complete
- [ ] Code review approved
- [ ] Documentation updated
- [ ] Feature flag configured

### Phase 1 Success Criteria

- [ ] 8 components migrated (100%)
- [ ] All tests passing (unit + integration)
- [ ] Bundle size increase <50KB
- [ ] Lighthouse score ≥90
- [ ] Zero production incidents
- [ ] Zero user complaints
- [ ] Gradual rollout completed (0% → 100%)
- [ ] Analytics data collected
- [ ] Retrospective conducted
- [ ] Lessons learned documented

---

## Post-Deployment

### Week 3 Activities

**Day 11-12: Monitoring at 25%**
- Monitor error rates, performance, user feedback
- Review analytics data
- Address any issues found
- Prepare status report

**Day 13-14: Increase to 50%**
- Increase feature flag to 50%
- Continue monitoring
- Performance comparison (custom vs Material)

**Day 15-17: Increase to 100%**
- Increase feature flag to 100%
- Final monitoring
- Prepare Phase 1 completion report

### Retrospective (End of Week 3)

**Questions to Answer:**
1. What went well?
2. What could be improved?
3. What patterns should we reuse?
4. What patterns should we avoid?
5. What surprised us?
6. How accurate were our estimates?

**Outputs:**
1. Lessons learned document
2. Updated migration playbook
3. Refined estimates for Phase 2
4. Process improvements

---

## Handoff to Phase 2

### Deliverables for Next Phase

1. **Updated Components**
   - app-pagination (new)
   - app-datepicker (new)
   - 8 migrated components

2. **Documentation**
   - Migration playbook (updated)
   - Performance baselines
   - Accessibility audit results
   - Lessons learned

3. **Metrics**
   - Actual effort vs. estimated
   - Bundle size impact
   - Performance benchmarks
   - Error rates

4. **Tools & Infrastructure**
   - Visual regression testing setup
   - Performance monitoring dashboards
   - Feature flag configuration patterns

---

## Appendix

### A. Component Migration Checklist

```markdown
## Component: [Name]

### Pre-Migration
- [ ] Read existing component code
- [ ] Identify Material dependencies
- [ ] List custom components needed
- [ ] Estimate effort

### Migration
- [ ] Create dual-run template structure
- [ ] Implement custom UI version
- [ ] Keep Material version (fallback)
- [ ] Add feature flag integration
- [ ] Update imports

### Testing
- [ ] Write unit tests (≥85% coverage)
- [ ] Write integration tests
- [ ] Run accessibility audit
- [ ] Run performance benchmark
- [ ] Visual regression tests

### Review
- [ ] Code review
- [ ] Design review
- [ ] QA testing
- [ ] Stakeholder approval

### Deployment
- [ ] Deploy to staging
- [ ] Deploy to production (0%)
- [ ] Gradual rollout
- [ ] Monitor metrics

### Completion
- [ ] 100% rollout
- [ ] Material code removal (Phase 6)
- [ ] Documentation updated
```

### B. Useful Commands

```bash
# Run unit tests with coverage
npm run test -- --coverage

# Run integration tests
npm run test:integration

# Build with stats
npm run build -- --stats-json

# Analyze bundle size
npx webpack-bundle-analyzer dist/stats.json

# Run Lighthouse
npm run lighthouse

# Run accessibility audit
npm run a11y

# Run visual regression
npm run percy

# Deploy to staging
npm run deploy:staging

# Deploy to production
npm run deploy:prod
```

### C. Contact Information

| Role | Name | Contact |
|------|------|---------|
| Engineering Lead | TBD | TBD |
| Frontend Engineer 1 | TBD | TBD |
| Frontend Engineer 2 | TBD | TBD |
| QA Engineer | TBD | TBD |
| UI/UX Designer | TBD | TBD |
| Product Owner | TBD | TBD |

---

## Sign-Off

**Plan Status:** ✅ READY FOR EXECUTION

**Prepared by:** AI Engineering Assistant
**Date:** November 17, 2025
**Estimated Start:** [TBD - Pending stakeholder approval]
**Estimated Completion:** 2 weeks from start

**Approval Required From:**
- [ ] Engineering Lead
- [ ] Product Owner
- [ ] QA Manager
- [ ] UI/UX Lead

**Next Steps:**
1. Get stakeholder approval
2. Assign team members
3. Schedule kickoff meeting
4. Begin Day 1 activities

---

**Document Version:** 1.0.0
**Classification:** Internal - Project Plan
**Distribution:** Engineering Team, Product, QA, Design
