-- Add NCR-required profile fields to AspNetUsers table
-- This script is idempotent (safe to run multiple times)

-- Mark AddUserDocuments migration as applied if not already
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209201138_AddUserDocuments', '8.0.0')
ON CONFLICT DO NOTHING;

-- South African Address fields
ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "StreetAddress" character varying(200) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "Suburb" character varying(100) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "City" character varying(100) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "Province" character varying(50) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "PostalCode" character varying(10) DEFAULT '';

-- Employment fields
ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "EmployerName" character varying(200) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "EmployeeNumber" character varying(50) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "PayrollReference" character varying(50) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "EmploymentType" character varying(50) DEFAULT '';

-- Banking fields
ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "BankName" character varying(100) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "AccountType" character varying(50) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "AccountNumber" character varying(50) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "BranchCode" character varying(10) DEFAULT '';

-- Next of Kin fields
ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "NextOfKinName" character varying(100) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "NextOfKinRelationship" character varying(50) DEFAULT '';

ALTER TABLE "AspNetUsers" 
ADD COLUMN IF NOT EXISTS "NextOfKinPhone" character varying(20) DEFAULT '';

-- Mark EnhanceUserProfileWithNCRFields migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209211231_EnhanceUserProfileWithNCRFields', '8.0.0')
ON CONFLICT DO NOTHING;

-- Verify columns were added
SELECT column_name, data_type, character_maximum_length 
FROM information_schema.columns 
WHERE table_name = 'AspNetUsers' 
AND column_name IN (
    'StreetAddress', 'Suburb', 'City', 'Province', 'PostalCode',
    'EmployerName', 'EmployeeNumber', 'PayrollReference', 'EmploymentType',
    'BankName', 'AccountType', 'AccountNumber', 'BranchCode',
    'NextOfKinName', 'NextOfKinRelationship', 'NextOfKinPhone'
)
ORDER BY column_name;
