# HoHema Loans - Loan Process Specification

**Document Version:** 1.0  
**Date:** 8 January 2026  
**Status:** Draft

---

## Table of Contents
1. [Overview](#overview)
2. [Loan Calculation Model](#loan-calculation-model)
3. [Loan Application Process](#loan-application-process)
4. [Admin Configuration](#admin-configuration)
5. [Income & Expense Management](#income--expense-management)
6. [Loan Purpose Categories](#loan-purpose-categories)
7. [Business Rules](#business-rules)
8. [Data Model](#data-model)

---

## Overview

HoHema Loans provides short-term, single-payment loans to workers based on their hourly/shift earnings. The system calculates loan eligibility, applies configurable interest and fees, and manages repayment at month-end.

### Key Principles
- **Earnings-Based Lending**: Loan amount based on actual work hours and pay rate
- **Responsible Lending**: Maximum 20% of monthly earnings can be borrowed
- **Single Payment**: Full repayment (principal + interest + fees) due once at month-end
- **Configurable Pricing**: Admin-controlled interest rates and fees
- **Affordability Verification**: Income vs. expenses validation before approval

---

## Loan Calculation Model

### 1. Earnings Calculation

**Formula:**
```
Monthly Earnings = Hours/Shifts Worked × Rate per Hour/Shift
```

**Example:**
- Hours worked: 160 hours
- Rate: R50 per hour
- Monthly Earnings: 160 × R50 = R8,000

### 2. Maximum Loan Amount

**Formula:**
```
Max Loan Amount = Monthly Earnings × 20%
```

**Example:**
- Monthly Earnings: R8,000
- Max Loan Amount: R8,000 × 20% = R1,600

### 3. Interest Calculation

**Formula:**
```
Interest Amount = Loan Amount × Interest Rate
```

**Default Settings:**
- Interest Rate: 5% (configurable by admin)

**Example:**
- Loan Amount: R1,600
- Interest: R1,600 × 5% = R80

### 4. Admin Fee

**Formula:**
```
Total Fees = Fixed Admin Fee (configurable)
```

**Default Settings:**
- Admin Fee: R50 (configurable by admin)

### 5. Total Repayment Amount

**Formula:**
```
Total Repayment = Loan Amount + Interest + Admin Fee
```

**Example:**
- Loan Amount: R1,600
- Interest: R80
- Admin Fee: R50
- **Total Repayment: R1,730**

### 6. Repayment Date

**Rules:**
- Must be between the 25th and 31st of the month
- User selects their preferred pay date
- Single lump-sum payment due on selected date

---

## Loan Application Process

### Step 1: Work & Earnings Information

**User Inputs:**
- Hours/Shifts worked this month
- Rate of pay (per hour or per shift)

**System Calculates:**
- Total monthly earnings
- Maximum loan amount (20% of earnings)

**Validation:**
```
✓ Hours/Shifts > 0
✓ Rate > 0
✓ Max Loan Amount > Minimum Threshold (e.g., R100)
```

### Step 2: Loan Amount Selection

**User Inputs:**
- Desired loan amount

**System Validation:**
```
✓ Loan Amount ≤ Max Loan Amount
✓ Loan Amount ≥ Minimum Loan (e.g., R100)
```

**System Displays:**
- Loan amount
- Interest amount
- Admin fee
- **Total repayment amount**
- Effective interest rate

### Step 3: Repayment Date Selection

**User Inputs:**
- Preferred repayment date (25th-31st of current month)

**Validation:**
```
✓ Date between 25th and 31st
✓ Date is in the current month
✓ Date has not passed
```

### Step 4: Loan Purpose

**User Selects from:**
- 🚌 Transport
- 🛒 Groceries
- 💳 Day-to-day expenses
- 📱 Airtime & data
- ⚡ Utilities (electricity, water)
- 🏥 Medical expenses
- 👨‍👩‍👧 Family support
- 📚 Education
- 🏠 Rent assistance
- 🔧 Emergency repairs
- 💰 Debt consolidation
- 🎯 Other (specify)

**Business Rule:**
- Purpose is required for reporting and compliance
- Multiple purposes can be selected (optional enhancement)

### Step 5: Income & Expense Verification

**Prompt:**
```
"Has your income or expenses changed since registration?"
```

**Option A: No Changes**
- Button: "No, everything is the same"
- System uses existing income/expense data
- Proceeds to affordability check

**Option B: Changes**
- Button: "Yes, I need to update my information"
- User redirected to Income & Expense update flow
- System recalculates affordability
- Returns to loan application

**Affordability Check:**
```
Available Funds = Total Monthly Income - Total Monthly Expenses
Required for Loan = Total Repayment Amount

✓ PASS: Available Funds ≥ Total Repayment Amount
✗ FAIL: Available Funds < Total Repayment Amount
```

### Step 6: Review & Confirmation

**System Displays:**
- Loan summary
- Earnings breakdown
- Total repayment breakdown
- Repayment date
- Affordability status
- Terms & conditions

**User Actions:**
- Review all details
- Accept terms & conditions
- Submit application

### Step 7: Application Submitted

**System Actions:**
- Create loan application record
- Set status to "Pending Review"
- Notify admin for review
- Send confirmation to user

---

## Admin Configuration

### System Settings (Admin Panel)

#### 1. Interest Rate Configuration
```
Field: Interest Rate
Type: Percentage (%)
Default: 5%
Range: 0% - 50%
Description: Interest charged on loan amount
```

#### 2. Admin Fee Configuration
```
Field: Admin Fee
Type: Currency (R)
Default: R50
Range: R0 - R500
Description: Fixed administrative fee per loan
```

#### 3. Maximum Loan Percentage
```
Field: Max Loan Percentage
Type: Percentage (%)
Default: 20%
Range: 10% - 50%
Description: Maximum percentage of earnings that can be borrowed
```

#### 4. Minimum Loan Amount
```
Field: Minimum Loan
Type: Currency (R)
Default: R100
Range: R50 - R500
Description: Minimum loan amount allowed
```

#### 5. Maximum Loan Amount
```
Field: Maximum Loan (Optional Cap)
Type: Currency (R)
Default: R5,000
Range: R1,000 - R10,000
Description: Absolute maximum loan amount regardless of earnings
```

### Admin Settings Table Structure
```sql
SystemSettings {
    Id: guid
    SettingKey: string (e.g., "InterestRate", "AdminFee")
    SettingValue: decimal
    SettingType: string (e.g., "Percentage", "Currency")
    Description: string
    UpdatedAt: datetime
    UpdatedBy: string (admin user ID)
}
```

---

## Income & Expense Management

### Income Categories (Revised)
1. **Employment Income**
   - Salary/Wages
   - Overtime pay
   - Bonuses
   - Commission

2. **Self-Employment**
   - Business income
   - Freelance work
   - Contract work

3. **Government Grants**
   - SASSA grant
   - Child support grant
   - Old age pension
   - Disability grant

4. **Other Income**
   - Rental income
   - Investment income
   - Alimony/Maintenance
   - Other (specify)

### Expense Categories (Revised)
1. **Housing**
   - Rent/Bond
   - Rates & taxes
   - Home insurance

2. **Utilities**
   - Electricity
   - Water
   - Gas
   - Internet/WiFi

3. **Transport**
   - Taxi fare
   - Petrol
   - Car payment
   - Car insurance
   - Maintenance

4. **Food & Household**
   - Groceries
   - Household supplies
   - Cleaning products

5. **Debt Obligations**
   - Other loans
   - Credit cards
   - Store accounts
   - Laybye

6. **Communication**
   - Cellphone
   - Airtime
   - Data

7. **Insurance**
   - Life insurance
   - Funeral cover
   - Medical aid

8. **Dependents**
   - Childcare
   - School fees
   - Child support payments

9. **Medical**
   - Medication
   - Doctor visits
   - Medical expenses

10. **Personal**
    - Clothing
    - Personal care
    - Entertainment

11. **Other**
    - Emergency fund contribution
    - Savings
    - Other (specify)

### Income & Expense Update Flow

**Trigger: User clicks "Yes, I need to update my information"**

1. **Display Current Data**
   - Show all current income sources with amounts
   - Show all current expenses with amounts
   - Show last updated date

2. **Edit Income**
   - Add new income sources
   - Edit existing amounts
   - Remove income sources
   - Mark as "Verified" or "Unverified"

3. **Edit Expenses**
   - Add new expense categories
   - Edit existing amounts
   - Remove expenses
   - Mark essential vs. non-essential

4. **Recalculate Affordability**
   ```
   Total Income = Sum of all income sources
   Total Expenses = Sum of all expenses
   Available Funds = Total Income - Total Expenses
   ```

5. **Save & Return**
   - Update timestamp
   - Save changes
   - Return to loan application (Step 5)

---

## Business Rules

### Loan Eligibility
1. User must have worked hours/shifts in the current month
2. User must have valid income and expense data
3. Loan amount must be ≤ 20% of monthly earnings
4. User must pass affordability check
5. User cannot have an active outstanding loan
6. User account must be verified

### Loan Limits
- **Minimum Loan:** R100 (configurable)
- **Maximum Loan Percentage:** 20% of earnings (configurable)
- **Absolute Maximum:** R5,000 (configurable, optional)
- **Interest Rate:** 5% (configurable)
- **Admin Fee:** R50 (configurable)

### Repayment Rules
1. Single payment due on selected date (25th-31st)
2. Payment includes: Principal + Interest + Admin Fee
3. No early repayment penalty
4. Late payment incurs penalty fees (configurable)
5. Failed payment triggers collections process

### Affordability Rules
1. Available funds must cover total repayment amount
2. Debt-to-income ratio checked (NCR compliance)
3. System flags high-risk applications for manual review
4. User must update income/expense if older than 30 days (optional rule)

---

## Data Model

### LoanApplication Table (Updated)
```sql
LoanApplication {
    Id: guid (PK)
    UserId: guid (FK -> User)
    ApplicationDate: datetime
    
    -- Earnings Information
    HoursWorked: decimal
    HourlyRate: decimal
    MonthlyEarnings: decimal (calculated)
    MaxLoanAmount: decimal (calculated)
    
    -- Loan Details
    LoanAmount: decimal
    InterestRate: decimal (from settings)
    InterestAmount: decimal (calculated)
    AdminFee: decimal (from settings)
    TotalRepayment: decimal (calculated)
    
    -- Repayment
    RepaymentDate: date (25th-31st)
    
    -- Purpose
    LoanPurpose: string (enum/lookup)
    PurposeDetails: string (optional)
    
    -- Affordability
    TotalIncome: decimal
    TotalExpenses: decimal
    AvailableFunds: decimal
    AffordabilityStatus: string (enum: Pass/Fail/Review)
    
    -- Status
    Status: string (enum: Draft, Pending, Approved, Rejected, Disbursed, Repaid, Defaulted)
    
    -- Audit
    CreatedAt: datetime
    UpdatedAt: datetime
    ReviewedBy: guid (FK -> User, nullable)
    ReviewedAt: datetime (nullable)
    ReviewNotes: string (nullable)
}
```

### SystemSettings Table
```sql
SystemSettings {
    Id: guid (PK)
    SettingKey: string (unique)
    SettingValue: string
    SettingType: string (Percentage, Currency, Number, Boolean)
    DisplayName: string
    Description: string
    MinValue: decimal (nullable)
    MaxValue: decimal (nullable)
    IsActive: boolean
    UpdatedAt: datetime
    UpdatedBy: guid (FK -> User)
}
```

### IncomeSource Table (Updated)
```sql
IncomeSource {
    Id: guid (PK)
    UserId: guid (FK -> User)
    Category: string (enum: Employment, SelfEmployment, Government, Other)
    SubCategory: string
    Description: string
    MonthlyAmount: decimal
    Frequency: string (enum: Monthly, Weekly, Once-off)
    IsVerified: boolean
    VerifiedAt: datetime (nullable)
    CreatedAt: datetime
    UpdatedAt: datetime
}
```

### ExpenseItem Table (Updated)
```sql
ExpenseItem {
    Id: guid (PK)
    UserId: guid (FK -> User)
    Category: string (enum: Housing, Utilities, Transport, Food, Debt, etc.)
    SubCategory: string
    Description: string
    MonthlyAmount: decimal
    Frequency: string (enum: Monthly, Weekly, Once-off)
    IsEssential: boolean
    IsFixed: boolean (recurring or variable)
    CreatedAt: datetime
    UpdatedAt: datetime
}
```

---

## Implementation Checklist

### Phase 1: Admin Configuration
- [ ] Create SystemSettings table
- [ ] Build admin settings management UI
- [ ] Add settings for: Interest Rate, Admin Fee, Max Loan %, Min/Max Loan amounts
- [ ] Create settings service/API endpoints
- [ ] Add validation for setting ranges

### Phase 2: Earnings & Loan Calculation
- [ ] Update LoanApplication model with earnings fields
- [ ] Create earnings calculation service
- [ ] Implement 20% max loan calculation
- [ ] Update loan calculator with configurable rates/fees
- [ ] Add validation for loan amount limits

### Phase 3: Loan Purpose
- [ ] Define loan purpose enum/categories
- [ ] Create purpose selection UI component
- [ ] Update loan application flow to include purpose
- [ ] Add purpose to database model

### Phase 4: Income & Expense Management
- [ ] Revise income categories
- [ ] Revise expense categories
- [ ] Create "Has anything changed?" prompt
- [ ] Build income/expense edit flow
- [ ] Implement save and recalculation
- [ ] Add last updated timestamp display

### Phase 5: Repayment Date
- [ ] Add repayment date selection UI (25th-31st)
- [ ] Validate date range
- [ ] Update loan terms calculation
- [ ] Display repayment date in summaries

### Phase 6: Affordability Integration
- [ ] Calculate available funds after expenses
- [ ] Compare against total repayment amount
- [ ] Flag applications that fail affordability
- [ ] Add manual review workflow for borderline cases

### Phase 7: Testing & Validation
- [ ] Unit tests for calculations
- [ ] Integration tests for full loan flow
- [ ] UAT with sample data
- [ ] Edge case testing

---

## Questions & Considerations

### 1. Multiple Loans
- Can a user have multiple loans at once, or only one active loan?
- **Recommendation:** One active loan at a time to simplify repayment tracking

### 2. Partial Hours
- How to handle partial hours/shifts (e.g., 4.5 hours)?
- **Recommendation:** Allow decimal values for hours

### 3. Variable Pay Rates
- Do workers have different rates for different shifts (day/night)?
- **Recommendation:** Start with single rate, can enhance later

### 4. Income Update Frequency
- Should we force income/expense updates if data is older than X days?
- **Recommendation:** Show warning if data >30 days old, encourage update

### 5. Failed Affordability
- What happens if user fails affordability check?
- **Recommendation:** Show reason, suggest reducing loan amount or updating expenses

### 6. Repayment Methods
- How will repayment be collected? (Direct debit, manual payment, etc.)
- **Recommendation:** Define in separate payment processing spec

### 7. Late Payments
- What penalties apply for missed repayment dates?
- **Recommendation:** Define penalty structure in separate collections spec

---

## Next Steps

1. **Review & Approve Specification**
   - Stakeholder review
   - Finalize business rules
   - Confirm calculation formulas

2. **Technical Design**
   - Database schema design
   - API endpoint specifications
   - Frontend component design

3. **Implementation**
   - Follow phased checklist above
   - Iterative development and testing
   - User acceptance testing

4. **Launch Preparation**
   - Admin training
   - User documentation
   - Compliance review (NCR, NCA)

---

**Document Control:**
- **Author:** Development Team
- **Reviewers:** Business Stakeholders
- **Next Review Date:** TBD
- **Change History:** Version 1.0 - Initial draft
