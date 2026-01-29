#!/bin/bash

# Enhanced script to generate token and run Bruno tests

set -e

API_URL="${API_URL:-https://localhost/adminapi}"
BRUNO_DIR="$(pwd)"

echo "🔧 Setting environment to ignore SSL certificates..."
export NODE_TLS_REJECT_UNAUTHORIZED=0

echo "🔑 Generating authentication token..."

# Generate random GUID (cross-platform)
generate_guid() {
  # Try different methods based on available tools
  if command -v uuidgen >/dev/null 2>&1; then
    # Unix/macOS
    uuidgen | tr '[:upper:]' '[:lower:]'
  elif command -v powershell.exe >/dev/null 2>&1; then
    # Windows with PowerShell
    powershell.exe -Command "[System.Guid]::NewGuid().ToString().ToLower()" 2>/dev/null | tr -d '\r'
  else
    # Fallback: Generate UUID-like string using bash
    local hex_chars="0123456789abcdef"
    local uuid=""
    for i in {1..8}; do uuid+="${hex_chars:$((RANDOM % 16)):1}"; done
    uuid+="-"
    for i in {1..4}; do uuid+="${hex_chars:$((RANDOM % 16)):1}"; done
    uuid+="-4"
    for i in {1..3}; do uuid+="${hex_chars:$((RANDOM % 16)):1}"; done
    uuid+="-"
    uuid+="${hex_chars:$((8 + RANDOM % 4)):1}"
    for i in {1..3}; do uuid+="${hex_chars:$((RANDOM % 16)):1}"; done
    uuid+="-"
    for i in {1..12}; do uuid+="${hex_chars:$((RANDOM % 16)):1}"; done
    echo "$uuid"
  fi
}

CLIENT_ID=$(generate_guid)
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

# Extract token using cross-platform method (works without jq and handles multiline JSON)
TOKEN=$(echo "$TOKEN_RESPONSE" | tr -d '\n\r' | sed -n 's/.*"access_token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')

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
