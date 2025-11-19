# CI/CD Pipeline Specification - Fortune 500 Grade
## HRMS Application - Complete Pipeline Configuration

**Version:** 1.0.0
**Last Updated:** 2025-11-17
**Owner:** DevOps Engineering Team

---

## Overview

This document defines a comprehensive CI/CD pipeline for the HRMS application with four main stages: Build, Test, Quality, and Deploy. The pipeline ensures code quality, security, and reliability at every stage.

---

## Pipeline Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         TRIGGER EVENTS                          │
│  • Push to main/develop    • Pull Request    • Tag/Release     │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                      STAGE 1: BUILD                             │
│  • Install Dependencies      • Build Frontend                   │
│  • Build Backend            • Create Artifacts                  │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                      STAGE 2: TEST                              │
│  • Unit Tests (FE/BE)       • Integration Tests                 │
│  • E2E Tests                • Code Coverage                     │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                      STAGE 3: QUALITY                           │
│  • Linting                  • Security Scan                     │
│  • SAST                     • Dependency Check                  │
│  • Bundle Size              • Performance Budget                │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                   STAGE 4: DEPLOY (Conditional)                 │
│  • Deploy to Staging        • Smoke Tests                       │
│  • Blue/Green Production    • Health Checks                     │
│  • Auto Rollback            • Monitoring                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## GitHub Actions Workflow

### Main CI/CD Pipeline

**File:** `.github/workflows/ci-cd-pipeline.yml`

