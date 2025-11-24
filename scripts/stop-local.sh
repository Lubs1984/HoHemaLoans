#!/bin/bash

# Stop local development servers script
echo "🛑 Stopping local development servers..."

# Kill processes on common ports
echo "Stopping processes on port 5001 (API)..."
lsof -ti:5001 | xargs kill -9 2>/dev/null || true

echo "Stopping processes on port 5173 (Frontend)..."
lsof -ti:5173 | xargs kill -9 2>/dev/null || true

echo "Stopping processes on port 5174 (Frontend alt)..."
lsof -ti:5174 | xargs kill -9 2>/dev/null || true

echo "✅ Local servers stopped"