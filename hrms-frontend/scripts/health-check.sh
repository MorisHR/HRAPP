#!/bin/bash

################################################################################
# HRMS Frontend - Health Check Script
################################################################################
#
# Purpose: Comprehensive health check for frontend deployments
# Features:
#   - HTTP endpoint validation
#   - Performance metrics
#   - SSL certificate checks
#   - CDN/Cache validation
#   - Service Worker verification
#   - API connectivity tests
#
# Usage:
#   ./scripts/health-check.sh [--environment <env>] [--verbose]
#
# Environments: staging, production, local
#
################################################################################

set -euo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Script options
ENVIRONMENT="${1:-production}"
VERBOSE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        *)
            shift
            ;;
    esac
done

# Environment URLs
case $ENVIRONMENT in
    production)
        BASE_URL="${PRODUCTION_URL:-https://hrms.example.com}"
        API_URL="${PRODUCTION_API_URL:-https://api.hrms.example.com}"
        ;;
    staging)
        BASE_URL="${STAGING_URL:-https://staging.hrms.example.com}"
        API_URL="${STAGING_API_URL:-https://api-staging.hrms.example.com}"
        ;;
    local)
        BASE_URL="http://localhost:4200"
        API_URL="http://localhost:5090"
        ;;
    *)
        echo "Unknown environment: $ENVIRONMENT"
        exit 1
        ;;
esac

# Health check results
TOTAL_CHECKS=0
PASSED_CHECKS=0
FAILED_CHECKS=0
WARNING_CHECKS=0

# ============================================================================
# LOGGING FUNCTIONS
# ============================================================================

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[✓ PASS]${NC} $1"
    PASSED_CHECKS=$((PASSED_CHECKS + 1))
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
}

log_warning() {
    echo -e "${YELLOW}[⚠ WARN]${NC} $1"
    WARNING_CHECKS=$((WARNING_CHECKS + 1))
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
}

log_error() {
    echo -e "${RED}[✗ FAIL]${NC} $1"
    FAILED_CHECKS=$((FAILED_CHECKS + 1))
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
}

log_verbose() {
    if [ "$VERBOSE" = true ]; then
        echo -e "${BLUE}  → ${NC}$1"
    fi
}

# ============================================================================
# HTTP CHECKS
# ============================================================================

check_http_response() {
    local url="$1"
    local expected_code="${2:-200}"
    local description="$3"

    log_verbose "Testing: $url"

    local response=$(curl -s -o /dev/null -w "%{http_code}|%{time_total}|%{size_download}" "$url" 2>/dev/null || echo "000|0|0")

    local http_code=$(echo "$response" | cut -d'|' -f1)
    local response_time=$(echo "$response" | cut -d'|' -f2)
    local content_size=$(echo "$response" | cut -d'|' -f3)

    if [ "$http_code" = "$expected_code" ]; then
        log_success "$description (HTTP $http_code, ${response_time}s, ${content_size} bytes)"
    else
        log_error "$description (Expected HTTP $expected_code, got $http_code)"
    fi
}

check_critical_endpoints() {
    log_info "Checking critical endpoints..."

    check_http_response "$BASE_URL" "200" "Homepage"
    check_http_response "$BASE_URL/auth/subdomain" "200" "Subdomain login page"
    check_http_response "$BASE_URL/admin/login" "200" "Admin login page"
    check_http_response "$BASE_URL/manifest.webmanifest" "200" "PWA manifest"
    check_http_response "$BASE_URL/ngsw.json" "200" "Service Worker config"
}

check_static_assets() {
    log_info "Checking static assets..."

    # Check if main bundle exists
    local main_js=$(curl -s "$BASE_URL" | grep -o 'main\.[a-z0-9]*\.js' | head -1 || echo "")

    if [ -n "$main_js" ]; then
        check_http_response "$BASE_URL/$main_js" "200" "Main JavaScript bundle"
    else
        log_error "Main JavaScript bundle not found in HTML"
    fi

    # Check if styles exist
    local styles_css=$(curl -s "$BASE_URL" | grep -o 'styles\.[a-z0-9]*\.css' | head -1 || echo "")

    if [ -n "$styles_css" ]; then
        check_http_response "$BASE_URL/$styles_css" "200" "Main stylesheet"
    else
        log_warning "Main stylesheet not found in HTML"
    fi

    # Check favicon
    check_http_response "$BASE_URL/favicon.ico" "200" "Favicon"
}

