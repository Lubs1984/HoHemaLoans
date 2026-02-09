# ✅ Ho Hema Loans - Launch Readiness Checklist

**Print this and track your progress!**  
**Target Launch:** April 2026

---

## 🔴 PHASE 1: LEGAL COMPLIANCE (Weeks 1-4) - **CRITICAL**

### NCR Registration & Display
- [ ] Submit NCR registration application (NCRCP)
- [ ] Receive NCRCP registration number
- [ ] Display NCRCP number on website footer
- [ ] Display NCRCP number on mobile/WhatsApp
- [ ] Display NCRCP number on all loan agreements
- [ ] Appoint compliance officer (internal or external)

### Interest Rate & Fee Caps (Code Implementation)
- [ ] Add interest rate validation to loan API
  - [ ] 0-R10K: Max 28% p.a.
  - [ ] R10K-R25K: Max 24% p.a.
  - [ ] R25K+: Max 22% p.a.
- [ ] Add initiation fee validation (Max R1,140 or 15%)
- [ ] Add monthly service fee validation (Max R60)
- [ ] Reject applications exceeding caps
- [ ] Log all validation failures for audit

### Form 39 - Credit Agreement
- [ ] Install PDF generation library (QuestPDF/DinkToPdf)
- [ ] Create Form 39 template with all sections:
  - [ ] Credit provider details + NCRCP number
  - [ ] Consumer full details
  - [ ] Loan amount and purpose
  - [ ] Interest rate (annual and monthly)
  - [ ] All fees itemized
  - [ ] Repayment schedule
  - [ ] Total amount repayable
  - [ ] Consumer rights section
  - [ ] Cooling-off period notice (5 days)
  - [ ] Early settlement rights
  - [ ] Complaint procedures
  - [ ] NCR contact information
- [ ] Implement ContractService.GenerateForm39()
- [ ] Test PDF generation with sample data
- [ ] Legal review of template

### Pre-Agreement Statement
- [ ] Create Pre-Agreement Statement template
- [ ] Include all cost breakdowns
- [ ] Include APR calculation
- [ ] Display before user signs
- [ ] Store with application

### Cooling-Off Period (5 Business Days)
- [ ] Add SignedAt timestamp to LoanApplication
- [ ] Add CoolingOffPeriodEndsAt calculated field
- [ ] Block disbursement during cooling-off period
- [ ] Create cancellation endpoint
- [ ] Refund initiation fee if cancelled during cooling-off
- [ ] Send cooling-off period reminder email/SMS

### Document Storage & Retention
- [ ] Set up Azure Blob Storage / AWS S3
- [ ] Configure encryption at rest (AES-256)
- [ ] Implement 5-year retention policy
- [ ] Create DocumentService for storage
- [ ] Store Form 39 after signature
- [ ] Store Pre-Agreement Statement
- [ ] Store affordability assessment
- [ ] Implement secure document retrieval
- [ ] Set up automatic deletion after 5 years

### Complaint Management System
- [ ] Create Complaints table
- [ ] Create complaint submission form
- [ ] Generate unique complaint reference numbers
- [ ] Auto-acknowledge within 2 business days
- [ ] Track 21-day resolution deadline
- [ ] Display NCR contact details (0860 627 627)
- [ ] Admin complaint management UI
- [ ] Email notifications for complaints

### Affordability Assessment Documentation
- [ ] Ensure affordability calculation saved to DB
- [ ] Store income sources with amounts
- [ ] Store expense items with amounts
- [ ] Store debt-to-income ratio
- [ ] Document assessment reasoning
- [ ] Store assessment date and expiry
- [ ] Flag high-risk applications for manual review

**✅ Phase 1 Complete When:**
- NCRCP number received and displayed everywhere
- Rate/fee caps enforced in code
- Form 39 generates correctly
- Cooling-off period functional
- Documents stored securely
- Complaint system operational

---

## 💰 PHASE 2: PAYMENT INTEGRATION (Weeks 3-6)

