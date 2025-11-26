#!/bin/bash
# Seed the Railway database with test users
# Usage: ./seed-railway-db.sh

set -e

echo "🌱 Ho Hema Loans - Database Seeding Script"
echo "=========================================="
echo ""

# Check if DATABASE_URL is set
if [ -z "$DATABASE_URL" ]; then
    echo "❌ ERROR: DATABASE_URL environment variable is not set"
    echo ""
    echo "To run this script on Railway:"
    echo "1. Go to Railway Dashboard → Your postgres service"
    echo "2. Click 'Variables' tab"
    echo "3. Copy the DATABASE_URL value"
    echo "4. Run: export DATABASE_URL='postgresql://...'"
    echo "5. Then run this script again"
    echo ""
    echo "Alternative: Run directly in Railway shell:"
    echo "  railway run ./scripts/seed-railway-db.sh"
    exit 1
fi

echo "✓ DATABASE_URL found"
echo "✓ Running seed script..."
echo ""

# Run the SQL seed script
psql "$DATABASE_URL" -f "$(dirname "$0")/seed-database.sql"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Database seeded successfully!"
    echo ""
    echo "Test users created:"
    echo "  - admin@hohema.com (Admin) - Password: AdminPassword123!"
    echo "  - john.doe@example.com (User) - Password: TestPassword123!"
    echo "  - jane.smith@example.com (User) - Password: TestPassword123!"
    echo "  - demo@example.com (User) - Password: TestPassword123!"
    echo "  - test@example.com (User) - Password: TestPassword123!"
    echo ""
    echo "You can now login at: https://hohemaweb-development.up.railway.app/login"
else
    echo ""
    echo "❌ Failed to seed database"
    exit 1
fi
