-- Fix malformed JSON in StepData column
-- This script cleans up any invalid JSON in the LoanApplications.StepData field

-- First, let's set any invalid JSON to NULL or empty object
UPDATE "LoanApplications"
SET "StepData" = '{}'::jsonb
WHERE "StepData" IS NOT NULL 
  AND NOT (
    "StepData"::text ~ '^(\{.*\}|\[.*\])$' 
    AND "StepData"::text !~ ',$'  -- Check for trailing comma
    AND "StepData"::text !~ ',\s*[}\]]'  -- Check for comma before closing bracket
  );

-- Alternative: Set to NULL if you prefer
-- UPDATE "LoanApplications"
-- SET "StepData" = NULL
-- WHERE "StepData" IS NOT NULL 
--   AND NOT ("StepData"::text ~ '^(\{.*\}|\[.*\])$');

-- Report on affected rows
DO $$
DECLARE
    affected_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO affected_count
    FROM "LoanApplications"
    WHERE "StepData" = '{}'::jsonb;
    
    RAISE NOTICE 'Fixed % loan applications with invalid StepData', affected_count;
END $$;
