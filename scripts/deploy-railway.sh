#!/bin/bash
# Railway deployment helper script

set -e

echo "🚀 HoHema Loans - Railway Deployment Helper"
echo "============================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if Railway CLI is installed
if ! command -v railway &> /dev/null; then
    echo -e "${YELLOW}⚠️  Railway CLI not found. Installing...${NC}"
    npm install -g @railway/cli
fi

echo -e "${BLUE}Step 1: Login to Railway${NC}"
railway login

echo ""
echo -e "${BLUE}Step 2: Create/Select Project${NC}"
railway init

echo ""
echo -e "${BLUE}Step 3: Getting Project Info${NC}"
PROJECT_ID=$(railway status | grep "Project:" | awk '{print $2}')
echo -e "${GREEN}✓ Project ID: $PROJECT_ID${NC}"

echo ""
echo -e "${BLUE}Step 4: Checking Services${NC}"
echo "Services in your project:"
railway service list

echo ""
echo -e "${YELLOW}📋 Manual Steps Needed:${NC}"
echo ""
echo "1. Go to https://railway.app/dashboard"
echo "2. Select your HoHemaLoans project"
echo ""
echo "3. Create/verify these services exist:"
echo "   - PostgreSQL (database)"
echo "   - API (from Dockerfile.api)"
echo "   - Frontend (from Dockerfile.frontend)"
echo ""
echo "4. Set environment variables:"
echo ""
echo "   For API Service:"
echo "   ├── DATABASE_URL=<PostgreSQL URL>"
echo "   ├── ASPNETCORE_ENVIRONMENT=Production"
echo "   ├── ConnectionStrings__DefaultConnection=\$DATABASE_URL"
echo "   ├── JwtSettings__SecretKey=<your-secret>"
echo "   └── ALLOWED_ORIGINS=<frontend-url>"
echo ""
echo "   For Frontend Service:"
echo "   └── VITE_API_URL=<api-url>/api"
echo ""
echo "5. Watch deployments:"
echo "   railway logs"
echo ""
echo "6. Get your URLs:"
echo "   railway open web"
echo ""
echo -e "${GREEN}✅ Setup complete!${NC}"
echo ""
echo "More info: https://docs.railway.app"
