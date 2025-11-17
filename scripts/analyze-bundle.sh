#!/bin/bash

################################################################################
# Bundle Analysis Script
# Purpose: Analyze Angular bundle composition and size
# Usage: ./scripts/analyze-bundle.sh [--skip-build] [--open-browser]
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
SKIP_BUILD=false
OPEN_BROWSER=false

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --skip-build)
      SKIP_BUILD=true
      shift
      ;;
    --open-browser)
      OPEN_BROWSER=true
      shift
      ;;
    *)
      echo -e "${RED}Unknown option: $1${NC}"
      echo "Usage: $0 [--skip-build] [--open-browser]"
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
}

print_warning() {
  echo -e "${YELLOW}⚠ $1${NC}"
}

print_info() {
  echo -e "${CYAN}ℹ $1${NC}"
}

################################################################################
# Analysis Functions
################################################################################

build_with_stats() {
  print_header "1. Building with Stats"

  cd "$FRONTEND_DIR"

  if [ "$SKIP_BUILD" = true ]; then
    print_warning "Skipping build (--skip-build flag set)"

    if [ ! -d "dist" ]; then
      print_error "No dist folder found. Cannot skip build."
      exit 1
    fi

    cd - > /dev/null
    return 0
  fi

  print_info "Building production bundle with stats..."

  # Angular 17+ uses esbuild, so we need to use different approach
  # Build with production configuration
  if npm run build -- --configuration production --stats-json 2>&1 | tee /tmp/bundle-build.log; then
    print_success "Production build completed"
  else
    # If stats-json fails, try regular build
    print_warning "Stats build failed, trying regular production build..."
    if npm run build -- --configuration production 2>&1 | tee /tmp/bundle-build.log; then
      print_success "Production build completed (without stats)"
    else
      print_error "Production build failed"
      cd - > /dev/null
      exit 1
    fi
  fi

  cd - > /dev/null
}

analyze_bundle_files() {
  print_header "2. Bundle File Analysis"

  cd "$FRONTEND_DIR"

  # Find the browser directory (Angular 17+ output structure)
  local browser_dir=""
  if [ -d "dist/hrms-frontend/browser" ]; then
    browser_dir="dist/hrms-frontend/browser"
  elif [ -d "dist/browser" ]; then
    browser_dir="dist/browser"
  elif [ -d "dist" ]; then
    browser_dir="dist"
  else
    print_error "Cannot find dist directory"
    cd - > /dev/null
    return 1
  fi

  print_success "Found build output: $browser_dir"

  # Analyze JavaScript files
  echo ""
  print_info "JavaScript Bundle Files:"
  echo ""

  find "$browser_dir" -name "*.js" -type f | while read -r file; do
    local size=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null)
    local size_kb=$((size / 1024))
    local basename=$(basename "$file")

    # Color code by size
    if [ "$size_kb" -lt 100 ]; then
      echo -e "  ${GREEN}$basename${NC} - ${size_kb} KB"
    elif [ "$size_kb" -lt 300 ]; then
      echo -e "  ${YELLOW}$basename${NC} - ${size_kb} KB"
    else
      echo -e "  ${RED}$basename${NC} - ${size_kb} KB"
    fi
  done

  # Total JavaScript size
  local total_js_size=$(find "$browser_dir" -name "*.js" -type f -exec stat -f%z {} 2>/dev/null \; -o -exec stat -c%s {} 2>/dev/null \; | awk '{sum+=$1} END {print sum}')
  local total_js_kb=$((total_js_size / 1024))

  echo ""
  print_info "Total JavaScript size: ${total_js_kb} KB"

  # Analyze CSS files
  echo ""
  print_info "CSS Bundle Files:"
  echo ""

  find "$browser_dir" -name "*.css" -type f | while read -r file; do
    local size=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null)
    local size_kb=$((size / 1024))
    local basename=$(basename "$file")
    echo -e "  ${CYAN}$basename${NC} - ${size_kb} KB"
  done

  # Total CSS size
  local total_css_size=$(find "$browser_dir" -name "*.css" -type f -exec stat -f%z {} 2>/dev/null \; -o -exec stat -c%s {} 2>/dev/null \; | awk '{sum+=$1} END {print sum}')
  local total_css_kb=$((total_css_size / 1024))

  echo ""
  print_info "Total CSS size: ${total_css_kb} KB"

  # Total bundle size
  local total_size=$((total_js_size + total_css_size))
  local total_kb=$((total_size / 1024))

  echo ""
  print_header "Bundle Size Summary"
  echo "  JavaScript: ${total_js_kb} KB"
  echo "  CSS: ${total_css_kb} KB"
  echo "  Total: ${total_kb} KB"

  # Check against budget
  if [ "$total_kb" -lt 500 ]; then
    print_success "Bundle size excellent (< 500 KB)"
  elif [ "$total_kb" -lt 1024 ]; then
    print_warning "Bundle size acceptable (500-1024 KB)"
  else
    print_error "Bundle size exceeds recommended limit (> 1 MB)"
  fi

  cd - > /dev/null
}

