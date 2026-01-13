# Railway Debugging Checklist

## Current Issue: 502 Application Failed to Respond

### Things to Check in Railway Dashboard:

1. **Deployment Logs**
   - Go to Railway Dashboard → Your Project → API Service → Deployments
   - Click on the latest deployment
   - Check "Deploy Logs" for build errors
   - Check "Runtime Logs" for startup messages

2. **Look for These Startup Messages:**
   ```
   [STARTUP] HoHema Loans API Starting...
   [STARTUP] Environment: Production
   [STARTUP] PORT: <number>
   [STARTUP] Binding: http://0.0.0.0:<port>
   [STARTUP] ✅ Application started successfully!
   ```

3. **Check Environment Variables**
   - Go to Variables tab in Railway
   - Verify these are set:
     - `DATABASE_URL` (automatically set by PostgreSQL service)
     - `ASPNETCORE_ENVIRONMENT=Production`
     - `ASPNETCORE_URLS=http://0.0.0.0:$PORT`
     - `Railway__SkipHttpsRedirection=true`
     - `CORS_ORIGINS=https://hohemaweb-development.up.railway.app`

4. **Database Connection**
   - Ensure PostgreSQL service is running
   - Check if DATABASE_URL variable is linked from Postgres service
   - Format should be: `postgresql://user:pass@host:port/db`

5. **Port Issues**
   - Railway assigns a random PORT (e.g., 8080, 3000, etc.)
   - Our app should bind to `$PORT` environment variable
   - Check logs for "Binding to Railway PORT: XXXX"

### Common Fixes:

#### If DATABASE_URL is missing:
1. Go to Railway Dashboard
2. Add PostgreSQL service if not present
3. Link DATABASE_URL variable from Postgres to API service

#### If app crashes on startup:
1. Check runtime logs for error messages
2. Look for database connection errors
3. Check if migrations are hanging

#### If port binding fails:
1. Verify ASPNETCORE_URLS is set correctly
2. Check if PORT environment variable is available
3. Restart the service

### Manual Restart:
If fixes are applied, restart the service:
1. Railway Dashboard → API Service
2. Settings → Restart Service
3. Or: Deployments → Click latest → Redeploy

### Test Locally:
To test if the issue is Railway-specific:
```bash
# Set environment variables
export DATABASE_URL="postgresql://user:pass@localhost:5432/hohema_loans"
export PORT=8080
export ASPNETCORE_ENVIRONMENT=Production

# Run the app
cd src/api/HoHemaLoans.Api
dotnet run

# Test health endpoint
curl http://localhost:8080/health
```

### Contact Railway Support:
If nothing works, check:
- Railway Status: https://status.railway.app
- Railway Discord: https://discord.gg/railway
- Provide: Project ID, Service name, Deployment ID from logs
