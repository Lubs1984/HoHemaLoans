-- Update LoanApplications table with missing columns
-- Run this in Railway PostgreSQL Query interface to fix validation errors

-- Add missing columns if they don't exist
DO $$ 
BEGIN
    -- Add Purpose column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'Purpose') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "Purpose" varchar(50) NOT NULL DEFAULT '';
    END IF;

    -- Add Notes column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'Notes') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "Notes" varchar(500);
    END IF;

    -- Add ApprovalDate column (in addition to ApprovedDate)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'ApprovalDate') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "ApprovalDate" timestamptz;
    END IF;

    -- Add WhatsAppInitiatedDate column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'WhatsAppInitiatedDate') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "WhatsAppInitiatedDate" timestamptz;
    END IF;

    -- Add WebInitiatedDate column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'WebInitiatedDate') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "WebInitiatedDate" timestamptz;
    END IF;

    -- Add IsAffordabilityIncluded column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'IsAffordabilityIncluded') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "IsAffordabilityIncluded" boolean NOT NULL DEFAULT false;
    END IF;

    -- Add BankName column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'BankName') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "BankName" varchar(20);
    END IF;

    -- Add AccountNumber column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'AccountNumber') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "AccountNumber" varchar(50);
    END IF;

    -- Add AccountHolderName column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'LoanApplications' AND column_name = 'AccountHolderName') THEN
        ALTER TABLE "LoanApplications" ADD COLUMN "AccountHolderName" varchar(100);
    END IF;

    -- Modify WhatsAppSessionId to varchar if it's uuid
    IF EXISTS (SELECT 1 FROM information_schema.columns 
               WHERE table_name = 'LoanApplications' 
               AND column_name = 'WhatsAppSessionId' 
               AND data_type = 'uuid') THEN
        ALTER TABLE "LoanApplications" ALTER COLUMN "WhatsAppSessionId" TYPE varchar(100) USING "WhatsAppSessionId"::text;
    END IF;

    -- Add default values for CurrentStep and ChannelOrigin if they don't have them
    ALTER TABLE "LoanApplications" ALTER COLUMN "CurrentStep" SET DEFAULT 0;
    ALTER TABLE "LoanApplications" ALTER COLUMN "ChannelOrigin" SET DEFAULT 'Web';

END $$;

-- Verify the changes
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'LoanApplications'
ORDER BY ordinal_position;