### Payment Provider Selection
- [ ] Research SA payment providers:
  - [ ] Stitch (Instant EFT)
  - [ ] Peach Payments
  - [ ] PayFast
  - [ ] Ozow
  - [ ] Capitec Business Payments
- [ ] Compare pricing and features
- [ ] Sign up for sandbox account
- [ ] Get API credentials (store in KeyVault)
- [ ] Review API documentation
- [ ] Test in sandbox environment

### Database Schema
- [ ] Create PaymentTransactions table
  - PaymentId (guid)
  - LoanApplicationId (FK)
  - TransactionType (Disbursement, Repayment)
  - Amount
  - BankAccount
  - PaymentReference
  - Status (Pending, Success, Failed)
  - ProviderTransactionId
  - InitiatedAt
  - CompletedAt
  - FailureReason
- [ ] Create PaymentWebhookLogs table for debugging

### Payment Service Implementation
- [ ] Create IPaymentService interface
- [ ] Implement PaymentService
- [ ] DisburseLoanAsync() method
  - Validate bank account
  - Call provider API
  - Store transaction record
  - Update loan status
  - Handle API errors
- [ ] CollectRepaymentAsync() method
  - Create debit order request
  - Schedule for repayment date
  - Store transaction record
  - Handle failures
- [ ] GetPaymentStatusAsync() method
- [ ] HandlePaymentWebhookAsync() method

### Disbursement Flow
- [ ] Integrate disbursement into loan approval
- [ ] Admin clicks "Approve & Disburse"
- [ ] Check cooling-off period has passed
- [ ] Validate signed contract exists
- [ ] Call PaymentService.DisburseLoanAsync()
- [ ] Update loan status to "Disbursed"
- [ ] Store payment reference
- [ ] Send confirmation to customer (SMS/WhatsApp/Email)

### Repayment Collection
- [ ] Schedule repayment on loan repayment date
- [ ] Create cron job / scheduled task
- [ ] Attempt collection via debit order
- [ ] Handle successful payment
  - Update loan status to "Repaid"
  - Send receipt to customer
  - Close loan application
- [ ] Handle failed payment
  - Mark as "InArrears"
  - Send failure notification
  - Schedule retry (T+1, T+3, T+7 days)
  - Escalate to collections if >30 days

### Payment Webhooks
- [ ] Create webhook endpoint (POST /api/payments/webhook)
- [ ] Verify webhook signature
- [ ] Parse payment status
- [ ] Update PaymentTransaction status
- [ ] Update LoanApplication status
- [ ] Log webhook for audit
- [ ] Return 200 OK to provider

### Bank Account Validation
- [ ] Validate account number format
- [ ] Validate bank name selection
- [ ] Optional: Call bank account verification API
- [ ] Store validated account for repayment

### Payment Reconciliation
- [ ] Admin view of all payment transactions
- [ ] Filter by status (pending, success, failed)
- [ ] Export for accounting
- [ ] Daily reconciliation report

**✅ Phase 2 Complete When:**
- Can disburse R100 test payment successfully
- Can collect R100 test repayment successfully
- Webhooks update statuses correctly
- Failed payments handled gracefully
- Admin can view all transactions

---

## 📝 PHASE 3: DIGITAL SIGNATURE (Weeks 4-6)

### OTP Generation & Delivery
- [ ] Create OTP generation service (6-digit)
- [ ] Store OTP with 10-minute expiry
- [ ] Send OTP via SMS (Twilio/Clickatell)
- [ ] Send OTP via WhatsApp
- [ ] Send OTP via email (fallback)
- [ ] Rate limit OTP requests (max 3 per hour)

### Signature Flow
- [ ] Add signature step to loan wizard
- [ ] Display contract preview PDF
- [ ] User checks "I agree to terms"
- [ ] Request OTP button
- [ ] OTP input field
- [ ] Verify OTP endpoint
- [ ] Store signature details:
  - SignedAt timestamp
  - SignatureMethod (OTP/SMS/WhatsApp)
  - SignatureIpAddress
  - SignatureDeviceInfo
- [ ] Generate final signed contract PDF
- [ ] Store in Azure Blob with reference
- [ ] Email copy to user

