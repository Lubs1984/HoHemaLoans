-- Find applications from November 27, 2025
SELECT "Id", "Amount", "Status", "ApplicationDate"
FROM "LoanApplications"
WHERE DATE("ApplicationDate") = '2025-11-27';

-- Delete applications from November 27, 2025
DELETE FROM "LoanApplications"
WHERE DATE("ApplicationDate") = '2025-11-27';

-- Show remaining applications count
SELECT COUNT(*) as remaining_applications FROM "LoanApplications";
