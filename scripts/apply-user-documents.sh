#!/bin/bash

# Apply UserDocuments table migration to Railway database
# This script creates the UserDocuments table and marks migrations as applied

set -e

echo "🚀 Applying UserDocuments table migration..."
echo ""

# Check if DATABASE_URL is provided
if [ -z "$1" ]; then
    echo "❌ ERROR: DATABASE_URL is required"
    echo ""
    echo "Usage:"
    echo "  ./scripts/apply-user-documents.sh 'postgresql://user:pass@host:port/db'"
    echo ""
    exit 1
fi

DATABASE_URL="$1"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SQL_FILE="$SCRIPT_DIR/add-user-documents-table.sql"

echo "📋 Executing SQL script: $SQL_FILE"
echo ""

# Use psql if available, otherwise suggest alternatives
if command -v psql &> /dev/null; then
    psql "$DATABASE_URL" -f "$SQL_FILE"
    echo ""
    echo "✅ Migration applied successfully!"
else
    echo "⚠️  psql not found. Please install PostgreSQL client:"
    echo ""
    echo "macOS:    brew install postgresql"
    echo "Ubuntu:   sudo apt-get install postgresql-client"
    echo ""
    echo "Or use Railway CLI:"
    echo "  railway run psql \$DATABASE_URL -f scripts/add-user-documents-table.sql"
    exit 1
fi
