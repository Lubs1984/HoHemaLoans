#!/bin/bash
# Run database migrations on Railway

echo "Running database migrations..."
cd /app
dotnet ef database update --no-build --verbose

echo "Migrations completed!"
