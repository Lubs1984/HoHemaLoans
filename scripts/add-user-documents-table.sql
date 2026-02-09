-- Migration: Add UserDocuments table only
-- This script adds the UserDocuments table without modifying existing tables

-- Create UserDocuments table
CREATE TABLE IF NOT EXISTS "UserDocuments" (
    "Id" uuid NOT NULL,
    "UserId" text NOT NULL,
    "DocumentType" integer NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "FilePath" character varying(500) NOT NULL,
    "FileSize" bigint NOT NULL,
    "ContentType" character varying(100) NOT NULL,
    "FileContentBase64" text NULL,
    "Status" integer NOT NULL DEFAULT 0,
    "UploadedAt" timestamp with time zone NOT NULL,
    "VerifiedAt" timestamp with time zone NULL,
    "VerifiedByUserId" text NULL,
    "RejectionReason" character varying(500) NULL,
    "Notes" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_UserDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserDocuments_AspNetUsers_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserDocuments_AspNetUsers_VerifiedByUserId" FOREIGN KEY ("VerifiedByUserId") 
        REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_UserDocuments_UserId" ON "UserDocuments" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserDocuments_Status" ON "UserDocuments" ("Status");
CREATE INDEX IF NOT EXISTS "IX_UserDocuments_DocumentType" ON "UserDocuments" ("DocumentType");
CREATE INDEX IF NOT EXISTS "IX_UserDocuments_UploadedAt" ON "UserDocuments" ("UploadedAt");
CREATE INDEX IF NOT EXISTS "IX_UserDocuments_VerifiedByUserId" ON "UserDocuments" ("VerifiedByUserId");

-- Mark migrations as applied (so EF doesn't try to recreate everything)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209194909_AddUserDocuments', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260209195443_AddBase64ToDocuments', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Display success message
DO $$
BEGIN
    RAISE NOTICE 'UserDocuments table created successfully!';
    RAISE NOTICE 'Migration records added to __EFMigrationsHistory';
END $$;
