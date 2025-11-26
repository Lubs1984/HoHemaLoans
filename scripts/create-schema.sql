-- Ho Hema Loans - Full Database Schema Creation
-- Run this in Railway PostgreSQL Query interface to create all tables

-- Create AspNetRoles table
CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id" text NOT NULL PRIMARY KEY,
    "Name" varchar(256),
    "NormalizedName" varchar(256),
    "ConcurrencyStamp" text
);

CREATE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

-- Create AspNetUsers table
CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id" text NOT NULL PRIMARY KEY,
    "UserName" varchar(256),
    "NormalizedUserName" varchar(256),
    "Email" varchar(256),
    "NormalizedEmail" varchar(256),
    "EmailConfirmed" boolean NOT NULL,
    "PasswordHash" text,
    "SecurityStamp" text,
    "ConcurrencyStamp" text,
    "PhoneNumber" text,
    "PhoneNumberConfirmed" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "LockoutEnd" timestamptz,
    "LockoutEnabled" boolean NOT NULL,
    "AccessFailedCount" integer NOT NULL,
    "FirstName" varchar(50) NOT NULL,
    "LastName" varchar(50) NOT NULL,
    "IdNumber" varchar(13) NOT NULL,
    "DateOfBirth" timestamp NOT NULL,
    "Address" varchar(100),
    "MonthlyIncome" decimal(18,2),
    "IsVerified" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz
);

CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AspNetUsers_IdNumber" ON "AspNetUsers" ("IdNumber");

-- Create AspNetUserClaims table
CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" serial PRIMARY KEY,
    "UserId" text NOT NULL,
    "ClaimType" text,
    "ClaimValue" text,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

-- Create AspNetUserLogins table
CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" text NOT NULL,
    "ProviderKey" text NOT NULL,
    "ProviderDisplayName" text,
    "UserId" text NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

-- Create AspNetUserRoles table
CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" text NOT NULL,
    "RoleId" text NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

-- Create AspNetUserTokens table
CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId" text NOT NULL,
    "LoginProvider" text NOT NULL,
    "Name" text NOT NULL,
    "Value" text,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- Create AspNetRoleClaims table
CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id" serial PRIMARY KEY,
    "RoleId" text NOT NULL,
    "ClaimType" text,
    "ClaimValue" text,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

-- Create WhatsAppContacts table
CREATE TABLE IF NOT EXISTS "WhatsAppContacts" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "PhoneNumber" varchar(20) NOT NULL,
    "DisplayName" varchar(100),
    "FirstName" varchar(100),
    "LastName" varchar(100),
    "ProfilePictureUrl" text,
    "UserId" text,
    "IsBlocked" boolean NOT NULL DEFAULT false,
    "LastInteractionAt" timestamptz,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz,
    CONSTRAINT "FK_WhatsAppContacts_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_WhatsAppContacts_PhoneNumber" ON "WhatsAppContacts" ("PhoneNumber");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppContacts_UserId" ON "WhatsAppContacts" ("UserId");

-- Create LoanApplications table
CREATE TABLE IF NOT EXISTS "LoanApplications" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" text NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "TermInMonths" integer NOT NULL,
    "InterestRate" decimal(5,4) NOT NULL,
    "MonthlyPayment" decimal(18,2) NOT NULL,
    "TotalAmount" decimal(18,2) NOT NULL,
    "Status" varchar(20) NOT NULL,
    "ApplicationDate" timestamptz NOT NULL,
    "ApprovedDate" timestamptz,
    "DisbursedDate" timestamptz,
    "RejectedDate" timestamptz,
    "RejectionReason" text,
    "CurrentStep" integer NOT NULL,
    "ChannelOrigin" varchar(20) NOT NULL,
    "WhatsAppSessionId" uuid,
    "StepData" jsonb,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz,
    CONSTRAINT "FK_LoanApplications_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_LoanApplications_UserId" ON "LoanApplications" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_LoanApplications_ApplicationDate" ON "LoanApplications" ("ApplicationDate");
CREATE INDEX IF NOT EXISTS "IX_LoanApplications_WhatsAppSessionId" ON "LoanApplications" ("WhatsAppSessionId");

-- Create WhatsAppSessions table
CREATE TABLE IF NOT EXISTS "WhatsAppSessions" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "PhoneNumber" varchar(20) NOT NULL,
    "UserId" text,
    "SessionStatus" varchar(20) NOT NULL,
    "CurrentStep" integer NOT NULL,
    "DraftApplicationId" uuid,
    "LastActivityAt" timestamptz NOT NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz,
    CONSTRAINT "FK_WhatsAppSessions_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WhatsAppSessions_LoanApplications_DraftApplicationId" FOREIGN KEY ("DraftApplicationId") REFERENCES "LoanApplications" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_WhatsAppSessions_PhoneNumber" ON "WhatsAppSessions" ("PhoneNumber");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppSessions_UserId" ON "WhatsAppSessions" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppSessions_SessionStatus" ON "WhatsAppSessions" ("SessionStatus");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppSessions_DraftApplicationId" ON "WhatsAppSessions" ("DraftApplicationId");

