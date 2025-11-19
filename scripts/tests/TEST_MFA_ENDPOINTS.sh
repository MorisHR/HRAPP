#!/bin/bash

# MFA Endpoint Testing Script
# ============================

API_URL="https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev/api"
SECRET_PATH="system-9f7a2b4c-3d8e-4a1b-8c9d-1e2f3a4b5c6d"

echo "========================================="
echo "   MFA ENDPOINT TESTING SUITE"
echo "========================================="
echo ""
echo "API URL: $API_URL"
echo "Secret Path: $SECRET_PATH"
echo ""

# Test 1: Health Check
echo "TEST 1: Health Check"
echo "-------------------"
curl -s "$API_URL/../health" | jq '.' || echo "Health check endpoint not accessible"
echo ""
echo ""

# Test 2: Secret URL Login (First Time - Should get MFA Setup)
echo "TEST 2: Secret URL Login (First Login - Expects MFA Setup)"
echo "------------------------------------------------------------"
echo "Request:"
echo "POST $API_URL/auth/$SECRET_PATH"
echo ""
echo "Payload:"
cat <<EOF | tee /tmp/login_request.json
{
  "email": "admin@morishr.com",
  "password": "Admin@123"
}
EOF
echo ""
echo ""
echo "Response:"
curl -X POST "$API_URL/auth/$SECRET_PATH" \
  -H "Content-Type: application/json" \
  -d @/tmp/login_request.json \
  -v 2>&1 | tee /tmp/login_response.txt
echo ""
echo ""

# Parse response for MFA setup data
if grep -q "requiresMfaSetup" /tmp/login_response.txt; then
  echo "✅ TEST 2 PASSED: MFA Setup Required (First Time Login)"
  echo ""
  echo "Extract QR Code and Backup Codes from response above"
  echo ""

  # Extract userId for next tests
  USER_ID=$(grep -oP '"userId":"[^"]*"' /tmp/login_response.txt | head -1 | cut -d'"' -f4)
  SECRET=$(grep -oP '"secret":"[^"]*"' /tmp/login_response.txt | head -1 | cut -d'"' -f4)

  if [ -n "$USER_ID" ]; then
    echo "UserId extracted: $USER_ID"
    echo "Secret extracted: $SECRET"
    echo ""

    # Save QR code if present
    QR_CODE=$(grep -oP '"qrCode":"[^"]*"' /tmp/login_response.txt | head -1 | cut -d'"' -f4)
    if [ -n "$QR_CODE" ]; then
      echo "QR Code (Base64) saved to /tmp/qr_code.txt"
      echo "$QR_CODE" > /tmp/qr_code.txt
      echo ""
      echo "To view QR code, decode the Base64 string or use online tool:"
      echo "https://base64.guru/converter/decode/image"
      echo ""
    fi

    # Extract backup codes
    echo "Backup Codes:"
    grep -oP '"backupCodes":\[([^\]]*)\]' /tmp/login_response.txt | head -1 | sed 's/"backupCodes":\[//' | sed 's/\]//' | tr ',' '\n'
    echo ""
  fi
elif grep -q "requiresMfaVerification" /tmp/login_response.txt; then
  echo "⚠️  MFA already set up for this account. Need TOTP code."
  echo ""
  USER_ID=$(grep -oP '"userId":"[^"]*"' /tmp/login_response.txt | head -1 | cut -d'"' -f4)
  echo "UserId: $USER_ID"
  echo ""
  echo "Skip to TEST 4 to verify MFA with TOTP code"
else
  echo "❌ TEST 2 FAILED: Unexpected response"
fi

echo ""
echo "========================================="
echo "   MANUAL TESTING STEPS"
echo "========================================="
echo ""
echo "1. SCAN QR CODE:"
echo "   - Open Google Authenticator app on your phone"
echo "   - Tap '+' → 'Scan QR Code'"
echo "   - Decode the Base64 QR code from /tmp/qr_code.txt"
echo "   - Or use manual entry with the secret above"
echo ""
echo "2. GET TOTP CODE:"
echo "   - Look at Google Authenticator"
echo "   - Find 'MorisHR (admin@morishr.com)'"
echo "   - Note the 6-digit code"
echo ""
echo "3. COMPLETE MFA SETUP:"
echo "   Run: bash /workspaces/HRAPP/test_mfa_complete.sh <USER_ID> <TOTP_CODE> <SECRET>"
echo ""
echo "4. VERIFY MFA (Subsequent Logins):"
echo "   Run: bash /workspaces/HRAPP/test_mfa_verify.sh <USER_ID> <TOTP_CODE>"
echo ""
echo "========================================="
echo ""

# Create helper scripts
cat > /workspaces/HRAPP/test_mfa_complete.sh <<'COMPLETE_SCRIPT'
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
COMPLETE_SCRIPT

cat > /workspaces/HRAPP/test_mfa_verify.sh <<'VERIFY_SCRIPT'
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
VERIFY_SCRIPT

chmod +x /workspaces/HRAPP/test_mfa_complete.sh
chmod +x /workspaces/HRAPP/test_mfa_verify.sh

echo "Helper scripts created:"
echo "  - /workspaces/HRAPP/test_mfa_complete.sh"
echo "  - /workspaces/HRAPP/test_mfa_verify.sh"
echo ""