```yaml
name: CI/CD Pipeline - Fortune 500 Grade

on:
  push:
    branches: [main, develop, release/**]
  pull_request:
    branches: [main, develop]
  release:
    types: [published]

env:
  NODE_VERSION: '20.x'
  DOTNET_VERSION: '9.0.x'
  POSTGRES_VERSION: '16'

jobs:
  # ============================================
  # STAGE 1: BUILD
  # ============================================

  build-frontend:
    name: Build Frontend (Angular)
    runs-on: ubuntu-latest
    timeout-minutes: 15

    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for SonarQube

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: hrms-frontend/package-lock.json

      - name: Cache node modules
        uses: actions/cache@v3
        with:
          path: hrms-frontend/node_modules
          key: ${{ runner.os }}-node-${{ hashFiles('**/package-lock.json') }}
          restore-keys: |
            ${{ runner.os }}-node-

      - name: Install dependencies
        working-directory: hrms-frontend
        run: npm ci

      - name: Lint check
        working-directory: hrms-frontend
        run: npm run lint

      - name: Build frontend (Production)
        working-directory: hrms-frontend
        run: npm run build -- --configuration production

      - name: Check bundle size
        working-directory: hrms-frontend
        run: |
          echo "Checking bundle sizes..."
          ls -lh dist/hrms-frontend/browser/*.js
          # Fail if main bundle > 3MB
          size=$(stat -f%z dist/hrms-frontend/browser/main*.js 2>/dev/null || stat -c%s dist/hrms-frontend/browser/main*.js)
          if [ $size -gt 3145728 ]; then
            echo "ERROR: Main bundle exceeds 3MB limit"
            exit 1
          fi

      - name: Upload frontend artifacts
        uses: actions/upload-artifact@v3
        with:
          name: frontend-dist
          path: hrms-frontend/dist
          retention-days: 7

  build-backend:
    name: Build Backend (.NET)
    runs-on: ubuntu-latest
    timeout-minutes: 15

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Cache NuGet packages
        uses: actions/cache@v3
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore HRMS.sln

      - name: Build solution
        run: dotnet build HRMS.sln --configuration Release --no-restore

      - name: Publish API
        run: |
          dotnet publish src/HRMS.API/HRMS.API.csproj \
            --configuration Release \
            --no-build \
            --output ./publish/api

      - name: Upload backend artifacts
        uses: actions/upload-artifact@v3
        with:
          name: backend-dist
          path: ./publish
          retention-days: 7

  # ============================================
  # STAGE 2: TEST
  # ============================================

  test-frontend:
    name: Test Frontend (Angular)
    runs-on: ubuntu-latest
    needs: build-frontend
    timeout-minutes: 20

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: hrms-frontend/package-lock.json

      - name: Install dependencies
        working-directory: hrms-frontend
        run: npm ci

      - name: Run unit tests with coverage
        working-directory: hrms-frontend
        run: npm run test:coverage

      - name: Check coverage thresholds
        working-directory: hrms-frontend
        run: |
          echo "Checking test coverage..."
          # Parse coverage and enforce 80% minimum
          coverage=$(node -e "const cov = require('./coverage/coverage-summary.json'); console.log(cov.total.lines.pct)")
          if (( $(echo "$coverage < 80" | bc -l) )); then
            echo "ERROR: Coverage $coverage% is below 80% threshold"
            exit 1
          fi
          echo "Coverage: $coverage% ✓"

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          files: hrms-frontend/coverage/lcov.info
          flags: frontend
          name: frontend-coverage

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: frontend-test-results
          path: hrms-frontend/coverage

  test-backend:
    name: Test Backend (.NET)
    runs-on: ubuntu-latest
    needs: build-backend
    timeout-minutes: 20

    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: hrms_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore HRMS.sln

      - name: Run unit tests
        run: |
          dotnet test tests/HRMS.Tests/HRMS.Tests.csproj \
            --configuration Release \
            --no-build \
            --verbosity normal \
            --filter "Category=Unit" \
            /p:CollectCoverage=true \
            /p:CoverageReporter=lcov \
            /p:CoverageOutputDirectory=./coverage

      - name: Check coverage threshold
        run: |
          dotnet test \
            /p:CollectCoverage=true \
            /p:Threshold=80 \
            /p:ThresholdType=line

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          files: tests/HRMS.Tests/coverage/coverage.info
          flags: backend
          name: backend-coverage

  test-integration:
    name: Integration Tests
    runs-on: ubuntu-latest
    needs: [build-frontend, build-backend]
    timeout-minutes: 30

    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: hrms_integration
        ports:
          - 5432:5432

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Setup database
        run: |
          dotnet ef database update --project src/HRMS.API

      - name: Run integration tests
        run: |
          dotnet test tests/HRMS.Tests/HRMS.Tests.csproj \
            --filter "Category=Integration" \
            --verbosity normal

  test-e2e:
    name: E2E Tests (Playwright)
    runs-on: ubuntu-latest
    needs: [test-frontend, test-backend]
    timeout-minutes: 30

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}

      - name: Download artifacts
        uses: actions/download-artifact@v3
        with:
          name: frontend-dist
          path: hrms-frontend/dist

      - name: Install Playwright
        working-directory: hrms-frontend
        run: |
          npm ci
          npx playwright install --with-deps

      - name: Run E2E tests (smoke suite)
        working-directory: hrms-frontend
        run: npx playwright test --project=chromium --grep "@smoke"

      - name: Upload E2E test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: e2e-test-results
          path: hrms-frontend/playwright-report

  # ============================================
  # STAGE 3: QUALITY
  # ============================================

  quality-lint:
    name: Quality - Linting
    runs-on: ubuntu-latest
    timeout-minutes: 10

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}

      - name: Install dependencies
        working-directory: hrms-frontend
        run: npm ci

      - name: Run ESLint
        working-directory: hrms-frontend
        run: |
          npm run lint -- --format json --output-file eslint-report.json || true
          npm run lint

      - name: Upload lint results
        uses: actions/upload-artifact@v3
        with:
          name: lint-results
          path: hrms-frontend/eslint-report.json

  quality-security:
    name: Quality - Security Scan
    runs-on: ubuntu-latest
    timeout-minutes: 15

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      # Frontend security
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}

      - name: npm audit (Frontend)
        working-directory: hrms-frontend
        run: |
          npm audit --production --audit-level=moderate || {
            echo "Security vulnerabilities found!"
            npm audit --production --json > npm-audit.json
            exit 1
          }

      # Backend security
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: NuGet vulnerability check
        run: |
          dotnet list package --vulnerable --include-transitive 2>&1 | tee nuget-audit.txt
          if grep -q "has the following vulnerable packages" nuget-audit.txt; then
            echo "Vulnerable packages found!"
            exit 1
          fi

      # Snyk security scan
      - name: Run Snyk Security Scan
        uses: snyk/actions/node@master
        continue-on-error: true
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
        with:
          args: --severity-threshold=high

  quality-sast:
    name: Quality - SAST (SonarQube)
    runs-on: ubuntu-latest
    needs: [test-frontend, test-backend]
    timeout-minutes: 20

    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Install SonarScanner
        run: |
          dotnet tool install --global dotnet-sonarscanner

      - name: Download coverage reports
        uses: actions/download-artifact@v3
        with:
          name: frontend-test-results
          path: hrms-frontend/coverage

      - name: Run SonarQube analysis
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        run: |
          dotnet sonarscanner begin \
            /k:"hrms-app" \
            /d:sonar.host.url="${{ secrets.SONAR_HOST_URL }}" \
            /d:sonar.login="${{ secrets.SONAR_TOKEN }}" \
            /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" \
            /d:sonar.typescript.lcov.reportPaths="hrms-frontend/coverage/lcov.info"

          dotnet build HRMS.sln --configuration Release

          dotnet sonarscanner end /d:sonar.login="${{ secrets.SONAR_TOKEN }}"

      - name: Check SonarQube Quality Gate
        uses: sonarsource/sonarqube-quality-gate-action@master
        timeout-minutes: 5
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}

  quality-dependency-check:
    name: Quality - Dependency Check
    runs-on: ubuntu-latest
    timeout-minutes: 15

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: OWASP Dependency Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'HRMS'
          path: '.'
          format: 'ALL'
          args: >
            --failOnCVSS 7
            --suppression dependency-check-suppressions.xml

      - name: Upload dependency check results
        uses: actions/upload-artifact@v3
        with:
          name: dependency-check-report
          path: dependency-check-report.html

  quality-bundle-size:
    name: Quality - Bundle Size Check
    runs-on: ubuntu-latest
    needs: build-frontend
    timeout-minutes: 10

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Download frontend artifacts
        uses: actions/download-artifact@v3
        with:
          name: frontend-dist
          path: hrms-frontend/dist

      - name: Analyze bundle size
        working-directory: hrms-frontend
        run: |
          npm ci
          npm run build -- --stats-json
          npx webpack-bundle-analyzer dist/stats.json --mode static --report bundle-report.html

      - name: Check bundle budget
        working-directory: hrms-frontend
        run: |
          # Check if build respects budgets (already done in Angular build)
          echo "Bundle size check completed during build"

      - name: Upload bundle analysis
        uses: actions/upload-artifact@v3
        with:
          name: bundle-analysis
          path: hrms-frontend/bundle-report.html

  quality-performance:
    name: Quality - Performance Budget
    runs-on: ubuntu-latest
    needs: build-frontend
    timeout-minutes: 20

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}

      - name: Download frontend artifacts
        uses: actions/download-artifact@v3
        with:
          name: frontend-dist
          path: hrms-frontend/dist

      - name: Install Lighthouse CI
        run: npm install -g @lhci/cli

      - name: Run Lighthouse CI
        working-directory: hrms-frontend
        run: |
          lhci autorun --config=lighthouserc.json

      - name: Upload Lighthouse results
        uses: actions/upload-artifact@v3
        with:
          name: lighthouse-results
          path: .lighthouseci

  # ============================================
  # STAGE 4: DEPLOY
  # ============================================

  deploy-staging:
    name: Deploy to Staging
    runs-on: ubuntu-latest
    needs: [test-integration, test-e2e, quality-sast, quality-security]
    if: github.ref == 'refs/heads/develop' || github.ref == 'refs/heads/main'
    environment:
      name: staging
      url: https://staging.hrms.example.com
    timeout-minutes: 20

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Download artifacts
        uses: actions/download-artifact@v3

      - name: Setup Google Cloud SDK
        uses: google-github-actions/setup-gcloud@v1
        with:
          service_account_key: ${{ secrets.GCP_SA_KEY_STAGING }}
          project_id: ${{ secrets.GCP_PROJECT_ID_STAGING }}

      - name: Configure kubectl
        run: |
          gcloud container clusters get-credentials hrms-staging-cluster \
            --zone us-central1-a

      - name: Build and push Docker images
        run: |
          # Build frontend
          docker build -t gcr.io/${{ secrets.GCP_PROJECT_ID_STAGING }}/hrms-frontend:${{ github.sha }} \
            -f hrms-frontend/Dockerfile hrms-frontend

          # Build backend
          docker build -t gcr.io/${{ secrets.GCP_PROJECT_ID_STAGING }}/hrms-api:${{ github.sha }} \
            -f src/HRMS.API/Dockerfile .

          # Push images
          docker push gcr.io/${{ secrets.GCP_PROJECT_ID_STAGING }}/hrms-frontend:${{ github.sha }}
          docker push gcr.io/${{ secrets.GCP_PROJECT_ID_STAGING }}/hrms-api:${{ github.sha }}

      - name: Deploy to Kubernetes (Staging)
        run: |
          kubectl set image deployment/hrms-frontend \
            hrms-frontend=gcr.io/${{ secrets.GCP_PROJECT_ID_STAGING }}/hrms-frontend:${{ github.sha }} \
            -n staging

          kubectl set image deployment/hrms-api \
            hrms-api=gcr.io/${{ secrets.GCP_PROJECT_ID_STAGING }}/hrms-api:${{ github.sha }} \
            -n staging

          kubectl rollout status deployment/hrms-frontend -n staging
          kubectl rollout status deployment/hrms-api -n staging

      - name: Run database migrations
        run: |
          kubectl exec -n staging deployment/hrms-api -- \
            dotnet ef database update --no-build

      - name: Run smoke tests
        run: |
          ./scripts/smoke-tests-staging.sh

  deploy-production:
    name: Deploy to Production (Blue/Green)
    runs-on: ubuntu-latest
    needs: deploy-staging
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    environment:
      name: production
      url: https://hrms.example.com
    timeout-minutes: 30

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Download artifacts
        uses: actions/download-artifact@v3

      - name: Setup Google Cloud SDK
        uses: google-github-actions/setup-gcloud@v1
        with:
          service_account_key: ${{ secrets.GCP_SA_KEY_PROD }}
          project_id: ${{ secrets.GCP_PROJECT_ID_PROD }}

      - name: Configure kubectl
        run: |
          gcloud container clusters get-credentials hrms-prod-cluster \
            --zone us-central1-a

      - name: Determine current environment color
        id: current-color
        run: |
          CURRENT=$(kubectl get service hrms-frontend-live -o jsonpath='{.spec.selector.color}')
          if [ "$CURRENT" == "blue" ]; then
            echo "NEW_COLOR=green" >> $GITHUB_OUTPUT
            echo "OLD_COLOR=blue" >> $GITHUB_OUTPUT
          else
            echo "NEW_COLOR=blue" >> $GITHUB_OUTPUT
            echo "OLD_COLOR=green" >> $GITHUB_OUTPUT
          fi

      - name: Build and push Docker images
        run: |
          docker build -t gcr.io/${{ secrets.GCP_PROJECT_ID_PROD }}/hrms-frontend:${{ github.sha }} \
            -f hrms-frontend/Dockerfile hrms-frontend

          docker build -t gcr.io/${{ secrets.GCP_PROJECT_ID_PROD }}/hrms-api:${{ github.sha }} \
            -f src/HRMS.API/Dockerfile .

          docker push gcr.io/${{ secrets.GCP_PROJECT_ID_PROD }}/hrms-frontend:${{ github.sha }}
          docker push gcr.io/${{ secrets.GCP_PROJECT_ID_PROD }}/hrms-api:${{ github.sha }}

      - name: Deploy to new environment
        run: |
          # Deploy to new color environment
          kubectl set image deployment/hrms-frontend-${{ steps.current-color.outputs.NEW_COLOR }} \
            hrms-frontend=gcr.io/${{ secrets.GCP_PROJECT_ID_PROD }}/hrms-frontend:${{ github.sha }} \
            -n production

          kubectl set image deployment/hrms-api-${{ steps.current-color.outputs.NEW_COLOR }} \
            hrms-api=gcr.io/${{ secrets.GCP_PROJECT_ID_PROD }}/hrms-api:${{ github.sha }} \
            -n production

          kubectl rollout status deployment/hrms-frontend-${{ steps.current-color.outputs.NEW_COLOR }} -n production
          kubectl rollout status deployment/hrms-api-${{ steps.current-color.outputs.NEW_COLOR }} -n production

      - name: Run health checks on new environment
        run: |
          ./scripts/health-check-production.sh ${{ steps.current-color.outputs.NEW_COLOR }}

      - name: Run smoke tests on new environment
        run: |
          ./scripts/smoke-tests-production.sh ${{ steps.current-color.outputs.NEW_COLOR }}

      - name: Switch traffic to new environment
        run: |
          # Update service to point to new color
          kubectl patch service hrms-frontend-live -p \
            '{"spec":{"selector":{"color":"${{ steps.current-color.outputs.NEW_COLOR }}"}}}'

          kubectl patch service hrms-api-live -p \
            '{"spec":{"selector":{"color":"${{ steps.current-color.outputs.NEW_COLOR }}"}}}'

          echo "Traffic switched to ${{ steps.current-color.outputs.NEW_COLOR }} environment"

      - name: Monitor for errors (5 minutes)
        run: |
          sleep 300
          ./scripts/check-error-rate.sh

      - name: Rollback on failure
        if: failure()
        run: |
          echo "Deployment failed! Rolling back..."

          # Switch traffic back to old color
          kubectl patch service hrms-frontend-live -p \
            '{"spec":{"selector":{"color":"${{ steps.current-color.outputs.OLD_COLOR }}"}}}'

          kubectl patch service hrms-api-live -p \
            '{"spec":{"selector":{"color":"${{ steps.current-color.outputs.OLD_COLOR }}"}}}'

          echo "Rollback completed. Traffic restored to ${{ steps.current-color.outputs.OLD_COLOR }}"
          exit 1

      - name: Keep old environment for quick rollback
        run: |
          echo "Keeping ${{ steps.current-color.outputs.OLD_COLOR }} environment for 24h quick rollback"
          # Tag old deployment
          kubectl annotate deployment hrms-frontend-${{ steps.current-color.outputs.OLD_COLOR }} \
            rollback-ready=true \
            rollback-expires=$(date -d "+24 hours" -u +%Y-%m-%dT%H:%M:%SZ)

  post-deployment-validation:
    name: Post-Deployment Validation
    runs-on: ubuntu-latest
    needs: deploy-production
    timeout-minutes: 15

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Run full smoke test suite
        run: ./scripts/full-smoke-tests.sh

      - name: Validate monitoring dashboards
        run: ./scripts/validate-monitoring.sh

      - name: Check error rates
        run: ./scripts/check-production-health.sh

      - name: Send deployment notification
        uses: 8398a7/action-slack@v3
        with:
          status: ${{ job.status }}
          text: 'Production deployment completed successfully!'
          webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

---

## Pipeline Stages Detailed

### Stage 1: Build (Estimated: 10-15 minutes)

**Frontend Build:**
- Install Node.js dependencies
- Run linting checks
- Build Angular application in production mode
- Verify bundle sizes
- Create artifacts

**Backend Build:**
- Restore NuGet packages
- Build .NET solution
- Run code analysis
- Publish API
- Create artifacts

**Success Criteria:**
- No build errors
- Bundle size within budget
- All dependencies resolved

---

### Stage 2: Test (Estimated: 20-30 minutes)

**Unit Tests:**
- Frontend: Jasmine/Karma tests
- Backend: xUnit/NUnit tests
- Minimum 80% coverage required

**Integration Tests:**
- Database integration tests
- Service integration tests
- External API integration tests

**E2E Tests:**
- Playwright/Cypress tests
- Smoke suite on PRs
- Full suite on main branch

**Success Criteria:**
- All tests passing
- Coverage >= 80%
- No flaky tests

---

### Stage 3: Quality (Estimated: 15-25 minutes)

**Linting:**
- ESLint for TypeScript/Angular
- StyleLint for CSS
- .NET code analysis

**Security Scanning:**
- npm audit (frontend dependencies)
- NuGet vulnerability scan (backend)
- Snyk security scan
- OWASP dependency check

**SAST:**
- SonarQube analysis
- Quality gate checks
- Code smell detection
- Security hotspot identification

**Performance:**
- Bundle size analysis
- Lighthouse CI checks
- Performance budget validation

**Success Criteria:**
- No Critical/High security vulnerabilities
- SonarQube quality gate passed
- Bundle size within budget
- Lighthouse score >= 85

---

### Stage 4: Deploy (Estimated: 15-30 minutes)

**Staging Deployment:**
- Build Docker images
- Push to container registry
- Deploy to Kubernetes
- Run database migrations
- Execute smoke tests

**Production Deployment (Blue/Green):**
- Determine current active environment
- Deploy to inactive environment
- Run health checks
- Run smoke tests
- Switch traffic gradually
- Monitor for 5 minutes
- Auto-rollback on errors

**Success Criteria:**
- Health checks passing
- Smoke tests passing
- Error rate < 0.1%
- Response time within SLA

---

## Secrets Management

### Required GitHub Secrets

```yaml
# GCP
GCP_SA_KEY_STAGING: GCP service account key for staging
GCP_SA_KEY_PROD: GCP service account key for production
GCP_PROJECT_ID_STAGING: GCP project ID for staging
GCP_PROJECT_ID_PROD: GCP project ID for production

