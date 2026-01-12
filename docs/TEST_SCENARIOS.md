# Test Data & Scenarios

This document describes the 4 test user scenarios created to validate the affordability assessment and loan application workflow.

## How to Use Test Data

### 1. Seed Test Data via API

**Endpoint:** `POST /api/testdata/seed`

**Note:** Only works in Development environment

```bash
curl -X POST http://localhost:5000/api/testdata/seed
```

**Response:**
```json
{
  "message": "Test data seeded successfully",
  "users": [
    {
      "email": "test.affluent@hohema.com",
      "password": "Test@123",
      "scenario": "High Earner - Excellent Affordability",
      "expectedStatus": "Affordable"
    },
    ...
  ]
}
```

### 2. View Test Scenarios

**Endpoint:** `GET /api/testdata/scenarios`

Returns detailed information about all test scenarios without seeding data.

---

## Test Scenarios

### Scenario 1: Alice Affluent - High Earner (Affordable)

**Email:** `test.affluent@hohema.com`  
**Password:** `Test@123`

**Profile:**
- **Monthly Income:** R 8,500
- **Monthly Expenses:** R 4,300
- **Existing Debt:** R 0
- **Disposable Income:** R 4,200 (49%)

**Income Sources:**
- Employment: R 8,500 (Senior Developer Salary)

**Expenses:**
- Housing: R 1,200 (Rent) - Essential
- Utilities: R 500 (Water, Electricity, Internet) - Essential
- Food: R 800 (Groceries) - Essential
- Transport: R 600 (Car payment & fuel) - Essential
- Communication: R 300 (Cell phone) - Non-essential
- Insurance: R 400 (Medical aid) - Essential
- Personal: R 500 (Entertainment & hobbies) - Non-essential

**Expected Affordability Status:** ✅ **Affordable**

**Test Cases:**
- ✅ Should pass affordability check
- ✅ Debt-to-income ratio < 25% (Conservative threshold)
- ✅ Safety buffer easily covered (R 850 = max(R500, 10% of income))
- ✅ Can afford loan up to ~R 15,000 (single payment)
- ✅ Should be approved for most loan requests

**Max Recommended Loan:** ~R 15,000 - R 18,000

---

### Scenario 2: Bob Builder - Middle Income Worker (Limited Affordability)

**Email:** `test.worker@hohema.com`  
**Password:** `Test@123`

**Profile:**
- **Monthly Income:** R 4,680 (R 4,180 + R 500 side jobs)
- **Monthly Expenses:** R 4,550
- **Existing Debt:** R 0
- **Disposable Income:** R 130 (~3%)

**Income Sources:**
- Employment: R 4,180 (Construction @ R 90/hr × 160 hrs)
- Other: R 500 (Weekend side jobs)

**Expenses:**
- Housing: R 1,800 (Rent) - Essential
- Utilities: R 400 (Basic utilities) - Essential
- Food: R 1,200 (Groceries for family) - Essential
- Transport: R 500 (Taxi fare) - Essential
- Communication: R 200 (Cell phone) - Non-essential
- Dependents: R 300 (School fees) - Essential
- Medical: R 150 (Clinic visits) - Essential

**Expected Affordability Status:** ⚠️ **Limited Affordability**

**Test Cases:**
- ⚠️ Should show limited affordability
- ⚠️ Debt-to-income ratio > 25% but < 35%
- ⚠️ Safety buffer of R 500 leaves very little disposable income
- ⚠️ Can afford small loans only (R 1,000 - R 3,000)
- ⚠️ Should be flagged for manual review

**Max Recommended Loan:** ~R 1,500 - R 2,500

---

### Scenario 3: Charlie Challenged - Struggling Worker (Not Affordable)

**Email:** `test.struggling@hohema.com`  
**Password:** `Test@123`

**Profile:**
- **Monthly Income:** R 3,200
- **Monthly Expenses:** R 4,550
- **Existing Debt:** R 800 (Debt-to-income: 25%)
- **Disposable Income:** -R 1,350 (NEGATIVE!)

**Income Sources:**
- Employment: R 3,200 (Retail salary)

**Expenses:**
- Housing: R 1,400 (Rent) - Essential
- Utilities: R 500 (Basic utilities) - Essential
- Food: R 1,000 (Groceries) - Essential
- Transport: R 400 (Taxi fare) - Essential
- Debt: R 800 (Existing loan repayment) - Essential
- Communication: R 150 (Cell phone) - Non-essential
- Dependents: R 200 (Child support) - Essential
- Medical: R 100 (Medication) - Essential

**Expected Affordability Status:** ❌ **Not Affordable**

**Test Cases:**
- ❌ Should fail affordability check
- ❌ Expenses exceed income
- ❌ Existing debt burden already at 25% of income
- ❌ Debt-to-income ratio exceeds NCA limits with new loan
- ❌ Should be rejected or require significant manual review
- ⚠️ User needs financial counseling to reduce expenses

**Max Recommended Loan:** R 0 (Should not receive additional credit)

---

### Scenario 4: Diana Developer - Junior with Student Debt (Limited Affordability)

**Email:** `test.newworker@hohema.com`  
**Password:** `Test@123`