# ============================================================================
# PERFORMANCE CHECKS
# ============================================================================

check_response_time() {
    log_info "Checking response time..."

    local response_time=$(curl -s -o /dev/null -w "%{time_total}" "$BASE_URL" 2>/dev/null || echo "999")
    local threshold="2.0"

    if (( $(echo "$response_time < $threshold" | bc -l) )); then
        log_success "Response time is acceptable (${response_time}s < ${threshold}s)"
    else
        log_warning "Response time is slow (${response_time}s >= ${threshold}s)"
    fi
}

check_content_compression() {
    log_info "Checking content compression..."

    local encoding=$(curl -s -I "$BASE_URL" | grep -i "content-encoding" || echo "")

    if echo "$encoding" | grep -qi "gzip\|br"; then
        log_success "Content compression enabled ($(echo $encoding | cut -d: -f2 | xargs))"
    else
        log_warning "Content compression not detected"
    fi
}

check_bundle_size() {
    log_info "Checking bundle size..."

    local main_js=$(curl -s "$BASE_URL" | grep -o 'main\.[a-z0-9]*\.js' | head -1 || echo "")

    if [ -n "$main_js" ]; then
        local bundle_size=$(curl -s "$BASE_URL/$main_js" | wc -c)
        local max_size=524288  # 512KB

        if [ "$bundle_size" -lt "$max_size" ]; then
            log_success "Main bundle size is acceptable ($((bundle_size / 1024))KB < $((max_size / 1024))KB)"
        else
            log_warning "Main bundle size is large ($((bundle_size / 1024))KB >= $((max_size / 1024))KB)"
        fi
    else
        log_error "Cannot check bundle size - main.js not found"
    fi
}

# ============================================================================
# SECURITY CHECKS
# ============================================================================

