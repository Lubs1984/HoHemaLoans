#!/bin/sh

# Default API URL for development
API_URL="${VITE_API_URL:-http://localhost:8080/api}"

# Create a runtime config file that will be injected into index.html
cat > /usr/share/nginx/html/config.js << EOF
// Runtime configuration - set by docker-entrypoint.sh
window.__API_URL__ = '$API_URL';
console.log('API URL set to:', window.__API_URL__);
EOF

# Start nginx
exec nginx -g "daemon off;"
