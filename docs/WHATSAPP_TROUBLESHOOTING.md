# WhatsApp Integration Troubleshooting Guide

## Current Issue: Phone Number ID Not Accessible

### Problem
```
Error: Unsupported post request. Object with ID '933810716485561' does not exist, 
cannot be loaded due to missing permissions, or does not support this operation.
```

### Root Causes

The error indicates one of these issues:

1. **Phone Number Not Added to Your App** ⚠️ MOST LIKELY
   - The phone number ID exists but is not connected to your Meta app
   - You need to add the phone number to your WhatsApp Business app

2. **Access Token Lacks Permissions**
   - Your current access token (user token for Ian Lubbe) doesn't have business management permissions
   - You need a **System User** or **Business** access token

3. **Phone Number Not Verified**
   - The phone number hasn't been registered with WhatsApp Business API

## Step-by-Step Fix

### Step 1: Verify Your Setup in Meta Business Suite

1. **Go to Meta Business Suite**
   - Visit: https://business.facebook.com/
   - Select your business account

2. **Check WhatsApp Accounts**
   - Go to Business Settings → Accounts → WhatsApp Accounts
   - Verify you see your WhatsApp Business Account ID: `2124933594979327`

3. **Check Phone Numbers**
   - Under your WhatsApp Account, check "Phone Numbers"
   - Verify the phone number ID `933810716485561` is listed
   - Check its status: Should be "Connected" or "Verified"

### Step 2: Add Phone Number to Your App (If Missing)

1. **Go to Meta Developers Console**
   - Visit: https://developers.facebook.com/apps
   - Select your app

2. **Navigate to WhatsApp → Getting Started**
   - Look for "Add a phone number" or "Select phone number"
   - Add phone number ID: `933810716485561`

3. **Complete Phone Number Setup**
   - Follow the verification process
   - Register the phone number for use with your app

### Step 3: Generate the Correct Access Token

#### Option A: Temporary Access Token (Development/Testing - 24 hours)
1. Go to Meta Developers Console → WhatsApp → API Setup
2. Under "Temporary access token" - Copy the token
3. This token expires in 24 hours - good for testing only

#### Option B: System User Token (Production - Never expires)
1. **Create a System User**
   - Go to Meta Business Suite → Business Settings → Users → System Users
   - Click "Add" → Create a system user
   - Role: Admin

2. **Generate a Token**
   - Click on the system user
   - Click "Generate New Token"
   - Select your app
   - Required permissions:
     - ✅ `whatsapp_business_management`
     - ✅ `whatsapp_business_messaging`
     - ✅ `business_management`
   - Click "Generate Token"
   - **Copy and save this token immediately** (shown only once)

3. **Assign Assets to System User**
   - In Business Settings → System Users
   - Click your system user → "Add Assets"
   - Add your WhatsApp Business Account
   - Permissions: "Full control"

### Step 4: Test the Configuration

Run these commands with your new access token:

```bash
# Replace YOUR_NEW_ACCESS_TOKEN with the token you generated
export WHATSAPP_TOKEN="YOUR_NEW_ACCESS_TOKEN"

# 1. Test getting your Business Account info
curl -s "https://graph.facebook.com/v24.0/2124933594979327?access_token=$WHATSAPP_TOKEN" | jq .

# 2. List phone numbers in your Business Account
curl -s "https://graph.facebook.com/v24.0/2124933594979327/phone_numbers?access_token=$WHATSAPP_TOKEN" | jq .

# 3. Get specific phone number details
curl -s "https://graph.facebook.com/v24.0/933810716485561?access_token=$WHATSAPP_TOKEN" | jq .

# 4. Send test message
curl -i -X POST \
  "https://graph.facebook.com/v24.0/933810716485561/messages" \
  -H "Authorization: Bearer $WHATSAPP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "messaging_product": "whatsapp",
    "to": "27619912528",
    "type": "template",
    "template": {
      "name": "hello_world",
      "language": { "code": "en_US" }
    }
  }'
```

### Step 5: Update Your Configuration

Once you have a working token:

