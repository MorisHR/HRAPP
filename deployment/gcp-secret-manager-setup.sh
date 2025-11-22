#!/bin/bash
# Google Cloud Secret Manager Setup Script
# This script creates all required secrets for HRMS production deployment

set -e  # Exit on error

echo "========================================="
echo "HRMS - Google Secret Manager Setup"
echo "========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if gcloud is installed
if ! command -v gcloud &> /dev/null; then
    echo -e "${RED}ERROR: gcloud CLI is not installed${NC}"
    echo "Install from: https://cloud.google.com/sdk/docs/install"
    exit 1
fi

# Check if user is authenticated
if ! gcloud auth list --filter=status:ACTIVE --format="value(account)" &> /dev/null; then
    echo -e "${RED}ERROR: Not authenticated with gcloud${NC}"
    echo "Run: gcloud auth login"
    exit 1
fi

# Get project ID
PROJECT_ID=$(gcloud config get-value project 2>/dev/null)
if [ -z "$PROJECT_ID" ]; then
    echo -e "${YELLOW}No default project set${NC}"
    read -p "Enter your GCP Project ID: " PROJECT_ID
    gcloud config set project "$PROJECT_ID"
fi

echo -e "${GREEN}Using GCP Project: $PROJECT_ID${NC}"
echo ""

# Enable Secret Manager API
echo "Enabling Secret Manager API..."
gcloud services enable secretmanager.googleapis.com --project="$PROJECT_ID"
echo -e "${GREEN}✓ Secret Manager API enabled${NC}"
echo ""

# Function to create secret
create_secret() {
    local SECRET_NAME=$1
    local SECRET_VALUE=$2
    local DESCRIPTION=$3

    echo "Creating secret: $SECRET_NAME"

    # Check if secret already exists
    if gcloud secrets describe "$SECRET_NAME" --project="$PROJECT_ID" &>/dev/null; then
        echo -e "${YELLOW}  ⚠ Secret $SECRET_NAME already exists. Creating new version...${NC}"
        echo -n "$SECRET_VALUE" | gcloud secrets versions add "$SECRET_NAME" \
            --data-file=- \
            --project="$PROJECT_ID"
    else
        echo -n "$SECRET_VALUE" | gcloud secrets create "$SECRET_NAME" \
            --replication-policy="automatic" \
            --data-file=- \
            --labels="app=hrms,env=production" \
            --project="$PROJECT_ID"

        # Add description if supported (GCP doesn't support descriptions directly, using labels)
        if [ -n "$DESCRIPTION" ]; then
            echo "  Description: $DESCRIPTION"
        fi
    fi

    echo -e "${GREEN}  ✓ Secret $SECRET_NAME created/updated${NC}"
}

# Generate new production secrets
echo "========================================="
echo "Generating Production Secrets"
echo "========================================="
echo ""

JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n')
ENCRYPTION_KEY=$(openssl rand -base64 32 | tr -d '\n')
REDIS_PASSWORD=$(openssl rand -base64 32 | tr -d '\n')
HANGFIRE_PASSWORD=$(openssl rand -base64 24 | tr -d '\n')

echo -e "${GREEN}✓ Generated secure random secrets${NC}"
echo ""

# Create secrets in Google Secret Manager
echo "========================================="
echo "Creating Secrets in Google Secret Manager"
echo "========================================="
echo ""

create_secret "HRMS_JWT_SECRET" "$JWT_SECRET" "JWT signing secret for authentication tokens"
create_secret "HRMS_ENCRYPTION_KEY" "$ENCRYPTION_KEY" "Column-level encryption key for sensitive data"
create_secret "HRMS_REDIS_PASSWORD" "$REDIS_PASSWORD" "Redis cache password"
create_secret "HRMS_HANGFIRE_PASSWORD" "$HANGFIRE_PASSWORD" "Hangfire dashboard password"

echo ""
echo "========================================="
echo "Manual Secrets (User Input Required)"
echo "========================================="
echo ""

# Database password
read -sp "Enter PostgreSQL database password: " DB_PASSWORD
echo ""
create_secret "HRMS_DB_PASSWORD" "$DB_PASSWORD" "PostgreSQL database password"

# SMTP password
read -sp "Enter SMTP email password: " SMTP_PASSWORD
echo ""
create_secret "HRMS_SMTP_PASSWORD" "$SMTP_PASSWORD" "SMTP email server password"

echo ""
echo "========================================="
echo "Service Account IAM Permissions"
echo "========================================="
echo ""

# Get App Engine default service account (or ask for custom one)
SERVICE_ACCOUNT="${PROJECT_ID}@appspot.gserviceaccount.com"
read -p "Enter service account email (default: $SERVICE_ACCOUNT): " CUSTOM_SA
if [ -n "$CUSTOM_SA" ]; then
    SERVICE_ACCOUNT="$CUSTOM_SA"
fi

echo "Granting Secret Manager access to: $SERVICE_ACCOUNT"

# Grant access to all HRMS secrets
for SECRET in "HRMS_JWT_SECRET" "HRMS_ENCRYPTION_KEY" "HRMS_REDIS_PASSWORD" \
              "HRMS_HANGFIRE_PASSWORD" "HRMS_DB_PASSWORD" "HRMS_SMTP_PASSWORD"; do
    gcloud secrets add-iam-policy-binding "$SECRET" \
        --member="serviceAccount:$SERVICE_ACCOUNT" \
        --role="roles/secretmanager.secretAccessor" \
        --project="$PROJECT_ID"
    echo -e "${GREEN}  ✓ Granted access to $SECRET${NC}"
done

echo ""
echo "========================================="
echo "Verification"
echo "========================================="
echo ""

# List all HRMS secrets
echo "Created secrets:"
gcloud secrets list --filter="labels.app=hrms" --project="$PROJECT_ID" --format="table(name,createTime)"

echo ""
echo "========================================="
echo "Next Steps"
echo "========================================="
echo ""
echo "1. Update appsettings.Production.json with your GCP Project ID:"
echo "   \"GoogleCloud\": {"
echo "     \"ProjectId\": \"$PROJECT_ID\","
echo "     \"SecretManagerEnabled\": true"
echo "   }"
echo ""
echo "2. Build and deploy your application to GCP"
echo ""
echo "3. The application will automatically load secrets from Secret Manager"
echo ""
echo "========================================="
echo "Secret Names Reference"
echo "========================================="
echo ""
echo "In your application code, secrets are accessed as:"
echo "  - JWT Secret:      HRMS_JWT_SECRET"
echo "  - Encryption Key:  HRMS_ENCRYPTION_KEY"
echo "  - Redis Password:  HRMS_REDIS_PASSWORD"
echo "  - Hangfire Pass:   HRMS_HANGFIRE_PASSWORD"
echo "  - DB Password:     HRMS_DB_PASSWORD"
echo "  - SMTP Password:   HRMS_SMTP_PASSWORD"
echo ""
echo -e "${GREEN}✓ Google Secret Manager setup complete!${NC}"
