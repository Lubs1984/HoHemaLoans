# Ho Hema Loans - Omnichannel Loan Application Design

**Version:** 1.0  
**Date:** November 26, 2025  
**Purpose:** Unified loan application experience across Web and WhatsApp channels with seamless switching

---

## 1. Overview

The omnichannel loan application system allows users to:
- **Start on Web** → Continue on WhatsApp
- **Start on WhatsApp** → Continue on Web
- **Switch freely** between channels without losing progress
- **Maintain consistent** affordability assessment and calculations
- **Access application state** from any channel

---

## 2. Core Architecture

### 2.1 Data Model Extensions

#### LoanApplication Enhancement (Existing Model)
```csharp
public class LoanApplication
{
    // Existing fields...
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }
    public string Purpose { get; set; }
    public LoanStatus Status { get; set; }
    
    // NEW: Omnichannel tracking
    public LoanApplicationChannel ChannelOrigin { get; set; }      // Web or WhatsApp
    public DateTime? WhatsAppInitiatedDate { get; set; }            // When started on WhatsApp
    public DateTime? WebInitiatedDate { get; set; }                 // When started on Web
    public int CurrentStep { get; set; }                            // 0-6: Which step in wizard
    public Dictionary<string, object>? StepData { get; set; }      // Store progress per step
    public string? WhatsAppSessionId { get; set; }                  // Link to WhatsApp conversation
    public bool IsAffordabilityIncluded { get; set; }              // Linked to affordability?
}

public enum LoanApplicationChannel
{
    Web,
    WhatsApp
}

public enum ApplicationStep
{
    LoanAmount = 0,          // Select amount
    TermMonths = 1,          // Select term
    Purpose = 2,             // Select purpose
    AffordabilityReview = 3, // Check affordability
    PreviewTerms = 4,        // Preview and confirm
    BankDetails = 5,         // Enter bank account
    DigitalSignature = 6     // Sign contract
}
```

#### WhatsAppSession Model (NEW)
```csharp
public class WhatsAppSession
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string PhoneNumber { get; set; }              // User's WhatsApp number
    
    [Required]
    public string UserId { get; set; }                   // Link to user account
    
    [Required]
    public Guid? DraftApplicationId { get; set; }        // Link to draft application
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    [StringLength(20)]
    public string SessionStatus { get; set; }             // Active, Completed, Abandoned
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; }
    
    [ForeignKey("DraftApplicationId")]
    public virtual LoanApplication DraftApplication { get; set; }
}
```

### 2.2 Database Relationships

```
ApplicationUser (1) ──┬──> (Many) LoanApplication
                      │
                      └──> (Many) WhatsAppSession

LoanApplication (1) ──> (1) WhatsAppSession (Optional)
                      ├──> (1) AffordabilityAssessment (Optional)
                      └──> (0..1) WhatsAppConversation
```

---

## 3. Application Flow States

### 3.1 Web Application Flow

```
User on Web Portal
        ↓
[Step 0] Loan Amount Selection
  - Show slider: R500 - R50,000
  - Auto-calculate max from affordability
  - Show monthly payment preview
  - "Create Application" button
        ↓
        ✓ Draft application created
        ✓ Store in LoanApplication (Status: Draft, ChannelOrigin: Web)
        ↓
[Step 1] Term Months Selection
  - Show options: 6, 12, 24, 36 months
  - Recalculate payment for selected term
  - "Next" button
        ↓
[Step 2] Purpose Selection
  - Dropdown: Emergency, Education, Medical, Other
  - Text description (optional)
  - "Next" button
        ↓
[Step 3] Affordability Review
  - Pull latest AffordabilityAssessment
  - Show:
    • Gross monthly income
    • Monthly expenses
    • Available funds
    • Debt-to-income ratio
    • Max recommended loan
    • Status: Affordable/Not Affordable
  - If NotAffordable: Show "Continue Anyway?" or "Exit"
  - If Affordable: Proceed automatically
        ↓
[Step 4] Preview Terms
  - Display loan summary:
    • Requested amount: R25,000
    • Monthly payment: R750
    • Term: 24 months
    • Interest rate: 12.5%
    • Total repayment: R18,000
  - "Confirm & Continue" button
  - "Back" button
        ↓
[Step 5] Bank Details Entry
  - Form fields:
    • Bank name (dropdown)
    • Account number
    • Account holder name
  - Client-side validation
  - "Verify & Continue" button
        ↓
[Step 6] Digital Signature
  - Display contract PDF
  - OTP input field
  - "Request OTP" button
  - Enter OTP and submit
  - Status: Pending (awaiting admin approval)
        ↓
Success Page: Application reference & next steps

OPTIONAL: "Continue on WhatsApp" link
  - Share application ID via QR code
  - Link: whatsapp://send?phone=[ADMIN]&text=App:%20[AppID]
```

