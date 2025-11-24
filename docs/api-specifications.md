# Ho Hema Loans - API Specifications

**Version:** 1.0  
**Date:** November 7, 2025  
**Base URL:** `https://api.hohema.co.za/v1`  
**Authentication:** JWT Bearer Token  

---

## Table of Contents

1. [Authentication & Authorization](#authentication--authorization)
2. [Core API Endpoints](#core-api-endpoints)
3. [Loan Management APIs](#loan-management-apis)
4. [Employee Management APIs](#employee-management-apis)
5. [WhatsApp Integration APIs](#whatsapp-integration-apis)
6. [Payment Processing APIs](#payment-processing-apis)
7. [Integration APIs](#integration-apis)
8. [Admin & Reporting APIs](#admin--reporting-apis)
9. [Error Handling](#error-handling)
10. [Rate Limiting & Security](#rate-limiting--security)

---

## Authentication & Authorization

### Authentication Flow

```http
POST /auth/login
Content-Type: application/json

{
  "phoneNumber": "+27123456789",
  "password": "SecurePassword123",
  "channel": "web" // web, whatsapp, mobile
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "user": {
      "id": "emp_123456",
      "phoneNumber": "+27123456789",
      "firstName": "John",
      "lastName": "Doe",
      "employeeNumber": "EMP001",
      "role": "employee"
    }
  }
}
```

### OTP Authentication (WhatsApp/SMS)

```http
POST /auth/request-otp
Content-Type: application/json

{
  "phoneNumber": "+27123456789",
  "idNumber": "9001011234567",
  "channel": "whatsapp"
}
```

```http
POST /auth/verify-otp
Content-Type: application/json

{
  "phoneNumber": "+27123456789",
  "otp": "123456",
  "sessionToken": "temp_session_token"
}
```

### Token Refresh

```http
POST /auth/refresh
Authorization: Bearer {refresh_token}
Content-Type: application/json

{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

---

## Core API Endpoints

### Health Check

```http
GET /health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-07T10:30:00Z",
  "version": "1.0.0",
  "services": {
    "database": "healthy",
    "redis": "healthy",
    "whatsapp": "healthy",
    "banking": "healthy"
  }
}
```

### System Configuration

```http
GET /config/employer/{employerId}/parameters
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "employerId": "emp_001",
    "companyName": "ABC Company",
    "advanceParameters": {
      "maxPercentage": 80,
      "transactionFee": 60.00,
      "minAmount": 100.00,
      "maxAmount": 5000.00
    },
    "loanParameters": {
      "maxShiftPercentage": 25,
      "maxLeavePercentage": 100,
      "initiationFeePercentage": 15.5,
      "adminFee": 50.00,
      "firstLoanInterest": 5.0,
      "subsequentInterest": 3.0,
      "maxTerm": 3
    }
  }
}
```

---

## Loan Management APIs

### Loan Application Submission

```http
POST /loans/applications
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "employeeId": "emp_123456",
  "productType": "short_term_loan", // advance, short_term_loan
  "requestedAmount": 2500.00,
  "expenses": {
    "rent": 1200.00,
    "transport": 500.00,
    "groceries": 800.00,
    "other": 300.00
  },
  "bankDetails": {
    "accountHolder": "John Doe",
    "bankName": "Standard Bank",
    "accountNumber": "1234567890",
    "branchCode": "051001"
  },
  "channel": "whatsapp"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "applicationId": "app_789012",
    "status": "pending_validation",
    "submittedAt": "2025-11-07T10:30:00Z",
    "estimatedProcessingTime": "15 minutes",
    "nextSteps": [
      {
        "step": "identity_verification",
        "description": "Upload ID document",
        "required": true
      },
      {
        "step": "employment_verification", 
        "description": "Verify employment details",
        "automated": true
      }
    ]
  }
}
```

### Get Loan Application Status

```http
GET /loans/applications/{applicationId}
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "applicationId": "app_789012",
    "status": "approved",
    "currentStep": "contract_signing",
    "progress": 80,
    "timeline": [
      {
        "step": "submitted",
        "timestamp": "2025-11-07T10:30:00Z",
        "status": "completed"
      },
      {
        "step": "identity_verified",
        "timestamp": "2025-11-07T10:32:00Z", 
        "status": "completed"
      },
      {
        "step": "employment_verified",
        "timestamp": "2025-11-07T10:33:00Z",
        "status": "completed"
      },
      {
        "step": "affordability_assessed",
        "timestamp": "2025-11-07T10:35:00Z",
        "status": "completed"
      },
      {
        "step": "approved",
        "timestamp": "2025-11-07T10:40:00Z",
        "status": "completed"
      },
      {
        "step": "contract_signing",
        "timestamp": null,
        "status": "pending"
      }
    ],
    "approvedAmount": 2000.00,
    "fees": {
      "initiationFee": 310.00,
      "adminFee": 50.00,
      "monthlyInterest": 100.00
    },
    "repaymentSchedule": [
      {
        "month": 1,
        "amount": 820.00,
        "dueDate": "2025-12-01"
      },
      {
        "month": 2,
        "amount": 820.00,
        "dueDate": "2026-01-01"
      },
      {
        "month": 3,
        "amount": 820.00,
        "dueDate": "2026-02-01"
      }
    ]
  }
}
```

### Upload Documents

```http
POST /loans/applications/{applicationId}/documents
Authorization: Bearer {access_token}
Content-Type: multipart/form-data

{
  "documentType": "id_document",
  "file": [binary data],
  "fileName": "id_document.jpg"
}
```

### Get Affordability Assessment

```http
GET /loans/affordability/{employeeId}
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "employeeId": "emp_123456",
    "assessment": {
      "monthlyIncome": 15000.00,
      "declaredExpenses": 12000.00,
      "estimatedExpenses": 11500.00,
      "disposableIncome": 3500.00,
      "maxLoanAmount": 2500.00,
      "creditScore": 750,
      "riskCategory": "low"
    },
    "availableProducts": [
      {
        "type": "advance",
        "maxAmount": 4000.00,
        "fee": 60.00,
        "available": true
      },
      {
        "type": "short_term_loan",
        "maxAmount": 2500.00,
        "initiationFee": 387.50,
        "adminFee": 50.00,
        "monthlyInterest": 125.00,
        "available": true
      }
    ]
  }
}
```

### Contract Management

```http
GET /loans/contracts/{applicationId}
Authorization: Bearer {access_token}
```

```http
POST /loans/contracts/{applicationId}/sign
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "agreementAccepted": true,
  "signatureMethod": "otp",
  "otp": "123456"
}
```

---

## Employee Management APIs

### Employee Profile

```http
GET /employees/{employeeId}/profile
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "employeeId": "emp_123456",
    "personalInfo": {
      "firstName": "John",
      "lastName": "Doe",
      "idNumber": "9001011234567",
      "phoneNumber": "+27123456789",
      "email": "john.doe@company.com"
    },
    "employmentInfo": {
      "employeeNumber": "EMP001",
      "employer": "ABC Company",
      "position": "Software Developer",
      "department": "IT",
      "startDate": "2023-01-15",
      "status": "active",
      "monthlyRate": 15000.00
    },
    "bankDetails": {
      "accountHolder": "John Doe",
      "bankName": "Standard Bank",
      "accountNumber": "****7890",
      "branchCode": "051001"
    }
  }
}
```

### Employment Verification

```http
POST /employees/verify
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "idNumber": "9001011234567",
  "employeeNumber": "EMP001",
  "phoneNumber": "+27123456789"
}
```

### Shift and Earnings Data

```http
GET /employees/{employeeId}/shifts
Authorization: Bearer {access_token}
Query: ?from=2025-10-01&to=2025-10-31
```

**Response:**
```json
{
  "success": true,
  "data": {
    "employeeId": "emp_123456",
    "period": {
      "from": "2025-10-01",
      "to": "2025-10-31"
    },
    "summary": {
      "totalShifts": 22,
      "totalHours": 176,
      "regularHours": 160,
      "overtimeHours": 16,
      "grossEarnings": 4800.00,
      "availableForAdvance": 3840.00
    },
    "shifts": [
      {
        "date": "2025-10-01",
        "startTime": "08:00",
        "endTime": "17:00",
        "regularHours": 8,
        "overtimeHours": 1,
        "earnings": 240.00
      }
    ]
  }
}
```

---

## WhatsApp Integration APIs

### Send WhatsApp Message

```http
POST /whatsapp/messages/send
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "to": "+27123456789",
  "type": "template",
  "template": {
    "name": "loan_approved",
    "language": {
      "code": "en"
    },
    "components": [
      {
        "type": "body",
        "parameters": [
          {
            "type": "text",
            "text": "John Doe"
          },
          {
            "type": "currency",
            "currency": {
              "fallback_value": "R2000",
              "code": "ZAR",
              "amount_1000": 2000000
            }
          }
        ]
      }
    ]
  }
}
```

### WhatsApp Flow Management

```http
POST /whatsapp/flows/start
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "phoneNumber": "+27123456789",
  "flowType": "loan_application",
  "initialData": {
    "employeeId": "emp_123456",
    "productType": "short_term_loan"
  }
}
```

### Webhook Handling

```http
POST /webhooks/whatsapp
Content-Type: application/json
X-Hub-Signature-256: sha256=signature

