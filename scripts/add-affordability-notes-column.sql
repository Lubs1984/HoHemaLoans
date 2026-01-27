-- Add AffordabilityNotes column to LoanApplications table
-- This column stores notes about the affordability assessment

ALTER TABLE "LoanApplications" 
ADD COLUMN IF NOT EXISTS "AffordabilityNotes" TEXT NULL;

-- Add comment to document the column
COMMENT ON COLUMN "LoanApplications"."AffordabilityNotes" IS 'Notes about the affordability assessment for this loan application';
