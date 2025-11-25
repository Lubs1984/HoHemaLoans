# WhatsApp API Testing Guide

This document shows how to test the WhatsApp integration endpoints.

## Base URL
```
http://localhost:5000/api/
```

## Authentication
Most endpoints require JWT Bearer token:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

## Endpoints

### 1. Send WhatsApp Message

**Endpoint:** `POST /whatsapp/messages`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "conversationId": 1,
  "messageText": "Hello! How can we help you today?",
  "type": "Text"
}
```

**Response (Success 201):**
```json
{
  "id": 1,
  "conversationId": 1,
  "messageText": "Hello! How can we help you today?",
  "type": "Text",
  "direction": "Outbound",
  "status": "Sent",
  "createdAt": "2024-11-24T10:30:00Z"
}
```

**cURL Example:**
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

---

### 2. Get Contacts

**Endpoint:** `GET /whatsapp/contacts`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "phoneNumber": "+27812345678",
    "displayName": "John Doe",
    "firstName": "John",
    "lastName": "Doe",
    "createdAt": "2024-11-24T09:00:00Z",
    "hasUser": false,
    "messageCount": 5,
    "conversationCount": 1,
    "lastMessageDate": "2024-11-24T10:15:00Z"
  }
]
```

**cURL Example:**
```bash
curl http://localhost:5000/api/whatsapp/contacts \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 3. Get Contact by Phone Number

**Endpoint:** `GET /whatsapp/contacts/{phoneNumber}`

**Path Parameters:**
- `phoneNumber`: Phone number with country code (e.g., "+27812345678")

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response (200 OK):**
```json
{
  "id": 1,
  "phoneNumber": "+27812345678",
  "displayName": "John Doe",
  "firstName": "John",
  "lastName": "Doe",
  "isActive": true,
  "createdAt": "2024-11-24T09:00:00Z",
  "messages": [
    {
      "id": 1,
      "messageText": "Hi, I want to apply for a loan",
      "type": "Text",
      "direction": "Inbound",
      "status": "Received",
      "createdAt": "2024-11-24T10:00:00Z"
    }
  ],
  "conversations": [
    {
      "id": 1,
      "subject": "Loan Application",
      "status": "Open",
      "type": "General"
    }
  ]
}
```

**cURL Example:**
```bash
curl "http://localhost:5000/api/whatsapp/contacts/%2B27812345678" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 4. Create Contact

**Endpoint:** `POST /whatsapp/contacts`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "phoneNumber": "+27812345678",
  "displayName": "John Doe",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "phoneNumber": "+27812345678",
  "displayName": "John Doe",
  "firstName": "John",
  "lastName": "Doe",
  "isActive": true,
  "createdAt": "2024-11-24T10:30:00Z"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/whatsapp/contacts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "phoneNumber": "+27812345678",
    "displayName": "Jane Smith",
    "firstName": "Jane",
    "lastName": "Smith"
  }'
```

---

### 5. Get Conversations

**Endpoint:** `GET /whatsapp/conversations`

**Query Parameters:**
- `status`: (optional) "Open", "Closed", "Archived"
- `type`: (optional) "General", "LoanApplication", "Support"
- `page`: (optional, default: 1)
- `pageSize`: (optional, default: 20)

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "subject": "Loan Application",
    "status": "Open",
    "type": "General",
    "createdAt": "2024-11-24T09:00:00Z",
    "updatedAt": "2024-11-24T10:15:00Z",
    "closedAt": null,
    "contact": {
      "id": 1,
      "phoneNumber": "+27812345678",
      "displayName": "John Doe"
    },
    "loanApplication": null,
    "messageCount": 5,
    "lastMessage": {
      "messageText": "When will I receive my loan?",
      "createdAt": "2024-11-24T10:15:00Z",
      "direction": "Inbound"
    }
  }
]
```

**cURL Example:**
```bash
curl "http://localhost:5000/api/whatsapp/conversations?status=Open&pageSize=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 6. Get Conversation Details

**Endpoint:** `GET /whatsapp/conversations/{conversationId}`

**Path Parameters:**
- `conversationId`: ID of the conversation

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response (200 OK):**
```json
{
  "id": 1,
  "subject": "Loan Application",
  "status": "Open",
  "type": "General",
  "createdAt": "2024-11-24T09:00:00Z",
  "updatedAt": "2024-11-24T10:15:00Z",
  "closedAt": null,
  "contact": {
    "id": 1,
    "phoneNumber": "+27812345678",
    "displayName": "John Doe",
    "firstName": "John",
    "lastName": "Doe"
  },
  "loanApplication": null,
  "messages": [
    {
      "id": 1,
      "messageText": "Hi, I'm interested in a loan",
      "type": "Text",
      "direction": "Inbound",
      "status": "Received",
      "createdAt": "2024-11-24T10:00:00Z",
      "handledBy": null
    },
    {
      "id": 2,
      "messageText": "Sure! Let me help you. What amount are you looking for?",
      "type": "Text",
      "direction": "Outbound",
      "status": "Delivered",
      "createdAt": "2024-11-24T10:02:00Z",
      "deliveredAt": "2024-11-24T10:02:15Z",
      "handledBy": {
        "id": "user-123",
        "name": "Support Agent",
        "email": "agent@hohema.com"
      }
    }
  ]
}
```

**cURL Example:**
```bash
curl http://localhost:5000/api/whatsapp/conversations/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 7. Create Conversation

