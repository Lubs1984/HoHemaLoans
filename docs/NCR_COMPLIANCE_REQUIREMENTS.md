# NCR Compliance Requirements for Ho Hema Loans
## National Credit Regulator - South Africa

**Document Version:** 1.0  
**Last Updated:** January 27, 2026  
**Compliance Framework:** National Credit Act 34 of 2005 (NCA)

---

## Executive Summary

This document outlines all National Credit Regulator (NCR) compliance requirements, mandatory forms, reports, and regulatory obligations for Ho Hema Loans to operate as a registered credit provider in South Africa.

---

## 1. NCR Registration & Licensing Requirements

### 1.1 Initial Registration
- [ ] **NCRCP Registration Number** - Obtain and display prominently
- [ ] **NCR Certificate** - Maintain valid registration certificate
- [ ] **Compliance Officer** - Appoint registered compliance officer
- [ ] **Registration Renewal** - Annual renewal process
- [ ] **Registration Fee Payment** - Annual fees paid on time

### 1.2 Display Requirements
- [ ] NCR registration number on all:
  - Website (footer/header)
  - Marketing materials
  - Loan agreements
  - Customer communications
  - WhatsApp automated messages
  - Physical office locations

---

## 2. Mandatory Forms (NCR)

### 2.1 Form 39 - Credit Agreement
**Purpose:** Primary credit agreement document  
**When Required:** Every credit agreement (loans only, not advances)  
**Retention Period:** 5 years after final payment

**Required Elements:**
- [ ] Credit provider's full name and NCR registration number
- [ ] Consumer's full name, ID number, and contact details
- [ ] Principal debt amount
- [ ] Interest rate (annual and monthly)
- [ ] All fees (initiation, monthly service, etc.)
- [ ] Total amount repayable
- [ ] Repayment schedule with dates and amounts
- [ ] Consumer's rights (cooling-off period, early settlement)
- [ ] Complaint procedures
- [ ] NCR contact information
- [ ] Reckless credit disclaimer
- [ ] Pre-agreement statement requirements

**System Implementation:**
```
- Generate Form 39 automatically upon loan approval
- Include all mandatory fields from loan application data
- Store signed copy in encrypted document storage
- Link to LoanApplications.ApplicationId
- Track version numbers for amendments
```

### 2.2 Form 1 - Application for Registration
**Purpose:** Credit provider registration  
**When Required:** Initial registration and renewals  
**Frequency:** Annual

**Required Elements:**
- [ ] Company registration documents
- [ ] Directors' details and declarations
- [ ] Compliance officer appointment
- [ ] Business address and contact details
- [ ] Proof of financial soundness
- [ ] Business plan

### 2.3 Pre-Agreement Statement (PAS)
**Purpose:** Quote provided before agreement  
**When Required:** Before signing credit agreement  
**Retention Period:** 5 years

**Required Elements:**
- [ ] All costs breakdown
- [ ] Interest rate (APR)
- [ ] Total amount repayable
- [ ] Consumer rights notice
- [ ] Cooling-off period notice (5 business days)

### 2.4 Affordability Assessment Form (Internal)
**Purpose:** Document affordability assessment process  
**When Required:** Every credit application  
**Retention Period:** 5 years

**Required Elements:**
- [ ] Gross monthly income
- [ ] Monthly expenses breakdown
- [ ] Existing debt obligations
- [ ] Discretionary income calculation
- [ ] Assessment outcome and reasoning
- [ ] Assessor name and timestamp

---

## 3. Mandatory Reporting to NCR

### 3.1 Monthly Returns
**Report:** Monthly Credit Information Return  
**Due Date:** 15th of each month for previous month  
**Format:** XML or CSV upload to NCR portal

**Required Data:**
- [ ] Total number of credit agreements entered
- [ ] Total value of credit granted
- [ ] Number of applications received
- [ ] Number of applications approved
- [ ] Number of applications declined
- [ ] Outstanding credit balance
- [ ] Number of accounts in arrears
- [ ] Number of accounts in default

### 3.2 Quarterly Returns
**Report:** Quarterly Compliance Report  
**Due Date:** 15 days after quarter end

**Required Data:**
- [ ] Compliance certificate
- [ ] Reckless lending assessments conducted
- [ ] Customer complaints received and resolved
- [ ] Adverse action reports
- [ ] Changes to business operations
- [ ] Debt counselling referrals

### 3.3 Annual Returns
**Report:** Annual Compliance Report  
**Due Date:** Within 90 days of financial year-end

