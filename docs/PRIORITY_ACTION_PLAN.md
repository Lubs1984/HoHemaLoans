# 🎯 Ho Hema Loans - Priority Action Plan

**Date:** February 9, 2026  
**Focus:** Critical Path to Launch

---

## 🚨 CRITICAL GAPS - MUST FIX BEFORE LAUNCH

### 1. NCR COMPLIANCE ⚠️ **LEGAL BLOCKER**

**Current Status:** 5% Complete  
**Risk Level:** 🔴 CRITICAL - Cannot operate legally without this

#### Immediate Actions Required:

**Week 1-2:**
- [ ] Register company with NCR (obtain NCRCP number)
- [ ] Display NCRCP number on website footer
- [ ] Implement interest rate cap validation
  - R0-R10,000: Max 28% p.a.
  - R10,001-R25,000: Max 24% p.a.
  - R25,001+: Max 22% p.a.
- [ ] Implement fee cap validation
  - Initiation fee: Max R1,140 or 15% (whichever greater)
  - Monthly service fee: Max R60
  
**Week 3-4:**
- [ ] Generate Form 39 (Credit Agreement) PDF
  - Template with all NCR-required fields
  - Auto-populate from loan application
  - Store securely with audit trail
- [ ] Implement 5-day cooling-off period
  - Block disbursement during period
  - Allow cancellation without penalty
- [ ] Create complaint management system
  - Complaint submission form
  - Unique reference numbers
  - 21-day resolution tracking
  - NCR contact info display

**Code Changes Needed:**
```csharp
// File: LoanApplicationsController.cs
// Add validation before creating loan

// Check interest rate cap
if (loanAmount <= 10000 && interestRate > 0.28m)
    return BadRequest("Interest rate exceeds NCR cap of 28% for loans under R10,000");

// Check fee caps
if (initiationFee > Math.Max(1140, loanAmount * 0.15m))
    return BadRequest("Initiation fee exceeds NCR limits");
```

---

### 2. PAYMENT INTEGRATION 💰 **REVENUE BLOCKER**

**Current Status:** 5% Complete  
**Risk Level:** 🔴 CRITICAL - Cannot disburse or collect payments

#### Immediate Actions Required:

**Week 1:**
- [ ] Research SA banking API providers:
  - Peach Payments
  - PayFast
  - Ozow
  - Stitch (recommended for instant EFT)
- [ ] Sign up for sandbox account
- [ ] Get API credentials

**Week 2-3:**
- [ ] Implement payment disbursement service
  ```csharp
  public interface IPaymentService
  {
      Task<PaymentResult> DisburseLoanAsync(string bankAccount, decimal amount);
      Task<PaymentResult> CollectRepaymentAsync(string bankAccount, decimal amount);
      Task<PaymentStatus> GetPaymentStatusAsync(string paymentId);
  }
  ```
- [ ] Create PaymentTransaction table
- [ ] Store payment references
- [ ] Handle webhooks for status updates

**Week 4:**
- [ ] Implement repayment collection
- [ ] Schedule automatic debit orders
- [ ] Failed payment handling
- [ ] Payment reconciliation

---

### 3. DIGITAL CONTRACT & SIGNATURE 📝 **LEGAL BLOCKER**

**Current Status:** 10% Complete  
**Risk Level:** 🔴 CRITICAL - Cannot legally lend without signed agreement

#### Immediate Actions Required:

**Week 1-2:**
- [ ] Create Form 39 PDF template
  - Use library: QuestPDF or DinkToPdf
  - Include all NCR-required sections:
    - Credit provider details + NCRCP number
    - Consumer details
    - Loan amount, interest rate, fees
    - Repayment schedule
    - Total cost of credit
    - Consumer rights (cooling-off, early settlement)
    - Complaint procedures
- [ ] Implement PDF generation service
  ```csharp
  public interface IContractService
  {
      Task<byte[]> GenerateForm39Async(LoanApplication application);
      Task<string> StoreContractAsync(Guid applicationId, byte[] pdfBytes);
      Task<byte[]> GetContractAsync(string contractReference);
  }
  ```

**Week 3:**
- [ ] Implement OTP-based digital signature
  - Generate 6-digit OTP
  - Send via SMS/WhatsApp
  - 10-minute expiry
  - Store signature timestamp
- [ ] Update LoanApplication with signature fields
  ```csharp
  public DateTime? SignedAt { get; set; }
  public string? SignatureOtp { get; set; }
  public string? SignatureIpAddress { get; set; }
  public string? ContractDocumentUrl { get; set; }
  ```

