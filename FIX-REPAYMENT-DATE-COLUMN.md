# Fix: Column ExpectedRepaymentDate Does Not Exist

## Problem
Error when loading loan applications:
```
{
  "error": "42703: column l.ExpectedRepaymentDate does not exist\nPOSITION: 254"
}
```

## Cause
The `ExpectedRepaymentDate` column exists in the Entity Framework model but was not added to the actual Railway database table.

## Solution

### Option 1: Quick Fix (Recommended)
Run the provided script to add the missing column:

```bash
# Set your Railway database URL
export DATABASE_URL='your-railway-postgres-connection-string'

# Run the fix script
./scripts/fix-repayment-date-column.sh
```

### Option 2: Manual SQL Execution
If you prefer to run the SQL manually through Railway's PostgreSQL interface:

1. Go to your Railway project
2. Open the PostgreSQL database
3. Go to the Query tab
4. Run the SQL from [scripts/add-expected-repayment-date.sql](../scripts/add-expected-repayment-date.sql)

### Option 3: Using Railway CLI
```bash
# Install Railway CLI if needed
npm install -g @railway/cli

# Login to Railway
railway login

# Link to your project
railway link

# Apply the SQL
railway run node scripts/apply-sql.js "$DATABASE_URL" scripts/add-expected-repayment-date.sql
```

## What the Fix Does
The script adds two columns to the `LoanApplications` table:
- `ExpectedRepaymentDate` (timestamp with time zone, nullable)
- `RepaymentDay` (integer, nullable)

Both columns are already defined in the C# model but were missing from the database schema.

## Verification
After applying the fix:
1. Reload the loan applications page
2. The error should be gone
3. Existing loan applications will show properly

## Prevention
This issue occurred because the Entity Framework migration wasn't applied to the Railway database. In the future:
- Always run migrations on deployment
- Consider adding a migration check to the CI/CD pipeline
- Document any manual SQL scripts that need to be run
