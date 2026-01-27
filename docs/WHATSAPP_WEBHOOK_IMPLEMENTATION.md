# WhatsApp Webhook Implementation Guide

## Overview
This document describes the complete WhatsApp webhook implementation for receiving and processing messages from Meta's WhatsApp Business API.

## Architecture

### Components

1. **WhatsAppWebhookController** (`/api/whatsappwebhook/webhook`)
   - Handles GET requests for webhook verification (Meta setup)
   - Handles POST requests for incoming messages and status updates
   - Processes and stores all messages in the database

2. **AdminController** (`/api/admin/whatsapp/*`)
   - Provides endpoints for viewing conversations in admin dashboard
   - Allows admins to send messages via WhatsApp
   - Returns formatted data for the frontend

3. **Database Models**
   - `WhatsAppContact`: Stores customer contact information
   - `WhatsAppConversation`: Groups messages into conversations
   - `WhatsAppMessage`: Individual message records with full metadata

## Webhook Endpoints

### 1. GET /api/whatsappwebhook/webhook
**Purpose**: Webhook verification by Meta

**Parameters**:
- `hub.mode`: Should be "subscribe"
- `hub.verify_token`: Your configured verification token
- `hub.challenge`: Random string to echo back

**Response**: Returns the challenge string if verification succeeds

**Example**:
```
GET /api/whatsappwebhook/webhook?hub.mode=subscribe&hub.verify_token=your_webhook_verify_token_123456&hub.challenge=abc123
```

### 2. POST /api/whatsappwebhook/webhook
**Purpose**: Receives incoming messages and status updates

**Headers**:
- `X-Hub-Signature-256`: HMAC SHA256 signature (optional but recommended)

**Request Body**:
```json
{
  "object": "whatsapp_business_account",
  "entry": [{
    "id": "BUSINESS_ACCOUNT_ID",
    "changes": [{
      "value": {
        "messaging_product": "whatsapp",
        "metadata": {
          "display_phone_number": "27...",
          "phone_number_id": "933810716485561"
        },
        "contacts": [{
          "profile": {
            "name": "Customer Name"
          },
          "wa_id": "27619912528"
        }],
        "messages": [{
          "from": "27619912528",
          "id": "wamid.xxx",
          "timestamp": "1706356800",
          "text": {
            "body": "Hello, I need a loan"
          },
          "type": "text"
        }]
      },
      "field": "messages"
    }]
  }]
}
```

**Response**:
```json
{
  "status": "received",
  "messagesProcessed": 1,
  "statusesProcessed": 0
}
```

**Important**: The webhook **always returns 200 OK** even on errors to prevent Meta from retrying failed requests indefinitely.

## Message Processing Flow

### Incoming Message Processing

1. **Parse Webhook Payload**
   - Deserialize JSON from Meta
   - Extract messages and status updates

2. **For Each Message**:
   - Extract sender phone number and message content
   - Get or create WhatsAppContact record
   - Get or create WhatsAppConversation (open conversation for this contact)
   - Create WhatsAppMessage record with:
     - Message text/content
     - Direction: `Inbound`
     - Status: `Received`
     - Type: `Text`, `Image`, `Interactive`, etc.
     - Timestamp from Meta

3. **Save to Database**
   - Add message to database
   - Update conversation `UpdatedAt` timestamp
   - Commit transaction

4. **Logging**
   - Log detailed information about each message
   - Include: phone number, message type, content preview
   - Success confirmation with message ID

### Status Update Processing

1. **Parse Status Updates**
   - Extract message ID and new status (`sent`, `delivered`, `read`, `failed`)

2. **Update Existing Message**
   - Find message by WhatsAppMessageId
   - Update status enum
   - Set `DeliveredAt` or `ReadAt` timestamps
   - Save changes

## Admin Dashboard Endpoints

### GET /api/admin/whatsapp/conversations
**Purpose**: Retrieve WhatsApp conversations for admin view

**Parameters**:
- `page`: Page number (default: 1)
- `pageSize`: Results per page (default: 50)
- `search`: Search by contact name or phone number

