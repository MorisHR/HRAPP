# Component Migration Checklist

## Fortune 500 Material UI to Custom Component Migration

This checklist ensures a systematic and safe migration process for each Angular component from Material UI to custom components.

---

## Pre-Migration Phase

### 1. Component Assessment

- [ ] **Identify Component Type**
  - [ ] Feature component
  - [ ] Shared component
  - [ ] Page/Container component
  - [ ] Form component

- [ ] **Document Current Dependencies**
  - [ ] List all Material UI imports
  - [ ] List all Material modules used
  - [ ] Document Material-specific features (dialogs, snackbars, etc.)
  - [ ] Identify custom Material themes/styling

- [ ] **Risk Assessment**
  - [ ] Component complexity: Low / Medium / High
  - [ ] User-facing: Yes / No
  - [ ] Critical path: Yes / No
  - [ ] Test coverage: Good / Partial / None

- [ ] **Create Feature Flag**
  - [ ] Add feature flag to environment configuration
  - [ ] Set initial rollout percentage (recommend: 0%)
  - [ ] Document flag name: `________________________`

### 2. Preparation

- [ ] **Create Git Branch**
  - [ ] Branch name: `migrate/[component-name]`
  - [ ] Branch from: `main`

- [ ] **Backup Current Implementation**
  - [ ] Copy component to `.backup` folder (if needed)
  - [ ] Document current behavior with screenshots
  - [ ] Save current test results

- [ ] **Setup Testing Environment**
  - [ ] Ensure tests can run locally
  - [ ] Verify test data is available
  - [ ] Document test user accounts (if needed)

---

## Migration Phase

### 3. Code Migration

- [ ] **Replace Material Imports**
  - [ ] Remove `@angular/material` imports
  - [ ] Remove `@angular/cdk` imports (if no longer needed)
  - [ ] Add custom component imports from `@shared/ui/components`

- [ ] **Update Component Template**
  - [ ] Replace Material components with custom equivalents:
    - [ ] `mat-form-field` → `<app-input>`
    - [ ] `mat-input` → `<app-input>`
    - [ ] `mat-button` → `<app-button>`
    - [ ] `mat-select` → `<app-select>` (when available)
    - [ ] `mat-checkbox` → `<app-checkbox>` (when available)
    - [ ] `mat-dialog` → Custom modal/dialog
    - [ ] Other: `________________________`

- [ ] **Update Component Logic**
  - [ ] Update form controls (FormControl, FormGroup)
  - [ ] Update validators (if Material-specific)
  - [ ] Update event handlers
  - [ ] Update error handling
  - [ ] Update accessibility attributes (aria-*)

- [ ] **Update Component Styling**
  - [ ] Remove Material theme references
  - [ ] Update custom styles for new components
  - [ ] Verify responsive behavior
  - [ ] Verify dark mode support (if applicable)

- [ ] **Implement Feature Flag Logic**
  ```typescript
  // Example implementation
  constructor(private featureFlagService: FeatureFlagService) {}

  ngOnInit() {
    this.useCustomComponents = this.featureFlagService.isEnabled('custom-[component]-ui');
  }
  ```
  - [ ] Inject FeatureFlagService
  - [ ] Add conditional rendering logic
  - [ ] Ensure both paths work correctly

### 4. Module Updates

- [ ] **Update Module Imports**
  - [ ] Remove MaterialModule imports (or specific Material modules)
  - [ ] Add SharedUiModule (or specific custom component modules)
  - [ ] Verify no unused imports remain

- [ ] **Update Barrel Exports** (if applicable)
  - [ ] Update index.ts files
  - [ ] Verify exports are correct

---

## Testing Phase

### 5. Unit Testing

- [ ] **Update Unit Tests**
  - [ ] Update test imports
  - [ ] Update test harnesses (Material → Custom)
  - [ ] Mock FeatureFlagService appropriately
  - [ ] Add tests for feature flag behavior

- [ ] **Run Tests**
  - [ ] All tests pass with feature flag OFF
  - [ ] All tests pass with feature flag ON
  - [ ] No console errors or warnings
  - [ ] Coverage maintained or improved

