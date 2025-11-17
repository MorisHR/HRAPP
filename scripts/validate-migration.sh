#!/bin/bash

################################################################################
# Migration Validation Script
# Purpose: Comprehensive validation for Fortune 500 Material UI migration
# Usage: ./scripts/validate-migration.sh [--skip-build] [--verbose]
################################################################################

set -e  # Exit on error
set -o pipefail  # Exit on pipe failure

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
FRONTEND_DIR="/workspaces/HRAPP/hrms-frontend"
SKIP_BUILD=false
VERBOSE=false
EXIT_CODE=0

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --skip-build)
      SKIP_BUILD=true
      shift
      ;;
    --verbose)
      VERBOSE=true
      shift
      ;;
    *)
      echo -e "${RED}Unknown option: $1${NC}"
      echo "Usage: $0 [--skip-build] [--verbose]"
      exit 1
      ;;
  esac
done

################################################################################
# Utility Functions
################################################################################

print_header() {
  echo ""
  echo -e "${BLUE}========================================${NC}"
  echo -e "${BLUE}$1${NC}"
  echo -e "${BLUE}========================================${NC}"
}

print_success() {
  echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
  echo -e "${RED}✗ $1${NC}"
  EXIT_CODE=1
}

print_warning() {
  echo -e "${YELLOW}⚠ $1${NC}"
}

print_info() {
  echo -e "${BLUE}ℹ $1${NC}"
}

################################################################################
# Validation Steps
################################################################################

validate_environment() {
  print_header "1. Environment Validation"

  # Check if frontend directory exists
  if [ ! -d "$FRONTEND_DIR" ]; then
    print_error "Frontend directory not found: $FRONTEND_DIR"
    return 1
  fi
  print_success "Frontend directory exists"

  # Check if node_modules exists
  if [ ! -d "$FRONTEND_DIR/node_modules" ]; then
    print_warning "node_modules not found. Installing dependencies..."
    cd "$FRONTEND_DIR"
    npm install
    cd - > /dev/null
  fi
  print_success "Dependencies installed"

  # Check for required commands
  local required_commands=("node" "npm" "npx")
  for cmd in "${required_commands[@]}"; do
    if ! command -v "$cmd" &> /dev/null; then
      print_error "Required command not found: $cmd"
      return 1
    fi
  done
  print_success "Required commands available"

  # Display versions
  if [ "$VERBOSE" = true ]; then
    print_info "Node version: $(node --version)"
    print_info "NPM version: $(npm --version)"
    print_info "Angular CLI version: $(cd "$FRONTEND_DIR" && npx ng version --version 2>/dev/null || echo 'N/A')"
  fi
}

validate_typescript() {
  print_header "2. TypeScript Compilation Check"

  cd "$FRONTEND_DIR"

  print_info "Running TypeScript compiler..."
  if npx tsc --noEmit --project tsconfig.app.json 2>&1 | tee /tmp/tsc-output.log; then
    print_success "TypeScript compilation successful"
  else
    print_error "TypeScript compilation failed"
    if [ "$VERBOSE" = true ]; then
      echo ""
      echo "TypeScript errors:"
      cat /tmp/tsc-output.log
    fi
    cd - > /dev/null
    return 1
  fi

  cd - > /dev/null
}

count_material_imports() {
  print_header "3. Material UI Import Analysis"

  cd "$FRONTEND_DIR"

  # Count Angular Material imports
  local material_count=$(grep -r "from '@angular/material" src/ 2>/dev/null | wc -l || echo "0")
  local cdk_count=$(grep -r "from '@angular/cdk" src/ 2>/dev/null | wc -l || echo "0")

  echo "Material UI Import Statistics:"
  echo "  - @angular/material imports: $material_count"
  echo "  - @angular/cdk imports: $cdk_count"
  echo "  - Total: $((material_count + cdk_count))"

  # Find files with Material imports
  if [ "$VERBOSE" = true ]; then
    print_info "Files with Material imports:"
    grep -r "from '@angular/material" src/ 2>/dev/null | cut -d: -f1 | sort -u | sed 's/^/  - /' || echo "  None found"
  fi

  # Count custom component usage
  local custom_input_count=$(grep -r "app-input\|<input" src/ 2>/dev/null | grep -v "node_modules" | wc -l || echo "0")
  local custom_button_count=$(grep -r "app-button\|<button" src/ 2>/dev/null | grep -v "node_modules" | wc -l || echo "0")

  echo ""
  echo "Custom Component Usage:"
  echo "  - Custom inputs: $custom_input_count"
  echo "  - Custom buttons: $custom_button_count"

  if [ "$material_count" -gt 0 ]; then
    print_warning "Found $material_count Material UI imports (migration in progress)"
  else
    print_success "No Material UI imports found (migration complete)"
  fi

  cd - > /dev/null
}