#### Local Development
```bash
# Update src/api/HoHemaLoans.Api/appsettings.Development.json
{
  "WhatsApp": {
    "AccessToken": "YOUR_NEW_SYSTEM_USER_TOKEN",
    "PhoneNumberId": "933810716485561",
    "BusinessAccountId": "2124933594979327",
    "VerifyToken": "your_webhook_verify_token_123456",
    "ApiVersion": "v24.0",
    "WebhookUrl": "https://your-local-ngrok-url/api/whatsappwebhook/webhook"
  }
}
```

#### Railway Deployment
```bash
# Update environment variables in Railway
railway variables set WhatsApp__AccessToken="YOUR_NEW_SYSTEM_USER_TOKEN"
railway variables set WhatsApp__PhoneNumberId="933810716485561"
railway variables set WhatsApp__BusinessAccountId="2124933594979327"
railway variables set WhatsApp__ApiVersion="v24.0"
railway variables set WhatsApp__VerifyToken="your_webhook_verify_token_123456"
```

## Common Issues and Solutions

### Issue: "Invalid OAuth 2.0 Access Token"
**Solution:** Token expired or invalid. Generate a new token (see Step 3).

### Issue: Phone number shows but can't send messages
**Solution:** Phone number needs to be registered. Use the registration script:
```bash
./scripts/whatsapp-register-phone.sh
```

### Issue: "This message is sent outside the 24-hour window"
**Solution:** 
- For users you haven't contacted in 24h, you must use approved message templates
- Create templates in Meta Business Suite → WhatsApp Manager → Message Templates

### Issue: Webhook not receiving messages
**Checklist:**
1. ✅ Webhook URL is accessible (test with curl)
2. ✅ Webhook is subscribed in Meta Developers Console
3. ✅ VerifyToken matches in your app and Meta
4. ✅ Phone number is registered and verified
5. ✅ Messages subscription is enabled (messages, message_status)

## Verification Checklist

Before your WhatsApp integration can work, verify:

- [ ] WhatsApp Business Account exists and you have admin access
- [ ] Phone number is added to the Business Account
- [ ] Phone number is added to your Meta app
- [ ] Phone number is verified/registered
- [ ] Access token has correct permissions (whatsapp_business_management, whatsapp_business_messaging)
- [ ] Access token is from a System User (for production) or valid temporary token
- [ ] System User has access to the WhatsApp Business Account
- [ ] Webhook URL is configured and verified in Meta Developers Console
- [ ] Environment variables are set correctly in Railway/local config

## Helpful Commands

### Check Token Permissions
```bash
curl -s "https://graph.facebook.com/v24.0/debug_token?input_token=$WHATSAPP_TOKEN&access_token=$WHATSAPP_TOKEN" | jq .
```

### List All Apps Your Token Has Access To
```bash
curl -s "https://graph.facebook.com/v24.0/me/accounts?access_token=$WHATSAPP_TOKEN" | jq .
```

### Get Business Account Details
```bash
curl -s "https://graph.facebook.com/v24.0/2124933594979327?fields=id,name,timezone_id&access_token=$WHATSAPP_TOKEN" | jq .
```

## Next Steps

1. Follow Step 1-3 above to get the correct access token
2. Test with the commands in Step 4
3. Update your configuration files (Step 5)
4. Test sending a message from your application
5. Set up webhook to receive messages
6. Test end-to-end flow

## Support Resources

- **Meta WhatsApp Business API Docs:** https://developers.facebook.com/docs/whatsapp/cloud-api
- **System User Guide:** https://developers.facebook.com/docs/development/create-an-app/app-dashboard/system-users
- **Token Guide:** https://developers.facebook.com/docs/development/create-an-app/app-dashboard/access-tokens
- **Phone Number Registration:** https://developers.facebook.com/docs/whatsapp/cloud-api/get-started/register-phone

## Current Configuration

**Your Details:**
- Phone Number ID: `933810716485561`
- Business Account ID: `2124933594979327`
- API Version: `v24.0`
- Test Number: `27619912528`
- User Token Owner: Ian Lubbe (ID: 10173478057130006)

**Issue:** Current access token is a user token for Ian Lubbe, which doesn't have business-level permissions to access the WhatsApp Business Account or Phone Number. You need a System User token or the correct Business token.
