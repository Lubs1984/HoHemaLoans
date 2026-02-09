# Railway Deployment Guide

## Overview
This guide covers deploying the HoHema Loans application to Railway.app with PostgreSQL database.

## Prerequisites
- Railway account (https://railway.app)
- GitHub repository connected to Railway
- Railway CLI (optional): `npm install -g @railway/cli`

## Quick Deploy

### 1. Create Railway Project
```bash
# Option A: Using Railway Dashboard
1. Go to https://railway.app/new
2. Select "Deploy from GitHub repo"
3. Connect your GitHub account and select the HoHemaLoans repository
4. Railway will detect the configuration automatically

# Option B: Using Railway CLI
railway login
railway init
railway up
```

### 2. Add PostgreSQL Database
```bash
# In Railway Dashboard:
1. Click "New" → "Database" → "Add PostgreSQL"
2. Railway automatically creates DATABASE_URL variable

# Using CLI:
railway add --database postgresql
```

### 3. Configure Environment Variables

In Railway Dashboard, add these variables to your API service:

#### Required Variables:
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
JWT_SECRET=<generate-secure-random-string>
JWT_ISSUER=https://hohema-api.railway.app
JWT_AUDIENCE=https://hohema-api.railway.app
```

#### Optional Variables:
```
CORS_ORIGINS=https://your-frontend.railway.app
WHATSAPP_API_URL=<your-whatsapp-api>
WHATSAPP_API_TOKEN=<your-token>
```

### 4. Run Database Migrations

After first deployment:

```bash
# Using Railway CLI:
railway run dotnet ef database update

# Or connect to the service and run:
railway shell
dotnet ef database update
```

## Service Configuration

### API Service (Port 8080)
- **Build**: Uses Dockerfile.api
- **Framework**: .NET 8.0
- **Health Check**: `/health` endpoint
- **Auto-deploys**: On push to main branch

### Database Service
- **Type**: PostgreSQL 15+
- **Automatic backups**: Enabled
- **Connection**: Automatically injected via DATABASE_URL

## Environment Variable Reference

### Connection Strings
```bash
# Railway automatically provides DATABASE_URL in this format:
# postgresql://user:password@host:port/database

# Convert to .NET format in your code or use directly:
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
```

### JWT Configuration
```bash
# Generate a secure secret (minimum 32 characters):
openssl rand -base64 32

JWT_SECRET=<your-generated-secret>
JWT_ISSUER=https://your-api-domain.railway.app
JWT_AUDIENCE=https://your-api-domain.railway.app
JWT_EXPIRATION_HOURS=24
```

### CORS Configuration
```bash
# Allow your frontend domain:
CORS_ORIGINS=https://your-frontend.railway.app,https://www.yourdomain.com
```

## Deployment Workflow

### Automatic Deployment
1. Push code to GitHub main branch
2. Railway automatically detects changes
3. Builds Docker image using Dockerfile.api
4. Runs health checks
5. Deploys new version with zero downtime

### Manual Deployment
```bash
# Using Railway CLI:
railway up

# Or trigger from dashboard:
# Project → Deployments → Deploy
```

## Post-Deployment Tasks

### 1. Verify API Health
```bash
curl https://your-api.railway.app/health
```

### 2. Run Migrations
```bash
railway run --service api dotnet ef database update
```

### 3. Seed Initial Data (Optional)
```bash
# For development/testing only:
curl -X POST https://your-api.railway.app/api/testdata/seed
```

### 4. Create Admin User
```bash
# Use your admin registration endpoint:
curl -X POST https://your-api.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@hohema.com",
    "password": "SecurePassword123!",
    "firstName": "Admin",
    "lastName": "User"
  }'
```

## Monitoring & Logs

### View Logs
```bash
# Using CLI:
railway logs

# Or in dashboard:
# Project → Service → Logs
```

### Monitor Metrics
- Dashboard shows CPU, Memory, Network usage
- Set up alerts for high resource usage
- Monitor deployment success/failure

## Troubleshooting

### Common Issues

#### 1. Database Connection Errors
```bash
# Verify DATABASE_URL is set:
railway variables

# Test connection:
railway run psql $DATABASE_URL
```

#### 2. Migration Failures
```bash
# Check current migration status:
railway run dotnet ef migrations list

# Reset database (WARNING: destroys data):
railway run dotnet ef database drop
railway run dotnet ef database update
```

#### 3. .NET Runtime Missing
- Ensure Dockerfile.api uses `mcr.microsoft.com/dotnet/aspnet:8.0`
- Verify .csproj has `<TargetFramework>net8.0</TargetFramework>`

#### 4. Port Binding Issues
```bash
# Railway provides $PORT variable automatically
# Ensure appsettings.json or Dockerfile sets:
ASPNETCORE_URLS=http://+:$PORT
```

### Health Check Debugging
```bash
# Test health endpoint locally:
curl http://localhost:8080/health

# Check Railway health check logs:
railway logs --service api | grep health
```

## Scaling & Performance

### Vertical Scaling
- Railway automatically allocates resources
- Upgrade plan for more CPU/Memory
- Monitor usage in dashboard

### Horizontal Scaling
```bash
# Scale to multiple replicas (paid plans):
railway scale --replicas 2
```

### Database Optimization
- Enable connection pooling in appsettings.json
- Add indexes for frequently queried fields
- Use Railway's automatic backups

## Security Best Practices

### 1. Environment Variables
- Never commit secrets to git
- Use Railway's encrypted variable storage
- Rotate JWT_SECRET regularly

### 2. Database Security
- Railway handles PostgreSQL security
- Use strong passwords
- Enable SSL connections

### 3. API Security
- Enable HTTPS only (Railway provides SSL)
- Configure CORS properly
- Implement rate limiting
- Use JWT authentication

### 4. Monitoring
```bash
# Set up alerts for:
- Failed deployments
- High error rates
- Database connection issues
- Memory/CPU spikes
```

## Cost Optimization

### Free Tier Limits
- $5 free credit per month
- Hobby plan: $5/month per service
- Monitor usage in dashboard

### Tips
- Use single environment for staging
- Clean up unused services
- Optimize Docker image size
- Enable caching in CI/CD

## CI/CD Integration

Railway automatically deploys on git push. For more control:

### GitHub Actions Example
```yaml
name: Deploy to Railway
on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Deploy to Railway
        uses: railway/cli@v3
        with:
          service: api
        env:
          RAILWAY_TOKEN: ${{ secrets.RAILWAY_TOKEN }}
```

## Rollback Procedure

### Using Dashboard
1. Go to Deployments tab
2. Find previous successful deployment
3. Click "Redeploy"

### Using CLI
```bash
# List deployments:
railway deployment list

# Rollback to specific deployment:
railway deployment rollback <deployment-id>
```

## Backup & Recovery

### Database Backups
```bash
# Railway auto-backups on paid plans
# Manual backup:
railway run pg_dump $DATABASE_URL > backup.sql

# Restore:
railway run psql $DATABASE_URL < backup.sql
```

### Configuration Backup
- Export environment variables regularly
- Keep copy of railway.toml in git
- Document custom configurations

## Support & Resources

- Railway Docs: https://docs.railway.app
- Railway Discord: https://discord.gg/railway
- Status Page: https://status.railway.app
- GitHub Issues: https://github.com/railwayapp/railway

## Next Steps

After successful deployment:
1. Set up custom domain (optional)
2. Configure monitoring/alerting
3. Set up staging environment
4. Implement backup strategy
5. Document deployment process for team
6. Set up frontend deployment (separate guide)
