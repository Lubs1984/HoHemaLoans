-- Comprehensive fix for all malformed JSON in database
-- This identifies and fixes ALL rows with invalid JSONB data

-- First, let's find all rows with problematic StepData
DO $$
DECLARE
    loan_record RECORD;
    fixed_count INTEGER := 0;
BEGIN
    -- Loop through all loan applications
    FOR loan_record IN 
        SELECT "Id", "StepData"::text as step_data_text
        FROM "LoanApplications"
        WHERE "StepData" IS NOT NULL
    LOOP
        BEGIN
            -- Try to parse the JSON - if it fails, we'll catch it
            PERFORM loan_record.step_data_text::jsonb;
        EXCEPTION WHEN OTHERS THEN
            -- If parsing fails, set to empty object
            UPDATE "LoanApplications"
            SET "StepData" = '{}'::jsonb
            WHERE "Id" = loan_record."Id";
            
            fixed_count := fixed_count + 1;
            RAISE NOTICE 'Fixed loan application %', loan_record."Id";
        END;
    END LOOP;
    
    RAISE NOTICE 'Total fixed: % loan applications', fixed_count;
END $$;

-- Also clean up any StepData with trailing commas or other common issues
UPDATE "LoanApplications"
SET "StepData" = '{}'::jsonb
WHERE "StepData" IS NOT NULL
  AND (
    "StepData"::text LIKE '%,}%'  -- Trailing comma before }
    OR "StepData"::text LIKE '%,]%'  -- Trailing comma before ]
    OR "StepData"::text LIKE '%},%'  -- Comma after }
    OR "StepData"::text LIKE '%],%'  -- Comma after ]
  );