### Signature Verification
- [ ] Create GetSignedContractAsync() endpoint
- [ ] Verify signature authenticity
- [ ] Display signature details
- [ ] Download signed PDF
- [ ] Audit trail of contract access

### Legal Validity
- [ ] Consult lawyer on OTP signature legality
- [ ] Add signature disclaimer
- [ ] Store consent timestamp
- [ ] Store device fingerprint
- [ ] Store IP address

**✅ Phase 3 Complete When:**
- User can sign contract via OTP
- Signed contract stored securely
- Signature cannot be repudiated
- Legal review passed

---

## ⚙️ PHASE 4: SYSTEM SETTINGS (Weeks 2-3)

### SystemSettings Service
- [ ] Create ISystemSettingsService interface
- [ ] Implement GetSettingAsync(string key)
- [ ] Implement UpdateSettingAsync(string key, decimal value)
- [ ] Implement GetAllSettingsAsync()
- [ ] Cache settings in memory (15 min TTL)

### Seed Initial Settings
- [ ] InterestRate: 0.05 (5%)
- [ ] AdminFee: 50
- [ ] MaxLoanPercentage: 0.20 (20%)
- [ ] MinLoanAmount: 100
- [ ] MaxLoanAmount: 5000
- [ ] MaxDebtToIncomeRatio: 0.35 (35%)
- [ ] AssessmentValidityDays: 30

### Update Loan Calculation
- [ ] Replace hardcoded rates with SystemSettings
- [ ] Fetch interest rate from settings
- [ ] Fetch admin fee from settings
- [ ] Fetch max loan % from settings
- [ ] Validate against min/max loan amounts

### Admin UI for Settings
- [ ] Create /admin/settings page
- [ ] Display all settings in table
- [ ] Edit interest rate form
- [ ] Edit admin fee form
- [ ] Edit loan limits form
- [ ] Validation (min/max values)
- [ ] Audit log of changes:
  - Who changed
  - When changed
  - Old value
  - New value
- [ ] Require admin role for access

**✅ Phase 4 Complete When:**
- Loan calculations use SystemSettings
- Admin can change rates without code deployment
- Changes logged for audit
- Settings cached for performance

---

## 📱 PHASE 5: WHATSAPP FLOWS (Weeks 5-8)

### Conversation Flow Design
- [ ] Document conversation tree (flowchart)
- [ ] Define all user messages and bot responses
- [ ] Define all steps in application flow
- [ ] Define error handling responses
- [ ] Define fallback to human agent

### Message Handler Implementation
- [ ] Create WhatsAppFlowHandler service
- [ ] Route messages based on session state
- [ ] Handle "GREETING" step
- [ ] Handle "COLLECT_HOURS" step
- [ ] Handle "COLLECT_RATE" step
- [ ] Handle "COLLECT_LOAN_AMOUNT" step
- [ ] Handle "COLLECT_REPAYMENT_DATE" step
- [ ] Handle "COLLECT_PURPOSE" step
- [ ] Handle "VERIFY_INFO" step
- [ ] Handle "CONFIRMATION" step

### Session State Management
- [ ] Track current step in WhatsAppSession
- [ ] Store step data in JSON field
- [ ] Handle user going back
- [ ] Handle user starting over
- [ ] Timeout inactive sessions (30 min)

### Integration with Loan Service
- [ ] Create draft application from WhatsApp
- [ ] Save data at each step
- [ ] Calculate loan eligibility
- [ ] Fetch affordability assessment
- [ ] Submit application when complete

### Balance Inquiry Flow
- [ ] User sends "Balance" or "Statement"
- [ ] Retrieve active loan
- [ ] Display:
  - Loan amount
  - Amount paid
  - Outstanding balance
  - Next payment date
  - Next payment amount

### Payment Reminder Flow
- [ ] Schedule reminder 3 days before due date
- [ ] Schedule reminder 1 day before due date
- [ ] Schedule reminder on due date
- [ ] Message includes:
  - Amount due
  - Due date
  - Payment methods