### 6. Integration Testing

- [ ] **Manual Testing Checklist**
  - [ ] Component renders correctly
  - [ ] All inputs accept data correctly
  - [ ] Form validation works as expected
  - [ ] Submit/action buttons work correctly
  - [ ] Error messages display correctly
  - [ ] Loading states work correctly
  - [ ] Disabled states work correctly

- [ ] **Visual Testing**
  - [ ] Layout matches design specifications
  - [ ] Spacing and alignment correct
  - [ ] Colors match brand guidelines
  - [ ] Fonts and typography correct
  - [ ] Icons display correctly
  - [ ] Responsive on mobile (< 768px)
  - [ ] Responsive on tablet (768px - 1024px)
  - [ ] Responsive on desktop (> 1024px)

- [ ] **Accessibility Testing**
  - [ ] Keyboard navigation works
  - [ ] Screen reader compatibility verified
  - [ ] Focus indicators visible
  - [ ] ARIA labels correct
  - [ ] Color contrast meets WCAG AA standards
  - [ ] Form errors announced to screen readers

- [ ] **Browser Testing**
  - [ ] Chrome (latest)
  - [ ] Firefox (latest)
  - [ ] Safari (latest)
  - [ ] Edge (latest)

### 7. Performance Testing

- [ ] **Bundle Size**
  - [ ] Run bundle size tracker: `node scripts/track-bundle-size.js`
  - [ ] Verify bundle size decreased or stayed same
  - [ ] Document size change: `________________________`

- [ ] **Runtime Performance**
  - [ ] Component load time acceptable
  - [ ] No memory leaks detected
  - [ ] Smooth animations (if any)
  - [ ] No layout shift (CLS)

---

## Deployment Phase

### 8. Code Review

- [ ] **Self Review**
  - [ ] Code follows project style guide
  - [ ] No commented-out code
  - [ ] No console.log statements
  - [ ] No TODO comments (or documented in backlog)
  - [ ] TypeScript strict mode compliance

- [ ] **Peer Review**
  - [ ] Pull request created
  - [ ] PR description includes testing notes
  - [ ] Screenshots/GIFs added to PR
  - [ ] At least 2 approvals received
  - [ ] All review comments addressed

### 9. Gradual Rollout

- [ ] **Phase 1: Internal Testing (0-10%)**
  - [ ] Deploy to staging environment
  - [ ] Set feature flag to 5%
  - [ ] Test with internal users
  - [ ] Monitor for errors (24 hours)
  - [ ] Collect feedback

- [ ] **Phase 2: Limited Rollout (10-25%)**
  - [ ] Increase feature flag to 25%
  - [ ] Monitor error rates
  - [ ] Monitor performance metrics
  - [ ] Collect user feedback
  - [ ] Duration: 48 hours minimum

- [ ] **Phase 3: Expanded Rollout (25-50%)**
  - [ ] Increase feature flag to 50%
  - [ ] Monitor error rates
  - [ ] Compare metrics with control group
  - [ ] Duration: 48 hours minimum

- [ ] **Phase 4: Majority Rollout (50-100%)**
  - [ ] Increase feature flag to 100%
  - [ ] Monitor for 72 hours
  - [ ] Verify all metrics stable

### 10. Monitoring

- [ ] **Error Monitoring**
  - [ ] Set up error alerts
  - [ ] Monitor error dashboard
  - [ ] Document baseline error rate: `________________________`
  - [ ] No significant error rate increase

- [ ] **Performance Monitoring**
  - [ ] Monitor page load times
  - [ ] Monitor API response times
  - [ ] Monitor user engagement metrics
  - [ ] Document baseline performance: `________________________`

- [ ] **User Feedback**
  - [ ] Monitor support tickets
  - [ ] Monitor user feedback channels
  - [ ] Document any issues: `________________________`

---

## Completion Phase

### 11. Cleanup