**Required Data:**
- [ ] Audited financial statements
- [ ] Compliance officer's annual report
- [ ] Total portfolio statistics
- [ ] Risk management report
- [ ] Training and development activities
- [ ] Complaints analysis
- [ ] Systems and controls evaluation

### 3.4 Credit Bureau Reporting
**Frequency:** Monthly  
**Bureaus:** TransUnion, Experian, Compuscan, XDS

**Required Data:**
- [ ] New credit agreements
- [ ] Payment behavior (current/arrears)
- [ ] Settled accounts
- [ ] Defaults and judgments
- [ ] Account closures

---

## 4. Compliance Thresholds & Limits

### 4.1 Interest Rate Caps
**As per NCA Section 103**

| Loan Amount | Maximum Annual Interest |
|-------------|------------------------|
| R0 - R10,000 | 28% per annum |
| R10,001 - R25,000 | 24% per annum |
| R25,001+ | 22% per annum |

**System Implementation:**
```sql
-- Validation rules in LoanApplications table
- InterestRate <= MaxAllowedRate based on ApprovedAmount
- Alert if approaching cap limits
- Automatic rate adjustment based on loan amount
```

### 4.2 Fee Caps
**Initiation Fee (NCA Section 101):**
- Maximum: R1,140 or 15% of principal (whichever is greater)
- Cap: R1,140 for loans under R10,000

**Monthly Service Fee:**
- Maximum: R60 per month

**Collection Costs:**
- As per Magistrates Court Act

**System Implementation:**
```sql
-- Validation in fee calculation
- InitiationFee <= MINIMUM(1140, ApprovedAmount * 0.15)
- MonthlyServiceFee <= 60
- Total cost of credit calculation
```

### 4.3 Affordability Assessment Requirements
**NCA Section 81**

**Mandatory Checks:**
- [ ] Verify consumer's income (pay slip, bank statements)
- [ ] Assess financial commitments (credit bureau report)
- [ ] Calculate debt-to-income ratio (max 35-40%)
- [ ] Evaluate living expenses
- [ ] Determine discretionary income
- [ ] Document assessment process

**System Implementation:**
```
- AffordabilityAssessment table to store all calculations
- Link to IncomeExpense records
- Store credit bureau report reference
- Flag if debt-to-income > 35%
- Require manual override approval if > 40%
```

---

## 5. Consumer Rights & Protections

### 5.1 Cooling-Off Period
**Requirement:** 5 business days from agreement signature  
**Consumer Right:** Cancel agreement without penalty  
**Implementation:**
- [ ] Clearly state in Form 39
- [ ] System flag: AgreementDate + 5 business days
- [ ] Block disbursement during cooling-off period
- [ ] Process cancellation requests automatically

### 5.2 Pre-Payment Rights
**Requirement:** Consumer may settle early  
**Calculation:**
- Outstanding principal
- Pro-rated interest
- Proportional fees
- Minus rebate for early settlement

**System Implementation:**
```
- Calculate early settlement amount
- Display in customer portal
- Generate settlement quote on demand
- Track settlement requests
```

### 5.3 Statement of Account
**Frequency:** Monthly (if requested)  
**Free of Charge:** Yes

**Required Elements:**
- [ ] Opening balance
- [ ] Payments received
- [ ] Interest charged
- [ ] Fees charged
- [ ] Closing balance
- [ ] Payment due date

---

## 6. Reckless Lending Prevention

### 6.1 Reckless Credit Assessment
**NCA Section 80-82**

**System must prevent:**
- [ ] Lending without affordability assessment
- [ ] Lending when consumer cannot afford repayment
- [ ] Failing to conduct proper credit checks
- [ ] Ignoring existing debt obligations

**Documentation Required:**
- [ ] Credit application form
- [ ] Proof of income
- [ ] Bank statements (3 months)
- [ ] Credit bureau report
- [ ] Affordability calculation worksheet
- [ ] Approval/decline reasoning

### 6.2 Reckless Credit Consequences
**Penalties:**
- Suspension/cancellation of interest
- Suspension of consumer obligations
- NCR administrative penalties
- Court orders

**System Protection:**
```
- Multi-level approval workflow
- Automated affordability checks with hard limits
- Credit bureau integration (mandatory)
- Decline if affordability fails
- Audit trail of all decisions
```

---

## 7. Debt Collection Compliance

### 7.1 Debt Collection Practices
**NCA Section 90-91**

