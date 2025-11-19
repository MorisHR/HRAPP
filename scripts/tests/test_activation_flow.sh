#!/bin/bash

echo "=========================================="
echo "Tenant Activation Flow Test"
echo "=========================================="
echo ""

# Step 1: Login as SuperAdmin
echo "Step 1: Login as SuperAdmin..."
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5090/api/auth/superadmin/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@morishr.com","password":"Admin@123"}')

echo "Login Response: $LOGIN_RESPONSE"
echo ""

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "ERROR: Failed to get authentication token"
  exit 1
fi

echo "Token obtained: ${TOKEN:0:50}..."
echo ""

# Step 2: Create a test tenant
echo "Step 2: Creating test tenant..."
RANDOM_SUFFIX=$RANDOM
CREATE_RESPONSE=$(curl -s -X POST http://localhost:5090/api/tenants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"companyName\": \"Test Company $RANDOM_SUFFIX\",
    \"subdomain\": \"testco$RANDOM_SUFFIX\",
    \"adminEmail\": \"admin@testco$RANDOM_SUFFIX.com\",
    \"adminFirstName\": \"John\",
    \"adminLastName\": \"Doe\",
    \"phoneNumber\": \"+23057123456\",
    \"employeeTier\": 1,
    \"maxUsers\": 50,
    \"isGovernmentEntity\": false
  }")

echo "$CREATE_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$CREATE_RESPONSE"
echo ""

# Check if tenant was created successfully
if echo "$CREATE_RESPONSE" | grep -q '"success":true'; then
  echo "✓ Tenant created successfully!"
  echo ""

  # Step 3: Extract activation token from database
  echo "Step 3: Extracting activation token from database..."
  ACTIVATION_TOKEN=$(PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master -t -c \
    "SELECT \"ActivationToken\" FROM \"Tenants\" WHERE \"Subdomain\" = 'testco$RANDOM_SUFFIX' AND \"ActivationToken\" IS NOT NULL;")

  ACTIVATION_TOKEN=$(echo $ACTIVATION_TOKEN | xargs)

  if [ -z "$ACTIVATION_TOKEN" ]; then
    echo "ERROR: Activation token not found"
    exit 1
  fi

  echo "Activation Token: $ACTIVATION_TOKEN"
  echo ""
  echo "Activation URL: http://localhost:4200/auth/activate?token=$ACTIVATION_TOKEN"
  echo ""

  # Step 4: Check Papercut for email
  echo "Step 4: Checking Papercut SMTP for activation email..."
  echo "Open Papercut UI: http://localhost:37408"
  echo "You should see an activation email sent to admin@testco$RANDOM_SUFFIX.com"
  echo ""

  # Step 5: Test activation endpoint
  echo "Step 5: Testing activation endpoint..."
  sleep 2

  ACTIVATE_RESPONSE=$(curl -s -X POST http://localhost:5090/api/tenants/activate \
    -H "Content-Type: application/json" \
    -d "{\"activationToken\": \"$ACTIVATION_TOKEN\"}")

  echo "$ACTIVATE_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$ACTIVATE_RESPONSE"
  echo ""

  # Check if activation was successful
  if echo "$ACTIVATE_RESPONSE" | grep -q '"success":true'; then
    echo "✓✓✓ ACTIVATION SUCCESSFUL! ✓✓✓"
    echo ""
    echo "Tenant Status in Database:"
    PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master -c \
      "SELECT \"CompanyName\", \"Subdomain\", \"Status\", \"ActivatedAt\", \"AdminEmail\" FROM \"Tenants\" WHERE \"Subdomain\" = 'testco$RANDOM_SUFFIX';"
    echo ""
    echo "You should also see a welcome email in Papercut!"
  else
    echo "ERROR: Activation failed"
    echo "$ACTIVATE_RESPONSE"
  fi
else
  echo "ERROR: Failed to create tenant"
  echo "$CREATE_RESPONSE"
fi

echo ""
echo "=========================================="
echo "Test Complete"
echo "=========================================="