### Error Handling
- [ ] Handle invalid user input
- [ ] Handle unexpected messages
- [ ] Offer retry or restart
- [ ] Provide help command
- [ ] Fallback to agent handoff

### Agent Handoff
- [ ] User types "Agent" or "Help"
- [ ] Mark session for agent review
- [ ] Notify admin of pending conversation
- [ ] Admin can reply via portal

**✅ Phase 5 Complete When:**
- User can apply for loan entirely via WhatsApp
- User can check balance via WhatsApp
- User receives payment reminders
- Errors handled gracefully
- Agent handoff works

---

## 🛠️ PHASE 6: ADMIN ENHANCEMENTS (Weeks 6-8)

### Loan Processing Dashboard
- [ ] Create queue view of pending applications
- [ ] Sort by submission date
- [ ] Filter by status
- [ ] Filter by affordability (pass/fail)
- [ ] Quick approve/reject buttons
- [ ] Bulk actions

### Loan Detail Enhancements
- [ ] Display full application details
- [ ] Display affordability assessment
- [ ] Display income/expense breakdown
- [ ] Display contract PDF preview
- [ ] Display payment history
- [ ] Display WhatsApp conversation (if applicable)
- [ ] Admin notes section

### User Management
- [ ] List all users
- [ ] Search by name, email, phone, ID number
- [ ] View user profile
- [ ] View user's loan history
- [ ] Deactivate/reactivate user
- [ ] Reset user password

### Reporting
- [ ] Daily loan applications report
- [ ] Daily disbursements report
- [ ] Daily repayments report
- [ ] Outstanding loans report
- [ ] Arrears report
- [ ] Revenue report
- [ ] Export to CSV/Excel

**✅ Phase 6 Complete When:**
- Admin can process loans efficiently
- Admin can manage users
- Basic reports available

---

## 🧪 PHASE 7: TESTING (Weeks 7-9)

### Unit Tests
- [ ] AffordabilityService tests (80% coverage)
- [ ] LoanApplicationsController tests
- [ ] PaymentService tests
- [ ] WhatsAppFlowHandler tests
- [ ] ContractService tests
- [ ] SystemSettingsService tests

### Integration Tests
- [ ] Full loan application flow (Web)
- [ ] Full loan application flow (WhatsApp)
- [ ] Payment disbursement flow
- [ ] Payment collection flow
- [ ] Signature flow
- [ ] Complaint flow

### End-to-End Tests
- [ ] User registration → Loan application → Approval → Disbursement
- [ ] Loan application → Rejection (affordability fail)
- [ ] Loan application → Cancellation during cooling-off
- [ ] Loan repayment → Success
- [ ] Loan repayment → Failure → Retry

### Security Testing
- [ ] SQL injection tests
- [ ] XSS tests
- [ ] CSRF protection tests
- [ ] Authentication bypass tests
- [ ] Authorization tests (role-based)
- [ ] Rate limiting tests
- [ ] Sensitive data exposure tests

### Load Testing
- [ ] 100 concurrent users
- [ ] 1000 loan applications per day
- [ ] Database query performance
- [ ] API response times (<500ms)

### User Acceptance Testing
- [ ] 10 beta users complete loan application
- [ ] Collect feedback
- [ ] Fix critical issues
- [ ] Iterate

**✅ Phase 7 Complete When:**
- 80% code coverage
- All critical paths tested
- No critical bugs
- Load test passed
- Security audit passed
- Beta users satisfied

---

## 🚀 PHASE 8: DEPLOYMENT (Weeks 8-10)

### Production Environment Setup
- [ ] Provision production database (Azure SQL / PostgreSQL)
- [ ] Set up Redis cache
- [ ] Set up Azure Blob Storage / S3
- [ ] Set up Application Insights / monitoring
- [ ] Set up Key Vault for secrets
- [ ] Configure CDN for frontend
- [ ] Set up SSL certificates
- [ ] Configure custom domain

### Database Migration
- [ ] Run migrations on production DB
- [ ] Seed SystemSettings
- [ ] Create admin user
- [ ] Backup database