- [ ] **Remove Old Code** (After 2 weeks of 100% rollout)
  - [ ] Remove Material UI implementation
  - [ ] Remove feature flag logic
  - [ ] Remove dual-path conditionals
  - [ ] Update tests to only test custom components

- [ ] **Remove Dependencies** (If no longer needed)
  - [ ] Check if Material modules can be removed from package.json
  - [ ] Run `npm uninstall @angular/material` (if fully migrated)
  - [ ] Update package-lock.json

- [ ] **Documentation**
  - [ ] Update component documentation
  - [ ] Update README if needed
  - [ ] Update architecture diagrams
  - [ ] Add to migration success log

### 12. Final Verification

- [ ] **Build and Deploy**
  - [ ] Clean build passes: `npm run build`
  - [ ] All tests pass: `npm run test`
  - [ ] TypeScript compilation passes: `npx tsc --noEmit`
  - [ ] Production deployment successful

- [ ] **Validation**
  - [ ] Run validation script: `./scripts/validate-migration.sh`
  - [ ] All validations pass
  - [ ] Bundle size within budget

---

## Rollback Procedure

### If Issues Are Detected:

1. **Immediate Response**
   - [ ] Set feature flag to 0% (disable new implementation)
   - [ ] Verify Material UI implementation still works
   - [ ] Run rollback verification: `./scripts/verify-rollback.sh`
   - [ ] Notify team via Slack/Teams

2. **Investigation**
   - [ ] Document the issue in detail
   - [ ] Capture error logs and screenshots
   - [ ] Identify root cause
   - [ ] Create bug ticket with reproduction steps

3. **Resolution**
   - [ ] Fix the issue in development
   - [ ] Add regression tests
   - [ ] Re-test thoroughly
   - [ ] Plan new rollout schedule

---

## Sign-Off Requirements

### Technical Sign-Off

- [ ] **Developer**
  - Name: `________________________`
  - Date: `________________________`
  - Signature: `________________________`

- [ ] **Code Reviewer**
  - Name: `________________________`
  - Date: `________________________`
  - Signature: `________________________`

- [ ] **QA Engineer**
  - Name: `________________________`
  - Date: `________________________`
  - Signature: `________________________`

### Business Sign-Off

- [ ] **Product Owner**
  - Name: `________________________`
  - Date: `________________________`
  - Signature: `________________________`
  - Notes: `________________________`

### Final Approval

- [ ] **Tech Lead / Engineering Manager**
  - Name: `________________________`
  - Date: `________________________`
  - Signature: `________________________`
  - Migration Status: Approved / Rejected / Needs Revision

---

## Component-Specific Notes

### Component Name: `________________________`

### Material Components Replaced:
- `________________________`
- `________________________`
- `________________________`

### Custom Components Used:
- `________________________`
- `________________________`
- `________________________`

### Known Issues / Edge Cases:
- `________________________`
- `________________________`
- `________________________`

### Performance Impact:
- Bundle size: `________________________`
- Load time: `________________________`
- Runtime: `________________________`

### Additional Notes:
```
________________________
________________________
________________________
```

---

## Metrics Summary

| Metric | Before | After | Change | Status |
|--------|--------|-------|--------|--------|
| Bundle Size | ______ | ______ | ______ | ✓ / ✗ |
| Load Time | ______ | ______ | ______ | ✓ / ✗ |
| Test Coverage | ______ | ______ | ______ | ✓ / ✗ |
| Error Rate | ______ | ______ | ______ | ✓ / ✗ |
| User Satisfaction | ______ | ______ | ______ | ✓ / ✗ |

---

## References

- Migration documentation: `/docs/migration-guide.md`
- Custom component library: `/hrms-frontend/src/app/shared/ui/components/`
- Feature flag service: `/hrms-frontend/src/app/core/services/feature-flag.service.ts`
- Validation scripts: `/scripts/validate-migration.sh`
- Rollback scripts: `/scripts/verify-rollback.sh`

---

**Migration completed:** ☐ Yes ☐ No

**Date completed:** `________________________`

**Next component to migrate:** `________________________`
