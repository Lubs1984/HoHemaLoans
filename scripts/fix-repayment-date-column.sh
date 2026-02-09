#!/bin/bash

# Apply the expected repayment date column fix to Railway database
# Usage: ./scripts/fix-repayment-date-column.sh

echo "🔧 Fixing ExpectedRepaymentDate column in Railway database..."
echo ""

if [ -z "$DATABASE_URL" ]; then
  echo "❌ ERROR: DATABASE_URL environment variable not set"
  echo "Please set your Railway database URL:"
  echo "  export DATABASE_URL='your-railway-postgres-url'"
  exit 1
fi

node scripts/apply-sql.js "$DATABASE_URL" scripts/add-expected-repayment-date.sql

if [ $? -eq 0 ]; then
  echo ""
  echo "✅ Database column fix applied successfully!"
  echo "You can now reload your loan applications page."
else
  echo ""
  echo "❌ Failed to apply database fix"
  exit 1
fi