check_for_analyzer_tool() {
  print_header "3. Bundle Analyzer Tool Check"

  cd "$FRONTEND_DIR"

  # Check if webpack-bundle-analyzer is installed
  if npm list webpack-bundle-analyzer > /dev/null 2>&1; then
    print_success "webpack-bundle-analyzer is installed"
    return 0
  else
    print_warning "webpack-bundle-analyzer not installed"
    echo ""
    print_info "To install bundle analyzer:"
    echo "  npm install --save-dev webpack-bundle-analyzer"
    echo ""
    print_info "Skipping visual bundle analysis..."
    cd - > /dev/null
    return 1
  fi
}

run_bundle_analyzer() {
  print_header "4. Visual Bundle Analysis"

  cd "$FRONTEND_DIR"

  # Check if stats.json exists
  local stats_file=""
  if [ -f "dist/hrms-frontend/stats.json" ]; then
    stats_file="dist/hrms-frontend/stats.json"
  elif [ -f "dist/stats.json" ]; then
    stats_file="dist/stats.json"
  fi

  if [ -z "$stats_file" ]; then
    print_warning "stats.json not found"
    print_info "Angular 17+ uses esbuild which doesn't generate webpack stats"
    print_info "Using alternative analysis method..."

    cd - > /dev/null
    return 1
  fi

  print_success "Found stats file: $stats_file"

  # Check if analyzer is available
  if ! npm list webpack-bundle-analyzer > /dev/null 2>&1; then
    print_warning "webpack-bundle-analyzer not installed, skipping"
    cd - > /dev/null
    return 1
  fi

  # Generate report
  print_info "Generating bundle analysis report..."

  local report_file="dist/bundle-report.html"

  if npx webpack-bundle-analyzer "$stats_file" --mode static --report "$report_file" --no-open; then
    print_success "Bundle report generated: $report_file"

    if [ "$OPEN_BROWSER" = true ]; then
      print_info "Opening report in browser..."
      if command -v xdg-open > /dev/null; then
        xdg-open "$report_file"
      elif command -v open > /dev/null; then
        open "$report_file"
      else
        print_warning "Cannot open browser automatically"
        print_info "Open manually: $report_file"
      fi
    fi
  else
    print_error "Failed to generate bundle report"
  fi

  cd - > /dev/null
}

analyze_dependencies() {
  print_header "5. Dependency Analysis"

  cd "$FRONTEND_DIR"

  print_info "Analyzing package dependencies..."

  # Get production dependencies
  echo ""
  print_info "Production Dependencies:"

  if [ -f "package.json" ]; then
    # Count total dependencies
    local dep_count=$(node -p "Object.keys(require('./package.json').dependencies || {}).length")
    local dev_dep_count=$(node -p "Object.keys(require('./package.json').devDependencies || {}).length")

    echo "  - Production: $dep_count packages"
    echo "  - Development: $dev_dep_count packages"
    echo "  - Total: $((dep_count + dev_dep_count)) packages"

    # Check for large dependencies
    echo ""
    print_info "Checking for large dependencies..."

    # Material UI
    if grep -q "@angular/material" package.json; then
      print_warning "Angular Material is installed (consider removing if fully migrated)"
    fi

    # Chart.js
    if grep -q "chart.js" package.json; then
      print_info "chart.js detected (visualization library)"
    fi

    # SignalR
    if grep -q "@microsoft/signalr" package.json; then
      print_info "SignalR detected (real-time communication)"
    fi
  fi

  cd - > /dev/null
}

analyze_source_code() {
  print_header "6. Source Code Analysis"

  cd "$FRONTEND_DIR"

  print_info "Analyzing source code composition..."

  # Count TypeScript files
  local ts_files=$(find src -name "*.ts" ! -name "*.spec.ts" | wc -l)
  local spec_files=$(find src -name "*.spec.ts" | wc -l)
  local html_files=$(find src -name "*.html" | wc -l)
  local scss_files=$(find src -name "*.scss" | wc -l)

  echo ""
  echo "Source File Statistics:"
  echo "  - TypeScript files: $ts_files"
  echo "  - Test files: $spec_files"
  echo "  - HTML templates: $html_files"
  echo "  - SCSS files: $scss_files"

  # Analyze component count
  local components=$(find src -name "*.component.ts" | wc -l)
  local services=$(find src -name "*.service.ts" | wc -l)

  echo ""
  echo "Application Structure:"
  echo "  - Components: $components"
  echo "  - Services: $services"

  # Material vs Custom component usage
  local material_usage=$(grep -r "from '@angular/material" src 2>/dev/null | wc -l || echo "0")
  local custom_usage=$(grep -r "from '@shared/ui\|from '@app/shared/ui" src 2>/dev/null | wc -l || echo "0")

  echo ""
  echo "UI Component Usage:"
  echo "  - Material UI imports: $material_usage"
  echo "  - Custom UI imports: $custom_usage"

  if [ "$material_usage" -gt "$custom_usage" ]; then
    print_warning "More Material imports than custom components"
    print_info "Migration may still be in progress"
  elif [ "$custom_usage" -gt "$material_usage" ]; then
    print_success "More custom components than Material UI"
    print_info "Migration is progressing well"
  fi

  cd - > /dev/null
}