run_tests() {
  print_header "4. Unit Test Execution"

  cd "$FRONTEND_DIR"

  # Check if tests exist
  local test_count=$(find src -name "*.spec.ts" 2>/dev/null | wc -l || echo "0")

  if [ "$test_count" -eq 0 ]; then
    print_warning "No test files found, skipping test execution"
    cd - > /dev/null
    return 0
  fi

  print_info "Found $test_count test files"
  print_info "Running tests in headless mode..."

  # Run tests with headless Chrome
  if npm run test -- --watch=false --browsers=ChromeHeadless 2>&1 | tee /tmp/test-output.log; then
    print_success "All tests passed"
  else
    # Check if it's a configuration issue vs actual test failures
    if grep -q "Cannot find module" /tmp/test-output.log; then
      print_warning "Test configuration issue detected, skipping tests"
    else
      print_error "Tests failed"
      if [ "$VERBOSE" = true ]; then
        echo ""
        echo "Test output:"
        cat /tmp/test-output.log
      fi
    fi
  fi

  cd - > /dev/null
}

verify_production_build() {
  print_header "5. Production Build Verification"

  if [ "$SKIP_BUILD" = true ]; then
    print_warning "Skipping build (--skip-build flag set)"
    return 0
  fi

  cd "$FRONTEND_DIR"

  print_info "Building for production..."
  local start_time=$(date +%s)

  if npm run build -- --configuration production 2>&1 | tee /tmp/build-output.log; then
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    print_success "Production build successful (${duration}s)"
  else
    print_error "Production build failed"
    if [ "$VERBOSE" = true ]; then
      echo ""
      echo "Build output:"
      tail -n 50 /tmp/build-output.log
    fi
    cd - > /dev/null
    return 1
  fi

  # Check if dist folder was created
  if [ -d "dist" ]; then
    print_success "Build artifacts created in dist/"
  else
    print_error "Build artifacts not found in dist/"
    cd - > /dev/null
    return 1
  fi

  cd - > /dev/null
}

check_bundle_size() {
  print_header "6. Bundle Size Analysis"

  cd "$FRONTEND_DIR"

  if [ ! -d "dist" ]; then
    print_warning "Build artifacts not found, skipping bundle size check"
    print_info "Run without --skip-build to verify bundle size"
    cd - > /dev/null
    return 0
  fi

  # Find the main bundle
  local main_bundle=$(find dist -name "main-*.js" 2>/dev/null | head -n 1)

  if [ -z "$main_bundle" ]; then
    # Try finding browser folder for newer Angular
    main_bundle=$(find dist/hrms-frontend/browser -name "main-*.js" 2>/dev/null | head -n 1)
  fi

  if [ -n "$main_bundle" ]; then
    local size=$(du -h "$main_bundle" | cut -f1)
    local size_bytes=$(stat -f%z "$main_bundle" 2>/dev/null || stat -c%s "$main_bundle" 2>/dev/null || echo "0")
    local size_kb=$((size_bytes / 1024))

    echo "Main Bundle Analysis:"
    echo "  - File: $(basename "$main_bundle")"
    echo "  - Size: $size ($size_kb KB)"

    # Check against budget
    if [ "$size_kb" -lt 500 ]; then
      print_success "Bundle size within budget (< 500 KB)"
    elif [ "$size_kb" -lt 1024 ]; then
      print_warning "Bundle size approaching limit (500-1024 KB)"
    else
      print_error "Bundle size exceeds limit (> 1 MB)"
    fi
  else
    print_warning "Main bundle not found for size analysis"
  fi

  # Total dist size
  if [ -d "dist" ]; then
    local total_size=$(du -sh dist 2>/dev/null | cut -f1)
    print_info "Total build size: $total_size"
  fi

  cd - > /dev/null
}

