# Omnichannel Loan Application - Testing Guide

**Date:** December 1, 2025

---

## Quick Start Testing

### Prerequisites
- Backend running: `http://localhost:5000`
- Frontend running: `http://localhost:3000`
- User logged in with valid JWT token

---

## Test Scenario 1: Complete Web Application

**Goal:** Apply for a loan entirely through the web interface

**Steps:**
1. Navigate to `/loans/apply`
2. **Step 0 - Loan Amount:**
   - Select amount: R25,000
   - Click "Next"
   - ✅ Verify: Draft application created in database
3. **Step 1 - Term:**
   - Select: 24 months
   - Click "Next"
   - ✅ Verify: Application updated with termMonths = 24
4. **Step 2 - Purpose:**
   - Select: "Emergency"
   - Click "Next"
5. **Step 3 - Affordability:**
   - View affordability assessment
   - Click "Continue"
   - ✅ Verify: `IsAffordabilityIncluded = true`
6. **Step 4 - Preview:**
   - Review loan terms
   - ✅ Verify: InterestRate, MonthlyPayment calculated
   - Click "Accept & Continue"
7. **Step 5 - Bank Details:**
   - Bank: ABSA
   - Account: 1234567890
   - Name: John Doe
   - Click "Next"
8. **Step 6 - Signature:**
   - Check "I agree"
   - Click "Send OTP" (skipped for now)
   - Click "Submit Application"
   - ✅ Verify: Status = Pending

**Expected Result:** Application submitted successfully, appears in `/loans` list

---

## Test Scenario 2: Affordability Synchronization

**Goal:** Update affordability on web, verify it syncs everywhere

**Steps:**
1. Navigate to `/affordability`
2. Update income: R35,000/month
3. Update expenses:
   - Rent: R8,000
   - Food: R3,000
   - Transport: R2,000
4. Click "Calculate Affordability"
5. ✅ Verify API response: `maxRecommendedLoanAmount` updated
6. Navigate to `/loans/apply`
7. At Step 0 (Loan Amount):
   - ✅ Verify: Slider shows new max amount
8. At Step 3 (Affordability Review):
   - ✅ Verify: Shows latest income/expenses
   - ✅ Verify: "Affordable" status displayed

**Expected Result:** Changes to affordability reflect immediately in loan application wizard

---

## Test Scenario 3: Resume Application (Web)

**Goal:** Start application, close browser, resume later

**Steps:**
1. Start new application: `/loans/apply`
2. Complete Steps 0-2
3. Note the application ID from browser console
4. **Close browser tab** (simulate interruption)
5. Later: Navigate to `/loans`
6. Find draft application in list
7. Click "Resume" button
8. ✅ Verify: Wizard opens at Step 3
9. ✅ Verify: Previous data (amount, term, purpose) pre-filled
10. Complete remaining steps

**Expected Result:** Application resumes from last saved step with all data intact

---

## Test Scenario 4: Multiple Draft Applications

**Goal:** Test that users can have multiple drafts

**Steps:**
1. Start Application A: Amount R10,000, save Step 0
2. Navigate back to `/loans`
3. Click "Apply for New Loan"
4. Start Application B: Amount R25,000, save Step 0
5. Navigate to `/loans`
6. ✅ Verify: Both drafts appear in list
7. Resume Application A
8. ✅ Verify: Shows R10,000 (not R25,000)
9. Complete Application A and submit
10. ✅ Verify: Application A status = Pending
11. ✅ Verify: Application B still in Draft

**Expected Result:** Multiple drafts handled correctly, no data mixing

---

## Test Scenario 5: Channel Switching (Simulation)

**Goal:** Simulate WhatsApp to Web channel switch

**Since WhatsApp bot isn't implemented yet, we'll simulate via API:**

### Using Postman/curl:

1. **Create draft via "WhatsApp":**
```bash
POST http://localhost:5000/api/loanapplications/draft
Authorization: Bearer {your_jwt_token}
Content-Type: application/json

{
  "channelOrigin": "WhatsApp"
}
```

Response: `{ "id": "abc-123-def", ... }`

2. **Update steps (simulate WhatsApp bot):**
```bash
PUT http://localhost:5000/api/loanapplications/abc-123-def/step/0
Authorization: Bearer {your_jwt_token}
Content-Type: application/json

{
  "amount": 15000
}
```

```bash
PUT http://localhost:5000/api/loanapplications/abc-123-def/step/1
{
  "termMonths": 12
}
```

3. **Resume on Web:**
```bash
POST http://localhost:5000/api/loanapplications/resume
Authorization: Bearer {your_jwt_token}
Content-Type: application/json

{
  "targetChannel": "Web"
}
```

Response: `{ "id": "abc-123-def", "currentStep": 1, "amount": 15000, ... }`

4. **Open Web UI:**
   - Navigate to `/loans/apply?resume=true`
   - ✅ Verify: Loads at Step 2 with R15,000 and 12 months pre-filled
   - Complete remaining steps on web
   - Submit

**Expected Result:** Application started on "WhatsApp" completed on Web seamlessly

---

## Test Scenario 6: Affordability Update During Application

**Goal:** Update affordability mid-application