**Prohibited Actions:**
- [ ] Harassment or intimidation
- [ ] Contact outside 7am-9pm
- [ ] Contact at workplace if prohibited
- [ ] False or misleading representations
- [ ] Disclosure to third parties

**Required Actions:**
- [ ] Send notice of default (10 business days to remedy)
- [ ] Provide debt counselling information
- [ ] Cease collection if under debt review
- [ ] Issue Section 129 notice before legal action

### 7.2 Collection Communication
**System Implementation:**
- [ ] WhatsApp payment reminders (opt-in required)
- [ ] SMS notifications (compliant timing)
- [ ] Email statements
- [ ] Log all communication attempts
- [ ] Respect communication preferences

---

## 8. Data Protection & POPIA Compliance

### 8.1 POPIA Requirements
**Protection of Personal Information Act**

**Implementation:**
- [ ] Consent management system
- [ ] Data minimization (collect only necessary data)
- [ ] Purpose specification
- [ ] Secure storage (encryption)
- [ ] Access controls
- [ ] Data retention policy (5 years post-settlement)
- [ ] Right to access/correction/deletion

### 8.2 Credit Bureau Consent
**Required Consent:**
- [ ] Credit check authorization
- [ ] Credit bureau reporting consent
- [ ] Data sharing with credit providers
- [ ] Retention period notification

---

## 9. Complaint Management System

### 9.1 Internal Complaint Process
**NCA Section 136**

**Requirements:**
- [ ] Complaint registration system
- [ ] Unique complaint reference number
- [ ] Acknowledgment within 2 business days
- [ ] Resolution within 21 business days
- [ ] Escalation procedure
- [ ] Complaint log and tracking

**System Implementation:**
```
- Complaints table in database
- WhatsApp complaint submission
- Web portal complaint form
- Email/phone complaint logging
- Automated acknowledgment
- Status tracking and notifications
- Resolution documentation
```

### 9.2 External Complaint Channels
**Consumer Must Be Informed Of:**
- [ ] NCR contact details (0860 627 627 / complaints@ncr.org.za)
- [ ] Ombud services
- [ ] Provincial consumer affairs offices

---

## 10. Document Management & Retention

### 10.1 Document Retention Requirements
**NCA Regulation 23**

| Document Type | Retention Period |
|---------------|------------------|
| Credit agreements (Form 39) | 5 years after settlement |
| Pre-agreement statements | 5 years after settlement |
| Affordability assessments | 5 years after settlement |
| Consumer correspondence | 5 years after settlement |
| Payment records | 5 years after settlement |
| Credit bureau reports | 5 years after settlement |
| Complaints records | 5 years after resolution |
| Audit records | 5 years |

**System Implementation:**
```
- Document management system
- Encrypted storage (Azure Blob Storage)
- Automatic retention enforcement
- Secure deletion after retention period
- Access audit logs
- Document versioning
```

### 10.2 Security Requirements
- [ ] Encryption at rest (AES-256)
- [ ] Encryption in transit (TLS 1.2+)
- [ ] Role-based access control
- [ ] Multi-factor authentication
- [ ] Audit logging
- [ ] Backup and disaster recovery

---

## 11. System Implementation Checklist

### 11.1 Database Schema Extensions

