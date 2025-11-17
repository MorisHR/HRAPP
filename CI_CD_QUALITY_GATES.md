# CI/CD Quality Gates - Fortune 500 Grade
## HRMS Application - Pre-Commit Hooks & Code Quality

**Version:** 1.0.0
**Last Updated:** 2025-11-17
**Owner:** DevOps Engineering Team

---

## Overview

This document defines comprehensive pre-commit hooks and code quality gates to ensure enterprise-grade code quality, security, and maintainability before code reaches the repository.

---

## Pre-Commit Hooks Configuration

### Installation & Setup

#### Install Husky (Git Hooks Manager)

```bash
# Frontend (Angular)
cd /workspaces/HRAPP/hrms-frontend
npm install --save-dev husky
npx husky install
npm pkg set scripts.prepare="husky install"

# Backend (.NET) - Use pre-commit framework
pip install pre-commit
```

#### Create `.husky/pre-commit` Hook

```bash
#!/usr/bin/env sh
. "$(dirname -- "$0")/_/husky.sh"

echo "Running pre-commit quality gates..."

# Run all quality checks
npm run pre-commit:check

# Exit with error code if checks fail
exit $?
```

### Pre-Commit Quality Gates

#### 1. Code Linting (ESLint/TSLint)

**Purpose:** Enforce consistent code style and catch common errors

**Frontend Configuration:**
```json
// Add to package.json
{
  "scripts": {
    "lint": "ng lint",
    "lint:fix": "ng lint --fix",
    "lint:staged": "eslint --ext .ts,.html --fix"
  }
}
```

**Rules Enforced:**
- No unused variables
- No console.log in production code
- No debugger statements
- Consistent naming conventions (camelCase, PascalCase)
- Maximum file length: 400 lines
- Maximum function complexity: 15
- No any types without explicit reason
- Proper TypeScript types on all functions

**Backend Configuration (.NET):**
```xml
<!-- Add to .editorconfig -->
[*.cs]
# Coding conventions
dotnet_sort_system_directives_first = true
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion

# Code quality rules
dotnet_code_quality.CA1062.null_check_validation_methods = true
dotnet_diagnostic.CA2007.severity = warning
```

#### 2. Format Checking (Prettier)

**Purpose:** Ensure consistent code formatting across the codebase

**Configuration:**
```json
// .prettierrc (Frontend)
{
  "printWidth": 100,
  "tabWidth": 2,
  "useTabs": false,
  "semi": true,
  "singleQuote": true,
  "trailingComma": "es5",
  "bracketSpacing": true,
  "arrowParens": "always",
  "endOfLine": "lf",
  "overrides": [
    {
      "files": "*.html",
      "options": {
        "parser": "angular"
      }
    }
  ]
}
```

**Scripts:**
```json
{
  "scripts": {
    "format": "prettier --write \"src/**/*.{ts,html,css,scss,json}\"",
    "format:check": "prettier --check \"src/**/*.{ts,html,css,scss,json}\""
  }
}
```

#### 3. Unit Test Execution

**Purpose:** Ensure all existing tests pass before commit

**Frontend Tests:**
```json
{
  "scripts": {
    "test:headless": "ng test --watch=false --browsers=ChromeHeadless",
    "test:coverage": "ng test --watch=false --code-coverage --browsers=ChromeHeadless"
  }
}
```

**Backend Tests:**
```bash
# Run all unit tests
dotnet test --filter "Category=Unit" --no-build --verbosity minimal

# Coverage threshold: 80%
dotnet test /p:CollectCoverage=true /p:CoverageThreshold=80
```

**Minimum Coverage Requirements:**
- Overall: 80%
- Business Logic: 90%
- API Controllers: 75%
- Services: 85%

#### 4. Type Checking (TypeScript)

**Purpose:** Catch type errors before runtime

```json
{
  "scripts": {
    "typecheck": "tsc --noEmit",
    "typecheck:strict": "tsc --noEmit --strict"
  }
}
```

**TypeScript Configuration:**
```json
// tsconfig.json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true
  }
}
```

#### 5. Commit Message Validation

**Purpose:** Enforce conventional commits for automated changelog generation

**Format:** `<type>(<scope>): <subject>`

**Allowed Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Test additions/modifications
- `chore`: Build process or auxiliary tool changes
- `ci`: CI/CD pipeline changes
- `security`: Security fixes

