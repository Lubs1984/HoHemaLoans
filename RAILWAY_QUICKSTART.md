# 🚀 Railway Deployment - Quick Start

## ⚡ 5-Minute Setup

### 1. **Go to Railway.app**
   - URL: https://railway.app
   - Click "Sign up with GitHub"
   - Authorize Railway to access your repos

### 2. **Create New Project**
   - Click "New Project"
   - Select "Deploy from GitHub repo"
   - Choose `Lubs1984/HoHemaLoans`
   - Name: `HoHemaLoans`

### 3. **Add Services** (In this order)

#### A. PostgreSQL Database
```
1. Click "+ New Service"
2. Select "Database" → "PostgreSQL"
3. Wait 2-3 minutes for startup
4. Note the connection URL (we'll use this)
```

#### B. API Service
```
1. Click "+ New Service"
2. Select "GitHub Repo"
3. Choose HoHemaLoans repo
4. Set:
   - Root Directory: src/api/HoHemaLoans.Api
   - Dockerfile: ../../Dockerfile.api
   - Port: 8080
5. Click Deploy
```

#### C. Frontend Service
```
1. Click "+ New Service"
2. Select "GitHub Repo"
3. Choose HoHemaLoans repo
4. Set:
   - Root Directory: src/frontend
   - Dockerfile: ../../Dockerfile.frontend
   - Port: 3000
5. Click Deploy
```

### 4. **Configure Environment Variables**

**For API Service:**
```
DATABASE_URL=<PostgreSQL CONNECTION URL>
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=$DATABASE_URL
JwtSettings__SecretKey=YourLongSecretKeyAtLeast32Chars!
JwtSettings__Issuer=HoHemaLoans
JwtSettings__Audience=HoHemaLoans
ALLOWED_ORIGINS=https://<your-frontend-railway-url>
```

**For Frontend Service:**
```
VITE_API_URL=https://<your-api-railway-url>/api
```

### 5. **Wait for Deployment**
- Railway auto-deploys from GitHub
- Watch the logs in each service
- Should be live in 5-10 minutes

### 6. **Test**
```bash
# API Health Check
curl https://<api-url>/api/health

# Frontend
Open https://<frontend-url> in browser
```

---

## 📝 Getting Your URLs

In Railway Dashboard:
- Select each service
- Click "Deployments"
- Find "URL" on the right panel
- Copy the URL

**Examples:**
- API: `https://hohema-api-prod-xyz.railway.app`
- Frontend: `https://hohema-frontend-xyz.railway.app`

---

## ✅ Success Indicators

### API Should Show
```json
{
  "status": "healthy",
  "timestamp": "2025-11-25T10:30:00Z",
  "service": "HoHema Loans API",
  "version": "1.0.0"
}
```

### Frontend Should Load
- Login page visible
- No console errors
- Can click "Sign in" button

### Login Should Work
```
Email: john.doe@example.com
Password: TestPassword123!
```

---

## 🔧 Troubleshooting

### "API not responding"
```
1. Check Logs in Railway
2. Ensure DATABASE_URL is set
3. Restart API service
```

### "Can't connect to database"
```
1. Copy PostgreSQL CONNECTION_URL exactly
2. Set as DATABASE_URL variable
3. Restart API
```

### "Frontend blank screen"
```
1. Check VITE_API_URL is correct
2. Check API URL includes /api at end
3. Refresh browser
```

### "Login fails"
```
1. Check API logs for database errors
2. Verify test users were created
3. Check database CONNECTION_URL
```

---

## 💰 Pricing

- **Free tier**: $200 credit (lasts ~8 months)
- **After free tier**: ~$15-20/month
- **No setup fees**
- **Cancel anytime**

---

## 🎯 Next Steps

1. ✅ Get live URL from Railway
2. ✅ Update GitHub repo with urls (optional)
3. ✅ Configure custom domain (optional)
4. ✅ Set up WhatsApp webhook
5. ✅ Configure analytics

---

## 📚 Helpful Links

- Railway Docs: https://docs.railway.app
- Railway GitHub Integration: https://docs.railway.app/guides/github
- Railway Environment Variables: https://docs.railway.app/deploy/config#environment-variables
- Railway Domains: https://docs.railway.app/guides/custom-domains

---

## 🆘 Getting Help

If something goes wrong:

1. **Check Logs**: Railway Dashboard → Service → Logs tab
2. **Restart Service**: Railway Dashboard → Deployment → Restart
3. **Check Variables**: Railway Dashboard → Variables tab
4. **Railway Support**: https://railway.app/support

---

**That's it! You're deployed on Railway! 🎉**