```sql
-- NCR Compliance tracking
CREATE TABLE ncr.ComplianceReports (
    ReportId uniqueidentifier PRIMARY KEY,
    ReportType nvarchar(50) NOT NULL, -- Monthly/Quarterly/Annual
    ReportingPeriod date NOT NULL,
    GeneratedDate datetime2 NOT NULL,
    SubmittedDate datetime2 NULL,
    SubmittedBy uniqueidentifier NULL,
    NCRConfirmationNumber nvarchar(50) NULL,
    Status nvarchar(20) NOT NULL, -- Draft/Submitted/Accepted
    ReportData nvarchar(max) NOT NULL, -- JSON
    CONSTRAINT CK_ReportType CHECK (ReportType IN ('Monthly', 'Quarterly', 'Annual'))
);

-- Form 39 generation tracking
CREATE TABLE ncr.Form39Documents (
    Form39Id uniqueidentifier PRIMARY KEY,
    ApplicationId uniqueidentifier NOT NULL,
    GeneratedDate datetime2 NOT NULL,
    VersionNumber int NOT NULL,
    DocumentUrl nvarchar(500) NOT NULL,
    SignedDate datetime2 NULL,
    ConsumerSignature nvarchar(max) NULL, -- Base64 signature
    CoolingOffEndDate date NOT NULL,
    Status nvarchar(20) NOT NULL, -- Generated/Signed/Cancelled
    CONSTRAINT FK_Form39_Application FOREIGN KEY (ApplicationId) 
        REFERENCES dbo.LoanApplications(ApplicationId)
);

-- Affordability assessment documentation
CREATE TABLE ncr.AffordabilityAssessments (
    AssessmentId uniqueidentifier PRIMARY KEY,
    ApplicationId uniqueidentifier NOT NULL,
    AssessmentDate datetime2 NOT NULL,
    GrossMonthlyIncome decimal(18,2) NOT NULL,
    MonthlyExpenses decimal(18,2) NOT NULL,
    ExistingDebtObligations decimal(18,2) NOT NULL,
    DiscretionaryIncome decimal(18,2) NOT NULL,
    DebtToIncomeRatio decimal(5,2) NOT NULL,
    CreditBureauReportRef nvarchar(100) NOT NULL,
    AssessmentOutcome nvarchar(20) NOT NULL, -- Approved/Declined
    AssessmentNotes nvarchar(max) NULL,
    AssessorId uniqueidentifier NOT NULL,
    CONSTRAINT FK_Affordability_Application FOREIGN KEY (ApplicationId)
        REFERENCES dbo.LoanApplications(ApplicationId)
);

-- Complaint management
CREATE TABLE ncr.CustomerComplaints (
    ComplaintId uniqueidentifier PRIMARY KEY,
    ComplaintReference nvarchar(20) NOT NULL UNIQUE, -- e.g., COMP-2026-001
    CustomerId uniqueidentifier NOT NULL,
    ApplicationId uniqueidentifier NULL,
    ComplaintDate datetime2 NOT NULL,
    ComplaintChannel nvarchar(20) NOT NULL, -- WhatsApp/Email/Phone/Web
    ComplaintCategory nvarchar(50) NOT NULL,
    ComplaintDescription nvarchar(max) NOT NULL,
    AcknowledgedDate datetime2 NULL,
    ResolvedDate datetime2 NULL,
    ResolutionDescription nvarchar(max) NULL,
    Status nvarchar(20) NOT NULL, -- Open/Acknowledged/Resolved/Escalated
    AssignedTo uniqueidentifier NULL,
    EscalatedToNCR bit NOT NULL DEFAULT 0,
    CONSTRAINT FK_Complaints_Customer FOREIGN KEY (CustomerId)
        REFERENCES dbo.AspNetUsers(Id)
);

-- Credit bureau reporting log
CREATE TABLE ncr.CreditBureauReporting (
    ReportingId uniqueidentifier PRIMARY KEY,
    ApplicationId uniqueidentifier NOT NULL,
    BureauName nvarchar(50) NOT NULL,
    ReportingDate datetime2 NOT NULL,
    ReportType nvarchar(50) NOT NULL, -- NewAccount/PaymentUpdate/Settlement/Default
    ReportData nvarchar(max) NOT NULL, -- JSON
    SubmissionStatus nvarchar(20) NOT NULL,
    BureauConfirmation nvarchar(100) NULL,
    CONSTRAINT FK_Bureau_Application FOREIGN KEY (ApplicationId)
        REFERENCES dbo.LoanApplications(ApplicationId)
);
```

### 11.2 API Endpoints Required

```csharp
// NCR Reporting
GET  /api/admin/reports/ncr/monthly/{year}/{month}
GET  /api/admin/reports/ncr/quarterly/{year}/{quarter}
GET  /api/admin/reports/ncr/annual/{year}
POST /api/admin/reports/ncr/submit

// Form 39 Management
POST /api/loans/{applicationId}/form39/generate
GET  /api/loans/{applicationId}/form39
POST /api/loans/{applicationId}/form39/sign
GET  /api/loans/{applicationId}/form39/download

// Affordability Assessment
POST /api/loans/{applicationId}/affordability/assess
GET  /api/loans/{applicationId}/affordability

// Complaints
POST /api/complaints/submit
GET  /api/complaints/{complaintId}
GET  /api/complaints/my-complaints
PATCH /api/admin/complaints/{complaintId}/resolve

// Pre-Agreement Statement
GET /api/loans/{applicationId}/pre-agreement-statement
```

### 11.3 WhatsApp Flow Updates

**Add to Review Screen:**
```json
{
    "type": "TextCaption",
    "text": "⚖️ NCR Registered: NCRCP12345"
},
{
    "type": "TextCaption",
    "text": "📋 Cooling-off period: 5 business days"
},
{
    "type": "TextCaption",
    "text": "📞 Complaints: 0860 627 627"
}
```

