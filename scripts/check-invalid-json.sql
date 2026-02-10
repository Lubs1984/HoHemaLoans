-- Check for rows that still have invalid JSON
DO $$
DECLARE
  loan_record RECORD;
  invalid_count INTEGER := 0;
BEGIN
  RAISE NOTICE 'Checking for invalid JSON in StepData...';
  
  FOR loan_record IN 
    SELECT "Id", "StepData"::text as step_data_text 
    FROM "LoanApplications" 
    WHERE "StepData" IS NOT NULL
  LOOP
    BEGIN
      -- Try to parse the JSON
      PERFORM loan_record.step_data_text::jsonb;
    EXCEPTION WHEN OTHERS THEN
      invalid_count := invalid_count + 1;
      RAISE NOTICE 'Invalid JSON in loan ID: % (length: %)', loan_record."Id", length(loan_record.step_data_text);
    END;
  END LOOP;
  
  RAISE NOTICE 'Total rows with invalid JSON: %', invalid_count;
END $$;
