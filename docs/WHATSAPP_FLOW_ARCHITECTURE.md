# WhatsApp Flow Architecture — Omnichannel Loan Application

## Overview

The WhatsApp loan application process uses **Meta WhatsApp Flows** to provide a structured, form-based experience that mirrors the web application's 7-step loan wizard. Users can start on WhatsApp, continue on web, or vice versa.

## Pre-Application Checklist

Before a user can apply for a loan, they must pass through these gates **in order**:

```
1. Registration    → Must have an account (web registration required)
2. Profile         → Complete personal, address, employment, banking, next-of-kin
3. Documents       → Upload SA ID/Passport + Proof of Address
4. Affordability   → Income & expenses assessment (valid for 30 days)
5. Loan Application → Same steps as web wizard (amount → term → purpose → review → sign)
```

The `WhatsAppFlowOrchestrationService` automatically checks readiness and guides users to the next required step.

## Architecture

```
WhatsApp User
    │
    ▼
Meta Cloud API ──webhook──► WhatsAppWebhookController
    │                              │
    │                              ▼
    │                   WhatsAppFlowOrchestrationService
    │                     ├── AssessReadiness()
    │                     ├── LaunchFlow() ──► SendFlowMessageAsync()
    │                     └── HandleIncomingMessage()
    │
    ├──flow callback──► WhatsAppFlowEndpointController
    │                         │
    │                         ▼
    │               HandleFlowDataExchange()
    │               HandleFlowCompletion()
    │                         │
    │                         ▼
    │               [Save to DB, calculate, route to next screen]
    │
    ▼
Meta WhatsApp Flows (4 flows)
    ├── profile_completion_flow  (6 screens)
    ├── document_upload_flow     (4 screens)
    ├── affordability_flow       (5 screens)
    └── loan_application_flow    (8 screens)
```

## Files Created/Modified

### New Files
| File | Purpose |
|------|---------|
| `src/whatsappflows/profile-completion-flow.json` | Meta Flow JSON: Profile completion (6 screens) |
| `src/whatsappflows/document-upload-flow.json` | Meta Flow JSON: Document upload (4 screens) |
| `src/whatsappflows/affordability-flow.json` | Meta Flow JSON: Affordability assessment (5 screens) |
| `src/whatsappflows/loan-application-flow.json` | Meta Flow JSON: Loan application (8 screens) |
| `src/api/.../Services/WhatsAppFlowOrchestrationService.cs` | Central orchestration brain |
| `src/api/.../Controllers/WhatsAppFlowEndpointController.cs` | Meta Flow data_exchange endpoint |

### Modified Files
| File | Change |
|------|--------|
| `src/api/.../Services/WhatsAppService.cs` | Added `SendFlowMessageAsync` to interface + implementation |
| `src/api/.../Controllers/WhatsAppWebhookController.cs` | Replaced dev-mode auto-reply with orchestration service |
| `src/api/.../Program.cs` | Registered `WhatsAppFlowOrchestrationService` in DI |

## Flow Details

### 1. Profile Completion Flow
**Flow ID:** `profile_completion_flow`
**Screens:** PERSONAL_INFO → ADDRESS_INFO → EMPLOYMENT_INFO → BANKING_INFO → NEXT_OF_KIN → PROFILE_SUMMARY

| Screen | Fields | Data Saved |
|--------|--------|------------|
| PERSONAL_INFO | First name, Last name, SA ID (13-digit), Date of birth | `ApplicationUser` |
| ADDRESS_INFO | Street, Suburb, City, Province (9 SA provinces), Postal code | `ApplicationUser` |
| EMPLOYMENT_INFO | Employer (from DB), Employee number, Job title | `ApplicationUser.BusinessId` |
| BANKING_INFO | Bank (6 SA banks), Account holder, Account number | `ApplicationUser` |
| NEXT_OF_KIN | Name, Relationship, Phone | `ApplicationUser` |
| PROFILE_SUMMARY | Read-only summary with masked ID & bank | Terminal screen |

### 2. Document Upload Flow
**Flow ID:** `document_upload_flow`
**Screens:** DOCUMENT_CHECKLIST → UPLOAD_ID → UPLOAD_PROOF_OF_ADDRESS → DOCUMENT_STATUS

