# Railway Deployment Guide

## Prerequisites
- GitHub account with HoHemaLoans repo
- Railway account (https://railway.app)
- $200 free credits (lasts ~7-8 months)

## Step-by-Step Deployment

### 1. Create Railway Project

```bash
# Via Railway Dashboard:
# 1. Go to https://railway.app/dashboard
# 2. Click "New Project"
# 3. Select "Deploy from GitHub repo"
# 4. Select Lubs1984/HoHemaLoans repo
# 5. Name project "HoHemaLoans"
```

### 2. Create PostgreSQL Database Service

In Railway Dashboard:
```
1. Click "+ New" → "Database" → "PostgreSQL"
2. Wait for database to start (2-3 minutes)
3. Copy CONNECTION_URL from Variables
```

### 3. Add Environment Variables

In Railway project settings → Variables:

```
# Database
DATABASE_URL=<PostgreSQL CONNECTION_URL from previous step>

# API (for production)
ASPNETCORE_ENVIRONMENT=Production
JwtSettings__SecretKey=YourVeryLongAndComplexSecretKeyThatIsAtLeast32CharactersLong2024!
JwtSettings__Issuer=HoHemaLoans
JwtSettings__Audience=HoHemaLoans

# API CORS (update with your Railway domain)
ALLOWED_ORIGINS=https://hohema-frontend-xyz.railway.app

# WhatsApp (get from Meta Developer)
WhatsApp__AccessToken=your_access_token
WhatsApp__PhoneNumberId=your_phone_number_id
WhatsApp__WebhookVerifyToken=your_webhook_verify_token
WhatsApp__BusinessAccountId=your_business_account_id

# Connection String for .NET
ConnectionStrings__DefaultConnection=$DATABASE_URL
```

### 4. Deploy API Service

In Railway Dashboard:
```
1. Click "+ New Service"
2. Select "GitHub Repo"
3. Select your HoHemaLoans repo
4. DO NOT set Root Directory - leave it blank!
5. Configure in the next screen:
   - Build Context: src/api/HoHemaLoans.Api
   - Dockerfile: Dockerfile (just the filename)
   - Port: 8080
6. Click Deploy
```

**Alternative - Set these correctly:**
- **Root Directory:** src/api/HoHemaLoans.Api
- **Dockerfile Name:** Dockerfile (Railway will find it in the root directory)

### 5. Deploy Frontend Service

In Railway Dashboard:
```
1. Click "+ New Service"
2. Select "GitHub Repo"
3. Select your HoHemaLoans repo
4. DO NOT set Root Directory - leave it blank!
5. Configure in the next screen:
   - Build Context: src/frontend
   - Dockerfile: Dockerfile (just the filename)
   - Port: 3000
6. Click Deploy
```

**Alternative - Set these correctly:**
- **Root Directory:** src/frontend
- **Dockerfile Name:** Dockerfile (Railway will find it in the root directory)

### 6. Configure Frontend API URL

Add to Railway Variables for Frontend Service:
```
VITE_API_URL=https://api-service-name.railway.app/api
```

(Replace api-service-name with your actual API service URL from Railway)

### 7. Set Up Custom Domain (Optional)

```
1. Purchase domain from Namecheap, GoDaddy, etc.
2. In Railway → Project Settings → Domains
3. Add custom domain
4. Update DNS records as instructed by Railway
```

## Verification

### Check API Health
```bash
curl https://api-service-name.railway.app/api/health
```

### Check Frontend
```bash
# Visit in browser
https://frontend-service-name.railway.app
```

### View Logs
```
1. Go to Railway Dashboard
2. Select Service
3. Click "Logs" tab
4. Watch real-time deployment logs
```

## Troubleshooting

### API Container Won't Start
```
1. Check Logs tab for error
2. Verify DATABASE_URL is correct
3. Ensure connection string format: 
   postgresql://user:password@host:5432/database
4. Restart service
```

### Frontend Can't Reach API
```
1. Check VITE_API_URL in variables
2. Verify API service is running
3. Check CORS settings in API
4. Update ALLOWED_ORIGINS in API variables
```

### Database Connection Error
```
1. Verify PostgreSQL service is running
2. Check DATABASE_URL in variables
3. Run health check on API to test DB connection
```

## Cost Breakdown

| Service | Cost | Notes |
|---------|------|-------|
| PostgreSQL | $5/mo | Managed database |
| API Service | $5-10/mo | Depends on usage |
| Frontend Service | $2-5/mo | Static site |
| **Total** | **$12-20/mo** | Very cheap! |

With $200 free credits, you have ~10 months free!

## Next Steps After Deployment

1. **Configure WhatsApp Webhook**
   - Get credentials from Meta Developer
   - Set webhook URL to: `https://api-service-name.railway.app/api/webhooks/whatsapp`
   - Verify webhook token

2. **Set Up Custom Domain**
   - Point domain DNS to Railway
   - Enable HTTPS (automatic)

3. **Enable Monitoring**
   - Watch real-time logs
   - Set up alerts for errors

4. **Backup Database**
   - Railway auto-backups daily
   - No additional setup needed

## Useful Commands

```bash
# SSH into API service
railway connect api

# View environment variables
railway variables ls

# Update environment variable
railway variables set KEY=VALUE

# Deploy specific branch
railway deploy --branch main

# Check deployment status
railway status
```

## Important Notes

- ⚠️ Railway automatically redeploys on `git push` to main
- ✅ HTTPS is automatic for all services
- ✅ Database backups are daily and automatic
- ✅ Scaling is automatic based on load
- 💾 You have 10 months with free credits
- 🔄 After free credits, ~$12-20/month

## Support

- Railway Docs: https://docs.railway.app
- Railway Discord: https://discord.gg/railway
- Railway Status: https://status.railway.app
