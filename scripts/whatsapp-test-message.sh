#!/bin/bash

# Test sending a WhatsApp message
# Usage: ./whatsapp-test-message.sh <recipient_phone_number>

PHONE_NUMBER_ID="933810716485561"
ACCESS_TOKEN="EAFm7ol0JuQYBQvmVliKPt4KZBAbyTK2W4pP0whWBW5PALDzf6e9CxbVGb9f9fFdLxhCg8P0AuXHMhFMJQXZBZAkxcTrqstpaK5eMZBX9AWF4XZBAFWcRj5RxbF3WAMjTCqha2tnQfAfGkKPqFUonZBZAT0QmaZCYhTLLMupaFRCqJWZBeYnvIonFuNZBLiVO1FCAZDZD"
RECIPIENT="${1:-27688839551}"  # Default to your number without +

echo "Sending WhatsApp test message to: +${RECIPIENT}"

curl -X POST "https://graph.facebook.com/v24.0/${PHONE_NUMBER_ID}/messages" \
  -H "Authorization: Bearer ${ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{
    \"messaging_product\": \"whatsapp\",
    \"to\": \"${RECIPIENT}\",
    \"type\": \"text\",
    \"text\": {
      \"body\": \"Hello from HoHema Loans! This is a test message from your WhatsApp Business API integration. 🎉\"
    }
  }"

echo ""