- Shows which documents are already on file
- Routes user to upload only what's missing
- Supports: SA ID Book, Smart ID Card, Passport, Asylum Permit
- Proof of Address: Utility bill, Bank statement, Lease, Municipal account, Phone bill

### 3. Affordability Flow
**Flow ID:** `affordability_flow`
**Screens:** INCOME_OVERVIEW → ADD_INCOME → EXPENSES_OVERVIEW → ADD_EXPENSE → AFFORDABILITY_RESULT

- Shows existing income/expenses if on file
- Options: keep existing, update, or add new
- Frequency conversion: weekly×4.33, fortnightly×2.17, annually÷12
- NCR max repayment = 50% of disposable income

### 4. Loan Application Flow
**Flow ID:** `loan_application_flow`
**Screens:** LOAN_AMOUNT → TERM_SELECTION → LOAN_PURPOSE → AFFORDABILITY_REVIEW → TERMS_PREVIEW → BANK_DETAILS → REVIEW_SIGN → APPLICATION_SUCCESS

Maps to web wizard steps:
| WhatsApp Screen | Web Step | Description |
|----------------|----------|-------------|
| LOAN_AMOUNT | Step 0 | R500–R50,000 |
| TERM_SELECTION | Step 1 | 6/12/24/36 months with estimated payments |
| LOAN_PURPOSE | Step 2 | Transport, groceries, expenses, etc. |
| AFFORDABILITY_REVIEW | Step 3 | Financial summary + pass/fail |
| TERMS_PREVIEW | Step 4 | Full breakdown: interest, fees, total |
| BANK_DETAILS | Step 5 | Pre-filled from profile |
| REVIEW_SIGN | Step 6 | 3 consents: terms, debit order, POPIA |
| APPLICATION_SUCCESS | — | Confirmation with tracking link |

## WhatsApp Commands

Users can text these keywords at any time:

| Command | Action |
|---------|--------|
| `APPLY`, `LOAN`, `START`, `NEW`, `YES` | Start loan application (runs readiness check first) |
| `CONTINUE`, `RESUME` | Resume draft application |
| `PROFILE` | Launch profile completion flow |
| `DOCUMENTS`, `DOCS` | Launch document upload flow |
| `STATUS`, `BALANCE` | Check loan application status |
| `HELP`, `MENU` | Show main menu with options |

## Omnichannel Continuity

- Each `LoanApplication` tracks `ChannelOrigin` (Web/WhatsApp), `CurrentStep`, and `StepData` (JSONB)
- Draft applications started on WhatsApp can be resumed on web and vice versa
- The `draft_id` is passed through every flow screen to maintain context
- `WhatsAppInitiatedDate` / `WebInitiatedDate` track which channel was used

## Meta Business Manager Setup

### Required Configuration
1. **Create 4 WhatsApp Flows** in Meta Business Manager using the JSON files in `src/whatsappflows/`
2. **Set Flow Endpoint URL** to: `https://your-domain/api/whatsapp/flow-endpoint`
3. **Update Flow IDs** in `WhatsAppFlowOrchestrationService.cs` constants to match the Meta-assigned IDs
4. **Configure encryption** keys for the flow endpoint (TODO: implement Meta's required AES-GCM encryption)

### Flow Token Format
The `flow_token` sent with each flow message is formatted as `"userId:flowId"`, which the endpoint controller parses to identify the user and flow context.

## API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/whatsappwebhook/webhook` | Meta webhook verification |
| POST | `/api/whatsappwebhook/webhook` | Receive incoming messages |
| POST | `/api/whatsapp/flow-endpoint` | Meta Flow data_exchange callbacks |
| GET | `/api/whatsapp/flow-endpoint` | Flow endpoint health check |

## TODO / Next Steps

- [ ] Implement Meta's AES-GCM encryption/decryption for flow endpoint
- [ ] Upload flow JSONs to Meta Business Manager and update flow ID constants
- [ ] Add WhatsApp media download for document photos (PhotoPicker)
- [ ] Add flow analytics/tracking
- [ ] Add rate limiting for flow endpoint
- [ ] Test cross-channel resume (start WhatsApp → continue web)
