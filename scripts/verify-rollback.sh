#!/bin/bash

################################################################################
# Rollback Verification Script
# Purpose: Verify Material UI components still work after feature flag disable
# Usage: ./scripts/verify-rollback.sh [--verbose] [--fix-imports]
################################################################################

set -e  # Exit on error
set -o pipefail  # Exit on pipe failure

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
FRONTEND_DIR="/workspaces/HRAPP/hrms-frontend"
VERBOSE=false
FIX_IMPORTS=false
EXIT_CODE=0

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --verbose)
      VERBOSE=true
      shift
      ;;
    --fix-imports)
      FIX_IMPORTS=true
      shift
      ;;
    *)
      echo -e "${RED}Unknown option: $1${NC}"
      echo "Usage: $0 [--verbose] [--fix-imports]"
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
  echo -e "${CYAN}ℹ $1${NC}"
}

################################################################################
# Verification Steps
################################################################################

check_feature_flags() {
  print_header "1. Feature Flag Status Check"

  cd "$FRONTEND_DIR"

  # Check if feature flag service exists
  if [ ! -f "src/app/core/services/feature-flag.service.ts" ]; then
    print_warning "Feature flag service not found (may not be implemented yet)"
    cd - > /dev/null
    return 0
  fi

  print_success "Feature flag service found"

  # Check environment configuration
  if [ -f "src/environments/environment.ts" ]; then
    local enabled_flags=$(grep -A 5 "featureFlags" src/environments/environment.ts 2>/dev/null | grep "enabled: true" | wc -l || echo "0")
    local disabled_flags=$(grep -A 5 "featureFlags" src/environments/environment.ts 2>/dev/null | grep "enabled: false" | wc -l || echo "0")

    echo "Feature Flag Configuration:"
    echo "  - Enabled flags: $enabled_flags"
    echo "  - Disabled flags: $disabled_flags"

    if [ "$VERBOSE" = true ]; then
      print_info "Feature flag configuration:"
      grep -A 10 "featureFlags" src/environments/environment.ts 2>/dev/null | sed 's/^/  /' || echo "  No flags configured"
    fi

    if [ "$enabled_flags" -eq 0 ]; then
      print_success "All feature flags are disabled (rollback state)"
    else
      print_warning "$enabled_flags feature flag(s) still enabled"
      print_info "Expected state for rollback: all flags disabled"
    fi
  else
    print_warning "Environment file not found"
  fi

  cd - > /dev/null
}

verify_material_imports() {
  print_header "2. Material UI Import Verification"

  cd "$FRONTEND_DIR"

  # Check if Material UI is still installed
  if [ ! -d "node_modules/@angular/material" ]; then
    print_error "Material UI not found in node_modules"
    print_info "Run: npm install to restore dependencies"
    cd - > /dev/null
    return 1
  fi
  print_success "Material UI package found"

  # Find files with Material imports
  local material_files=$(find src/app -name "*.ts" -exec grep -l "from '@angular/material" {} \; 2>/dev/null | wc -l || echo "0")

  echo "Material UI Import Statistics:"
  echo "  - Files with Material imports: $material_files"

  if [ "$material_files" -eq 0 ]; then
    print_warning "No Material imports found - rollback may not work!"
    print_info "Original Material UI code may have been removed"
    return 1
  else
    print_success "Found $material_files file(s) with Material imports"
  fi

  # List specific Material components used
  if [ "$VERBOSE" = true ]; then
    print_info "Material components in use:"
    grep -rh "from '@angular/material" src/app 2>/dev/null | \
      sed 's/.*@angular\/material\///' | \
      sed 's/[";].*//' | \
      sort -u | \
      sed 's/^/  - @angular\/material\//' || echo "  None found"
  fi

  cd - > /dev/null
}

check_material_modules() {
  print_header "3. Material Module Configuration"

  cd "$FRONTEND_DIR"

  # Look for Material module imports in app.config.ts or main.ts
  local module_files=$(find src/app -name "*.ts" -exec grep -l "Material.*Module" {} \; 2>/dev/null)

  if [ -z "$module_files" ]; then
    print_warning "No Material module imports found in TypeScript files"
  else
    print_success "Material modules configured"

    if [ "$VERBOSE" = true ]; then
      print_info "Files with Material module imports:"
      echo "$module_files" | sed 's/^/  - /'
    fi
  fi

  # Check package.json
  if grep -q "@angular/material" package.json 2>/dev/null; then
    local material_version=$(grep "@angular/material" package.json | sed 's/.*"@angular\/material": "//' | sed 's/".*//')
    print_success "Material UI in package.json (version: $material_version)"
  else
    print_error "Material UI not found in package.json"
  fi

  cd - > /dev/null
}

