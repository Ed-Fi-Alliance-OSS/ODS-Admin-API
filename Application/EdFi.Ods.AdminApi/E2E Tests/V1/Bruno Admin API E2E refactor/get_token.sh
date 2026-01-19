#!/bin/bash

# Enhanced script to generate token and run Bruno tests

set -e

API_URL="${API_URL:-https://localhost/adminapi}"
BRUNO_DIR="$(pwd)"

echo "🔧 Setting environment to ignore SSL certificates..."
export NODE_TLS_REJECT_UNAUTHORIZED=0

echo "🔑 Generating authentication token..."

# Generate random GUID
CLIENT_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
echo "📝 Client ID: $CLIENT_ID"

# Generate client secret with exact requirements
generate_client_secret() {
  local chars="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#\$%^&*()_+{}:<>?|[],./"
  local length=64
  local result="aA1!"

  for i in $(seq 5 $length); do
    local rand_index=$((RANDOM % ${#chars}))
    result+="${chars:$rand_index:1}"
  done

  echo "$result"
}

CLIENT_SECRET=$(generate_client_secret)
echo "🔐 Client Secret: ${CLIENT_SECRET:0:20}... (length: ${#CLIENT_SECRET})"

# Register client
echo "📋 Registering client..."
REGISTER_RESPONSE=$(curl -k -s -X POST "$API_URL/connect/register" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "ClientId=$CLIENT_ID" \
  --data-urlencode "ClientSecret=$CLIENT_SECRET" \
  --data-urlencode "DisplayName=$CLIENT_ID")

echo "📋 Register response: $REGISTER_RESPONSE"

if echo "$REGISTER_RESPONSE" | grep -q '"error"'; then
  echo "❌ Registration error: $REGISTER_RESPONSE"
  exit 1
fi

# Get token
echo "🎫 Getting token..."
TOKEN_RESPONSE=$(curl -k -s -X POST "$API_URL/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "client_id=$CLIENT_ID" \
  --data-urlencode "client_secret=$CLIENT_SECRET" \
  --data-urlencode "grant_type=client_credentials" \
  --data-urlencode "scope=edfi_admin_api/full_access")

echo "🎫 Token response: ${TOKEN_RESPONSE:0:100}..."

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo "❌ Error getting token: $TOKEN_RESPONSE"
  exit 1
fi

echo "✅ Token obtained successfully (length: ${#TOKEN})"

# Create new variables file with the token
VARS_FILE="environments/local.bru"
cat > "$VARS_FILE" << EOF
vars {
  API_URL: https://localhost/adminapi
  TOKEN: $TOKEN
  CLIENT_ID: $CLIENT_ID
  CLIENT_SECRET: $CLIENT_SECRET
  VENDORSCOUNT: 10
  APPLICATIONCOUNT: 10
  CLAIMSETCOUNT: 10
  ODSINSTANCESCOUNT: 10
}
EOF

echo "📁 Variables updated in $VARS_FILE"
