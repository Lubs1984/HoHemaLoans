# WhatsApp Integration Setup for Railway

## API Version & Documentation

**API Version:** v24.0  
**OpenAPI Reference:** https://github.com/facebook/openapi

## Environment Variables (Already Configured)

Add these to your Railway API service under **Variables**:

```bash
WhatsApp__AccessToken=EAAI5txi38s8BQGvigGoI1LT43A84Y5JK6ePoOj6ijqqPkmmnSnRNnzSLVx0XfaS68epituqfQKHlPvZB2zpOZBPZB6FhbdAZBOS1jmlt7jC5hVWFxzdjsNZCqMjN4t6uk2vEKN7Ry1ixZBVHvanz6tI78oZCm1ZChsgmuLOWVpwTop5qsvFpqBJztlvPqbBdSDRjruJjX3ZAHPbi7Hw36ArGGkiYZBtJVINqwOXaCSc0a8iUgtRNLdbZBONkZCr5ZCDbz0qoyS0Kk9svnQnWPq9CSsQZDZD

WhatsApp__ApiVersion=v24.0

WhatsApp__PhoneNumberId=933810716485561

WhatsApp__BusinessAccountId=2124933594979327

WhatsApp__VerifyToken=your_webhook_verify_token_123456

WhatsApp__WebhookUrl=https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook
```

## Webhook Configuration

### Railway Webhook URL:
```
https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook
```

### Meta WhatsApp Business Platform Setup

1. **Go to Meta Developers Console**
   - https://developers.facebook.com/apps
   - Select your app
   - Navigate to WhatsApp → Configuration

2. **Configure Webhook**
   - **Callback URL:** `https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook`
   - **Verify Token:** `your_webhook_verify_token_123456`
   - Click "Verify and Save"

3. **Subscribe to Webhook Fields**
   - ✅ messages
   - ✅ message_status (optional, for delivery receipts)

4. **Test the Webhook**
   ```bash
   # Test verification endpoint
   curl "https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook?hub.mode=subscribe&hub.verify_token=your_webhook_verify_token_123456&hub.challenge=test123"
   
   # Should return: test123
   ```

## Available WhatsApp Endpoints

### 1. Receive Webhook (Meta → Your API)
- **Endpoint:** `POST /api/whatsappwebhook/webhook`
- **Purpose:** Receives messages from WhatsApp users
- **Authentication:** Webhook signature validation

### 2. Verify Webhook (Meta → Your API)
- **Endpoint:** `GET /api/whatsappwebhook/webhook`
- **Purpose:** Initial webhook verification
- **Query Params:**
  - `hub.mode=subscribe`
  - `hub.verify_token=your_webhook_verify_token_123456`
  - `hub.challenge=<random_string>`

### 3. Send Message (Your API → Meta)
- **Endpoint:** `POST /api/admin/whatsapp/send-message`
- **Purpose:** Send messages to WhatsApp users
- **Authentication:** Bearer token (JWT)
- **Body:**
  ```json
  {
    "conversationId": "conversation-guid",
    "message": "Hello from HoHema Loans!"
  }
  ```

### 4. Get Conversations
- **Endpoint:** `GET /api/admin/whatsapp/conversations`
- **Purpose:** List all WhatsApp conversations
- **Authentication:** Bearer token (JWT)

## Testing the Integration

### 1. Verify Webhook is Active
```bash
# Check health endpoint
curl https://hohemaapi-development.up.railway.app/health

# Test webhook verification
curl "https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook?hub.mode=subscribe&hub.verify_token=your_webhook_verify_token_123456&hub.challenge=test123"
```

### 2. Send a Test Message
From your WhatsApp Business phone number, send a message to a test user. Check Railway logs:
```
[WHATSAPP] Received message from: +27...
[WHATSAPP] Processing message...
```

### 3. Check Database
Login to your app and check:
- **WhatsApp Contacts:** Should show the sender
- **WhatsApp Messages:** Should show the received message
- **WhatsApp Conversations:** Should show the conversation thread

## Troubleshooting

### Webhook Verification Fails
1. Ensure `WhatsApp__VerifyToken` matches exactly in Railway variables
2. Check Railway API logs for verification attempts
3. Verify the URL is accessible: `https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook`

### Messages Not Received
1. Check Meta Webhook subscription is active
2. Verify `WhatsApp__AccessToken` is valid (tokens expire)
3. Check Railway logs for incoming webhook calls
4. Ensure database connection is working

### Cannot Send Messages
1. Verify `WhatsApp__PhoneNumberId` is correct
2. Check `WhatsApp__AccessToken` has send message permissions
3. Verify recipient phone number format: `+27xxxxxxxxx`
4. Check Railway logs for API errors

## Token Expiration

WhatsApp Access Tokens expire after **60-90 days**. When expired:

1. Go to Meta Developers Console
2. Navigate to your app → WhatsApp → Getting Started
3. Generate a new access token
4. Update `WhatsApp__AccessToken` in Railway variables
5. Redeploy the API service

## Security Notes

- ✅ Webhook signature validation is enabled
- ✅ HTTPS is enforced by Railway
- ✅ Access token is stored as environment variable (not in code)
- ⚠️ Never commit access tokens to git
- ⚠️ Rotate tokens regularly
- ⚠️ Monitor Railway logs for suspicious activity

## Production Checklist

Before going to production:

- [ ] Generate a long-term access token (system user token)
- [ ] Update webhook URL to production: `https://api.hohema.com/api/whatsappwebhook/webhook`
- [ ] Set up webhook signature validation secret
- [ ] Enable rate limiting on webhook endpoint
- [ ] Set up monitoring alerts for webhook failures
- [ ] Test full conversation flow
- [ ] Verify message delivery and status updates
- [ ] Document emergency procedures for token rotation

## Support & Resources

- **Meta WhatsApp API Docs:** https://developers.facebook.com/docs/whatsapp
- **WhatsApp Business Platform:** https://business.whatsapp.com
- **Railway Support:** https://railway.app/help
- **API Health Check:** https://hohemaapi-development.up.railway.app/health