test_material_compilation() {
  print_header "4. TypeScript Compilation Test"

  cd "$FRONTEND_DIR"

  print_info "Testing TypeScript compilation with Material UI..."

  if npx tsc --noEmit --project tsconfig.app.json 2>&1 | tee /tmp/rollback-tsc-output.log; then
    print_success "TypeScript compilation successful"
  else
    print_error "TypeScript compilation failed"

    # Check for Material-related errors
    if grep -q "@angular/material" /tmp/rollback-tsc-output.log; then
      print_error "Material UI compilation errors detected"
      if [ "$VERBOSE" = true ]; then
        echo ""
        echo "Material-related errors:"
        grep "@angular/material" /tmp/rollback-tsc-output.log | sed 's/^/  /'
      fi
    fi

    cd - > /dev/null
    return 1
  fi

  cd - > /dev/null
}

check_broken_imports() {
  print_header "5. Broken Import Detection"

  cd "$FRONTEND_DIR"

  # Check for common broken import patterns
  local broken_patterns=(
    "from '@shared/ui/components"
    "from '@app/shared/ui"
    "from '../../shared/ui/components"
    "from '../shared/ui/components"
  )

  local broken_found=false

  for pattern in "${broken_patterns[@]}"; do
    local count=$(grep -r "$pattern" src/app 2>/dev/null | grep -v "node_modules" | wc -l || echo "0")
    if [ "$count" -gt 0 ]; then
      print_warning "Found $count custom component import(s): $pattern"
      broken_found=true

      if [ "$VERBOSE" = true ]; then
        grep -r "$pattern" src/app 2>/dev/null | grep -v "node_modules" | cut -d: -f1 | sort -u | sed 's/^/  - /'
      fi
    fi
  done

  if [ "$broken_found" = false ]; then
    print_success "No custom component imports detected"
  else
    print_warning "Custom component imports found - may cause issues during rollback"

    if [ "$FIX_IMPORTS" = true ]; then
      print_info "Would fix imports (feature not yet implemented)"
      # TODO: Implement automatic import fixing
    fi
  fi

  cd - > /dev/null
}

verify_material_components() {
  print_header "6. Material Component Availability"

  cd "$FRONTEND_DIR"

  # List of common Material components to check
  local material_components=(
    "MatFormFieldModule"
    "MatInputModule"
    "MatButtonModule"
    "MatSelectModule"
    "MatCheckboxModule"
    "MatDialogModule"
    "MatSnackBarModule"
    "MatToolbarModule"
    "MatCardModule"
  )

  local available_count=0
  local checked_count=0

  echo "Checking Material component availability:"

  for component in "${material_components[@]}"; do
    checked_count=$((checked_count + 1))

    # Check if component is referenced in code
    if grep -rq "$component" src/app 2>/dev/null; then
      available_count=$((available_count + 1))
      if [ "$VERBOSE" = true ]; then
        echo -e "  ${GREEN}✓${NC} $component (in use)"
      fi
    else
      if [ "$VERBOSE" = true ]; then
        echo -e "  ${YELLOW}-${NC} $component (not in use)"
      fi
    fi
  done

  echo ""
  echo "  Available: $available_count / $checked_count checked"

  if [ "$available_count" -eq 0 ]; then
    print_warning "No Material components found in use"
  else
    print_success "$available_count Material component(s) available"
  fi

  cd - > /dev/null
}

test_material_build() {
  print_header "7. Material UI Build Test"

  cd "$FRONTEND_DIR"

  print_info "Running development build to verify Material UI..."

  # Try a dev build (faster than prod)
  if npm run build -- --configuration development 2>&1 | tee /tmp/rollback-build-output.log; then
    print_success "Development build successful"

    # Check for Material-related warnings
    if grep -q "@angular/material" /tmp/rollback-build-output.log; then
      print_warning "Material UI warnings detected in build"
      if [ "$VERBOSE" = true ]; then
        grep "@angular/material" /tmp/rollback-build-output.log | sed 's/^/  /'
      fi
    fi
  else
    print_error "Development build failed"

    if [ "$VERBOSE" = true ]; then
      echo ""
      echo "Build errors:"
      tail -n 20 /tmp/rollback-build-output.log
    fi

    cd - > /dev/null
    return 1
  fi

  cd - > /dev/null
}

check_dual_implementation() {
  print_header "8. Dual Implementation Check"

  cd "$FRONTEND_DIR"

  # Check for components that might have both Material and custom implementations
  print_info "Checking for dual implementations..."

  local components_with_flags=$(grep -r "featureFlagService\|isEnabled" src/app 2>/dev/null | grep -v "node_modules" | cut -d: -f1 | sort -u | wc -l || echo "0")

  if [ "$components_with_flags" -gt 0 ]; then
    print_success "Found $components_with_flags component(s) with feature flag logic"
    print_info "These components can switch between Material and custom UI"

    if [ "$VERBOSE" = true ]; then
      print_info "Components with feature flags:"
      grep -r "featureFlagService\|isEnabled" src/app 2>/dev/null | grep -v "node_modules" | cut -d: -f1 | sort -u | sed 's/^/  - /'
    fi
  else
    print_warning "No components with feature flag logic found"
    print_info "Components may have been fully migrated (no rollback path)"
  fi

  cd - > /dev/null
}

