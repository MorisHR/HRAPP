#!/bin/bash

# ============================================
# BRANDING FIX SCRIPT - hrms.com → morishr.com
# ============================================
#
# This script updates all instances of "hrms.com" to "morishr.com"
# across the codebase for demo readiness.
#
# CHANGES:
# - Email templates and notifications
# - Configuration files
# - Frontend environment files
# - JWT Issuer/Audience
# - Documentation
#
# USAGE: bash scripts/fix-branding.sh
#
# ============================================

echo "🎨 Starting branding fix: hrms.com → morishr.com"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Counter
count=0

# Find and replace in specific directories (exclude backups, node_modules, dist)
echo ""
echo "${YELLOW}Processing files...${NC}"

# Backend files
find src/HRMS.API -type f \( -name "*.cs" -o -name "*.json" \) ! -path "*/backups/*" -exec grep -l "hrms\.com" {} \; | while read file; do
    sed -i 's/hrms\.com/morishr.com/g' "$file"
    echo "  ✓ Updated: $file"
    ((count++))
done

find src/HRMS.Application -type f -name "*.cs" -exec grep -l "hrms\.com" {} \; | while read file; do
    sed -i 's/hrms\.com/morishr.com/g' "$file"
    echo "  ✓ Updated: $file"
    ((count++))
done

find src/HRMS.Infrastructure -type f -name "*.cs" -exec grep -l "hrms\.com" {} \; | while read file; do
    sed -i 's/hrms\.com/morishr.com/g' "$file"
    echo "  ✓ Updated: $file"
    ((count++))
done

# Frontend files
find hrms-frontend/src/app -type f \( -name "*.ts" -o -name "*.html" -o -name "*.json" \) ! -path "*/node_modules/*" ! -path "*/dist/*" -exec grep -l "hrms\.com" {} \; | while read file; do
    sed -i 's/hrms\.com/morishr.com/g' "$file"
    echo "  ✓ Updated: $file"
    ((count++))
done

# Configuration files
if grep -q "hrms\.com" src/HRMS.API/appsettings.json 2>/dev/null; then
    sed -i 's/hrms\.com/morishr.com/g' src/HRMS.API/appsettings.json
    echo "  ✓ Updated: src/HRMS.API/appsettings.json"
    ((count++))
fi

if grep -q "hrms\.com" src/HRMS.API/appsettings.Development.json 2>/dev/null; then
    sed -i 's/hrms\.com/morishr.com/g' src/HRMS.API/appsettings.Development.json
    echo "  ✓ Updated: src/HRMS.API/appsettings.Development.json"
    ((count++))
fi

# Frontend environment files
if [ -f "hrms-frontend/src/environments/environment.ts" ]; then
    sed -i 's/hrms\.com/morishr.com/g' hrms-frontend/src/environments/environment.ts
    echo "  ✓ Updated: hrms-frontend/src/environments/environment.ts"
    ((count++))
fi

if [ -f "hrms-frontend/src/environments/environment.prod.ts" ]; then
    sed -i 's/hrms\.com/morishr.com/g' hrms-frontend/src/environments/environment.prod.ts
    echo "  ✓ Updated: hrms-frontend/src/environments/environment.prod.ts"
    ((count++))
fi

# Documentation (optional - exclude if you want to keep historical references)
# find docs -type f -name "*.md" -exec grep -l "hrms\.com" {} \; | while read file; do
#     sed -i 's/hrms\.com/morishr.com/g' "$file"
#     echo "  ✓ Updated: $file"
#     ((count++))
# done

echo ""
echo "${GREEN}✅ Branding fix complete!${NC}"
echo "${GREEN}   Updated $count files${NC}"
echo ""
echo "To verify changes, run:"
echo "  grep -r 'hrms\.com' src/HRMS.API src/HRMS.Application src/HRMS.Infrastructure hrms-frontend/src/app --exclude-dir=backups"
echo ""
