# 🎯 Deploy to Railway - Complete Step-by-Step Guide

## Pre-Deployment Checklist

- [ ] GitHub account ready
- [ ] HoHemaLoans repo pushed to GitHub
- [ ] No uncommitted changes locally
- [ ] Opened Railway.app in browser

---

## 🔴 Step 1: Create Railway Account (2 minutes)

### 1.1 Go to Railway Website
```
URL: https://railway.app
```

### 1.2 Sign Up
- Click "Sign up with GitHub"
- Click "Authorize railway" to allow access
- You're now logged in

### 1.3 Welcome to Dashboard
You should see the Railway dashboard with "New Project" button

---

## 🟠 Step 2: Create New Project (1 minute)

### 2.1 Click "New Project"
- Select "Deploy from GitHub repo"
- Railway will ask for authorization

### 2.2 Select Repository
- Search for "HoHemaLoans"
- Click to select `Lubs1984/HoHemaLoans`

### 2.3 Choose Your Region 🌍 **IMPORTANT**
Railway will ask which region to use:

**RECOMMENDED OPTIONS:**
- **Johannesburg (sa-east-1)** ← **BEST FOR SOUTH AFRICA** (50-80ms)
- **Frankfurt (eu-central-1)** ← Good for Northern Europe (120-150ms)
- **London (eu-west-2)** ← UK/EU alternative

**DO NOT choose US regions** - too slow for South Africa!

See `RAILWAY_REGIONS.md` for complete region guide

### 2.4 Name Your Project
- Name: `HoHemaLoans` (or your preference)
- Click Create

---

## 🟡 Step 3: Add PostgreSQL Database (3 minutes)

### 3.1 Click "+ New Service"
- Select "Database"
- Select "PostgreSQL"
- Railway will create the database

### 3.2 Wait for Database
- You'll see a loading screen
- Database is ready when you see "Connected"
- This takes 2-3 minutes

### 3.3 Copy Connection URL
- Click on PostgreSQL service
- Go to Variables tab
- Find `DATABASE_URL`
- Copy the entire URL (we'll use this later)

**Example format:**
```
postgresql://user:password@host:5432/railway
```

---

## 🟢 Step 4: Deploy API Service (5 minutes)

### 4.1 Click "+ New Service"
- Select "GitHub Repo"
- Choose your HoHemaLoans repo

### 4.2 Configure API Service
In the popup that appears:

```
Root Directory: src/api/HoHemaLoans.Api
Dockerfile Path: ../../Dockerfile.api
Port: 8080
```

### 4.3 Deploy
- Click Deploy
- Railway will start building
- Watch the Logs tab as it builds

### 4.4 Wait for Deployment
- First time takes 5-10 minutes
- You'll see "Deployment successful" in logs
- Green checkmark appears when live

---

## 🔵 Step 5: Deploy Frontend Service (5 minutes)

### 5.1 Click "+ New Service" Again
- Select "GitHub Repo"
- Choose HoHemaLoans repo

### 5.2 Configure Frontend Service
```
Root Directory: src/frontend
Dockerfile Path: ../../Dockerfile.frontend
Port: 3000
```

### 5.3 Deploy
- Click Deploy
- Railway builds and deploys frontend
- Takes 3-5 minutes

---

## 🟣 Step 6: Get Your Service URLs (1 minute)

### 6.1 For API Service
- Click on API service
- Look for a URL ending in `.railway.app`
- Example: `https://hohema-api-prod-xyz.railway.app`
- **Copy this URL**

### 6.2 For Frontend Service
- Click on Frontend service
- Look for a URL ending in `.railway.app`
- Example: `https://hohema-frontend-xyz.railway.app`
- **Copy this URL**

---

## 🔐 Step 7: Configure Environment Variables (5 minutes)

### 7.1 For API Service

Go to API Service → Variables tab

**Add these environment variables:**

| Variable | Value | Notes |
|----------|-------|-------|
| `DATABASE_URL` | `postgresql://user:pass@host:5432/railway` | Copy from PostgreSQL variables |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Exact spelling |
| `ConnectionStrings__DefaultConnection` | `$DATABASE_URL` | With $ prefix |
| `JwtSettings__SecretKey` | `YourVeryLongAndComplexSecretKeyThatIsAtLeast32CharactersLong!` | Secure key for JWT tokens |
| `JwtSettings__Issuer` | `HoHemaLoans` | Exact spelling |
| `JwtSettings__Audience` | `HoHemaLoans` | Exact spelling |
| `ALLOWED_ORIGINS` | `https://hohema-frontend-xyz.railway.app` | Your frontend URL |

### 7.2 For Frontend Service

Go to Frontend Service → Variables tab

**Add this environment variable:**

| Variable | Value |
|----------|-------|
| `VITE_API_URL` | `https://hohema-api-prod-xyz.railway.app/api` |

**⚠️ Important:** 
- Replace `hohema-api-prod-xyz` with your actual API service name
- Include `/api` at the end

---

## 🧪 Step 8: Test Your Deployment (5 minutes)

### 8.1 Test API Health
```bash
curl https://hohema-api-xyz.railway.app/api/health
```

Should return:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-25T10:30:00Z",
  "service": "HoHema Loans API",
  "version": "1.0.0"
}
```

### 8.2 Test Frontend
- Open `https://hohema-frontend-xyz.railway.app` in browser
- Should see login page
- No error messages in browser console

### 8.3 Test Login
```
Email: john.doe@example.com
Password: TestPassword123!
```

Should:
- Show a loading spinner
- Redirect to dashboard
- Display user profile

---

## ✅ Success! You're Live!

Your app is now deployed on Railway and accessible online.

**Your URLs:**
- Frontend: `https://hohema-frontend-xyz.railway.app`
- API: `https://hohema-api-xyz.railway.app`

---

## 🎨 Optional: Add Custom Domain

### 1. Buy a Domain
- Go to Namecheap, GoDaddy, Route53, etc.
- Buy your domain (e.g., `hohema.co.za`)

### 2. Add Domain to Railway
- Go to Railway Project Settings
- Click "Domains"
- Click "Add"
- Enter your domain

### 3. Update DNS Records
- Railway will show DNS instructions
- Update your domain registrar's DNS records
- Wait 24 hours for DNS to propagate

---

## 💾 Automatic Updates

Your app automatically redeploys whenever you:
```bash
git push origin main
```

Changes go live in 5-10 minutes automatically! ✨

---

## 📊 Monitor Your App

### View Logs
```
Railway Dashboard → Service → Logs
```

### Check Deployments
```
Railway Dashboard → Service → Deployments
```

### Monitor Usage
```
Railway Dashboard → Project Settings → Usage
```

---

## 🆘 Troubleshooting

### "API service won't start"
1. Check Logs tab for errors
2. Verify DATABASE_URL is correct
3. Restart service: Deployment → Restart

### "Frontend is blank"
1. Check VITE_API_URL in Variables
2. Must end with `/api`
3. Restart frontend service

### "Login fails"
1. Check API logs for database errors
2. Verify DATABASE_URL works
3. Check if test users exist in database

### "Can't connect to database"
1. Verify DATABASE_URL format
2. Copy exactly from PostgreSQL variables
3. Check PostgreSQL service is running

---

## 📞 Need Help?

- Railway Docs: https://docs.railway.app
- Railway Discord: https://discord.gg/railway
- Check your service Logs for detailed errors

---

**Congratulations! Your app is now live on Railway! 🎉**

Total setup time: ~30 minutes
Monthly cost: ~$15-20 (with $200 free credits = 8+ months free)
