#!/bin/sh

set -e

# Default API URL for development
API_URL="${VITE_API_URL:-http://localhost:8080/api}"

echo "Starting nginx frontend service..."
echo "API URL: $API_URL"

# Ensure directory exists and is writable
NGINX_HTML="/usr/share/nginx/html"
if [ ! -d "$NGINX_HTML" ]; then
    echo "ERROR: $NGINX_HTML does not exist"
    exit 1
fi

# Create a runtime config file that will be injected into index.html
CONFIG_FILE="$NGINX_HTML/config.js"
cat > "$CONFIG_FILE" << EOF
// Runtime configuration - set by docker-entrypoint.sh
window.__API_URL__ = '$API_URL';
console.log('API URL set to:', window.__API_URL__);
EOF

# Verify file was created
if [ -f "$CONFIG_FILE" ]; then
    echo "✓ Config file created successfully at $CONFIG_FILE"
    echo "  File size: $(wc -c < "$CONFIG_FILE") bytes"
else
    echo "ERROR: Failed to create config.js"
    exit 1
fi

echo "Starting nginx..."

# Start nginx in foreground
exec nginx -g "daemon off;"
