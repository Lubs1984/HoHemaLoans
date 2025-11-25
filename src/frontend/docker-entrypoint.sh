#!/bin/sh

set -e

# Default API URL for development
API_URL="${VITE_API_URL:-http://localhost:8080/api}"

echo "Starting nginx frontend service..."
echo "API URL: $API_URL"

# Create a runtime config file that will be injected into index.html
cat > /usr/share/nginx/html/config.js << EOF
// Runtime configuration - set by docker-entrypoint.sh
window.__API_URL__ = '$API_URL';
console.log('API URL set to:', window.__API_URL__);
EOF

echo "Config file created successfully"

# Check if nginx is available
which nginx || echo "WARNING: nginx not found in PATH"

# Start nginx in foreground
exec nginx -g "daemon off;"