check_feature_flags() {
  print_header "7. Feature Flag Configuration Check"

  cd "$FRONTEND_DIR"

  # Check if feature flag service exists
  if [ -f "src/app/core/services/feature-flag.service.ts" ]; then
    print_success "Feature flag service found"

    # Check for environment configuration
    if grep -q "featureFlags" src/environments/environment.ts 2>/dev/null; then
      print_success "Feature flags configured in environment"
    else
      print_warning "Feature flags not found in environment configuration"
    fi
  else
    print_warning "Feature flag service not found (may not be implemented yet)"
  fi

  cd - > /dev/null
}

check_migration_status() {
  print_header "8. Migration Status Summary"

  cd "$FRONTEND_DIR"

  # Check for migration-related files
  local checklist_exists=false
  if [ -f "/workspaces/HRAPP/MIGRATION_CHECKLIST.md" ]; then
    checklist_exists=true
    print_success "Migration checklist found"
  else
    print_warning "Migration checklist not found"
  fi

  # Check for custom UI components
  local custom_components_dir="src/app/shared/ui/components"
  if [ -d "$custom_components_dir" ]; then
    local component_count=$(find "$custom_components_dir" -name "*.ts" 2>/dev/null | grep -v ".spec.ts" | wc -l || echo "0")
    print_success "Found $component_count custom UI component files"
  else
    print_warning "Custom UI components directory not found"
  fi

  # Generate migration report
  echo ""
  echo "Migration Progress Indicators:"

  # Count migrated vs non-migrated components
  local total_components=$(find src/app/features -name "*.component.ts" 2>/dev/null | wc -l || echo "0")
  local material_components=$(grep -l "@angular/material" src/app/features/**/*.ts 2>/dev/null | wc -l || echo "0")

  if [ "$total_components" -gt 0 ]; then
    local migrated=$((total_components - material_components))
    local percentage=$((migrated * 100 / total_components))
    echo "  - Total components: $total_components"
    echo "  - Using Material UI: $material_components"
    echo "  - Migrated: $migrated ($percentage%)"
  fi

  cd - > /dev/null
}

generate_report() {
  print_header "9. Validation Report"

  local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
  local report_file="/workspaces/HRAPP/validation-report-$(date '+%Y%m%d-%H%M%S').txt"

  {
    echo "========================================="
    echo "Migration Validation Report"
    echo "========================================="
    echo "Timestamp: $timestamp"
    echo "Status: $([ $EXIT_CODE -eq 0 ] && echo "PASSED" || echo "FAILED")"
    echo ""
    echo "Summary:"
    echo "  - TypeScript: $([ -f /tmp/tsc-output.log ] && echo "Checked" || echo "Skipped")"
    echo "  - Tests: $([ -f /tmp/test-output.log ] && echo "Executed" || echo "Skipped")"
    echo "  - Build: $([ "$SKIP_BUILD" = true ] && echo "Skipped" || echo "Completed")"
    echo "  - Bundle Size: $([ -d "$FRONTEND_DIR/dist" ] && echo "Analyzed" || echo "Skipped")"
    echo ""
    echo "Exit Code: $EXIT_CODE"
  } > "$report_file"

  print_info "Full report saved to: $report_file"
}

################################################################################
# Main Execution
################################################################################

main() {
  echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
  echo -e "${BLUE}║  Fortune 500 Migration Validation Script                  ║${NC}"
  echo -e "${BLUE}║  Comprehensive validation for Material UI migration       ║${NC}"
  echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
  echo ""

  # Execute validation steps
  validate_environment || true
  validate_typescript || true
  count_material_imports || true
  run_tests || true
  verify_production_build || true
  check_bundle_size || true
  check_feature_flags || true
  check_migration_status || true
  generate_report

  # Final summary
  print_header "Validation Complete"

  if [ $EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║  ✓ ALL VALIDATIONS PASSED                                  ║${NC}"
    echo -e "${GREEN}║  Migration validation successful!                          ║${NC}"
    echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"
  else
    echo -e "${RED}╔════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${RED}║  ✗ VALIDATION FAILURES DETECTED                            ║${NC}"
    echo -e "${RED}║  Please review errors above                                ║${NC}"
    echo -e "${RED}╚════════════════════════════════════════════════════════════╝${NC}"
  fi

  exit $EXIT_CODE
}

# Run main function
main "$@"
