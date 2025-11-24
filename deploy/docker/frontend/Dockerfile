# Development Dockerfile for React Frontend
FROM node:20-alpine AS development
WORKDIR /app

# Copy package files
COPY src/frontend/package*.json ./
RUN npm ci

# Copy source code
COPY src/frontend/ ./

# Create non-root user
RUN addgroup -g 1001 -S appuser && \
    adduser -S -D -H -u 1001 -h /app -s /sbin/nologin -G appuser -g appuser appuser

# Set permissions
RUN chown -R appuser:appuser /app

# Use non-root user
USER appuser

# Expose port
EXPOSE 5173

# Start development server
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]