**Validation Script:**
```bash
#!/bin/bash
# .husky/commit-msg

commit_msg=$(cat "$1")
pattern="^(feat|fix|docs|style|refactor|perf|test|chore|ci|security)(\(.+\))?: .{1,100}$"

if ! echo "$commit_msg" | grep -Eq "$pattern"; then
  echo "ERROR: Invalid commit message format"
  echo "Format: <type>(<scope>): <subject>"
  echo "Example: feat(auth): add multi-factor authentication"
  exit 1
fi
```

#### 6. No console.log in Production Code

**Purpose:** Prevent debugging statements in production

**ESLint Rule:**
```json
{
  "rules": {
    "no-console": ["error", {
      "allow": ["warn", "error"]
    }]
  }
}
```

**Pre-commit Check:**
```bash
# Check for console.log statements
if git diff --cached --name-only | grep -E '\.(ts|js)$' | xargs grep -l 'console\.log'; then
  echo "ERROR: console.log() found in staged files"
  echo "Please remove console.log statements or use console.warn/error"
  exit 1
fi
```

#### 7. No Debugger Statements

**Purpose:** Prevent debugger statements in production

**ESLint Rule:**
```json
{
  "rules": {
    "no-debugger": "error"
  }
}
```

#### 8. TODO Comment Validation

**Purpose:** Ensure TODOs reference ticket numbers

**Pattern:** `// TODO(JIRA-123): Description`

**Validation Script:**
```bash
#!/bin/bash
# Check TODO comments have ticket references

todos=$(git diff --cached | grep -i "^\+.*TODO" | grep -v "TODO([A-Z]+-[0-9]+)")

if [ -n "$todos" ]; then
  echo "ERROR: TODO comments must reference a ticket"
  echo "Format: // TODO(JIRA-123): Description"
  echo ""
  echo "Found invalid TODOs:"
  echo "$todos"
  exit 1
fi
```

---

## Complete Pre-Commit Hook Script

**File:** `/workspaces/HRAPP/.husky/pre-commit`

```bash
#!/usr/bin/env sh
. "$(dirname -- "$0")/_/husky.sh"

echo "======================================"
echo "Running Pre-Commit Quality Gates"
echo "======================================"

FAILED=0

# 1. Linting
echo ""
echo "[1/8] Running ESLint..."
npm run lint:staged || FAILED=1

# 2. Format checking
echo ""
echo "[2/8] Checking code formatting..."
npm run format:check || FAILED=1

# 3. Type checking
echo ""
echo "[3/8] Running TypeScript type checking..."
npm run typecheck || FAILED=1

# 4. Unit tests (changed files only)
echo ""
echo "[4/8] Running unit tests..."
npm run test:headless || FAILED=1

# 5. Check for console.log
echo ""
echo "[5/8] Checking for console.log statements..."
if git diff --cached --name-only | grep -E '\.(ts|js)$' | xargs grep -l 'console\.log' 2>/dev/null; then
  echo "ERROR: console.log() found in staged files"
  FAILED=1
fi

# 6. Check for debugger
echo ""
echo "[6/8] Checking for debugger statements..."
if git diff --cached | grep -E "^\+.*debugger" 2>/dev/null; then
  echo "ERROR: debugger statement found in staged files"
  FAILED=1
fi

# 7. Check TODO comments
echo ""
echo "[7/8] Validating TODO comments..."
todos=$(git diff --cached | grep -i "^\+.*TODO" | grep -v "TODO([A-Z]+-[0-9]+)")
if [ -n "$todos" ]; then
  echo "ERROR: TODO comments must reference a ticket (e.g., TODO(JIRA-123))"
  echo "$todos"
  FAILED=1
fi

# 8. Check for sensitive data
echo ""
echo "[8/8] Checking for sensitive data..."
if git diff --cached | grep -iE "(password|api[_-]?key|secret|token|private[_-]?key)" | grep -v "PasswordHash" 2>/dev/null; then
  echo "WARNING: Possible sensitive data detected. Please review."
  read -p "Continue anyway? (y/N) " -n 1 -r
  echo
  if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    FAILED=1
  fi
fi

# Summary
echo ""
echo "======================================"
if [ $FAILED -eq 0 ]; then
  echo "✓ All pre-commit checks passed!"
  echo "======================================"
  exit 0
else
  echo "✗ Pre-commit checks failed!"
  echo "======================================"
  echo ""
  echo "Please fix the issues above before committing."
  echo "To bypass these checks (not recommended), use: git commit --no-verify"
  exit 1
fi
```

