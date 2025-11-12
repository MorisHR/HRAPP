#!/bin/bash

# ============================================
# HRMS Development Secrets Setup Script
# ============================================
# This script helps developers configure local development secrets
# using .NET User Secrets (secure, not committed to Git)
#
# Usage:
#   chmod +x scripts/setup-dev-secrets.sh
#   ./scripts/setup-dev-secrets.sh
#
# What this script does:
# 1. Initializes .NET User Secrets for the API project
# 2. Generates a secure JWT secret
# 3. Prompts for database password
# 4. Prompts for SMTP credentials (optional)
# 5. Stores all secrets securely in User Secrets
# ============================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Project path
PROJECT_PATH="src/HRMS.API/HRMS.API.csproj"

echo -e "${BLUE}============================================${NC}"
echo -e "${BLUE}HRMS Development Secrets Setup${NC}"
echo -e "${BLUE}============================================${NC}"
echo ""

# Check if project exists
if [ ! -f "$PROJECT_PATH" ]; then
    echo -e "${RED}ERROR: Project file not found at $PROJECT_PATH${NC}"
    echo -e "${YELLOW}Please run this script from the HRAPP root directory${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Found project at $PROJECT_PATH${NC}"
echo ""

# Initialize User Secrets
echo -e "${BLUE}Initializing .NET User Secrets...${NC}"
dotnet user-secrets init --project "$PROJECT_PATH" 2>/dev/null || true
echo -e "${GREEN}✓ User Secrets initialized${NC}"
echo ""

# ============================================
# 1. JWT Secret
# ============================================
echo -e "${BLUE}1. JWT Secret Configuration${NC}"
echo -e "${YELLOW}Generating a secure random JWT secret...${NC}"

# Generate a secure random secret (44 characters base64)
JWT_SECRET=$(openssl rand -base64 32 2>/dev/null || python3 -c "import secrets; print(secrets.token_urlsafe(32))" 2>/dev/null)

if [ -z "$JWT_SECRET" ]; then
    echo -e "${RED}ERROR: Failed to generate JWT secret${NC}"
    echo -e "${YELLOW}Please enter a JWT secret manually (minimum 32 characters):${NC}"
    read -r JWT_SECRET
fi

dotnet user-secrets set "JwtSettings:Secret" "$JWT_SECRET" --project "$PROJECT_PATH"
echo -e "${GREEN}✓ JWT Secret configured (length: ${#JWT_SECRET} characters)${NC}"
echo ""

# ============================================
# 2. Database Password
# ============================================
echo -e "${BLUE}2. Database Password Configuration${NC}"
echo -e "${YELLOW}Current connection string uses PostgreSQL on localhost${NC}"
echo -e "${YELLOW}Enter your PostgreSQL password (or press Enter to use 'postgres'):${NC}"
read -s DB_PASSWORD

if [ -z "$DB_PASSWORD" ]; then
    DB_PASSWORD="postgres"
    echo -e "${YELLOW}Using default password: postgres${NC}"
fi

# Update the connection string with password
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=hrms_master;Username=postgres;Password=$DB_PASSWORD;SSL Mode=Prefer" --project "$PROJECT_PATH"
echo -e "${GREEN}✓ Database password configured${NC}"
echo ""

# ============================================
# 3. SMTP Configuration (Optional)
# ============================================
echo -e "${BLUE}3. SMTP Configuration (Optional)${NC}"
echo -e "${YELLOW}Do you want to configure SMTP credentials? (y/N):${NC}"
read -r CONFIGURE_SMTP

if [ "$CONFIGURE_SMTP" = "y" ] || [ "$CONFIGURE_SMTP" = "Y" ]; then
    echo -e "${YELLOW}Enter SMTP server (default: mail.smtp2go.com):${NC}"
    read -r SMTP_SERVER
    SMTP_SERVER=${SMTP_SERVER:-mail.smtp2go.com}

    echo -e "${YELLOW}Enter SMTP port (default: 2525):${NC}"
    read -r SMTP_PORT
    SMTP_PORT=${SMTP_PORT:-2525}

    echo -e "${YELLOW}Enter SMTP username:${NC}"
    read -r SMTP_USERNAME

    echo -e "${YELLOW}Enter SMTP password:${NC}"
    read -s SMTP_PASSWORD
    echo ""

    echo -e "${YELLOW}Enter From Email (default: noreply@localhost):${NC}"
    read -r FROM_EMAIL
    FROM_EMAIL=${FROM_EMAIL:-noreply@localhost}

    # Set SMTP secrets
    dotnet user-secrets set "EmailSettings:SmtpServer" "$SMTP_SERVER" --project "$PROJECT_PATH"
    dotnet user-secrets set "EmailSettings:SmtpPort" "$SMTP_PORT" --project "$PROJECT_PATH"
    dotnet user-secrets set "EmailSettings:SmtpUsername" "$SMTP_USERNAME" --project "$PROJECT_PATH"
    dotnet user-secrets set "EmailSettings:SmtpPassword" "$SMTP_PASSWORD" --project "$PROJECT_PATH"
    dotnet user-secrets set "EmailSettings:FromEmail" "$FROM_EMAIL" --project "$PROJECT_PATH"

    echo -e "${GREEN}✓ SMTP credentials configured${NC}"
