-- Add ExpectedRepaymentDate and RepaymentDay columns to LoanApplications table
-- Run this script if you get error: column l.ExpectedRepaymentDate does not exist

-- Add ExpectedRepaymentDate column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'ExpectedRepaymentDate'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "ExpectedRepaymentDate" timestamp with time zone NULL;
        RAISE NOTICE 'Added ExpectedRepaymentDate column';
    ELSE
        RAISE NOTICE 'ExpectedRepaymentDate column already exists';
    END IF;
END $$;

-- Add RepaymentDay column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'RepaymentDay'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "RepaymentDay" integer NULL;
        RAISE NOTICE 'Added RepaymentDay column';
    ELSE
        RAISE NOTICE 'RepaymentDay column already exists';
    END IF;
END $$;