generate_rollback_report() {
  print_header "9. Rollback Verification Report"

  local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
  local report_file="/workspaces/HRAPP/rollback-verification-$(date '+%Y%m%d-%H%M%S').txt"

  {
    echo "========================================="
    echo "Rollback Verification Report"
    echo "========================================="
    echo "Timestamp: $timestamp"
    echo "Status: $([ $EXIT_CODE -eq 0 ] && echo "PASSED - Rollback Possible" || echo "FAILED - Issues Detected")"
    echo ""
    echo "Summary:"
    echo "  - Material UI installed: $([ -d "$FRONTEND_DIR/node_modules/@angular/material" ] && echo "Yes" || echo "No")"
    echo "  - Material imports found: $(find "$FRONTEND_DIR/src/app" -name "*.ts" -exec grep -l "from '@angular/material" {} \; 2>/dev/null | wc -l)"
    echo "  - Feature flags configured: $([ -f "$FRONTEND_DIR/src/app/core/services/feature-flag.service.ts" ] && echo "Yes" || echo "No")"
    echo "  - TypeScript compilation: $([ -f /tmp/rollback-tsc-output.log ] && echo "Checked" || echo "Skipped")"
    echo "  - Build verification: $([ -f /tmp/rollback-build-output.log ] && echo "Checked" || echo "Skipped")"
    echo ""
    echo "Exit Code: $EXIT_CODE"
    echo ""
    echo "Recommendations:"
    if [ $EXIT_CODE -eq 0 ]; then
      echo "  - Rollback to Material UI is safe"
      echo "  - Disable feature flags to revert to Material UI"
      echo "  - Monitor for any issues after rollback"
    else
      echo "  - Review errors above before attempting rollback"
      echo "  - Restore Material UI dependencies if missing"
      echo "  - Check for broken imports"
    fi
  } > "$report_file"

  print_info "Report saved to: $report_file"
}

################################################################################
# Rollback Actions
################################################################################

disable_feature_flags() {
  print_header "Rollback Action: Disable Feature Flags"

  cd "$FRONTEND_DIR"

  if [ ! -f "src/environments/environment.ts" ]; then
    print_error "Environment file not found"
    cd - > /dev/null
    return 1
  fi

  print_info "This would disable all feature flags in environment.ts"
  print_warning "Not implemented - manual edit required"

  echo ""
  print_info "To manually disable feature flags:"
  echo "  1. Edit: src/environments/environment.ts"
  echo "  2. Set all featureFlags.enabled to false"
  echo "  3. Or set rolloutPercentage to 0"
  echo "  4. Rebuild application: npm run build"

  cd - > /dev/null
}

################################################################################
# Main Execution
################################################################################

main() {
  echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
  echo -e "${BLUE}║  Rollback Verification Script                              ║${NC}"
  echo -e "${BLUE}║  Verify Material UI still works after migration attempt   ║${NC}"
  echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
  echo ""

  # Execute verification steps
  check_feature_flags || true
  verify_material_imports || true
  check_material_modules || true
  test_material_compilation || true
  check_broken_imports || true
  verify_material_components || true
  test_material_build || true
  check_dual_implementation || true
  generate_rollback_report

  # Optionally perform rollback actions
  if [ "$EXIT_CODE" -eq 0 ]; then
    echo ""
    read -p "Would you like to disable feature flags now? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
      disable_feature_flags
    fi
  fi

  # Final summary
  print_header "Verification Complete"

  if [ $EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║  ✓ ROLLBACK VERIFICATION PASSED                            ║${NC}"
    echo -e "${GREEN}║  Material UI components are functional                     ║${NC}"
    echo -e "${GREEN}║  Safe to rollback by disabling feature flags               ║${NC}"
    echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"
  else
    echo -e "${RED}╔════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${RED}║  ✗ ROLLBACK VERIFICATION FAILED                            ║${NC}"
    echo -e "${RED}║  Issues detected with Material UI                          ║${NC}"
    echo -e "${RED}║  Review errors before attempting rollback                  ║${NC}"
    echo -e "${RED}╚════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    print_info "Recommended actions:"
    echo "  1. Run: npm install (restore dependencies)"
    echo "  2. Verify Material UI imports exist in components"
    echo "  3. Check for removed Material UI code"
    echo "  4. Run: ./scripts/validate-migration.sh --verbose"
  fi

  exit $EXIT_CODE
}

# Run main function
main "$@"