### 3.2 WhatsApp Application Flow

```
User Messages WhatsApp Bot
        ↓
"Hi, I want to apply for a loan"
        ↓
Bot: "Welcome to Ho Hema Loans! 🎉
Let's get you started.
Reply with:
1. Apply for loan
2. Check status
3. Get information"
        ↓
User: "1" (Apply for loan)
        ✓ WhatsAppSession created
        ✓ Draft LoanApplication created (Status: Draft, ChannelOrigin: WhatsApp)
        ↓
Bot: "Step 1 of 6: Loan Amount
How much do you need? (R500-R50,000)
Reply with amount, e.g. 25000"
        ↓
User: "25000"
        ✓ Update LoanApplication.Amount & CurrentStep
        ↓
Bot: "Great! Monthly payment would be ~R750
Step 2 of 6: Loan Term
How many months?
Reply: 6, 12, 24, or 36"
        ↓
User: "24"
        ✓ Update LoanApplication.TermMonths & CurrentStep
        ↓
Bot: "Step 3 of 6: Loan Purpose
Why do you need the loan?
Reply: EMERGENCY, EDUCATION, MEDICAL, or OTHER"
        ↓
User: "EMERGENCY"
        ✓ Update LoanApplication.Purpose & CurrentStep
        ↓
Bot: "Step 4 of 6: Affordability Check
Checking your eligibility based on your income and expenses...

Good news! You're AFFORDABLE for this loan! ✅
Max recommended: R45,000
Your requested: R25,000
Available funds: R8,500/month"
        ↓
Bot: "Step 5 of 6: Bank Details
To continue, please reply with:
Bank name | Account number | Account holder name

Example: ABSA | 1234567890 | John Doe"
        ↓
User: "ABSA | 1234567890 | John Doe"
        ✓ Update LoanApplication & Store bank details
        ↓
Bot: "Step 6 of 6: Digital Signature
Contract sent below 👇
Please review and confirm you agree.

[Contract text/PDF link]

Reply: AGREE to proceed with OTP
Reply: CANCEL to exit"
        ↓
User: "AGREE"
        ↓
Bot: "Sending OTP to verify... 📱
Enter OTP received in SMS:
(Code valid for 10 minutes)"
        ↓
User: "123456"
        ✓ Validate OTP
        ✓ LoanApplication.Status = Pending
        ↓
Bot: "✅ Application submitted!
Your reference: APP-2025-11-26-00123
Status: Under Review
Next steps:
1. Check status: Reply STATUS
2. Continue on web: [Web link with reference]"

OPTIONAL: "Continue on Web" link
  - Deep link with application ID
  - User logs in and sees application in-progress
```

---

## 4. Channel Switching Flows

### 4.1 Switch from Web to WhatsApp

**Scenario:** User started application on Web, now wants to continue on WhatsApp

```
User on Web Portal [Step 2 of 6]
        ↓
Display Button: "Continue on WhatsApp ↗"
        ↓
User clicks button
        ↓
Show Modal:
"Continue this application on WhatsApp?
Your progress will be saved.
Send this to your WhatsApp:"

QR Code: [App ID encoded]
Link: [Deep link to WhatsApp]
Manual: "Or message your app ID: APP-2025-11-26-00123"
        ↓
User opens WhatsApp
        ↓
Bot receives: "CONTINUE APP-2025-11-26-00123"
        ↓
Bot retrieves LoanApplication by ID
        ↓
Bot identifies current step [Step 2]
        ↓
Bot: "Welcome back! 👋
I found your application from the web.
You were at Step 3 of 6: Loan Purpose
Let's continue..."
        ↓
Resumes from current step
        ✓ Update WhatsAppSession with ApplicationId
        ✓ LoanApplication.WhatsAppSessionId = SessionId
```

### 4.2 Switch from WhatsApp to Web

**Scenario:** User started application on WhatsApp, now wants to complete on Web

```
User on WhatsApp [Step 4 of 6]
        ↓
User replies: "SWITCH WEB"
        ↓
Bot: "Great! Complete your application on the web.
Log in here: [Web link]
Your application ID: APP-2025-11-26-00123
Press 'Resume Application' to continue"
        ↓
User clicks web link
        ↓
User logs in on web portal
        ↓
Web shows: "Resume Application"
Resume button with: "Continue from Step 4 (Bank Details)"
        ↓
User clicks "Resume"
        ↓
Web retrieves LoanApplication
        ↓
Web displays current step [Step 4]
        ↓
Loads form with existing data
        ✓ LoanApplication status remains consistent
        ✓ All previous inputs preserved
```

