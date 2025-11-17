# Pull Request Quality Gates - Fortune 500 Grade
## HRMS Application - PR Standards & Requirements

**Version:** 1.0.0
**Last Updated:** 2025-11-17
**Owner:** DevOps Engineering Team

---

## Overview

This document defines comprehensive pull request quality gates that must be met before code can be merged into protected branches (main, develop, release/*).

---

## PR Quality Gate Checklist

### Mandatory Requirements (Auto-Enforced)

- [ ] All CI/CD pipeline checks pass (green)
- [ ] Minimum test coverage: 80%
- [ ] No failing tests
- [ ] No security vulnerabilities (Critical/High)
- [ ] Bundle size within budget
- [ ] Performance budget met
- [ ] Code review approved (minimum 2 approvals)
- [ ] All conversations resolved
- [ ] Branch up to date with target branch
- [ ] No merge conflicts

---

## 1. Test Coverage Requirements

### Minimum Coverage Thresholds

**Overall Application:**
- Line Coverage: >= 80%
- Branch Coverage: >= 75%
- Function Coverage: >= 85%

**By Layer:**
```yaml
Controllers:
  minimum: 75%
  target: 85%

Services:
  minimum: 85%
  target: 95%

Repositories:
  minimum: 80%
  target: 90%

Business Logic:
  minimum: 90%
  target: 100%

Utilities:
  minimum: 80%
  target: 95%
```

### Coverage Reporting

**Generate Coverage Report:**
```bash
# Frontend (Angular)
npm run test:coverage

# Backend (.NET)
dotnet test /p:CollectCoverage=true \
  /p:CoverageReporterOutput=coverage/ \
  /p:CoverageReporter=lcov
```

**Coverage Report Tools:**
- **Frontend:** Istanbul/NYC with Karma
- **Backend:** Coverlet with ReportGenerator
- **Integration:** Codecov.io or SonarQube

**PR Comment Template:**
```markdown
## Test Coverage Report

| Metric | Current | Previous | Target | Status |
|--------|---------|----------|--------|--------|
| Line Coverage | 82.5% | 81.2% | 80% | ✅ Pass |
| Branch Coverage | 76.3% | 75.1% | 75% | ✅ Pass |
| Function Coverage | 88.1% | 87.5% | 85% | ✅ Pass |

### Coverage by Component
- Authentication Module: 95.2% ✅
- Employee Management: 84.7% ✅
- Payroll System: 79.8% ⚠️ Below target (85%)
- Reporting: 91.3% ✅

**Note:** Payroll system coverage decreased. Please add tests for new edge cases.
```

---

## 2. No Failing Tests

### Test Execution Requirements

**All test suites must pass:**
- Unit Tests (Frontend & Backend)
- Integration Tests
- Component Tests (Angular)
- API Tests
- E2E Tests (critical paths only for PRs)

**Test Execution:**
```bash
# Frontend tests
npm run test:ci

# Backend tests
dotnet test --configuration Release --no-build --verbosity minimal

# Integration tests
./scripts/run-integration-tests.sh

# E2E tests (smoke suite)
npm run e2e:smoke
```

**Flaky Test Policy:**
- Tests that fail intermittently must be fixed or quarantined
- Maximum 2 retries allowed for flaky tests
- Flaky tests must be tracked in JIRA and resolved within 1 sprint

---

## 3. Security Vulnerability Scanning

### Security Checks Required

**Frontend (npm audit):**
```bash
# Check for vulnerabilities
npm audit --production --audit-level=moderate

# Auto-fix where possible
npm audit fix --production

# Generate report
npm audit --json > security-report.json
```

**Severity Thresholds:**
- **Critical:** 0 allowed (PR blocked)
- **High:** 0 allowed (PR blocked)
- **Moderate:** <= 5 allowed (requires review)
- **Low:** <= 10 allowed (documented)

**Backend (NuGet Audit):**
```bash
# Enable package vulnerability scanning
dotnet restore --use-lock-file
dotnet list package --vulnerable --include-transitive

# Generate report
dotnet list package --vulnerable --include-transitive --format json
```

**SAST (Static Application Security Testing):**
- **Tool:** SonarQube or Snyk
- **Scan:** Every PR
- **Gate:** No new critical/high issues

**Example Configuration:**
```yaml
# sonar-project.properties
sonar.projectKey=hrms-app
sonar.projectName=HRMS Application
sonar.sources=src
sonar.tests=tests
sonar.exclusions=**/node_modules/**,**/dist/**
sonar.typescript.lcov.reportPaths=coverage/lcov.info

# Quality Gates
sonar.qualitygate.wait=true
sonar.qualitygate.timeout=300

# Security
sonar.security.hotspotRating=A
sonar.security.reviewPriority=high
```

**Dependency Scanning with Snyk:**
```bash
# Install Snyk
npm install -g snyk

# Authenticate
snyk auth

# Test for vulnerabilities
snyk test

# Monitor project
snyk monitor
```

---

## 4. Bundle Size Check

### Bundle Size Budget (Frontend)

**Production Bundle Limits:**
```json
// angular.json
{
  "budgets": [
    {
      "type": "initial",
      "maximumWarning": "2mb",
      "maximumError": "3mb"
    },
    {
      "type": "anyComponentStyle",
      "maximumWarning": "6kb",
      "maximumError": "10kb"
    },
    {
      "type": "bundle",
      "name": "vendor",
      "maximumWarning": "1.5mb",
      "maximumError": "2mb"
    }
  ]
}
```

**Bundle Analysis:**
```bash
# Build with stats
npm run build -- --stats-json

# Analyze bundle
npm run analyze-bundle

# Or use webpack-bundle-analyzer
npx webpack-bundle-analyzer dist/stats.json
```

**Bundle Size Report:**
```markdown
## Bundle Size Analysis

| Bundle | Size | Limit | Status |
|--------|------|-------|--------|
| main.js | 1.8 MB | 3 MB | ✅ Pass |
| vendor.js | 1.4 MB | 2 MB | ✅ Pass |
| polyfills.js | 125 KB | 500 KB | ✅ Pass |
| styles.css | 245 KB | 500 KB | ✅ Pass |
| **Total** | **3.57 MB** | **5 MB** | ✅ Pass |

### Largest Dependencies
1. @angular/material - 512 KB
2. chart.js - 234 KB
3. rxjs - 189 KB

**Note:** No bundle size increase from baseline.
```

---

## 5. Performance Budget Check

### Performance Metrics

**Lighthouse CI Requirements:**
```json
// lighthouserc.json
{
  "ci": {
    "collect": {
      "url": ["http://localhost:4200"],
      "numberOfRuns": 3
    },
    "assert": {
      "preset": "lighthouse:recommended",
      "assertions": {
        "categories:performance": ["error", {"minScore": 0.85}],
        "categories:accessibility": ["error", {"minScore": 0.90}],
        "categories:best-practices": ["error", {"minScore": 0.90}],
        "categories:seo": ["error", {"minScore": 0.85}],
        "first-contentful-paint": ["error", {"maxNumericValue": 2000}],
        "interactive": ["error", {"maxNumericValue": 4000}],
        "speed-index": ["error", {"maxNumericValue": 3500}],
        "total-blocking-time": ["error", {"maxNumericValue": 300}]
      }
    },
    "upload": {
      "target": "temporary-public-storage"
    }
  }
}
```

**Performance Budget:**
- **First Contentful Paint (FCP):** < 2s
- **Time to Interactive (TTI):** < 4s
- **Speed Index:** < 3.5s
- **Total Blocking Time:** < 300ms
- **Lighthouse Performance Score:** >= 85

**API Performance:**
- **P50 Response Time:** < 100ms
- **P95 Response Time:** < 500ms
- **P99 Response Time:** < 1000ms
- **Error Rate:** < 0.1%

**Database Performance:**
- **Query Time P95:** < 100ms
- **Connection Pool:** 80% availability
- **Deadlocks:** 0 per day

---

## 6. Code Review Requirements

### Review Process

**Minimum Approvals:**
- **Main Branch:** 2 approvals required
  - 1 from Senior Developer/Tech Lead
  - 1 from another team member
- **Develop Branch:** 1 approval required
- **Feature Branches:** 1 approval required

**Required Reviewers:**
- Changes to auth/security: Security Team Lead
- Database migrations: Database Administrator
- Infrastructure changes: DevOps Engineer
- API contracts: API Team Lead

**Review Checklist for Reviewers:**
```markdown
## Code Review Checklist

### Code Quality
- [ ] Code follows project style guide
- [ ] No code smells or anti-patterns
- [ ] Proper error handling implemented
- [ ] No hardcoded values (use configuration)
- [ ] Logging is appropriate and meaningful

### Testing
- [ ] Tests cover happy path and edge cases
- [ ] Tests are maintainable and readable
- [ ] No flaky tests introduced
- [ ] Test data is appropriate

### Security
- [ ] No sensitive data in code
- [ ] Input validation implemented
- [ ] SQL injection prevention verified
- [ ] XSS prevention verified
- [ ] Authentication/authorization correct

### Performance
- [ ] No N+1 query problems
- [ ] Appropriate caching implemented
- [ ] Database indexes considered
- [ ] No memory leaks

### Documentation
- [ ] Public APIs documented
- [ ] Complex logic has comments
- [ ] README updated if needed
- [ ] Architecture decision recorded (if applicable)
```

**Auto-Assignment Rules:**
```yaml
# .github/CODEOWNERS
# Code ownership for automatic review requests

# Auth & Security
src/HRMS.API/Controllers/Auth*.cs @security-team
src/HRMS.Application/Services/Auth*.cs @security-team

# Database
**/Migrations/** @database-team
**/DbContext.cs @database-team

# Frontend
hrms-frontend/src/app/auth/** @frontend-team @security-team
hrms-frontend/src/app/components/** @frontend-team

# Infrastructure
deployment/** @devops-team
.github/workflows/** @devops-team

# Documentation
*.md @technical-writers
```

---

## 7. Documentation Requirements

### Documentation Checklist

**Code-Level Documentation:**
- [ ] Public APIs have XML documentation (C#)
- [ ] Complex functions have JSDoc comments (TypeScript)
- [ ] README updated for new features
- [ ] API endpoints documented in Swagger

**Feature Documentation:**
- [ ] User-facing changes documented
- [ ] Migration guide for breaking changes
- [ ] Configuration changes documented
- [ ] Environment variable updates documented

**Example Documentation:**
```typescript
/**
 * Authenticates a user and returns a JWT token.
 *
 * @param credentials - User credentials containing email and password
 * @returns Promise resolving to authentication response with token
 * @throws {UnauthorizedException} When credentials are invalid
 * @throws {AccountLockedException} When account is locked
 *
 * @example
 * ```typescript
 * const response = await authService.login({
 *   email: 'user@example.com',
 *   password: 'SecurePass123!'
 * });
 * console.log(response.token);
 * ```
 */
async login(credentials: LoginCredentials): Promise<AuthResponse> {
  // Implementation
}
```

---

## 8. Changelog Updates

### Changelog Format

**File:** `CHANGELOG.md`

**Format:** Keep a Changelog (https://keepachangelog.com/)

```markdown
# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Multi-factor authentication for admin users (#123)
- Export employee data to CSV functionality (#145)

### Changed
- Updated Angular from v19 to v20 (#156)
- Improved performance of payroll calculation by 40% (#167)

### Fixed
- Fixed issue where leave balance wasn't updating (#178)
- Resolved memory leak in SignalR connection (#189)

### Security
- Updated vulnerable dependencies (CVE-2024-12345) (#190)
- Implemented rate limiting on login endpoint (#191)

## [1.2.0] - 2025-11-15

### Added
- Employee self-service portal
- Biometric device integration

[Unreleased]: https://github.com/org/hrms/compare/v1.2.0...HEAD
[1.2.0]: https://github.com/org/hrms/releases/tag/v1.2.0
```

**PR Requirement:**
- All PRs must update CHANGELOG.md under [Unreleased] section
- Use appropriate category: Added, Changed, Deprecated, Removed, Fixed, Security

---

## 9. Branch Protection Rules

### Protected Branches

**Main Branch:**
```yaml
Protection Settings:
  - Require pull request reviews: true
  - Required approving reviews: 2
  - Dismiss stale reviews: true
  - Require review from Code Owners: true
  - Require status checks to pass: true
  - Required status checks:
      - build-frontend
      - build-backend
      - test-frontend
      - test-backend
      - security-scan
      - code-coverage
      - bundle-size-check
  - Require branches to be up to date: true
  - Require conversation resolution: true
  - Require signed commits: true
  - Include administrators: true
  - Restrict pushes: true
  - Allow force pushes: false
  - Allow deletions: false
```

**Develop Branch:**
```yaml
Protection Settings:
  - Require pull request reviews: true
  - Required approving reviews: 1
  - Require status checks to pass: true
  - Required status checks:
      - build-frontend
      - build-backend
      - test-frontend
      - test-backend
  - Require branches to be up to date: true
  - Allow force pushes: false
```

---

## 10. Automated PR Checks

### GitHub Actions Workflow

**File:** `.github/workflows/pr-quality-gates.yml`

```yaml
name: PR Quality Gates

on:
  pull_request:
    branches: [main, develop]

jobs:
  pr-checks:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0  # For SonarQube

      # Size labeler
      - name: Label PR by size
        uses: codelytv/pr-size-labeler@v1
        with:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          xs_label: 'size/xs'
          xs_max_size: 10
          s_label: 'size/s'
          s_max_size: 100
          m_label: 'size/m'
          m_max_size: 500
          l_label: 'size/l'
          l_max_size: 1000
          xl_label: 'size/xl'

      # Check for breaking changes
      - name: Check for breaking changes
        uses: actions/github-script@v6
        with:
          script: |
            const title = context.payload.pull_request.title;
            const body = context.payload.pull_request.body || '';

            if (title.includes('BREAKING') || body.includes('BREAKING CHANGE')) {
              github.rest.issues.addLabels({
                owner: context.repo.owner,
                repo: context.repo.repo,
                issue_number: context.payload.pull_request.number,
                labels: ['breaking-change']
              });
            }

      # Auto-assign reviewers
      - name: Auto-assign reviewers
        uses: kentaro-m/auto-assign-action@v1.2.1
        with:
          configuration-path: '.github/auto-assign.yml'
```

---

## PR Templates

### Pull Request Template

**File:** `.github/pull_request_template.md`

```markdown
## Description
<!-- Provide a brief description of the changes in this PR -->

## Type of Change
<!-- Mark the relevant option with an "x" -->

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Performance improvement
- [ ] Code refactoring
- [ ] Documentation update
- [ ] Configuration change
- [ ] Dependency update

## Related Issues
<!-- Link to related issues using # notation -->

Closes #
Related to #

## Changes Made
<!-- List the specific changes made in this PR -->

-
-
-

## Testing Performed
<!-- Describe the testing you performed to verify your changes -->

- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing performed
- [ ] E2E tests added/updated

### Test Scenarios Covered
1.
2.
3.

## Screenshots (if applicable)
<!-- Add screenshots to help explain your changes -->

## Performance Impact
<!-- Describe any performance impact -->

- [ ] No performance impact
- [ ] Performance improved (describe below)
- [ ] Potential performance impact (describe below)

**Details:**

## Security Considerations
<!-- Describe any security implications -->

- [ ] No security impact
- [ ] Security improvement (describe below)
- [ ] Requires security review (describe below)

**Details:**

## Database Changes
<!-- Describe any database schema changes -->

- [ ] No database changes
- [ ] Migration added
- [ ] Data migration required
- [ ] Rollback script included

## Breaking Changes
<!-- List any breaking changes and migration path -->

- [ ] No breaking changes
- [ ] Breaking changes (describe below)

**Details:**

## Checklist
<!-- Verify all items before requesting review -->

- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] No new warnings generated
- [ ] Tests added and passing (coverage >= 80%)
- [ ] No security vulnerabilities introduced
- [ ] CHANGELOG.md updated
- [ ] All conversations resolved
- [ ] Branch is up to date with target branch

## Reviewer Notes
<!-- Any specific areas reviewers should focus on -->

## Deployment Notes
<!-- Any special deployment instructions -->

- [ ] No special deployment steps required
- [ ] Requires configuration changes (documented above)
- [ ] Requires database migration (documented above)
- [ ] Requires environment variable updates (documented above)
```

---

## Quality Gate Metrics Dashboard

### PR Quality Metrics to Track

```yaml
Metrics:
  # Velocity
  - PR Merge Time (from open to merge): Target < 24h
  - Review Time (from PR open to first review): Target < 4h
  - Review Rounds: Target <= 2

  # Quality
  - PRs Merged Without Changes: Target > 60%
  - PRs Failing Quality Gates: Target < 10%
  - Test Coverage Trend: Target increasing

  # Size
  - Average PR Size: Target < 500 lines
  - Large PRs (>1000 lines): Target < 5%

  # Security
  - PRs Introducing Vulnerabilities: Target 0%
  - Security Review Time: Target < 8h
```

---

## Enforcement & Exceptions

### Quality Gate Failures

**Automatic Rejection:**
- Critical/High security vulnerabilities
- Test coverage < 70%
- Failing tests
- Merge conflicts

**Manual Review Required:**
- Test coverage 70-80%
- Bundle size warning exceeded
- Performance degradation detected
- Moderate security issues

### Exception Process

**Emergency Hotfix:**
1. Create issue documenting emergency
2. Get approval from Tech Lead + Product Manager
3. Bypass quality gates with `--skip-ci` label
4. Create follow-up ticket to address skipped checks
5. Follow-up PR required within 48 hours

**Large Refactoring:**
1. Split into multiple PRs if possible
2. If not possible, get pre-approval from Tech Lead
3. Extended review time allowed (up to 48h)
4. May require demo/walkthrough session

---

## Success Criteria

A PR is ready to merge when:

1. ✅ All CI/CD checks pass
2. ✅ Test coverage >= 80%
3. ✅ No security vulnerabilities (Critical/High)
4. ✅ Performance budgets met
5. ✅ 2 approvals received (for main branch)
6. ✅ All conversations resolved
7. ✅ Documentation updated
8. ✅ CHANGELOG.md updated
9. ✅ No merge conflicts
10. ✅ Branch is up to date

---

## Estimated Implementation Time

- **Initial Setup:** 16 hours
- **GitHub Actions Configuration:** 8 hours
- **Documentation:** 4 hours
- **Team Training:** 4 hours
- **Total:** 32 hours (1 week)

---

## Priority

**P0 (Critical)** - Must be implemented before next release

---

## Dependencies

- GitHub Enterprise or GitHub Pro (for advanced branch protection)
- SonarQube or Snyk (for security scanning)
- Codecov or similar (for coverage reporting)
- Lighthouse CI (for performance testing)

---

**Document Owner:** DevOps Team
**Last Review Date:** 2025-11-17
**Next Review Date:** 2026-02-17
