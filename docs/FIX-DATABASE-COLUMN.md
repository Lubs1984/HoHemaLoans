# Add AffordabilityNotes Column to Railway Database

## The Issue
The application is failing with error: `column l.AffordabilityNotes does not exist`

## Solution: Add the Column via Railway Dashboard

### Option 1: Railway Database Query Interface

1. **Go to Railway Dashboard:**
   - https://railway.app/
   - Select project: **Ho Hema**
   - Select environment: **development**

2. **Open Database Service:**
   - Click on **HohemaDB** (PostgreSQL service)
   - Click **"Query"** tab or **"Data"** tab

3. **Run this SQL:**
   ```sql
   ALTER TABLE "LoanApplications" 
   ADD COLUMN IF NOT EXISTS "AffordabilityNotes" TEXT NULL;
   
   COMMENT ON COLUMN "LoanApplications"."AffordabilityNotes" 
   IS 'Notes about the affordability assessment for this loan application';
   ```

4. **Click Execute** or **Run**

5. **Verify:**
   ```sql
   SELECT column_name, data_type, is_nullable 
   FROM information_schema.columns 
   WHERE table_name = 'LoanApplications' 
   AND column_name = 'AffordabilityNotes';
   ```

### Option 2: Use Railway CLI with Docker

If you have Docker installed:

```bash
# Get database connection string
railway variables --service HohemaDB

# Run PostgreSQL client in Docker
docker run --rm -it postgres:latest psql "YOUR_DATABASE_URL_HERE" -c "ALTER TABLE \"LoanApplications\" ADD COLUMN IF NOT EXISTS \"AffordabilityNotes\" TEXT NULL;"
```

### Option 3: TablePlus / pgAdmin

1. Get your database connection details from Railway
2. Connect using TablePlus, pgAdmin, or any PostgreSQL client
3. Run the ALTER TABLE command above

---

## After Adding the Column

The API should work correctly and you'll be able to:
- View loan applications
- See affordability notes in the UI
- Use the Resume Application feature on the dashboard

## Files Created

- Migration script: `scripts/add-affordability-notes-column.sql`

This file can be used when setting up new environments or for documentation.