---

## Backend Pre-Commit Configuration

**File:** `/workspaces/HRAPP/.pre-commit-config.yaml`

```yaml
repos:
  # C# Formatting
  - repo: local
    hooks:
      - id: dotnet-format
        name: dotnet format
        entry: dotnet format
        language: system
        pass_filenames: false
        files: \.cs$

  # Security scanning
  - repo: local
    hooks:
      - id: security-scan
        name: Security Code Scan
        entry: bash -c 'dotnet restore && dotnet build /p:EnableSecurityCodeScan=true'
        language: system
        pass_filenames: false
        files: \.cs$

  # Unit tests
  - repo: local
    hooks:
      - id: dotnet-test
        name: .NET Unit Tests
        entry: dotnet test --filter "Category=Unit" --no-build
        language: system
        pass_filenames: false
        files: \.cs$

  # Check for secrets
  - repo: https://github.com/Yelp/detect-secrets
    rev: v1.4.0
    hooks:
      - id: detect-secrets
        args: ['--baseline', '.secrets.baseline']
        exclude: package-lock.json
```

---

## Quality Gate Success Criteria

### Mandatory Checks (Must Pass)
- All linting rules pass
- Code is properly formatted
- No TypeScript type errors
- All unit tests pass
- No console.log in production code
- No debugger statements
- TODO comments have ticket references
- No secrets in code

### Warning Checks (Can Override with Justification)
- Test coverage < 80%
- Complex functions (complexity > 15)
- Large files (> 400 lines)
- Possible sensitive data patterns

---

## Bypassing Pre-Commit Hooks

**NOT RECOMMENDED** - Use only in emergencies:

```bash
# Bypass all hooks (requires justification in PR)
git commit --no-verify -m "emergency: fix production issue"

# Document why hooks were bypassed in PR description
```

---

## Integration with IDE

### Visual Studio Code

**Extensions Required:**
- ESLint
- Prettier - Code formatter
- C# (OmniSharp)
- Angular Language Service

**Settings (.vscode/settings.json):**
```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  },
  "eslint.validate": [
    "javascript",
    "typescript",
    "html"
  ],
  "typescript.tsdk": "node_modules/typescript/lib",
  "files.trimTrailingWhitespace": true,
  "files.insertFinalNewline": true
}
```

### Visual Studio 2022

**Extensions Required:**
- CodeMaid
- Roslynator
- SonarLint

**Configure on save:**
- Tools → Options → Text Editor → C# → Code Style → Formatting
- Enable "Format document on save"

---

## Monitoring & Metrics

### Track Quality Metrics
- Pre-commit hook success rate
- Most common violations
- Time to fix violations
- Coverage trends over time

### Weekly Quality Report
```bash
# Generate weekly quality report
./scripts/generate-quality-report.sh

# Outputs:
# - Linting violations by type
# - Test coverage trends
# - Most violated rules
# - Top contributors to violations
```

---

## Troubleshooting

### Issue: Hooks not running

**Solution:**
```bash
# Re-install hooks
npx husky install
chmod +x .husky/pre-commit
```

### Issue: Tests failing on commit

**Solution:**
```bash
# Run tests manually to see detailed output
npm run test:headless

# Fix failing tests before committing
```

### Issue: Format check failing

**Solution:**
```bash
# Auto-fix formatting issues
npm run format

# Then stage changes and commit
git add .
git commit
```

---

## Maintenance

### Update Hooks Quarterly
- Review and update linting rules
- Add new quality checks as needed
- Update dependencies
- Review bypass incidents

### Team Training
- Onboard new team members on pre-commit hooks
- Document common issues and solutions
- Share best practices in team meetings

---

**Estimated Implementation Time:** 8 hours
**Priority:** P0 (Critical)
**Dependencies:** Node.js, npm, Git, .NET SDK

---

## Next Steps

1. Install Husky and configure hooks
2. Test hooks with sample commits
3. Train team on new workflow
4. Monitor compliance for 1 week
5. Adjust rules based on feedback