---

## 5. API Endpoints

### 5.1 Web Portal Endpoints

#### Create New Application (Draft)
```
POST /api/loanapplications
Headers: Authorization: Bearer {token}

Request:
{
  "channelOrigin": "Web"
}

Response:
{
  "id": "app-id-uuid",
  "status": "Draft",
  "currentStep": 0,
  "channelOrigin": "Web",
  "createdAt": "2025-11-26T10:30:00Z"
}
```

#### Get Application by ID (for resume)
```
GET /api/loanapplications/{id}
Headers: Authorization: Bearer {token}

Response:
{
  "id": "app-id-uuid",
  "amount": 25000,
  "termMonths": 24,
  "purpose": "EMERGENCY",
  "status": "Draft",
  "currentStep": 4,
  "channelOrigin": "Web",
  "stepData": {
    "amount": 25000,
    "termMonths": 24,
    "purpose": "EMERGENCY",
    "bankName": "ABSA",
    "accountNumber": "1234567890"
  },
  "monthlyPayment": 750,
  "totalAmount": 18000,
  "affordabilityStatus": "Affordable"
}
```

#### Update Application Step
```
PUT /api/loanapplications/{id}/step/{stepNumber}
Headers: Authorization: Bearer {token}

Request:
{
  "stepNumber": 2,
  "data": {
    "purpose": "EMERGENCY"
  }
}

Response:
{
  "currentStep": 2,
  "status": "Draft",
  "updatedAt": "2025-11-26T10:35:00Z"
}
```

#### Submit Application
```
POST /api/loanapplications/{id}/submit
Headers: Authorization: Bearer {token}

Request:
{
  "otp": "123456"
}

Response:
{
  "id": "app-id-uuid",
  "status": "Pending",
  "submittedAt": "2025-11-26T10:40:00Z",
  "referenceNumber": "REF-20251126-00123"
}
```

#### Get User Applications List
```
GET /api/loanapplications
Headers: Authorization: Bearer {token}

Response:
[
  {
    "id": "app-id-1",
    "amount": 25000,
    "status": "Pending",
    "channelOrigin": "Web",
    "createdAt": "2025-11-26T09:00:00Z"
  },
  {
    "id": "app-id-2",
    "amount": 15000,
    "status": "Approved",
    "channelOrigin": "WhatsApp",
    "createdAt": "2025-11-25T14:20:00Z"
  }
]
```

### 5.2 WhatsApp Endpoints

#### Webhook Receiver (Existing, Enhanced)
```
POST /api/whatsapp/webhook
Headers: Authorization: Bearer {webhookToken}

Request (from Meta):
{
  "object": "whatsapp_business_account",
  "entry": [{
    "changes": [{
      "value": {
        "messages": [{
          "from": "27822531234",
          "id": "wamid.xxx",
          "timestamp": "1700976000",
          "text": { "body": "25000" }
        }],
        "contacts": [{
          "profile": { "name": "John Doe" },
          "wa_id": "27822531234"
        }]
      }
    }]
  }]
}

Response:
{ "status": "received" }
```

#### Get WhatsApp Session
```
GET /api/whatsapp/session/{phoneNumber}
Headers: Authorization: Bearer {webhookToken}

Response:
{
  "id": "session-uuid",
  "phoneNumber": "27822531234",
  "draftApplicationId": "app-id-uuid",
  "currentStep": 4,
  "sessionStatus": "Active",
  "createdAt": "2025-11-26T09:00:00Z"
}
```

#### Resume from WhatsApp
```
POST /api/loanapplications/{id}/resume-whatsapp
Headers: Authorization: Bearer {webhookToken}

Request:
{
  "phoneNumber": "27822531234"
}

Response:
{
  "application": { ...application data },
  "session": { ...session data },
  "resumeStep": 4
}
```

---

## 6. Implementation Plan

### Phase A: Backend Foundation (Week 1)

#### A1: Database Migrations
- [ ] Create migration: AddOmnichannelTracking
  - Add fields to LoanApplication
  - Create WhatsAppSession table
  - Add foreign key relationships
  
#### A2: Models & DTOs
- [ ] Create WhatsAppSessionDto
- [ ] Create ApplicationStepDto
- [ ] Extend LoanApplicationDto with new fields

