# Railway Environment Variables Setup

## Required Environment Variables for API Service

### Database Connection
```bash
# Automatically provided by Railway PostgreSQL service
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
```

### JWT Configuration
```bash
# Generate a secure secret: openssl rand -base64 32
JWT_SECRET=<your-generated-32-char-secret>
JWT_ISSUER=https://hohemaapi-development.up.railway.app
JWT_AUDIENCE=https://hohemaapi-development.up.railway.app
JWT_EXPIRATION_HOURS=24
```

### CORS Configuration
```bash
# Add all frontend domains (comma-separated)
CORS_ORIGINS=https://hohemaweb-development.up.railway.app,https://your-custom-domain.com
```

### ASP.NET Core Configuration
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:$PORT
```

### Optional: WhatsApp Integration
```bash
WhatsApp__AccessToken=<your-whatsapp-access-token>
WhatsApp__PhoneNumberId=933810716485561
WhatsApp__BusinessAccountId=2124933594979327
WhatsApp__VerifyToken=<your-webhook-verify-token>
WhatsApp__ApiVersion=v24.0
```

## How to Set Environment Variables in Railway

### Option 1: Railway Dashboard
1. Go to your Railway project
2. Select the API service
3. Click "Variables" tab
4. Add each variable with its value
5. Click "Deploy" to apply changes

### Option 2: Railway CLI
```bash
# Set individual variables
railway variables set CORS_ORIGINS="https://hohemaweb-development.up.railway.app"
railway variables set JWT_SECRET="your-secret-here"

# Or set multiple at once
railway variables set \
  CORS_ORIGINS="https://hohemaweb-development.up.railway.app" \
  JWT_SECRET="your-secret-here" \
  JWT_ISSUER="https://hohemaapi-development.up.railway.app" \
  JWT_AUDIENCE="https://hohemaapi-development.up.railway.app"
```

## Verification

After setting variables, check the deployment logs:

```bash
railway logs

# Look for these debug messages:
[DEBUG] CORS_ORIGINS: https://hohemaweb-development.up.railway.app
[DEBUG] CORS allowed origins: http://localhost:5173, ..., https://hohemaweb-development.up.railway.app
```

## Production vs Development

For production deployment on `main` branch, use different URLs:

```bash
# Production variables
CORS_ORIGINS=https://hohemaweb-production.up.railway.app,https://www.hohema.com
JWT_ISSUER=https://api.hohema.com
JWT_AUDIENCE=https://api.hohema.com
ASPNETCORE_ENVIRONMENT=Production
```