### Environment Configuration
- [ ] Set production environment variables
- [ ] Configure CORS for production domain
- [ ] Configure JWT settings (production secret)
- [ ] Configure WhatsApp webhook URL
- [ ] Configure payment provider (production keys)
- [ ] Configure email/SMS services
- [ ] Configure logging levels

### CI/CD Pipeline
- [ ] Set up GitHub Actions / Azure DevOps
- [ ] Run tests on every commit
- [ ] Build Docker images
- [ ] Deploy to staging on PR merge
- [ ] Manual approval for production deploy
- [ ] Automated deployment to production

### Monitoring & Alerting
- [ ] Set up Application Insights
- [ ] Track API response times
- [ ] Track error rates
- [ ] Set up alerts:
  - API error rate >1%
  - Response time >2 seconds
  - Failed payment rate >5%
  - Database connection failures
- [ ] Set up Sentry for error tracking
- [ ] Set up uptime monitoring (Pingdom/UptimeRobot)

### Disaster Recovery
- [ ] Daily database backups
- [ ] Test backup restoration
- [ ] Document recovery procedures
- [ ] Failover plan

**✅ Phase 8 Complete When:**
- Production environment fully configured
- CI/CD pipeline operational
- Monitoring and alerting active
- Disaster recovery tested

---

## 📋 PRE-LAUNCH CHECKLIST (Week 10)

### Legal & Compliance
- [ ] NCR registration approved
- [ ] NCRCP number active
- [ ] Contracts reviewed by lawyer
- [ ] POPIA compliance reviewed
- [ ] Terms & Conditions finalized
- [ ] Privacy Policy published
- [ ] Cookie Policy published

### Security
- [ ] Security audit passed
- [ ] Penetration test passed
- [ ] SSL certificate valid
- [ ] All secrets in KeyVault
- [ ] No hardcoded credentials
- [ ] CORS configured correctly
- [ ] Rate limiting enabled

### Operations
- [ ] Admin trained on system
- [ ] Customer support trained
- [ ] Operations manual written
- [ ] FAQ page created
- [ ] Contact information updated
- [ ] Business hours defined
- [ ] SLA defined

### Marketing
- [ ] Website live
- [ ] WhatsApp number registered
- [ ] Marketing materials ready
- [ ] Social media accounts set up
- [ ] Launch announcement ready

### Final Smoke Tests
- [ ] Register new user ✅
- [ ] Apply for loan ✅
- [ ] Admin approves loan ✅
- [ ] Payment disbursed ✅
- [ ] User receives funds ✅
- [ ] User signs contract ✅
- [ ] Repayment collected ✅
- [ ] Loan marked as paid ✅
- [ ] WhatsApp flow works ✅

---

## 🎉 LAUNCH DAY

- [ ] Deploy to production (early morning)
- [ ] Verify all services running
- [ ] Run smoke tests
- [ ] Monitor logs for errors
- [ ] Announce launch
- [ ] Monitor first applications closely
- [ ] Be ready to rollback if critical issues

---

## 📊 POST-LAUNCH (Week 11+)

### Week 1 Post-Launch
- [ ] Monitor all critical metrics
- [ ] Review first 100 applications
- [ ] Fix any critical bugs immediately
- [ ] Collect user feedback
- [ ] Daily stand-ups

### Week 2-4 Post-Launch
- [ ] Analyze conversion rates
- [ ] Optimize onboarding flow
- [ ] Address user complaints
- [ ] Implement quick wins
- [ ] Plan next iteration

### Month 2-3
- [ ] Credit bureau integration
- [ ] Advanced reporting
- [ ] Mobile app development
- [ ] Marketing optimization

---

**🎯 CRITICAL SUCCESS FACTORS:**

1. ✅ NCR Compliance - No shortcuts
2. ✅ Payment Integration - Must work flawlessly
3. ✅ Security - Protect customer data
4. ✅ User Experience - Simple and fast
5. ✅ Operations - Admin efficiency

**Good luck! 🚀**

---

**Print Date:** _____________  
**Completed:** _____ / 150 items  
**Progress:** _____%  
**Target Launch:** April 2026