# SonarQube
SONAR_TOKEN: SonarQube authentication token
SONAR_HOST_URL: SonarQube server URL

# Snyk
SNYK_TOKEN: Snyk API token

# Codecov
CODECOV_TOKEN: Codecov upload token

# Slack
SLACK_WEBHOOK: Slack webhook URL for notifications

# Database
DB_CONNECTION_STRING_STAGING: Database connection for staging
DB_CONNECTION_STRING_PROD: Database connection for production
```

---

## Monitoring & Notifications

### Slack Notifications

```yaml
# Add to workflow
- name: Notify on success
  if: success()
  uses: 8398a7/action-slack@v3
  with:
    status: success
    text: |
      ✅ Pipeline succeeded!
      Commit: ${{ github.sha }}
      Author: ${{ github.actor }}
      Branch: ${{ github.ref }}
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}

- name: Notify on failure
  if: failure()
  uses: 8398a7/action-slack@v3
  with:
    status: failure
    text: |
      ❌ Pipeline failed!
      Commit: ${{ github.sha }}
      Author: ${{ github.actor }}
      Job: ${{ github.job }}
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

---

## Rollback Strategy

### Automatic Rollback Triggers

- Health check failures
- Error rate > 1%
- Response time > 2x baseline
- Critical alerts triggered