{
  "object": "whatsapp_business_account",
  "entry": [
    {
      "id": "business_account_id",
      "changes": [
        {
          "value": {
            "messaging_product": "whatsapp",
            "metadata": {
              "display_phone_number": "27123456789",
              "phone_number_id": "phone_number_id"
            },
            "messages": [
              {
                "from": "+27987654321",
                "id": "message_id",
                "timestamp": "1699372800",
                "text": {
                  "body": "I want to apply for a loan"
                },
                "type": "text"
              }
            ]
          },
          "field": "messages"
        }
      ]
    }
  ]
}
```

---

## Payment Processing APIs

### Payment Initiation

```http
POST /payments/initiate
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "applicationId": "app_789012",
  "amount": 2000.00,
  "recipientAccount": {
    "accountNumber": "1234567890",
    "bankCode": "051001",
    "accountHolder": "John Doe"
  },
  "urgency": "standard" // standard, urgent
}
```

### Payment Status

```http
GET /payments/{paymentId}/status
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "paymentId": "pay_456789",
    "status": "completed",
    "amount": 2000.00,
    "processedAt": "2025-11-07T11:00:00Z",
    "reference": "HOHEMA789012",
    "bankReference": "BNK123456789"
  }
}
```

### Batch Payments

```http
POST /payments/batch
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "batchId": "batch_20251107_001",
  "payments": [
    {
      "applicationId": "app_789012",
      "amount": 2000.00,
      "recipientAccount": {
        "accountNumber": "1234567890",
        "bankCode": "051001"
      }
    }
  ]
}
```

---

## Integration APIs

### Payroll Integration

```http
GET /integrations/payroll/employees
Authorization: Bearer {integration_token}
Query: ?lastSync=2025-11-06T00:00:00Z
```

```http
POST /integrations/payroll/deductions
Authorization: Bearer {integration_token}
Content-Type: application/json

