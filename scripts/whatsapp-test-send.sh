#!/bin/bash

# WhatsApp Test Message Script
# Sends a test message to verify your WhatsApp Business API is working

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}=== WhatsApp Test Message Sender ===${NC}\n"

# Configuration
PHONE_NUMBER_ID="417098424822509"
API_VERSION="v22.0"

# Get access token
echo -e "${YELLOW}Enter your WhatsApp Business Access Token:${NC}"
read -r ACCESS_TOKEN

if [ -z "$ACCESS_TOKEN" ]; then
    echo -e "${RED}Error: Access token is required${NC}"
    exit 1
fi

# Get recipient phone number
echo -e "${YELLOW}Enter recipient phone number (with country code, e.g., 27619912528):${NC}"
read -r TO_NUMBER

if [ -z "$TO_NUMBER" ]; then
    echo -e "${RED}Error: Phone number is required${NC}"
    exit 1
fi

# Get message
echo -e "${YELLOW}Enter message (or press Enter for default):${NC}"
read -r MESSAGE

if [ -z "$MESSAGE" ]; then
    MESSAGE="Hello from HoHema Loans! 🏠 This is a test message from your loan application system."
fi

echo -e "\n${YELLOW}Sending message...${NC}"

RESPONSE=$(curl -s -X POST "https://graph.facebook.com/$API_VERSION/$PHONE_NUMBER_ID/messages" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"messaging_product\": \"whatsapp\",
    \"to\": \"$TO_NUMBER\",
    \"type\": \"text\",
    \"text\": {
      \"body\": \"$MESSAGE\"
    }
  }")

echo "Response: $RESPONSE"

if echo "$RESPONSE" | grep -q "messages"; then
    MESSAGE_ID=$(echo "$RESPONSE" | jq -r '.messages[0].id' 2>/dev/null)
    echo -e "\n${GREEN}✓ Message sent successfully!${NC}"
    echo -e "Message ID: $MESSAGE_ID"
    echo -e "Recipient: $TO_NUMBER"
else
    echo -e "\n${RED}Error sending message:${NC}"
    echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"
    exit 1
fi