**Profile:**
- **Monthly Income:** R 6,300 (R 5,500 salary + R 800 freelance)
- **Monthly Expenses:** R 6,450
- **Existing Debt:** R 500 (Student loan - Debt-to-income: 8%)
- **Disposable Income:** -R 150 (slightly negative)

**Income Sources:**
- Employment: R 5,500 (Junior Developer Salary)
- Self-Employment: R 800 (Freelance projects)

**Expenses:**
- Housing: R 2,500 (Apartment rent) - Essential
- Utilities: R 600 (All utilities) - Essential
- Food: R 1,200 (Groceries & eating out) - Essential
- Transport: R 700 (Car payment) - Essential
- Debt: R 500 (Student loan) - Essential
- Communication: R 250 (Phone & internet) - Non-essential
- Insurance: R 400 (Car & medical) - Essential
- Personal: R 300 (Entertainment) - Non-essential

**Expected Affordability Status:** ⚠️ **Limited Affordability**

**Test Cases:**
- ⚠️ Should show limited affordability
- ⚠️ Existing student debt at manageable level (8%)
- ⚠️ Total debt-to-income would exceed 25% with new loan
- ⚠️ Safety buffer (R 630) pushes disposable income negative
- ⚠️ Can afford very small loans only (R 1,000 - R 2,000)
- 💡 Could improve affordability by reducing non-essential expenses (R 550)

**Max Recommended Loan:** ~R 1,000 - R 2,000

---

## Testing Workflow

### Step 1: Seed Test Data
```bash
POST /api/testdata/seed
```

### Step 2: Login as Test User
```bash
POST /api/auth/login
{
  "email": "test.affluent@hohema.com",
  "password": "Test@123"
}
```

### Step 3: Check Affordability
```bash
GET /api/affordability
```

Expected results vary by user (see scenarios above).

### Step 4: Apply for Worker Loan
```bash
POST /api/loanapplications
{
  "hoursWorked": 160,
  "hourlyRate": 90,
  "monthlyEarnings": 14400,
  "amount": 2000,
  "totalAmount": 2100,
  "appliedInterestRate": 5,
  "appliedAdminFee": 50,
  "repaymentDay": 30,
  "purpose": "Medical emergency",
  "hasIncomeExpenseChanged": false,
  "termMonths": 1
}
```

### Step 5: Verify Affordability Integration
Check that the loan application includes:
- `affordabilityStatus`: Matches expected status
- `passedAffordabilityCheck`: true/false based on scenario
- `affordabilityNotes`: Detailed assessment notes

---

## Expected Outcomes Summary

| User | Income | Expenses | Debt | DTI Ratio | Status | Max Loan |
|------|--------|----------|------|-----------|--------|----------|
| Alice Affluent | R 8,500 | R 4,300 | 0% | 51% | ✅ Affordable | R 15k-18k |
| Bob Builder | R 4,680 | R 4,550 | 0% | 97% | ⚠️ Limited | R 1.5k-2.5k |
| Charlie Challenged | R 3,200 | R 4,550 | 25% | 142% | ❌ Not Affordable | R 0 |
| Diana Developer | R 6,300 | R 6,450 | 8% | 102% | ⚠️ Limited | R 1k-2k |

---

## Key Validation Points

### 1. Safety Buffer Calculation
- Minimum R 500 or 10% of income (whichever is higher)
- Alice: R 850, Bob: R 500, Charlie: R 500, Diana: R 630

### 2. Debt-to-Income Thresholds
- ✅ **Affordable**: DTI < 25% (conservative)
- ⚠️ **Limited**: DTI between 25% - 35%
- ❌ **Not Affordable**: DTI > 35% (NCA limit) OR negative disposable income

### 3. NCA Compliance
- Maximum 35% debt-to-income ratio (regulatory requirement)
- Charlie already at 25% with existing debt
- System should flag applications that would exceed 35%

### 4. Assessment Validity
- Affordability assessments valid for 30 days
- Should prompt user to update income/expenses if stale

---

## Cleanup Test Data

To remove test users and their data:

```sql
DELETE FROM "Incomes" WHERE "UserId" IN (
  SELECT "Id" FROM "AspNetUsers" WHERE "Email" LIKE 'test.%@hohema.com'
);

DELETE FROM "Expenses" WHERE "UserId" IN (
  SELECT "Id" FROM "AspNetUsers" WHERE "Email" LIKE 'test.%@hohema.com'
);

DELETE FROM "LoanApplications" WHERE "UserId" IN (
  SELECT "Id" FROM "AspNetUsers" WHERE "Email" LIKE 'test.%@hohema.com'
);

DELETE FROM "AffordabilityAssessments" WHERE "UserId" IN (
  SELECT "Id" FROM "AspNetUsers" WHERE "Email" LIKE 'test.%@hohema.com'
);

DELETE FROM "AspNetUsers" WHERE "Email" LIKE 'test.%@hohema.com';
```

---

## Notes

- All test users have password: `Test@123`
- Email confirmed: `true` (no need to verify email)
- Created 6 months ago (realistic data)
- Phone numbers and ID numbers are randomly generated
- Test data only works in Development environment