{
  "payrollPeriod": "2025-11",
  "deductions": [
    {
      "employeeNumber": "EMP001",
      "amount": 820.00,
      "reference": "HOHEMA_app_789012_1",
      "description": "Ho Hema Loan Repayment"
    }
  ]
}
```

### Time & Attendance Integration

```http
GET /integrations/attendance/shifts/{employeeId}
Authorization: Bearer {integration_token}
Query: ?from=2025-10-01&to=2025-10-31
```

### Banking Integration

```http
POST /integrations/banking/validate-account
Authorization: Bearer {integration_token}
Content-Type: application/json

{
  "accountNumber": "1234567890",
  "bankCode": "051001",
  "accountHolder": "John Doe"
}
```

---

## Admin & Reporting APIs

### Application Management

```http
GET /admin/applications
Authorization: Bearer {admin_token}
Query: ?status=pending&from=2025-11-01&limit=50
```

### Compliance Reporting

```http
GET /admin/reports/ncr-compliance
Authorization: Bearer {admin_token}
Query: ?month=2025-11
```

**Response:**
```json
{
  "success": true,
  "data": {
    "reportPeriod": "2025-11",
    "totalLoans": 1250,
    "totalValue": 2500000.00,
    "averageAmount": 2000.00,
    "complianceScore": 100,
    "violations": [],
    "breakdown": {
      "shortTermLoans": {
        "count": 800,
        "value": 1600000.00
      },
      "advances": {
        "count": 450,
        "value": 900000.00
      }
    }
  }
}
```

### System Analytics

```http
GET /admin/analytics/dashboard
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "kpis": {
      "applicationsToday": 45,
      "approvalRate": 85.5,
      "averageProcessingTime": "12 minutes",
      "systemUptime": 99.8
    },
    "channelBreakdown": {
      "whatsapp": 70,
      "web": 25,
      "phone": 5
    },
    "recentAlerts": [
      {
        "type": "info",
        "message": "System maintenance scheduled for tonight",
        "timestamp": "2025-11-07T09:00:00Z"
      }
    ]
  }
}
```

---

## Error Handling

### Standard Error Response Format

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input parameters",
    "details": {
      "field": "phoneNumber",
      "reason": "Invalid South African phone number format"
    },
    "timestamp": "2025-11-07T10:30:00Z",
    "requestId": "req_123456789"
  }
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Invalid input parameters |
| `AUTHENTICATION_REQUIRED` | 401 | Missing or invalid authentication |
| `INSUFFICIENT_PERMISSIONS` | 403 | User lacks required permissions |
| `RESOURCE_NOT_FOUND` | 404 | Requested resource not found |
| `DUPLICATE_APPLICATION` | 409 | Application already exists |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `EXTERNAL_SERVICE_ERROR` | 502 | External service unavailable |
| `COMPLIANCE_VIOLATION` | 422 | Request violates compliance rules |

---

## Rate Limiting & Security

### Rate Limits

| Endpoint Category | Limit | Window |
|-------------------|-------|--------|
| Authentication | 5 requests | 1 minute |
| Application Submission | 3 requests | 10 minutes |
| Document Upload | 10 requests | 5 minutes |
| General API | 100 requests | 1 minute |
| WhatsApp Webhooks | 1000 requests | 1 minute |

### Security Headers

All API responses include security headers:

```http
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self'
```

### Request/Response Logging

All API requests are logged with:
- Request ID for tracking
- User identification
- Timestamp and duration
- IP address and user agent
- Request/response size
- Error details (if applicable)

---

## API Versioning

The API uses URL versioning with the format `/v{major}`. Breaking changes increment the major version, while backward-compatible changes maintain the same version.

**Current Version:** `v1`  
**Supported Versions:** `v1`  
**Deprecation Policy:** 12 months notice for version deprecation

---

## Development & Testing

### Base URLs

- **Production:** `https://api.hohema.co.za/v1`
- **Staging:** `https://api-staging.hohema.co.za/v1`
- **Development:** `https://api-dev.hohema.co.za/v1`

### Postman Collection

A complete Postman collection with all endpoints and example requests is available at:
`https://docs.hohema.co.za/postman-collection.json`

### OpenAPI Specification

Full OpenAPI 3.0 specification available at:
`https://api.hohema.co.za/v1/swagger.json`