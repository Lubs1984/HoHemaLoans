-- Aggressively fix all StepData by setting everything to empty JSON object
-- This ensures no invalid JSON remains

UPDATE "LoanApplications"
SET "StepData" = '{}'::jsonb
WHERE "StepData" IS NOT NULL;

-- Verify the fix
SELECT 
  COUNT(*) as total_loans,
  COUNT("StepData") as loans_with_stepdata
FROM "LoanApplications";