#### A3: Core Service Layer
- [ ] Create `OmnichannelLoanService`:
  - CreateDraftApplication(userId, channel)
  - UpdateApplicationStep(applicationId, stepNumber, data)
  - ResumeApplication(applicationId, channel)
  - SubmitApplication(applicationId, otp)

#### A4: API Endpoints
- [ ] POST /api/loanapplications (create draft)
- [ ] GET /api/loanapplications/{id} (retrieve)
- [ ] PUT /api/loanapplications/{id}/step/{step} (update step)
- [ ] POST /api/loanapplications/{id}/submit (submit)
- [ ] GET /api/loanapplications (list user's applications)

### Phase B: WhatsApp Integration (Week 1-2)

#### B1: WhatsApp Service
- [ ] Create `WhatsAppApplicationService`:
  - InitiateApplication(phoneNumber)
  - ProcessApplicationStep(phoneNumber, stepNumber, data)
  - RetrieveApplicationState(phoneNumber)
  - VerifyOTP(phoneNumber, otp)

#### B2: WhatsApp Flows
- [ ] Define interactive flow JSON for:
  - Loan amount selection
  - Term months selection
  - Purpose selection
  - Bank details entry
  - OTP verification

#### B3: Message Templates
- [ ] Create templates for:
  - Step prompts
  - Affordability results
  - Success confirmations
  - Error messages

### Phase C: Frontend Web Portal (Week 2)

#### C1: Components
- [ ] `LoanApplicationsList.tsx`
  - Display all user applications
  - Show channel origin badge
  - Resume button for drafts
  
- [ ] `LoanApplicationWizard.tsx`
  - Multi-step form component
  - Step indicator
  - Navigation (prev/next)
  
- [ ] Application step components:
  - `LoanAmountStep.tsx`
  - `TermMonthsStep.tsx`
  - `PurposeStep.tsx`
  - `AffordabilityReviewStep.tsx`
  - `PreviewTermsStep.tsx`
  - `BankDetailsStep.tsx`
  - `DigitalSignatureStep.tsx`

#### C2: Integration
- [ ] Fetch user applications on dashboard load
- [ ] Show "Resume" vs "New Application" options
- [ ] Implement step-by-step form flow
- [ ] Add "Continue on WhatsApp" button on each step
- [ ] Implement OTP submission
- [ ] Show success page with reference number

### Phase D: Testing & Validation (Week 3)

#### D1: API Testing
- [ ] Test all endpoints with various states
- [ ] Test channel switching scenarios
- [ ] Test affordability integration
- [ ] Test OTP verification

#### D2: End-to-End Testing
- [ ] Web → Web flow
- [ ] WhatsApp → WhatsApp flow
- [ ] Web → WhatsApp switch
- [ ] WhatsApp → Web switch
- [ ] Multi-step form with validation

#### D3: Deployment
- [ ] Deploy to staging
- [ ] Test with real WhatsApp sandbox
- [ ] Performance testing
- [ ] Security validation

---

## 7. Security Considerations

### 7.1 Authentication & Authorization
- ✅ All endpoints require JWT token or webhook token
- ✅ WhatsApp phone number linked to user account
- ✅ OTP verification for application submission
- ✅ Application access only by owner or admin

### 7.2 Data Protection
- ✅ Encrypt bank details in database
- ✅ Mask sensitive data in logs
- ✅ HTTPS for all API calls
- ✅ Rate limiting on application submission

### 7.3 Session Security
- ✅ WhatsApp sessions expire after inactivity
- ✅ Draft applications auto-save progress
- ✅ Session linking prevents unauthorized access
- ✅ Audit trail for all application changes

---

## 8. User Experience Flow Summary

### Best Case Scenario: Web to WhatsApp

1. User opens web portal
2. Clicks "Apply for Loan"
3. Completes steps 1-3 on web
4. Clicks "Continue on WhatsApp"
5. Shares to WhatsApp
6. Bot recognizes application
7. Bot resumes from step 4
8. User completes final steps on WhatsApp
9. Application submitted with status "Pending"
10. User can check status on web portal anytime

### Best Case Scenario: WhatsApp to Web

1. User messages WhatsApp bot
2. Bot initiates application
3. User completes steps 1-4 on WhatsApp
4. User replies "CONTINUE WEB"
5. User follows web link and logs in
6. Web shows "Resume from Step 5"
7. User completes steps 5-6 on web
8. Application submitted
9. User receives confirmation on both channels

---

## 9. Success Metrics

- ✅ Users can switch channels without losing progress
- ✅ 90%+ application completion rate
- ✅ <5 minute average time per application
- ✅ 0 data loss during channel switching
- ✅ 100% affordability assessment consistency
- ✅ <2% OTP verification failures

