#!/bin/bash

# ═══════════════════════════════════════════════════════════
# MAT-ICON TO APP-ICON MIGRATION SCRIPT
# Automatically migrates <mat-icon> to <app-icon>
# Part of Phase 2 migration - Fortune 500 HRMS project
# ═══════════════════════════════════════════════════════════

set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# File to migrate (passed as argument or default)
FILE_PATH="${1:-}"

if [ -z "$FILE_PATH" ]; then
  echo -e "${RED}Error: No file path provided${NC}"
  echo "Usage: $0 <file-path>"
  exit 1
fi

if [ ! -f "$FILE_PATH" ]; then
  echo -e "${RED}Error: File not found: $FILE_PATH${NC}"
  exit 1
fi

echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}MAT-ICON TO APP-ICON MIGRATION${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${YELLOW}Target file:${NC} $FILE_PATH"

# Create backup
BACKUP_FILE="${FILE_PATH}.mat-icon-backup"
cp "$FILE_PATH" "$BACKUP_FILE"
echo -e "${GREEN}✓${NC} Backup created: $BACKUP_FILE"

# Count mat-icons before migration
BEFORE_COUNT=$(grep -o '<mat-icon' "$FILE_PATH" | wc -l)
echo -e "${YELLOW}Mat-icons found:${NC} $BEFORE_COUNT"
echo ""

# Migration patterns
echo -e "${BLUE}Applying migration patterns...${NC}"

# Pattern 1: Simple static icons: <mat-icon>name</mat-icon> → <app-icon name="name"></app-icon>
sed -i 's|<mat-icon>\([^<{]*\)</mat-icon>|<app-icon name="\1"></app-icon>|g' "$FILE_PATH"
echo -e "${GREEN}✓${NC} Pattern 1: Static icons migrated"

# Pattern 2: Icons with interpolation: <mat-icon>{{ expr }}</mat-icon> → <app-icon [name]="expr"></app-icon>
sed -i 's|<mat-icon>{{ *\([^}]*\) *}}</mat-icon>|<app-icon [name]="\1"></app-icon>|g' "$FILE_PATH"
echo -e "${GREEN}✓${NC} Pattern 2: Interpolated icons migrated"

# Pattern 3: Icons with [class] binding (preserve it)
# <mat-icon [class]="expr">{{ name }}</mat-icon> → <app-icon [name]="name" [class]="expr"></app-icon>
sed -i 's|<mat-icon \[class\]="\([^"]*\)">{{ *\([^}]*\) *}}</mat-icon>|<app-icon [name]="\2" [class]="\1"></app-icon>|g' "$FILE_PATH"
echo -e "${GREEN}✓${NC} Pattern 3: Icons with class binding migrated"

# Pattern 4: Icons with multiple lines/whitespace
# Handle icons that span multiple lines (common in formatted code)
perl -i -0pe 's/<mat-icon\s+\[class\.(\w+)\]="([^"]*)"\s*>\s*\{\{\s*([^}]+?)\s*\}\}\s*<\/mat-icon>/<app-icon [name]="\3" [class.\1]="\2"><\/app-icon>/gs' "$FILE_PATH"
echo -e "${GREEN}✓${NC} Pattern 4: Multi-line icons with dynamic class migrated"

# Count mat-icons after migration
AFTER_COUNT=$(grep -o '<mat-icon' "$FILE_PATH" 2>/dev/null | wc -l || echo "0")
MIGRATED=$((BEFORE_COUNT - AFTER_COUNT))

echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}MIGRATION SUMMARY${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "${YELLOW}Before:${NC} $BEFORE_COUNT mat-icons"
echo -e "${YELLOW}After:${NC} $AFTER_COUNT mat-icons remaining"
echo -e "${GREEN}Migrated:${NC} $MIGRATED icons"

if [ $AFTER_COUNT -eq 0 ]; then
  echo ""
  echo -e "${GREEN}✅ SUCCESS: All mat-icons migrated!${NC}"
  echo -e "${YELLOW}Backup saved at:${NC} $BACKUP_FILE"
else
  echo ""
  echo -e "${YELLOW}⚠️  WARNING: $AFTER_COUNT mat-icons remain${NC}"
  echo "Manual review required for complex patterns:"
  echo ""
  grep -n '<mat-icon' "$FILE_PATH" || true
  echo ""
  echo -e "${YELLOW}Backup saved at:${NC} $BACKUP_FILE"
fi

echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "1. Review the migrated file"
echo "2. Test the component"
echo "3. If satisfied, remove backup: rm $BACKUP_FILE"
echo "4. If issues, restore: mv $BACKUP_FILE $FILE_PATH"
