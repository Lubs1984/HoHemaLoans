#!/bin/bash

# Railway Migration Script
# This script runs EF Core migrations on Railway's PostgreSQL database

set -e

echo "🚀 Running migrations on Railway database..."
echo ""

# Change to API directory
cd "$(dirname "$0")/../src/api/HoHemaLoans.Api"

# Link to the API service and run migrations
railway link --service HoHemaApi
railway run dotnet ef database update

echo ""
echo "✅ Migrations applied successfully!"