**Steps:**
1. Start loan application
2. Complete Step 0: Amount R40,000
3. Complete Step 1: Term 24 months
4. **Before Step 3**, open new tab
5. Navigate to `/affordability`
6. Update income: R45,000 (higher)
7. Update expenses: R15,000
8. Save affordability
9. Return to loan application tab
10. Click "Next" to reach Step 3
11. ✅ Verify: Step 3 shows NEW affordability data
12. ✅ Verify: "Affordable" status (because of higher income)

**Expected Result:** Affordability synced automatically when reaching Step 3

---

## Database Verification Queries

### Check Application State
```sql
SELECT 
    Id,
    UserId,
    Amount,
    TermMonths,
    Purpose,
    Status,
    ChannelOrigin,
    CurrentStep,
    IsAffordabilityIncluded,
    InterestRate,
    MonthlyPayment,
    CreatedAt as ApplicationDate
FROM LoanApplications
WHERE UserId = '{your_user_id}'
ORDER BY ApplicationDate DESC;
```

### Check WhatsApp Sessions
```sql
SELECT 
    ws.Id,
    ws.PhoneNumber,
    ws.SessionStatus,
    ws.CreatedAt,
    la.Amount,
    la.CurrentStep,
    la.Status
FROM WhatsAppSessions ws
LEFT JOIN LoanApplications la ON ws.DraftApplicationId = la.Id
WHERE ws.UserId = '{your_user_id}'
ORDER BY ws.CreatedAt DESC;
```

### Check Affordability
```sql
SELECT 
    UserId,
    GrossMonthlyIncome,
    TotalMonthlyExpenses,
    AvailableFunds,
    MaxRecommendedLoanAmount,
    AffordabilityStatus,
    AssessmentDate
FROM AffordabilityAssessments
WHERE UserId = '{your_user_id}';
```

---

## API Endpoint Testing (Postman Collection)

### 1. Create Draft
```
POST /api/loanapplications/draft
Headers: Authorization: Bearer {token}
Body: { "channelOrigin": "Web" }
```

### 2. Update Step 0 (Amount)
```
PUT /api/loanapplications/{id}/step/0
Headers: Authorization: Bearer {token}
Body: { "amount": 25000 }
```

### 3. Update Step 1 (Term)
```
PUT /api/loanapplications/{id}/step/1
Body: { "termMonths": 24 }
```

### 4. Update Step 2 (Purpose)
```
PUT /api/loanapplications/{id}/step/2
Body: { "purpose": "Emergency" }
```

### 5. Update Step 3 (Affordability - auto-syncs)
```
PUT /api/loanapplications/{id}/step/3
Body: {}
```

### 6. Update Step 4 (Preview - calculates terms)
```
PUT /api/loanapplications/{id}/step/4
Body: {}
```

### 7. Update Step 5 (Bank Details)
```
PUT /api/loanapplications/{id}/step/5
Body: {
  "bankName": "ABSA Bank",
  "accountNumber": "1234567890",
  "accountHolderName": "John Doe"
}
```

### 8. Submit Application
```
POST /api/loanapplications/{id}/submit
Body: { "otp": "123456" }
```

### 9. Resume Application
```
POST /api/loanapplications/resume
Body: { "targetChannel": "Web" }
```

---

## Success Criteria

### ✅ Omnichannel Features Working:
- [ ] Draft application created with channel tracking
- [ ] Step updates save data correctly
- [ ] Affordability syncs at Step 3
- [ ] Loan terms calculated at Step 4
- [ ] Bank details saved at Step 5
- [ ] Application submits successfully
- [ ] Resume returns correct application
- [ ] Multiple drafts don't conflict
- [ ] Channel origin tracked (Web/WhatsApp)

### ✅ Data Consistency:
- [ ] StepData dictionary updated
- [ ] CurrentStep increments correctly
- [ ] Status changes: Draft → Pending
- [ ] Affordability linked to application
- [ ] Calculations accurate (interest, payment, total)

### ✅ Frontend Integration:
- [ ] Wizard loads resume applications
- [ ] Previous data pre-fills correctly
- [ ] Navigation works (next/prev)
- [ ] Submit button enables when valid
- [ ] Success page shows after submit

---

## Common Issues & Fixes

### Issue: "Application not found"
**Cause:** Wrong user ID or deleted application  
**Fix:** Verify JWT token user ID matches application userId

### Issue: Affordability not updating
**Cause:** Step 3 not reached  
**Fix:** Affordability syncs automatically when user reaches Step 3

### Issue: Resume returns empty
**Cause:** No draft applications exist  
**Fix:** Create at least one draft before calling resume

### Issue: Monthly payment = 0
**Cause:** Step 4 not reached yet  
**Fix:** Calculations happen at Step 4, navigate there first

---

## Next: WhatsApp Bot Testing

Once WhatsApp bot is implemented, test:
1. Send "Apply for loan" to bot
2. Bot creates draft via API
3. Complete steps 0-3 on WhatsApp
4. Reply "CONTINUE WEB"
5. Open web link with `?resume=true`
6. Complete steps 4-6 on web
7. Verify status = Pending in both channels

---

**Testing completed successfully when all scenarios pass! 🎉**
