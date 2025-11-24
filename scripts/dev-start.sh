#!/bin/bash

echo "🚀 Starting Ho Hema Loans Development Environment"
echo "=================================================="

# Start PostgreSQL and API in Docker
echo "📦 Starting PostgreSQL database..."
docker-compose up -d postgres

echo "⏳ Waiting for PostgreSQL to be healthy..."
sleep 10

echo "🔧 Starting API service..."
docker-compose up -d api

echo "⏳ Waiting for API to start..."
sleep 10

echo "✅ Services Status:"
echo "==================="
docker-compose ps

echo ""
echo "🌐 Service URLs:"
echo "=================="
echo "• Frontend (Local):   http://localhost:5173"
echo "• API (Docker):       http://localhost:5001"
echo "• Swagger UI:         http://localhost:5001/swagger"
echo "• PostgreSQL:         localhost:5432 (hohema_user/hohema_password_2024!)"
echo ""
echo "📊 Database Tables:"
echo "=================="
echo "• Users & Authentication (AspNetUsers)"
echo "• Loan Applications (LoanApplications)"  
echo "• WhatsApp Contacts (WhatsAppContacts)"
echo "• WhatsApp Conversations (WhatsAppConversations)"
echo "• WhatsApp Messages (WhatsAppMessages)"
echo ""
echo "🔧 API Health Check:"
echo "==================="
curl -s http://localhost:5001/api/health | jq .

echo ""
echo "📋 Next Steps:"
echo "==============="
echo "1. Frontend is running locally for optimal development"
echo "2. API and database are containerized for consistency"  
echo "3. Register users via API or frontend"
echo "4. Test WhatsApp communication features via API"
echo "5. Create loan applications through frontend"
echo ""
echo "Ready for development! 🎉"