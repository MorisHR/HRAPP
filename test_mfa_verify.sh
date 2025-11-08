#!/bin/bash
API_URL="https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api"
USER_ID="$1"
CODE="$2"

if [ -z "$USER_ID" ] || [ -z "$CODE" ]; then
  echo "Usage: $0 <USER_ID> <TOTP_CODE_OR_BACKUP_CODE>"
  echo "Example: $0 '123e4567-e89b-12d3-a456-426614174000' '123456'"
  exit 1
fi

echo "Testing MFA Verification..."
echo "User ID: $USER_ID"
echo "Code: $CODE"
echo ""

curl -X POST "$API_URL/auth/mfa/verify" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$USER_ID\",
    \"code\": \"$CODE\"
  }" | jq '.'