**Week 4:**
- [ ] Integrate into loan application wizard
  - Step 6: Display contract preview
  - User reviews and checks "I agree"
  - OTP sent and verified
  - Contract finalized and stored
- [ ] Set up Azure Blob Storage for contracts
  - Encrypted storage
  - 5-year retention policy
  - Secure signed URLs for access

---

### 4. SYSTEM SETTINGS MANAGEMENT ⚙️ **OPERATIONAL BLOCKER**

**Current Status:** Table exists, not used  
**Risk Level:** 🟡 HIGH - Cannot adjust rates without code changes

#### Immediate Actions Required:

**Week 1:**
- [ ] Create SystemSettingsService
  ```csharp
  public interface ISystemSettingsService
  {
      Task<decimal> GetInterestRateAsync();
      Task<decimal> GetAdminFeeAsync();
      Task<decimal> GetMaxLoanPercentageAsync();
      Task UpdateSettingAsync(string key, decimal value);
  }
  ```
- [ ] Seed initial settings:
  - InterestRate: 0.05 (5%)
  - AdminFee: 50
  - MaxLoanPercentage: 0.20 (20%)
  - MinLoanAmount: 100
  - MaxLoanAmount: 5000

**Week 2:**
- [ ] Update loan calculation to use settings
- [ ] Create admin UI for settings management
  - Settings page in admin panel
  - Edit interest rate
  - Edit admin fee
  - Edit loan limits
  - Audit trail of changes

---

### 5. WHATSAPP INTERACTIVE FLOWS 📱 **USER EXPERIENCE**

**Current Status:** Infrastructure 60%, Flows 0%  
**Risk Level:** 🟡 HIGH - Marketing promise not delivered

#### Immediate Actions Required:

**Week 1-2:**
- [ ] Design conversation flow
  ```
  User: "Hi" / "Loan"
  Bot: "Welcome! Would you like to:
       1. Apply for a loan
       2. Check balance
       3. Make payment
       Reply with a number"
  
  [If user selects 1]
  Bot: "Great! Let's apply. How many hours did you work this month?"
  User: "160"
  Bot: "What's your hourly rate?"
  User: "R100"
  Bot: "You earned R16,000. You can borrow up to R3,200 (20%).
       How much do you need?"
  [Continue steps...]
  ```

**Week 3:**
- [ ] Implement message handler router
  ```csharp
  public class WhatsAppFlowHandler
  {
      public async Task HandleMessageAsync(WhatsAppMessage message)
      {
          var session = await GetOrCreateSessionAsync(message.ContactId);
          
          switch (session.CurrentStep)
          {
              case "GREETING":
                  await HandleGreetingAsync(session, message);
                  break;
              case "COLLECT_HOURS":
                  await HandleHoursInputAsync(session, message);
                  break;
              // ... more steps
          }
      }
  }
  ```

**Week 4:**
- [ ] Test end-to-end WhatsApp application
- [ ] Add fallback to human agent
- [ ] Handle errors gracefully

---

## 📅 4-WEEK SPRINT PLAN

### Sprint 1 (Week 1-2): NCR Foundation + Payment Research

**Goals:**
- NCR registration initiated
- Interest/fee caps implemented
- Payment provider selected and sandbox setup
- Form 39 template created

**Deliverables:**
- [ ] NCR registration application submitted
- [ ] Rate/fee validation in LoanApplicationsController
- [ ] Payment provider sandbox account
- [ ] Form 39 PDF template with all fields

**Success Metrics:**
- Loan applications reject if rates/fees exceed NCR limits
- Form 39 can be generated manually from loan data

---

### Sprint 2 (Week 3-4): Payment Integration + Digital Signature

**Goals:**
- Payment disbursement working in sandbox
- Digital signature fully functional
- Contracts stored securely

**Deliverables:**
- [ ] PaymentService integrated
- [ ] Payment disbursement tested in sandbox
- [ ] OTP-based signature working
- [ ] Azure Blob Storage configured
- [ ] Contract generation integrated into wizard

**Success Metrics:**
- Can disburse R100 test payment
- User can sign contract via OTP
- Signed contract PDF stored and retrievable

---

### Sprint 3 (Week 5-6): WhatsApp Flows + System Settings

**Goals:**
- WhatsApp loan application flow complete
- Admin can configure rates via UI
- End-to-end testing