**Response**:
```json
{
  "totalCount": 25,
  "pageCount": 1,
  "currentPage": 1,
  "pageSize": 50,
  "data": [{
    "id": 1,
    "subject": "Incoming WhatsApp Conversation",
    "status": "Open",
    "type": "General",
    "createdAt": "2026-01-27T10:00:00Z",
    "updatedAt": "2026-01-27T10:30:00Z",
    "contact": {
      "id": 1,
      "phoneNumber": "27619912528",
      "displayName": "Customer Name"
    },
    "messageCount": 5,
    "unreadCount": 2,
    "lastMessage": {
      "id": 5,
      "messageText": "Thanks!",
      "direction": "Inbound",
      "createdAt": "2026-01-27T10:30:00Z",
      "status": "Received",
      "type": "Text"
    },
    "messages": [
      {
        "id": 5,
        "messageText": "Thanks!",
        "direction": "Inbound",
        "createdAt": "2026-01-27T10:30:00Z",
        "status": "Received",
        "type": "Text",
        "mediaUrl": null,
        "mediaType": null,
        "deliveredAt": null,
        "readAt": null
      }
      // ... more messages
    ]
  }]
}
```

### POST /api/admin/whatsapp/send-message
**Purpose**: Send a message from admin to customer

**Request Body**:
```json
{
  "conversationId": "1",
  "content": "Hello, how can we help you?"
}
```

**Response**:
```json
{
  "message": "Message sent",
  "messageId": 6,
  "conversationId": 1,
  "timestamp": "2026-01-27T10:35:00Z"
}
```

**Note**: Currently stores the message in the database but doesn't actually send via WhatsApp API. You'll need to integrate with Meta's Send Message API.

## Comprehensive Logging

### Webhook Logging

All webhook operations are extensively logged:

1. **Request Receipt**:
   ```
   ========== WhatsApp Webhook POST Received ==========
   Request ContentType: application/json
   Request Headers: Content-Type, X-Hub-Signature-256, ...
   ```

2. **Payload Parsing**:
   ```
   Parsing webhook payload...
   Webhook payload parsed successfully. Entries: 1
   ```

3. **Message Processing**:
   ```
   Processing 1 incoming messages
   Creating WhatsApp message record: From=27619912528, Type=Text, Text=Hello, I need a loan
   ```

4. **Database Save**:
   ```
   ✅ SUCCESS: Saved message to database. MessageId=5, From=27619912528, ConversationId=1, ContactId=1
   Message details - Type: Text, Direction: Inbound, Status: Received, Text: Hello, I need a loan
   ```

5. **Completion**:
   ```
   Webhook processing complete. Messages: 1, Statuses: 0
   ```

### Admin Dashboard Logging

1. **Conversation Retrieval**:
   ```
   Admin requesting WhatsApp conversations. Page=1, PageSize=50, Search=null
   Found 25 total WhatsApp conversations
   Returning 25 conversations for page 1
   ```

2. **Message Sending**:
   ```
   Admin sending WhatsApp message. ConversationId=1, ContentLength=28
   ✅ Message saved to database. MessageId=6, To=27619912528
   ```

## Configuration

### Environment Variables

Set these in Railway or `appsettings.json`:

```json
{
  "WhatsApp": {
    "AccessToken": "YOUR_ACCESS_TOKEN",
    "PhoneNumberId": "933810716485561",
    "BusinessAccountId": "2124933594979327",
    "ApiVersion": "v24.0",
    "WebhookVerifyToken": "your_webhook_verify_token_123456"
  }
}
```

### Railway Environment Variables

```bash
railway variables set WhatsApp__AccessToken="YOUR_TOKEN"
railway variables set WhatsApp__PhoneNumberId="933810716485561"
railway variables set WhatsApp__BusinessAccountId="2124933594979327"
railway variables set WhatsApp__ApiVersion="v24.0"
railway variables set WhatsApp__WebhookVerifyToken="your_webhook_verify_token_123456"
```

## Testing the Webhook

### 1. Verify Webhook Setup

Test webhook verification:
```bash
curl "https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook?hub.mode=subscribe&hub.verify_token=your_webhook_verify_token_123456&hub.challenge=test123"
```

Expected response: `test123`

### 2. Test Incoming Message

Send test webhook payload:
```bash
curl -X POST https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook \
  -H "Content-Type: application/json" \
  -d '{
    "object": "whatsapp_business_account",
    "entry": [{
      "id": "2124933594979327",
      "changes": [{
        "value": {
          "messaging_product": "whatsapp",
          "metadata": {
            "display_phone_number": "27...",
            "phone_number_id": "933810716485561"
          },
          "contacts": [{
            "profile": { "name": "Test User" },
            "wa_id": "27619912528"
          }],
          "messages": [{
            "from": "27619912528",
            "id": "wamid.test123",
            "timestamp": "1706356800",
            "text": { "body": "Test message" },
            "type": "text"
          }]
        },
        "field": "messages"
      }]
    }]
  }'
```

