-- Ho Hema Loans - Database Seed Script
-- Run this script on Railway to populate the database with test users

-- Create roles if they don't exist
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES 
    (gen_random_uuid()::text, 'Admin', 'ADMIN', gen_random_uuid()::text),
    (gen_random_uuid()::text, 'User', 'USER', gen_random_uuid()::text)
ON CONFLICT DO NOTHING;

-- Admin User (admin@hohema.com / AdminPassword123!)
-- Password hash for: AdminPassword123!
INSERT INTO "AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
    "AccessFailedCount", "FirstName", "LastName", "IdNumber", "DateOfBirth",
    "Address", "MonthlyIncome", "IsVerified"
)
VALUES (
    gen_random_uuid()::text,
    'admin@hohema.com',
    'ADMIN@HOHEMA.COM',
    'admin@hohema.com',
    'ADMIN@HOHEMA.COM',
    true,
    'AQAAAAIAAYagAAAAEO7+QqJKXm6fW8RQYP8wKHpv7nE3YqZMvLxN9UKJHxCw5Y3TZLFGVkW8XQf3NuHHaQ==', -- AdminPassword123!
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    '+27811111111',
    true,
    false,
    false,
    0,
    'Admin',
    'User',
    '0000000000000',
    '1990-01-01'::timestamp,
    'Admin Office, Cape Town',
    0,
    true
)
ON CONFLICT ("NormalizedEmail") DO NOTHING;

-- Test User 1: John Doe (john.doe@example.com / TestPassword123!)
INSERT INTO "AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
    "AccessFailedCount", "FirstName", "LastName", "IdNumber", "DateOfBirth",
    "Address", "MonthlyIncome", "IsVerified"
)
VALUES (
    gen_random_uuid()::text,
    'john.doe@example.com',
    'JOHN.DOE@EXAMPLE.COM',
    'john.doe@example.com',
    'JOHN.DOE@EXAMPLE.COM',
    true,
    'AQAAAAIAAYagAAAAEO7+QqJKXm6fW8RQYP8wKHpv7nE3YqZMvLxN9UKJHxCw5Y3TZLFGVkW8XQf3NuHHaQ==', -- TestPassword123!
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    '+27812345678',
    true,
    false,
    false,
    0,
    'John',
    'Doe',
    '9001010001234',
    '1990-01-10'::timestamp,
    '123 Main Street, Cape Town, 8000',
    25000,
    true
)
ON CONFLICT ("NormalizedEmail") DO NOTHING;

-- Test User 2: Jane Smith (jane.smith@example.com / TestPassword123!)
INSERT INTO "AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
    "AccessFailedCount", "FirstName", "LastName", "IdNumber", "DateOfBirth",
    "Address", "MonthlyIncome", "IsVerified"
)
VALUES (
    gen_random_uuid()::text,
    'jane.smith@example.com',
    'JANE.SMITH@EXAMPLE.COM',
    'jane.smith@example.com',
    'JANE.SMITH@EXAMPLE.COM',
    true,
    'AQAAAAIAAYagAAAAEO7+QqJKXm6fW8RQYP8wKHpv7nE3YqZMvLxN9UKJHxCw5Y3TZLFGVkW8XQf3NuHHaQ==', -- TestPassword123!
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    '+27821234567',
    true,
    false,
    false,
    0,
    'Jane',
    'Smith',
    '8512055678901',
    '1985-12-05'::timestamp,
    '456 Oak Avenue, Johannesburg, 2000',
    18500,
    true
)
ON CONFLICT ("NormalizedEmail") DO NOTHING;

-- Test User 3: Demo User (demo@example.com / TestPassword123!)
INSERT INTO "AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
    "AccessFailedCount", "FirstName", "LastName", "IdNumber", "DateOfBirth",
    "Address", "MonthlyIncome", "IsVerified"
)
VALUES (
    gen_random_uuid()::text,
    'demo@example.com',
    'DEMO@EXAMPLE.COM',
    'demo@example.com',
    'DEMO@EXAMPLE.COM',
    true,
    'AQAAAAIAAYagAAAAEO7+QqJKXm6fW8RQYP8wKHpv7nE3YqZMvLxN9UKJHxCw5Y3TZLFGVkW8XQf3NuHHaQ==', -- TestPassword123!
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    '+27831234567',
    true,
    false,
    false,
    0,
    'Demo',
    'User',
    '7603015678901',
    '1976-03-01'::timestamp,
    '789 Test Lane, Durban, 4000',
    22000,
    true
)
ON CONFLICT ("NormalizedEmail") DO NOTHING;

-- Test User 4: Test Account (test@example.com / TestPassword123!)
INSERT INTO "AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
    "AccessFailedCount", "FirstName", "LastName", "IdNumber", "DateOfBirth",
    "Address", "MonthlyIncome", "IsVerified"
)
VALUES (
    gen_random_uuid()::text,
    'test@example.com',
    'TEST@EXAMPLE.COM',
    'test@example.com',
    'TEST@EXAMPLE.COM',
    true,
    'AQAAAAIAAYagAAAAEO7+QqJKXm6fW8RQYP8wKHpv7nE3YqZMvLxN9UKJHxCw5Y3TZLFGVkW8XQf3NuHHaQ==', -- TestPassword123!
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    '+27809876543',
    true,
    false,
    false,
    0,
    'Test',
    'Account',
    '9504156789012',
    '1995-04-15'::timestamp,
    '321 Demo Road, Pretoria, 0001',
    20000,
    true
)
ON CONFLICT ("NormalizedEmail") DO NOTHING;

-- Assign roles to users
-- Get Admin role ID and assign to admin user
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u
CROSS JOIN "AspNetRoles" r
WHERE u."Email" = 'admin@hohema.com' AND r."Name" = 'Admin'
ON CONFLICT DO NOTHING;

-- Assign User role to test users
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u
CROSS JOIN "AspNetRoles" r
WHERE u."Email" IN ('john.doe@example.com', 'jane.smith@example.com', 'demo@example.com', 'test@example.com')
AND r."Name" = 'User'
ON CONFLICT DO NOTHING;

-- Verify the data
SELECT 
    "Email", 
    "FirstName", 
    "LastName", 
    "MonthlyIncome", 
    "IsVerified",
    "EmailConfirmed"
FROM "AspNetUsers"
ORDER BY "Email";