**Deliverables:**
- [ ] WhatsApp conversation flow implemented
- [ ] System settings admin UI
- [ ] Integration tests for critical paths
- [ ] User acceptance testing

**Success Metrics:**
- User can apply for loan via WhatsApp from start to finish
- Admin can change interest rate without code deployment
- All critical user journeys pass testing

---

### Sprint 4 (Week 7-8): Compliance Completion + Launch Prep

**Goals:**
- All NCR requirements met
- Security audit passed
- Production deployment ready

**Deliverables:**
- [ ] NCRCP number displayed
- [ ] Cooling-off period enforced
- [ ] Complaint system live
- [ ] Security audit conducted
- [ ] Load testing completed
- [ ] Production environment configured

**Success Metrics:**
- NCR compliance checklist 100%
- No critical security vulnerabilities
- System handles 100 concurrent users
- Ready for beta launch

---

## 🎯 QUICK WINS (Do These First)

### Today (2-4 hours)
1. [ ] Add NCR rate validation to LoanApplicationsController
2. [ ] Seed SystemSettings table with initial values
3. [ ] Create admin settings page HTML skeleton

### This Week (8-16 hours)
1. [ ] Research and select payment provider
2. [ ] Create Form 39 PDF template
3. [ ] Implement SystemSettingsService
4. [ ] Design WhatsApp conversation flow (document)

### Next Week (20-30 hours)
1. [ ] Integrate Form 39 generation
2. [ ] Build admin settings UI
3. [ ] Start payment service implementation
4. [ ] Create OTP signature flow

---

## 📊 RESOURCE ALLOCATION RECOMMENDATION

### If You Have 1 Developer:
- **Focus:** NCR Compliance + Payment (Sprints 1-2)
- **Timeline:** 4 weeks to MVP
- **Defer:** WhatsApp flows, advanced reporting

### If You Have 2 Developers:
- **Dev 1:** NCR + Contracts + Compliance
- **Dev 2:** Payment Integration + System Settings
- **Timeline:** 3 weeks to MVP
- **Then:** Both work on WhatsApp flows

### If You Have 3+ Developers:
- **Dev 1:** NCR Compliance (contracts, Form 39, caps)
- **Dev 2:** Payment Integration (disbursement, collection)
- **Dev 3:** WhatsApp Flows + System Settings
- **Timeline:** 2 weeks to MVP

---

## 🚀 LAUNCH PHASES

### Phase 1: Private Beta (Week 9)
**Criteria:**
- ✅ NCR compliant
- ✅ Payment disbursement works
- ✅ Digital contracts signed
- ✅ Web portal functional
- ❌ WhatsApp flows (manual support)

**Users:** 10-20 friendly customers  
**Goal:** Validate end-to-end process

### Phase 2: Controlled Launch (Week 11-12)
**Criteria:**
- ✅ All Phase 1 items
- ✅ WhatsApp loan application flow
- ✅ Repayment collection working
- ✅ Basic reporting

**Users:** 100-200 customers  
**Goal:** Stress test and iterate

### Phase 3: Public Launch (Week 14+)
**Criteria:**
- ✅ All compliance requirements
- ✅ Full payment processing
- ✅ Customer support system
- ✅ Monitoring and alerting
- ✅ 24/7 operations ready

**Users:** Unlimited  
**Goal:** Scale and grow

---

## ⚠️ SHOW-STOPPERS TO AVOID

1. **DON'T launch without NCR registration** → Criminal offense
2. **DON'T disburse loans without signed contracts** → Unenforceable debt
3. **DON'T collect payments without proper authorization** → Fraud
4. **DON'T ignore fee/rate caps** → NCR penalties + fines
5. **DON'T skip security testing** → Data breach risk

---

## 📞 SUPPORT & ESCALATION

### Need Help With:
- **NCR Compliance:** Consult compliance attorney
- **Payment Integration:** Contact payment provider support
- **Security:** Hire security consultant
- **WhatsApp:** Meta Business Support
- **Infrastructure:** Azure/Railway support

### Decision Points:
- **Payment Provider Selection:** Week 1
- **Contract Template Approval:** Week 2 (legal review)
- **Launch Date:** Week 8 (after Sprint 4)

---

**Next Update:** February 16, 2026  
**Owner:** Development Team Lead  
**Approval Required:** Business Owner, Compliance Officer

---

*Focus. Execute. Launch. 🚀*