Expected response:
```json
{
  "status": "received",
  "messagesProcessed": 1,
  "statusesProcessed": 0
}
```

### 3. Check Logs

View Railway logs to confirm message processing:
```bash
railway logs
```

Look for:
- ✅ SUCCESS: Saved message to database
- Message details with MessageId, ConversationId, ContactId

### 4. View in Admin Dashboard

Navigate to: https://hohemaweb-development.up.railway.app/admin/whatsapp

You should see:
- The new conversation
- The test message
- Contact information

## Message Types Supported

1. **Text Messages**: Plain text content
2. **Images**: With optional caption
3. **Documents**: PDFs, files with caption
4. **Audio**: Voice messages
5. **Video**: Video files with caption
6. **Interactive**: Button clicks, list selections
7. **Reactions**: Emoji reactions to messages
8. **Location**: Shared location data
9. **Contact**: Shared contact cards

## Status Updates

Messages go through these states:
- `Received`: Initial state for inbound messages
- `Sent`: Message sent from your system
- `Delivered`: Delivered to recipient's device
- `Read`: Recipient opened/read the message
- `Failed`: Delivery failed

## Security Considerations

### 1. Signature Verification

The webhook includes signature verification (currently a placeholder):

```csharp
private async Task<bool> VerifySignatureAsync(string signature)
{
    // Extract signature from header
    // Compute HMAC SHA256 with your App Secret
    // Compare with received signature
    return true; // Implement with your app secret
}
```

**To implement**:
1. Get your App Secret from Meta Developer Console
2. Add to configuration
3. Implement HMAC SHA256 verification

### 2. Token Security

- Store access tokens in environment variables, not code
- Use system user tokens with appropriate permissions
- Rotate tokens periodically

### 3. Webhook URL

- Use HTTPS only (Railway provides this)
- Consider IP whitelisting if supported

## Troubleshooting

### Messages Not Appearing in Admin Dashboard

1. **Check webhook is configured in Meta**
   - Go to Meta Developer Console → WhatsApp → Configuration
   - Verify webhook URL: `https://hohemaapi-development.up.railway.app/api/whatsappwebhook/webhook`
   - Verify token matches configuration

2. **Check logs**:
   ```bash
   railway logs | grep "WhatsApp"
   ```

3. **Test webhook directly**:
   - Use curl to send test payload
   - Check response status

4. **Check database**:
   ```sql
   SELECT * FROM "WhatsAppMessages" ORDER BY "CreatedAt" DESC LIMIT 10;
   SELECT * FROM "WhatsAppConversations" ORDER BY "UpdatedAt" DESC LIMIT 10;
   SELECT * FROM "WhatsAppContacts";
   ```

### Webhook Returns Error

1. **Check request format**: Ensure JSON is valid
2. **Check authentication**: Verify token in headers
3. **Check logs**: Look for specific error messages
4. **Database connection**: Ensure database is accessible

### Status Updates Not Working

1. **Verify WhatsAppMessageId** is stored correctly
2. **Check message exists** in database before status update
3. **Review status update payload** from Meta

## Next Steps

1. **Implement Actual Message Sending**
   - Integrate Meta's Send Message API in `SendMessage` endpoint
   - Add retry logic for failed sends
   - Handle delivery receipts

2. **Add Message Templates**
   - Create and manage WhatsApp templates
   - Send template messages for loan notifications
   - Handle template parameters

3. **Implement WhatsApp Flows**
   - Deploy the loan application flow
   - Handle flow responses
   - Link flows to loan applications

4. **Add Real-Time Updates**
   - Implement SignalR for real-time message delivery to admin dashboard
   - Push notifications for new messages

5. **Enhanced Security**
   - Implement full signature verification
   - Add rate limiting
   - Monitor for abuse

## Related Documentation

- [WHATSAPP_SETUP.md](./WHATSAPP_SETUP.md) - Initial setup guide
- [WHATSAPP_TROUBLESHOOTING.md](./WHATSAPP_TROUBLESHOOTING.md) - Common issues
- [RAILWAY_ENV_VARS.md](./RAILWAY_ENV_VARS.md) - Environment configuration
- [hohema-loan-flow.json](../src/whatsappflows/hohema-loan-flow.json) - WhatsApp Flow definition