**Endpoint:** `POST /whatsapp/conversations`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "phoneNumber": "+27812345678",
  "subject": "Loan Application Inquiry",
  "type": "General",
  "loanApplicationId": null
}
```

**Response (201 Created):**
```json
{
  "id": 2,
  "contactId": 1,
  "subject": "Loan Application Inquiry",
  "type": "General",
  "status": "Open",
  "createdAt": "2024-11-24T10:30:00Z"
}
```

**cURL Example:**
```bash
curl -X POST http://localhost:5000/api/whatsapp/conversations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "phoneNumber": "+27812345678",
    "subject": "New Loan Application",
    "type": "General"
  }'
```

---

### 8. Update Conversation Status

**Endpoint:** `PUT /whatsapp/conversations/{conversationId}/status`

**Path Parameters:**
- `conversationId`: ID of the conversation

**Headers:**
```
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "status": "Closed"
}
```

**Valid Status Values:**
- `Open`
- `Closed`
- `Archived`

**Response (200 OK):**
```json
{
  "message": "Conversation status updated to Closed"
}
```

**cURL Example:**
```bash
curl -X PUT http://localhost:5000/api/whatsapp/conversations/1/status \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "status": "Closed"
  }'
```

---

### 9. Webhook Verification (GET)

**Endpoint:** `GET /whatsappwebhook/webhook`

**Query Parameters:**
- `mode`: "subscribe"
- `token`: Your verify token from config
- `challenge`: Challenge string from Meta

**Response (200 OK):**
```
<challenge_value>
```

**Note:** This endpoint is called automatically by Meta, but you can test it with:

```bash
curl "http://localhost:5000/api/whatsappwebhook/webhook?mode=subscribe&token=your_webhook_verify_token_123456&challenge=test123"
```

---

### 10. Webhook Receive (POST)

**Endpoint:** `POST /whatsappwebhook/webhook`

**Headers:**
```
Content-Type: application/json
X-Hub-Signature-256: sha256=<signature>
```

**Request Body (from Meta):**
```json
{
  "object": "whatsapp_business_account",
  "entry": [
    {
      "id": "BUSINESS_ACCOUNT_ID",
      "changes": [
        {
          "field": "messages",
          "value": {
            "messaging_product": "whatsapp",
            "metadata": {
              "display_phone_number": "16505551234",
              "phone_number_id": "102226471358871"
            },
            "contacts": [
              {
                "profile": {
                  "name": "John Doe"
                },
                "wa_id": "27812345678"
              }
            ],
            "messages": [
              {
                "from": "27812345678",
                "id": "wamid.D25=",
                "timestamp": "1671791486",
                "type": "text",
                "text": {
                  "body": "I want to apply for a loan"
                }
              }
            ]
          }
        }
      ]
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "success": true
}
```

---

## Testing with Postman

1. Create a new collection
2. Set base URL: `{{base_url}}` = `http://localhost:5000/api`
3. Add Bearer token to Authorization:
   - Type: Bearer Token
   - Token: `{{jwt_token}}`
4. Import example requests above

**Environment Variables:**
```json
{
  "base_url": "http://localhost:5000/api",
  "jwt_token": "YOUR_JWT_TOKEN_HERE"
}
```

---

## Testing with VS Code REST Client

Install "REST Client" extension, then create `test.http`:

```http
@base_url = http://localhost:5000/api
@token = YOUR_JWT_TOKEN_HERE

### Get all contacts
GET {{base_url}}/whatsapp/contacts
Authorization: Bearer {{token}}

### Create a conversation
POST {{base_url}}/whatsapp/conversations
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "phoneNumber": "+27812345678",
  "subject": "Loan Application",
  "type": "General"
}

### Send a message
POST {{base_url}}/whatsapp/messages
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "conversationId": 1,
  "messageText": "Hello! Welcome to Ho Hema Loans",
  "type": "Text"
}

### Get conversation details
GET {{base_url}}/whatsapp/conversations/1
Authorization: Bearer {{token}}
```

Then use Ctrl+Alt+R to run requests.

---

## Integration Testing Scenarios

### Scenario 1: Incoming Message from Customer
1. Customer sends message via WhatsApp
2. Meta sends webhook to `/api/whatsappwebhook/webhook`
3. API stores message in database
4. Message appears in conversation thread

### Scenario 2: Send Templated Response
1. Customer sends inquiry
2. Agent/Bot sends templated response via `/api/whatsapp/messages`
3. Message sent to customer's WhatsApp
4. Status updates to "Delivered" when received

### Scenario 3: Multi-turn Conversation
1. Customer initiates loan inquiry
2. API sends menu of options via interactive message
3. Customer selects option
4. API sends relevant response
5. Conversation history maintained