check_ssl_certificate() {
    if [[ "$BASE_URL" != https://* ]]; then
        log_warning "Skipping SSL check (not HTTPS)"
        return
    fi

    log_info "Checking SSL certificate..."

    local domain=$(echo "$BASE_URL" | sed 's|https://||' | cut -d'/' -f1)
    local cert_expiry=$(echo | openssl s_client -servername "$domain" -connect "$domain":443 2>/dev/null | openssl x509 -noout -enddate 2>/dev/null | cut -d= -f2 || echo "")

    if [ -n "$cert_expiry" ]; then
        local expiry_epoch=$(date -d "$cert_expiry" +%s 2>/dev/null || date -j -f "%b %d %T %Y %Z" "$cert_expiry" +%s 2>/dev/null || echo "0")
        local current_epoch=$(date +%s)
        local days_until_expiry=$(( (expiry_epoch - current_epoch) / 86400 ))

        if [ "$days_until_expiry" -gt 30 ]; then
            log_success "SSL certificate valid ($days_until_expiry days remaining)"
        elif [ "$days_until_expiry" -gt 0 ]; then
            log_warning "SSL certificate expires soon ($days_until_expiry days remaining)"
        else
            log_error "SSL certificate has expired"
        fi
    else
        log_error "Cannot check SSL certificate"
    fi
}

check_security_headers() {
    log_info "Checking security headers..."

    local headers=$(curl -s -I "$BASE_URL")

    # Check for important security headers
    if echo "$headers" | grep -qi "X-Content-Type-Options"; then
        log_success "X-Content-Type-Options header present"
    else
        log_warning "X-Content-Type-Options header missing"
    fi

    if echo "$headers" | grep -qi "X-Frame-Options\|Content-Security-Policy"; then
        log_success "Clickjacking protection enabled"
    else
        log_warning "Clickjacking protection headers missing"
    fi

    if echo "$headers" | grep -qi "Strict-Transport-Security"; then
        log_success "HSTS header present"
    else
        log_warning "HSTS header missing (recommended for HTTPS)"
    fi
}

# ============================================================================
# API CONNECTIVITY CHECKS
# ============================================================================

check_api_health() {
    log_info "Checking API connectivity..."

    local api_health_endpoint="$API_URL/api/health"

    local http_code=$(curl -s -o /dev/null -w "%{http_code}" "$api_health_endpoint" 2>/dev/null || echo "000")

    if [ "$http_code" = "200" ]; then
        log_success "API health endpoint responding (HTTP $http_code)"
    else
        log_error "API health endpoint not responding (HTTP $http_code)"
    fi
}

check_cors_configuration() {
    log_info "Checking CORS configuration..."

    local cors_header=$(curl -s -I -H "Origin: $BASE_URL" "$API_URL/api/health" 2>/dev/null | grep -i "access-control-allow-origin" || echo "")

    if [ -n "$cors_header" ]; then
        log_success "CORS headers configured"
    else
        log_warning "CORS headers not detected (may cause issues)"
    fi
}

# ============================================================================
# SERVICE WORKER CHECKS
# ============================================================================

check_service_worker() {
    log_info "Checking Service Worker..."

    local ngsw_json=$(curl -s "$BASE_URL/ngsw.json" 2>/dev/null || echo "")

    if [ -n "$ngsw_json" ]; then
        log_success "Service Worker configuration found"

        # Check if ngsw.json is valid JSON
        if echo "$ngsw_json" | jq . &>/dev/null; then
            log_success "Service Worker configuration is valid JSON"
        else
            log_error "Service Worker configuration is invalid JSON"
        fi
    else
        log_warning "Service Worker configuration not found"
    fi
}

# ============================================================================
# CACHE CHECKS
# ============================================================================

check_caching_headers() {
    log_info "Checking cache headers..."

    local cache_header=$(curl -s -I "$BASE_URL" | grep -i "cache-control" || echo "")

    if [ -n "$cache_header" ]; then
        log_verbose "Cache-Control: $(echo $cache_header | cut -d: -f2 | xargs)"
        log_success "Cache headers configured"
    else
        log_warning "Cache headers not detected"
    fi
}

# ============================================================================
# SUMMARY
# ============================================================================

print_summary() {
    local success_rate=0
    if [ "$TOTAL_CHECKS" -gt 0 ]; then
        success_rate=$(echo "scale=1; ($PASSED_CHECKS * 100) / $TOTAL_CHECKS" | bc)
    fi

    echo ""
    echo "================================================================"
    echo "  HEALTH CHECK SUMMARY - $ENVIRONMENT"
    echo "================================================================"
    echo "  Total Checks: $TOTAL_CHECKS"
    echo "  Passed: $PASSED_CHECKS ($(printf "%.0f" "$success_rate")%)"
    echo "  Warnings: $WARNING_CHECKS"
    echo "  Failed: $FAILED_CHECKS"
    echo ""

    if [ "$FAILED_CHECKS" -eq 0 ]; then
        echo -e "${GREEN}✓ All critical checks passed${NC}"
        echo "================================================================"
        return 0
    else
        echo -e "${RED}✗ $FAILED_CHECKS critical check(s) failed${NC}"
        echo "================================================================"
        return 1
    fi
}

# ============================================================================
# MAIN
# ============================================================================

main() {
    echo ""
    echo "================================================================"
    echo "  HRMS Frontend Health Check"
    echo "================================================================"
    echo "  Environment: $ENVIRONMENT"
    echo "  Base URL: $BASE_URL"
    echo "  API URL: $API_URL"
    echo "  Timestamp: $(date)"
    echo "================================================================"
    echo ""

    # Run all checks
    check_critical_endpoints
    check_static_assets
    check_response_time
    check_content_compression
    check_bundle_size
    check_ssl_certificate
    check_security_headers
    check_api_health
    check_cors_configuration
    check_service_worker
    check_caching_headers

    # Print summary
    print_summary
}

# Run main and exit with appropriate code
main
exit_code=$?
exit $exit_code