-- Create WhatsAppConversations table
CREATE TABLE IF NOT EXISTS "WhatsAppConversations" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "ContactId" uuid NOT NULL,
    "Subject" varchar(200),
    "Status" varchar(20) NOT NULL,
    "Type" varchar(20) NOT NULL,
    "LoanApplicationId" uuid,
    "LastMessageAt" timestamptz,
    "MessageCount" integer NOT NULL DEFAULT 0,
    "UnreadCount" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz,
    CONSTRAINT "FK_WhatsAppConversations_WhatsAppContacts_ContactId" FOREIGN KEY ("ContactId") REFERENCES "WhatsAppContacts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WhatsAppConversations_LoanApplications_LoanApplicationId" FOREIGN KEY ("LoanApplicationId") REFERENCES "LoanApplications" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_WhatsAppConversations_ContactId" ON "WhatsAppConversations" ("ContactId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppConversations_LoanApplicationId" ON "WhatsAppConversations" ("LoanApplicationId");

-- Create WhatsAppMessages table
CREATE TABLE IF NOT EXISTS "WhatsAppMessages" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "ConversationId" uuid NOT NULL,
    "ContactId" uuid NOT NULL,
    "WhatsAppMessageId" varchar(100),
    "MessageText" text NOT NULL,
    "Type" varchar(20) NOT NULL,
    "Direction" varchar(20) NOT NULL,
    "Status" varchar(20) NOT NULL,
    "MediaUrl" varchar(500),
    "MediaType" varchar(100),
    "MediaCaption" varchar(200),
    "ErrorMessage" varchar(500),
    "TemplateName" varchar(100),
    "TemplateParameters" jsonb,
    "IsRead" boolean NOT NULL DEFAULT false,
    "HandledByUserId" text,
    "HandledAt" timestamptz,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz,
    CONSTRAINT "FK_WhatsAppMessages_WhatsAppConversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "WhatsAppConversations" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WhatsAppMessages_WhatsAppContacts_ContactId" FOREIGN KEY ("ContactId") REFERENCES "WhatsAppContacts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WhatsAppMessages_AspNetUsers_HandledByUserId" FOREIGN KEY ("HandledByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_WhatsAppMessages_ConversationId" ON "WhatsAppMessages" ("ConversationId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppMessages_ContactId" ON "WhatsAppMessages" ("ContactId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppMessages_HandledByUserId" ON "WhatsAppMessages" ("HandledByUserId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppMessages_WhatsAppMessageId" ON "WhatsAppMessages" ("WhatsAppMessageId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppMessages_CreatedAt" ON "WhatsAppMessages" ("CreatedAt");

-- Create Incomes table
CREATE TABLE IF NOT EXISTS "Incomes" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" text NOT NULL,
    "SourceType" varchar(50) NOT NULL,
    "Description" varchar(200) NOT NULL,
    "MonthlyAmount" decimal(18,2) NOT NULL,
    "Frequency" varchar(20) NOT NULL,
    "IsVerified" boolean NOT NULL DEFAULT false,
    "Notes" varchar(500),
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz,
    CONSTRAINT "FK_Incomes_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Incomes_UserId" ON "Incomes" ("UserId");

-- Create Expenses table
CREATE TABLE IF NOT EXISTS "Expenses" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" text NOT NULL,
    "Category" varchar(50) NOT NULL,
    "Description" varchar(200) NOT NULL,
    "MonthlyAmount" decimal(18,2) NOT NULL,
    "Frequency" varchar(20) NOT NULL,
    "IsEssential" boolean NOT NULL DEFAULT false,
    "Notes" varchar(500),
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz,
    CONSTRAINT "FK_Expenses_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Expenses_UserId" ON "Expenses" ("UserId");

-- Create AffordabilityAssessments table
CREATE TABLE IF NOT EXISTS "AffordabilityAssessments" (
    "Id" uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" text NOT NULL,
    "AssessmentDate" timestamptz NOT NULL,
    "GrossMonthlyIncome" decimal(18,2) NOT NULL,
    "NetMonthlyIncome" decimal(18,2) NOT NULL,
    "TotalMonthlyExpenses" decimal(18,2) NOT NULL,
    "EssentialExpenses" decimal(18,2) NOT NULL,
    "NonEssentialExpenses" decimal(18,2) NOT NULL,
    "DebtToIncomeRatio" decimal(5,4) NOT NULL,
    "ExpenseToIncomeRatio" decimal(5,4) NOT NULL,
    "AvailableFunds" decimal(18,2) NOT NULL,
    "AffordabilityStatus" varchar(20) NOT NULL,
    "AssessmentNotes" text,
    "MaxRecommendedLoanAmount" decimal(18,2) NOT NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz,
    CONSTRAINT "FK_AffordabilityAssessments_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AffordabilityAssessments_UserId" ON "AffordabilityAssessments" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AffordabilityAssessments_AssessmentDate" ON "AffordabilityAssessments" ("AssessmentDate");

-- Create EF Migrations History table (required by Entity Framework)
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL PRIMARY KEY,
    "ProductVersion" varchar(32) NOT NULL
);

-- Insert initial migration record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251126_InitialCreate', '9.0.0')
ON CONFLICT DO NOTHING;

-- Success message
SELECT 'Database schema created successfully!' as status;
