-- Add missing columns to LoanApplications table
-- Run this script if you get errors about missing columns

-- Add HasIncomeExpenseChanged column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'HasIncomeExpenseChanged'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "HasIncomeExpenseChanged" boolean NOT NULL DEFAULT false;
        RAISE NOTICE 'Added HasIncomeExpenseChanged column';
    ELSE
        RAISE NOTICE 'HasIncomeExpenseChanged column already exists';
    END IF;
END $$;

-- Ensure other affordability-related columns exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'MaxLoanAmount'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "MaxLoanAmount" numeric(18,2) NULL;
        RAISE NOTICE 'Added MaxLoanAmount column';
    ELSE
        RAISE NOTICE 'MaxLoanAmount column already exists';
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'AppliedInterestRate'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "AppliedInterestRate" numeric(5,2) NULL;
        RAISE NOTICE 'Added AppliedInterestRate column';
    ELSE
        RAISE NOTICE 'AppliedInterestRate column already exists';
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'AppliedAdminFee'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "AppliedAdminFee" numeric(18,2) NULL;
        RAISE NOTICE 'Added AppliedAdminFee column';
    ELSE
        RAISE NOTICE 'AppliedAdminFee column already exists';
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'HourlyRate'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "HourlyRate" numeric(18,2) NULL;
        RAISE NOTICE 'Added HourlyRate column';
    ELSE
        RAISE NOTICE 'HourlyRate column already exists';
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'HoursWorked'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "HoursWorked" numeric(18,2) NULL;
        RAISE NOTICE 'Added HoursWorked column';
    ELSE
        RAISE NOTICE 'HoursWorked column already exists';
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'LoanApplications' 
        AND column_name = 'MonthlyEarnings'
    ) THEN
        ALTER TABLE "LoanApplications" 
        ADD COLUMN "MonthlyEarnings" numeric(18,2) NULL;
        RAISE NOTICE 'Added MonthlyEarnings column';
    ELSE
        RAISE NOTICE 'MonthlyEarnings column already exists';
    END IF;
END $$;
