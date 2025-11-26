# Database Seeding Scripts

Scripts to populate your Ho Hema Loans database with test users and data.

## 📁 Files

- **`seed-database.sql`** - SQL script with test users and roles
- **`seed-railway-db.sh`** - Bash script to run SQL on Railway database
- **`migrate.sh`** - Run database migrations

## 🚀 Quick Start

### Option 1: Run via Railway Dashboard (Easiest)

1. Go to [Railway Dashboard](https://railway.app/dashboard)
2. Click on your **postgres** service (not the API)
3. Click **"Data"** tab
4. Click **"Query"** button
5. Copy the contents of `seed-database.sql`
6. Paste into the query editor
7. Click **"Run"**
8. ✅ Done! Test users are created

### Option 2: Run via Railway CLI

```bash
# Install Railway CLI if needed
npm i -g @railway/cli

# Login to Railway
railway login

# Link to your project
railway link

# Get DATABASE_URL
railway variables

# Export the DATABASE_URL (copy from above)
export DATABASE_URL="postgresql://postgres:..."

# Run the seed script
./scripts/seed-railway-db.sh
```

### Option 3: Run Locally

If you have PostgreSQL installed locally:

```bash
# Set your local database URL
export DATABASE_URL="postgresql://hohema_user:hohema_password_2024!@localhost:5432/hohema_loans"

# Run the seed script
./scripts/seed-railway-db.sh
```

Or directly with psql:

```bash
psql "postgresql://hohema_user:hohema_password_2024!@localhost:5432/hohema_loans" -f scripts/seed-database.sql
```

## 👤 Test Users Created

All users have **verified email** and **active accounts**.

| Email | Password | Role | Income |
|-------|----------|------|--------|
| admin@hohema.com | AdminPassword123! | Admin | R0 |
| john.doe@example.com | TestPassword123! | User | R25,000 |
| jane.smith@example.com | TestPassword123! | User | R18,500 |
| demo@example.com | TestPassword123! | User | R22,000 |
| test@example.com | TestPassword123! | User | R20,000 |

## ✅ Verify Success

After running the script, check that users were created:

```sql
SELECT "Email", "FirstName", "LastName", "MonthlyIncome", "IsVerified"
FROM "AspNetUsers"
ORDER BY "Email";
```

Expected output:
```
        Email            | FirstName | LastName | MonthlyIncome | IsVerified
-------------------------+-----------+----------+---------------+------------
 admin@hohema.com        | Admin     | User     |             0 | t
 demo@example.com        | Demo      | User     |         22000 | t
 jane.smith@example.com  | Jane      | Smith    |         18500 | t
 john.doe@example.com    | John      | Doe      |         25000 | t
 test@example.com        | Test      | Account  |         20000 | t
```

## 🔐 Password Hashes

The SQL script includes pre-hashed passwords using ASP.NET Core Identity's password hasher:

- **AdminPassword123!** → Used for admin account
- **TestPassword123!** → Used for all test users

**Note**: The password hash in the script is a placeholder. When you run the API for the first time, it will create users with proper password hashes via `DbInitializer.cs`.

## 🔄 Re-running the Script

The script uses `ON CONFLICT DO NOTHING`, so:
- ✅ Safe to run multiple times
- ✅ Won't create duplicate users
- ✅ Won't overwrite existing data
- ✅ Will only insert new users that don't exist

## 🛠️ Troubleshooting

### "psql: command not found"

Install PostgreSQL client:
```bash
# macOS
brew install postgresql

# Ubuntu/Debian
sudo apt-get install postgresql-client

# Windows
# Download from: https://www.postgresql.org/download/windows/
```

### "DATABASE_URL not set"

Get it from Railway:
```bash
railway variables | grep DATABASE_URL
```

Or from Railway dashboard:
1. Click postgres service
2. Variables tab
3. Copy DATABASE_URL

### "Permission denied"

Make script executable:
```bash
chmod +x scripts/seed-railway-db.sh
```

### "Connection refused"

Check:
1. DATABASE_URL is correct
2. Railway database is running
3. Your IP is allowed (Railway allows all by default)
4. Database service is deployed

### Password hashes don't work

The placeholder password hashes in the SQL script may not work with .NET's password hasher. Instead:

1. **Let the API create users** - It automatically seeds on startup via `DbInitializer.cs`
2. **Or register manually** - Use the `/api/auth/register` endpoint
3. **Or use a real hash** - Generate with:
   ```csharp
   var hasher = new PasswordHasher<ApplicationUser>();
   var hash = hasher.HashPassword(null, "TestPassword123!");
   Console.WriteLine(hash);
   ```

## 💡 Alternative: API Auto-Seeding

The API automatically creates test users on startup via `DbInitializer.cs`:

1. Deploy API to Railway
2. Wait for deployment to complete
3. Check logs for: `✅ Created test user: ...`
4. Users are automatically created on first run

**Advantage**: Uses proper password hashing from .NET Identity

## 📝 Notes

- Scripts are idempotent (safe to run multiple times)
- Uses PostgreSQL-specific syntax (`gen_random_uuid()`)
- Roles (Admin, User) are created first
- UserRoles junction table is populated
- All users are email-confirmed and verified
- Suitable for development/testing only (not production)

## 🔗 Related Files

- `/src/api/HoHemaLoans.Api/Data/DbInitializer.cs` - C# seeding logic
- `/TEST-USERS.md` - Test user documentation
- `/scripts/migrate.sh` - Database migration script

## 🎯 Next Steps

After seeding:
1. Try logging in: https://hohemaweb-development.up.railway.app/login
2. Use: `test@example.com` / `TestPassword123!`
3. Check dashboard loads correctly
4. Create a test loan application

---

**Pro Tip**: The easiest method is **Option 1** (Railway Dashboard Query) - no CLI needed!
