#!/bin/bash
API_URL="https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api"
USER_ID="$1"
TOTP_CODE="$2"
SECRET="$3"

if [ -z "$USER_ID" ] || [ -z "$TOTP_CODE" ] || [ -z "$SECRET" ]; then
  echo "Usage: $0 <USER_ID> <TOTP_CODE> <SECRET>"
  echo "Example: $0 '123e4567-e89b-12d3-a456-426614174000' '123456' 'JBSWY3DPEHPK3PXP'"
  exit 1
fi

echo "Testing MFA Complete Setup..."
echo "User ID: $USER_ID"
echo "TOTP Code: $TOTP_CODE"
echo ""

# Note: Need to get backup codes from previous response
# For testing, using dummy codes - replace with actual codes from step 1

curl -X POST "$API_URL/auth/mfa/complete-setup" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$USER_ID\",
    \"totpCode\": \"$TOTP_CODE\",
    \"secret\": \"$SECRET\",
    \"backupCodes\": [\"ABCD1234\", \"EFGH5678\"]
  }" | jq '.'