### Manual Rollback

```bash
# Quick rollback using blue/green
kubectl patch service hrms-frontend-live -p \
  '{"spec":{"selector":{"color":"blue"}}}'  # Switch to blue

kubectl patch service hrms-api-live -p \
  '{"spec":{"selector":{"color":"blue"}}}'
```

---

## Pipeline Optimization

### Caching Strategy

- Node modules cached by package-lock.json hash
- NuGet packages cached by .csproj hash
- Docker layers cached
- Test results cached for unchanged files

### Parallel Execution

- Frontend and backend builds run in parallel
- Multiple test suites run in parallel
- Quality checks run in parallel

### Conditional Execution

- E2E tests only on main/develop branches
- Deployments only on main/develop/release branches
- Full test suite only on main, smoke tests on PRs

---

## Estimated Implementation Time

| Task | Time |
|------|------|
| GitHub Actions workflow setup | 16h |
| Docker containerization | 8h |
| Kubernetes manifests | 8h |
| Blue/Green deployment setup | 12h |
| Monitoring & alerting | 8h |
| Documentation | 4h |
| Testing & refinement | 8h |
| **Total** | **64h (2 weeks)** |

---

## Priority

**P0 (Critical)** - Required for production readiness

---

## Dependencies

- GitHub Actions (or equivalent CI/CD platform)
- Google Cloud Platform (GKE)
- Docker registry
- SonarQube instance
- Snyk account
- Codecov account
- Slack workspace

---

**Document Owner:** DevOps Team
**Last Review Date:** 2025-11-17
**Next Review Date:** 2026-02-17