generate_recommendations() {
  print_header "7. Optimization Recommendations"

  cd "$FRONTEND_DIR"

  echo ""
  print_info "Bundle Optimization Tips:"

  # Check for lazy loading
  local lazy_routes=$(grep -r "loadChildren" src 2>/dev/null | wc -l || echo "0")
  if [ "$lazy_routes" -gt 0 ]; then
    print_success "Lazy loading in use ($lazy_routes routes)"
  else
    print_warning "Consider implementing lazy loading for feature modules"
  fi

  # Check for tree-shaking
  if grep -q "\"optimization\": true" angular.json 2>/dev/null; then
    print_success "Build optimization enabled"
  else
    print_warning "Verify optimization is enabled in angular.json"
  fi

  # Material UI removal
  if grep -q "@angular/material" package.json 2>/dev/null; then
    echo ""
    print_info "Material UI Removal Checklist:"
    echo "  1. Verify all components migrated to custom UI"
    echo "  2. Run: grep -r '@angular/material' src/"
    echo "  3. If no imports found, remove package:"
    echo "     npm uninstall @angular/material @angular/cdk"
    echo "  4. Expected bundle size reduction: ~300-500 KB"
  fi

  # General recommendations
  echo ""
  print_info "General Recommendations:"
  echo "  - Implement component lazy loading"
  echo "  - Use OnPush change detection strategy"
  echo "  - Optimize images (WebP format, lazy loading)"
  echo "  - Remove unused dependencies"
  echo "  - Enable gzip/brotli compression"
  echo "  - Consider code splitting for large features"

  cd - > /dev/null
}

save_analysis_report() {
  print_header "8. Saving Analysis Report"

  local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
  local report_file="/workspaces/HRAPP/bundle-analysis-$(date '+%Y%m%d-%H%M%S').txt"

  cd "$FRONTEND_DIR"

  {
    echo "========================================="
    echo "Bundle Analysis Report"
    echo "========================================="
    echo "Generated: $timestamp"
    echo ""
    echo "Build Information:"
    echo "  - Angular Version: $(grep '@angular/core' package.json | sed 's/.*: "//' | sed 's/".*//')"
    echo "  - Node Version: $(node --version)"
    echo "  - Build Configuration: production"
    echo ""
    echo "Bundle Size:"

    # Get bundle sizes
    local browser_dir="dist/hrms-frontend/browser"
    if [ ! -d "$browser_dir" ]; then
      browser_dir="dist"
    fi

    if [ -d "$browser_dir" ]; then
      local total_js=$(find "$browser_dir" -name "*.js" -exec stat -f%z {} 2>/dev/null \; -o -exec stat -c%s {} 2>/dev/null \; | awk '{sum+=$1} END {print sum/1024}')
      local total_css=$(find "$browser_dir" -name "*.css" -exec stat -f%z {} 2>/dev/null \; -o -exec stat -c%s {} 2>/dev/null \; | awk '{sum+=$1} END {print sum/1024}')
      echo "  - JavaScript: ${total_js} KB"
      echo "  - CSS: ${total_css} KB"
      echo "  - Total: $((total_js + total_css)) KB"
    fi

    echo ""
    echo "Recommendations:"
    echo "  - Review bundle-report.html for detailed breakdown"
    echo "  - Consider removing unused dependencies"
    echo "  - Implement lazy loading for feature modules"
    echo "  - Complete Material UI migration to reduce bundle size"
  } > "$report_file"

  print_success "Analysis report saved: $report_file"

  cd - > /dev/null
}

################################################################################
# Main Execution
################################################################################

main() {
  echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
  echo -e "${BLUE}║  Bundle Analysis Script                                    ║${NC}"
  echo -e "${BLUE}║  Analyze Angular bundle composition and size              ║${NC}"
  echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"

  # Execute analysis steps
  build_with_stats
  analyze_bundle_files
  check_for_analyzer_tool && run_bundle_analyzer || true
  analyze_dependencies
  analyze_source_code
  generate_recommendations
  save_analysis_report

  # Final summary
  print_header "Analysis Complete"

  echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
  echo -e "${GREEN}║  ✓ BUNDLE ANALYSIS COMPLETE                                ║${NC}"
  echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"

  echo ""
  print_info "Next Steps:"
  echo "  1. Review bundle-analysis-*.txt report"
  echo "  2. Track bundle size over time: node scripts/track-bundle-size.js"
  echo "  3. Compare with previous builds"
  echo "  4. Implement recommended optimizations"

  if [ -f "$FRONTEND_DIR/dist/bundle-report.html" ]; then
    echo ""
    print_success "Visual report available: dist/bundle-report.html"
  fi
}

# Run main function
main "$@"