else
    echo -e "${YELLOW}Skipping SMTP configuration. Using MailHog (localhost:1025) for development.${NC}"
    echo -e "${YELLOW}Start MailHog with: docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog${NC}"
fi
echo ""

# ============================================
# 4. Summary
# ============================================
echo -e "${BLUE}============================================${NC}"
echo -e "${GREEN}✓ Development Secrets Setup Complete!${NC}"
echo -e "${BLUE}============================================${NC}"
echo ""
echo -e "${GREEN}Configured secrets:${NC}"
echo -e "  • JWT Secret: ${GREEN}✓${NC} (${#JWT_SECRET} characters)"
echo -e "  • Database Password: ${GREEN}✓${NC}"
if [ "$CONFIGURE_SMTP" = "y" ] || [ "$CONFIGURE_SMTP" = "Y" ]; then
    echo -e "  • SMTP Credentials: ${GREEN}✓${NC}"
else
    echo -e "  • SMTP Credentials: ${YELLOW}Skipped (using MailHog)${NC}"
fi
echo ""

echo -e "${BLUE}View all secrets:${NC}"
echo -e "  dotnet user-secrets list --project $PROJECT_PATH"
echo ""

echo -e "${BLUE}Remove a secret:${NC}"
echo -e "  dotnet user-secrets remove \"JwtSettings:Secret\" --project $PROJECT_PATH"
echo ""

echo -e "${BLUE}Clear all secrets:${NC}"
echo -e "  dotnet user-secrets clear --project $PROJECT_PATH"
echo ""

# ============================================
# 5. Frontend Environment Setup
# ============================================
echo -e "${BLUE}============================================${NC}"
echo -e "${BLUE}Frontend Environment Setup${NC}"
echo -e "${BLUE}============================================${NC}"
echo ""

FRONTEND_ENV_FILE="hrms-frontend/src/environments/environment.ts"
FRONTEND_ENV_TEMPLATE="hrms-frontend/src/environments/environment.ts.template"

if [ ! -f "$FRONTEND_ENV_FILE" ]; then
    if [ -f "$FRONTEND_ENV_TEMPLATE" ]; then
        echo -e "${YELLOW}Frontend environment file not found. Creating from template...${NC}"
        cp "$FRONTEND_ENV_TEMPLATE" "$FRONTEND_ENV_FILE"

        # Generate a UUID for superAdminSecretPath
        SUPER_ADMIN_UUID=$(python3 -c "import uuid; print('system-' + str(uuid.uuid4()))" 2>/dev/null || echo "system-GENERATE-YOUR-OWN-UUID")

        # Update the placeholder
        if command -v sed &> /dev/null; then
            sed -i "s/system-GENERATE-YOUR-OWN-UUID-HERE/$SUPER_ADMIN_UUID/g" "$FRONTEND_ENV_FILE" 2>/dev/null || \
            sed -i '' "s/system-GENERATE-YOUR-OWN-UUID-HERE/$SUPER_ADMIN_UUID/g" "$FRONTEND_ENV_FILE"
        fi

        echo -e "${GREEN}✓ Created $FRONTEND_ENV_FILE${NC}"
        echo -e "${YELLOW}SuperAdmin Secret Path: $SUPER_ADMIN_UUID${NC}"
        echo -e "${RED}IMPORTANT: Keep this path secret! Update backend route to match.${NC}"
    else
        echo -e "${YELLOW}Template not found. Please create environment.ts manually.${NC}"
    fi
else
    echo -e "${GREEN}✓ Frontend environment file already exists${NC}"
fi
echo ""

# ============================================
# 6. Final Notes
# ============================================
echo -e "${BLUE}============================================${NC}"
echo -e "${BLUE}Next Steps:${NC}"
echo -e "${BLUE}============================================${NC}"
echo ""
echo -e "1. ${GREEN}Start PostgreSQL:${NC}"
echo -e "   docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=$DB_PASSWORD postgres:15"
echo ""
echo -e "2. ${GREEN}Start MailHog (for email testing):${NC}"
echo -e "   docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog"
echo -e "   View emails at: http://localhost:8025"
echo ""
echo -e "3. ${GREEN}Run database migrations:${NC}"
echo -e "   cd src/HRMS.API"
echo -e "   dotnet ef database update"
echo ""
echo -e "4. ${GREEN}Start the API:${NC}"
echo -e "   cd src/HRMS.API"
echo -e "   dotnet run"
echo ""
echo -e "5. ${GREEN}Start the frontend:${NC}"
echo -e "   cd hrms-frontend"
echo -e "   npm install"
echo -e "   npm start"
echo ""
echo -e "${GREEN}All secrets are stored securely in:${NC}"
echo -e "  ~/.microsoft/usersecrets/<user-secrets-id>/secrets.json"
echo -e "${YELLOW}(This file is NOT committed to Git)${NC}"
echo ""
echo -e "${GREEN}Setup complete! Happy coding! 🚀${NC}"
