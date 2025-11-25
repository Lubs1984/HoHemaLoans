# WhatsApp Integration Setup Guide

This guide walks you through setting up WhatsApp Business API integration for Ho Hema Loans.

## Prerequisites

- Meta Developer Account (free)
- WhatsApp Business Account
- ngrok (for local webhook testing) - https://ngrok.com/download
- .NET 8 SDK
- Your test phone number (or use Meta's sandbox number)

## Step 1: Create Meta Developer Account & WhatsApp App

### 1.1 Register Meta Developer Account
1. Go to https://developers.facebook.com/
2. Click "Create Account" (or log in if you already have one)
3. Create a new app:
   - Click "Create App"
   - Select "Business" as the app type
   - Fill in app details and complete setup

### 1.2 Add WhatsApp Product
1. In your app dashboard, click "Add Product"
2. Find "WhatsApp" and click "Set Up"
3. This will add WhatsApp Business to your app

### 1.3 Get Your Credentials
1. Go to **WhatsApp > Getting Started**
2. You'll see:
   - **Business Phone Number ID** (looks like: 100123456789)
   - You'll need to create a test number for development

### 1.4 Generate Access Token
1. Go to **App Settings > Basic**
2. Copy your **App ID**
3. Go to **Settings > User Roles > Test Users**
4. Create or note your test user
5. Generate a temporary access token for testing OR
6. In **Settings > Basic**, create a long-lived access token (recommended)

## Step 2: Configure Your API

### 2.1 Set Environment Variables

Update `appsettings.Development.json`:

```json
{
  "WhatsApp": {
    "AccessToken": "YOUR_ACCESS_TOKEN_HERE",
    "PhoneNumberId": "YOUR_PHONE_NUMBER_ID_HERE",
    "VerifyToken": "your_webhook_verify_token_random_string_12345",
    "ApiVersion": "v18.0",
    "WebhookUrl": "https://your-ngrok-url.ngrok.io/api/whatsappwebhook/webhook"
  }
}
```

**Important:** 
- `AccessToken`: Get from Meta dashboard
- `PhoneNumberId`: From WhatsApp > Getting Started
- `VerifyToken`: Create any secure random string (for webhook verification)
- `WebhookUrl`: Will update after ngrok is running

## Step 3: Set Up Local Webhook Testing with ngrok

### 3.1 Install and Run ngrok

```bash
# Download from https://ngrok.com/download or install via Homebrew
brew install ngrok

# Start ngrok tunnel to your local API (default port 5000)
ngrok http 5000
```

You'll see output like:
```
Session Status                online
Account                       your-email@example.com
Version                       3.x.x
Region                        us (United States)
Web Interface                 http://127.0.0.1:4040
Forwarding                    https://abc123def456.ngrok.io -> http://localhost:5000
```

**Copy the HTTPS URL** (e.g., `https://abc123def456.ngrok.io`)

### 3.2 Update Your Configuration

Update `appsettings.Development.json` with your ngrok URL:
```json
{
  "WhatsApp": {
    ...
    "WebhookUrl": "https://abc123def456.ngrok.io/api/whatsappwebhook/webhook"
  }
}
```

## Step 4: Configure Webhook in Meta Dashboard

### 4.1 Set Webhook URL
1. Go to **WhatsApp > Configuration**
2. In "Webhook URL" field, enter:
   ```
   https://your-ngrok-url.ngrok.io/api/whatsappwebhook/webhook
   ```

### 4.2 Set Verify Token
1. In "Verify Token" field, enter the same token from your config:
   ```
   your_webhook_verify_token_random_string_12345
   ```

### 4.3 Click "Verify and Save"
Meta will send a GET request to verify your webhook. Your API should respond with the challenge.

### 4.4 Subscribe to Webhook Events
1. Check the boxes for events you want to receive:
   - ✅ `messages` (for incoming messages)
   - ✅ `message_status` (for delivery confirmations)
   - ✅ `message_template_status_update` (optional)

## Step 5: Start Testing

### 5.1 Run Your API
```bash
cd src/api/HoHemaLoans.Api
dotnet run
```

You should see:
- `info: HoHemaLoans.Api.Services.WhatsAppService[0] Webhook verified successfully`
- Your API listening on `https://localhost:5000`

### 5.2 Send Test Message via Meta Dashboard

1. Go to **WhatsApp > Getting Started**
2. Scroll to "Send Test Message"
3. Enter your test phone number
4. Click "Send Test Message"

You should see:
- Message appears in your WhatsApp on the test number
- Message stored in database (`WhatsAppMessages` table)
- Status updates logged

### 5.3 Send Message from WhatsApp

1. Reply to the test message from your phone
2. In your API logs, you should see:
   - `Parsed webhook payload. Entry count: 1`
   - `Processed incoming message from +27812345678`

## Step 6: Send Messages from Your API

### 6.1 Using the WhatsApp Controller

Send a message to a customer:
```bash
curl -X POST http://localhost:5000/api/whatsapp/messages \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "conversationId": 1,
    "messageText": "Hello from Ho Hema Loans!",
    "type": "Text"
  }'
```

### 6.2 Check Message Status

```bash
curl http://localhost:5000/api/whatsapp/conversations/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Testing Checklist

- [ ] Meta Developer account created
- [ ] WhatsApp Business app created
- [ ] Access token generated
- [ ] Phone Number ID obtained
- [ ] ngrok running and URL obtained
- [ ] Webhook URL configured in Meta dashboard
- [ ] Verify Token configured and tested
- [ ] Test message sent from Meta dashboard
- [ ] Message received in database
- [ ] API can send messages back
- [ ] Message status updates working
- [ ] Webhook signature verification (optional but recommended)

## Troubleshooting

### "Webhook verification failed"
- Check that `VerifyToken` in config matches Meta dashboard
- Ensure ngrok tunnel is running
- Check API logs for verification request

### "Unauthorized: Invalid signature" (if signature verification enabled)
- Verify you're using the correct App Secret
- Ensure request body is not modified

### Messages not appearing in database
- Check API logs for parsing errors
- Verify webhook is actually being called (check ngrok web interface: http://127.0.0.1:4040)
- Check database connection string

### No incoming messages received
- Verify webhook subscription is enabled for "messages" event
- Check that your test phone number is properly configured
- Look at ngrok web interface to see what requests are coming in

## Next Steps

1. **Implement WhatsApp Flows** - Create interactive conversation flows for loan applications
2. **Add Template Messages** - Pre-created message templates for notifications
3. **Set Up OTP System** - Integrate OTP delivery via WhatsApp for authentication
4. **Build Loan Application Flow** - Conversational application process via WhatsApp

## References

- [Meta WhatsApp API Documentation](https://developers.facebook.com/docs/whatsapp/cloud-api)
- [WhatsApp Webhook Documentation](https://developers.facebook.com/docs/whatsapp/webhooks)
- [ngrok Documentation](https://ngrok.com/docs)
