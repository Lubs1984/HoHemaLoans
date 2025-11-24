#!/bin/bash

# Ho Hema Loans - Docker Development Setup
# This script builds and runs the complete application stack using Docker Compose

echo "🚀 Ho Hema Loans - Starting Docker Development Environment"
echo "============================================================"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check if Docker Compose is available
if ! command -v docker-compose > /dev/null 2>&1; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose and try again."
    exit 1
fi

echo "✅ Docker is running"
echo "✅ Docker Compose is available"
echo ""

# Stop any running containers
echo "🛑 Stopping existing containers..."
docker-compose down --remove-orphans

# Remove old volumes (optional - comment out to preserve data)
# echo "🗑️  Removing old volumes..."
# docker-compose down -v

# Build images
echo "🔨 Building Docker images..."
docker-compose build --no-cache

# Start services
echo "🚀 Starting services..."
docker-compose up -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 10

# Check service health
echo "🏥 Checking service health..."

# Check API health
echo "Checking API service..."
for i in {1..30}; do
    if curl -f http://localhost:5001/api/health > /dev/null 2>&1; then
        echo "✅ API service is healthy"
        break
    else
        echo "⏳ Waiting for API service... ($i/30)"
        sleep 2
    fi
    
    if [ $i -eq 30 ]; then
        echo "❌ API service failed to start"
        docker-compose logs api
        exit 1
    fi
done

# Check Frontend health
echo "Checking Frontend service..."
for i in {1..30}; do
    if curl -f http://localhost:5174 > /dev/null 2>&1; then
        echo "✅ Frontend service is healthy"
        break
    else
        echo "⏳ Waiting for Frontend service... ($i/30)"
        sleep 2
    fi
    
    if [ $i -eq 30 ]; then
        echo "❌ Frontend service failed to start"
        docker-compose logs frontend
        exit 1
    fi
done

echo ""
echo "🎉 Ho Hema Loans is now running!"
echo "============================================================"
echo "🌐 Frontend:    http://localhost:5174"
echo "🔌 API:         http://localhost:5001"
echo "📚 API Docs:    http://localhost:5001/swagger"
echo "💾 Database:    SQLite (persistent volume)"
echo "============================================================"
echo ""
echo "📋 Useful commands:"
echo "  View logs:     docker-compose logs -f"
echo "  Stop services: docker-compose down"
echo "  Restart:       docker-compose restart"
echo "  Shell access:  docker-compose exec api bash"
echo ""
echo "🔍 Service status:"
docker-compose ps