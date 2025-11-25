#!/bin/sh

# Default API URL for development
API_URL="${VITE_API_URL:-http://localhost:8080/api}"

# Create a config file that will be injected into the HTML
cat > /usr/share/nginx/html/config.js << EOF
window.__API_URL__ = '$API_URL';
EOF

# Replace the API base URL in main.*.js files
# This is a workaround since Vite builds are static
find /usr/share/nginx/html -name "main.*.js" -type f -exec sed -i "s|http://localhost:5001/api|$API_URL|g" {} \;
find /usr/share/nginx/html -name "main.*.js" -type f -exec sed -i "s|http://localhost:8080/api|$API_URL|g" {} \;

# Start nginx
exec nginx -g "daemon off;"
