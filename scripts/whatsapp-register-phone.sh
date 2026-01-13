#!/bin/bash

# WhatsApp Business API Phone Number Registration Script
# This script helps register your phone number with Meta's WhatsApp Business API

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== WhatsApp Business API Phone Number Registration ===${NC}\n"

# Configuration
PHONE_NUMBER_ID="417098424822509"
API_VERSION="v22.0"

# Prompt for access token
echo -e "${YELLOW}Enter your WhatsApp Business Access Token:${NC}"
echo "(Get this from Meta Developers Console > WhatsApp > API Setup)"
read -r ACCESS_TOKEN

if [ -z "$ACCESS_TOKEN" ]; then
    echo -e "${RED}Error: Access token is required${NC}"
    exit 1
fi

echo -e "\n${GREEN}Step 1: Request Verification Code${NC}"
echo "Choose verification method:"
echo "1) SMS"
echo "2) Voice Call"
read -p "Enter choice (1 or 2): " choice

if [ "$choice" == "1" ]; then
    METHOD="sms"
elif [ "$choice" == "2" ]; then
    METHOD="voice"
else
    echo -e "${RED}Invalid choice${NC}"
    exit 1
fi

echo -e "\n${YELLOW}Requesting verification code via $METHOD...${NC}"

RESPONSE=$(curl -s -X POST "https://graph.facebook.com/$API_VERSION/$PHONE_NUMBER_ID/register" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"messaging_product\": \"whatsapp\",
    \"pin\": \"$(openssl rand -hex 3)\"
  }")

echo "Response: $RESPONSE"

if echo "$RESPONSE" | grep -q "error"; then
    echo -e "${RED}Error requesting verification code:${NC}"
    echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"
    exit 1
fi

echo -e "${GREEN}✓ Verification code request sent!${NC}"
echo -e "\n${YELLOW}You should receive a code via $METHOD shortly.${NC}"

# Step 2: Verify the code
echo -e "\n${GREEN}Step 2: Enter Verification Code${NC}"
read -p "Enter the 6-digit code you received: " CODE

if [ -z "$CODE" ]; then
    echo -e "${RED}Error: Verification code is required${NC}"
    exit 1
fi

echo -e "\n${YELLOW}Verifying code...${NC}"

VERIFY_RESPONSE=$(curl -s -X POST "https://graph.facebook.com/$API_VERSION/$PHONE_NUMBER_ID/verify_code" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"code\": \"$CODE\"
  }")

echo "Response: $VERIFY_RESPONSE"

if echo "$VERIFY_RESPONSE" | grep -q "success.*true"; then
    echo -e "\n${GREEN}✓✓✓ Phone number registered successfully! ✓✓✓${NC}"
    echo -e "${GREEN}Your WhatsApp Business number is now ready to send and receive messages.${NC}"
else
    echo -e "${RED}Error verifying code:${NC}"
    echo "$VERIFY_RESPONSE" | jq '.' 2>/dev/null || echo "$VERIFY_RESPONSE"
    exit 1
fi

echo -e "\n${GREEN}Next steps:${NC}"
echo "1. Update your Railway environment variable WhatsApp__AccessToken with: $ACCESS_TOKEN"
echo "2. Test sending a message with: ./scripts/whatsapp-test-send.sh"