### 11.4 Automated Compliance Jobs

```csharp
// Schedule daily/monthly jobs
- GenerateMonthlyNCRReport (1st of month)
- SubmitCreditBureauUpdates (daily)
- CheckCoolingOffPeriods (daily)
- SendPaymentReminders (daily, 7am-9pm only)
- GenerateQuarterlyReport (quarterly)
- CheckDocumentRetention (weekly)
- ArchiveExpiredDocuments (monthly)
```

---

## 12. Audit & Monitoring

### 12.1 Compliance Monitoring Dashboard

**Key Metrics:**
- [ ] % of loans with affordability assessment
- [ ] % within interest rate caps
- [ ] % within fee caps
- [ ] Average approval time
- [ ] Complaints open vs resolved
- [ ] Document retention compliance
- [ ] NCR report submission status

### 12.2 Alerts & Notifications

**Trigger Alerts For:**
- [ ] Interest rate exceeding cap
- [ ] Missing affordability assessment
- [ ] Overdue NCR report submission
- [ ] Cooling-off period violations
- [ ] Complaint response overdue (21 days)
- [ ] Missing credit bureau reporting
- [ ] Document retention policy breach

---

## 13. Training Requirements

### 13.1 Staff Training
**Required Training:**
- [ ] NCA overview and requirements
- [ ] Affordability assessment procedures
- [ ] Reckless lending prevention
- [ ] Complaint handling
- [ ] Consumer rights
- [ ] POPIA compliance
- [ ] Debt collection practices

**Frequency:** Annual refresher + onboarding

### 13.2 Documentation
- [ ] Training register
- [ ] Training materials
- [ ] Assessment records
- [ ] Competency certificates

---

## 14. External Integrations Required

### 14.1 Credit Bureaus
**Providers:**
- [ ] TransUnion
- [ ] Experian
- [ ] Compuscan
- [ ] XDS

**Services:**
- Credit checks
- Payment reporting
- Dispute resolution

### 14.2 NCR Portal
- [ ] NCR Connect portal access
- [ ] API integration (if available)
- [ ] Bulk upload templates
- [ ] Submission confirmation

### 14.3 Debt Counselling Networks
- [ ] Register with debt counsellors
- [ ] Implement Section 86(4)(b) notices
- [ ] Cease collection when under review

---

## 15. Penalties & Consequences

### 15.1 NCR Administrative Penalties
**Non-Compliance Can Result In:**
- Fines up to R1 million or 10% of annual turnover
- Suspension of registration
- Cancellation of registration
- Criminal prosecution

### 15.2 Consumer Remedies
**Consumers May Apply For:**
- Suspension of interest
- Suspension of obligations
- Debt relief
- Court orders

---

## 16. Implementation Priority

### Phase 1 - Critical (Immediate)
1. ✅ Form 39 generation system
2. ✅ Affordability assessment documentation
3. ✅ Interest rate and fee cap validation
4. ✅ NCR registration display
5. ✅ Cooling-off period tracking

### Phase 2 - High Priority (Month 1)
6. ⏳ Monthly NCR reporting system
7. ⏳ Credit bureau integration
8. ⏳ Complaint management system
9. ⏳ Document retention system
10. ⏳ Pre-agreement statement generation

### Phase 3 - Medium Priority (Month 2-3)
11. ⏳ Quarterly and annual reporting
12. ⏳ Compliance monitoring dashboard
13. ⏳ Training system
14. ⏳ Audit trail enhancements
15. ⏳ Automated compliance alerts

---

## 17. Resources & Contacts

### NCR Contact Details
- **Website:** www.ncr.org.za
- **Email:** complaints@ncr.org.za
- **Phone:** 011 554 2600 / 0860 627 627
- **Address:** 127-15th Road, Randjiespark, Midrand, 1685

### Relevant Legislation
- National Credit Act 34 of 2005
- National Credit Regulations
- POPIA Act 4 of 2013
- Consumer Protection Act 68 of 2008

### Industry Bodies
- Banking Association South Africa (BASA)
- National Credit Regulator (NCR)
- Credit Ombud
- Provincial consumer affairs offices

---

## Document Control

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-27 | System Architect | Initial comprehensive requirements document |

---

**Next Steps:**
1. Review with legal compliance officer
2. Prioritize implementation based on phases
3. Design database schema extensions
4. Develop API endpoints
5. Create automated reporting jobs
6. Build compliance dashboard
7. Train staff on new systems
8. Submit for NCR approval (if required)
