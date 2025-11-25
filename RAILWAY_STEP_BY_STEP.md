# 🚀 Railway Deployment - EXACT Step-by-Step Instructions

## START HERE - From Repository Selection

---

## 📍 STEP 1: You've Selected the Repo

**Current state:** You clicked "Deploy from GitHub" and selected `Lubs1984/HoHemaLoans`

**What you see:** Repo is selected, Railway asking what to do next

---

## ✅ STEP 2: Create PostgreSQL Database First

### ACTION:
1. **Click: "+ New"** (bottom right area)
2. **Click: "Database"**
3. **Click: "PostgreSQL"**
4. **Click: "Create"**

### WHAT HAPPENS:
- PostgreSQL starts initializing
- Takes 2-3 minutes
- You'll see a loading screen
- Then you see "PostgreSQL" service in your dashboard

### Wait for it to say: "Connected" ✓

---

## ✅ STEP 3: Get PostgreSQL Connection String

### ACTION:
1. **Click: "PostgreSQL"** (the service you just created)
2. **Click: "Variables"** tab (top menu)
3. **Find: "DATABASE_URL"**
4. **Copy the entire value**

### WHAT IT LOOKS LIKE:
```
DATABASE_URL
postgresql://postgres:RandomPassword123@container-hostname:5432/railway
```
postgresql://postgres:oMqnhfiIfJkDWZWCsxIYDCiRsjSQAIKG@postgres.railway.internal:5432/railway

### SAVE THIS SOMEWHERE - You need it later! 📝

---

## ✅ STEP 4: Add API Service

### ACTION:
1. **Click: "+ New"** button again
2. **Click: "GitHub Repo"** (NOT "Docker Image")
3. **Select: HoHemaLoans** repo
4. **Click: "Continue"** or next

### CONFIGURATION SCREEN:

You'll see a form like:
```
┌────────────────────────────────────┐
│ Configure Deploy                   │
├────────────────────────────────────┤
│ Root Directory:  _________________ │
│ Dockerfile:      _________________ │
│ Port:            _________________ │
└────────────────────────────────────┘
```

### FILL IN EXACTLY:

```
Root Directory:  src/api/HoHemaLoans.Api
Dockerfile:      Dockerfile
Port:            8080
```

**COPY-PASTE THESE VALUES - Don't make up paths!**

### CLICK: "Deploy" Button

---

## ✅ STEP 5: Wait for API to Deploy

### WHAT YOU'LL SEE:
```
[Deployment Status]
Building... 
├─ Downloading base image
├─ Restoring packages
├─ Building project
├─ Publishing...
└─ Starting container
```

### Takes: 5-10 minutes first time

### When done you'll see: ✓ Healthy

---

## ✅ STEP 6: Add Frontend Service

### ACTION:
1. **Click: "+ New"** again
2. **Click: "GitHub Repo"**
3. **Select: HoHemaLoans** repo
4. **Click: Continue**

### CONFIGURATION SCREEN:

```
┌────────────────────────────────────┐
│ Configure Deploy                   │
├────────────────────────────────────┤
│ Root Directory:  _________________ │
│ Dockerfile:      _________________ │
│ Port:            _________________ │
└────────────────────────────────────┘
```

### FILL IN EXACTLY:

```
Root Directory:  src/frontend
Dockerfile:      Dockerfile
Port:            3000
```

### CLICK: "Deploy" Button

---

## ✅ STEP 7: Wait for Frontend to Deploy

### Takes: 3-5 minutes

### When done you'll see: ✓ Healthy

---

## ✅ STEP 8: Get Your Service URLs

### FOR API SERVICE:

1. **Click: "api"** service
2. **Look at right panel**
3. **Find: URL** (looks like: `https://hohema-api-xyz.railway.app`)
4. **Copy this URL** 📋

### FOR FRONTEND SERVICE:

1. **Click: "frontend"** service
2. **Look at right panel**
3. **Find: URL** (looks like: `https://hohema-frontend-xyz.railway.app`)
4. **Copy this URL** 📋

---

## ✅ STEP 9: Set Environment Variables for API

### ACTION:
1. **Click: "api"** service
2. **Click: "Variables"** tab
3. **Click: "+ Add"** button

### ADD THESE VARIABLES (one by one):

**Variable 1:**
```
Key:   DATABASE_URL
Value: (paste the PostgreSQL DATABASE_URL you copied earlier)
```

**Variable 2:**
```
Key:   ASPNETCORE_ENVIRONMENT
Value: Production
```

**Variable 3:**
```
Key:   JwtSettings__SecretKey
Value: YourVeryLongAndComplexSecretKeyThatIsAtLeast32CharactersLong2024!
```

**Variable 4:**
```
Key:   JwtSettings__Issuer
Value: HoHemaLoans
```

**Variable 5:**
```
Key:   JwtSettings__Audience
Value: HoHemaLoans
```

**Variable 6:**
```
Key:   FRONTEND_URL
Value: https://hohema-frontend-xyz.railway.app
```
(Replace `hohema-frontend-xyz` with your ACTUAL frontend URL from Step 8)

### AFTER EACH VARIABLE: Click "Add" or press Enter

---

## ✅ STEP 10: Set Environment Variable for Frontend

### ACTION:
1. **Click: "frontend"** service
2. **Click: "Variables"** tab
3. **Click: "+ Add"**

### ADD THIS VARIABLE:

```
Key:   VITE_API_URL
Value: https://hohema-api-xyz.railway.app/api
```
(Replace `hohema-api-xyz` with your ACTUAL API URL from Step 8)

**IMPORTANT: Include `/api` at the end!**

---

## ✅ STEP 11: Services Will Auto-Restart

After you add variables, services will restart automatically.

**Wait 2-3 minutes for restart to complete.**

---

## ✅ STEP 12: Test Everything

### TEST 1: API Health Check

```bash
curl https://your-api-url.railway.app/api/health
```

Should return:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-25T10:00:00Z",
  "service": "HoHema Loans API"
}
```

### TEST 2: Open Frontend in Browser

Open: `https://your-frontend-url.railway.app`

Should see: **Login page**

### TEST 3: Try Logging In

```
Email:    john.doe@example.com
Password: TestPassword123!
```

Should see: **Dashboard page**

---

## 🎉 YOU'RE DONE!

Your app is now LIVE on Railway! 🚀

**Your URLs:**
- Frontend: https://hohema-frontend-xyz.railway.app
- API: https://hohema-api-xyz.railway.app

---

## 📋 Complete Checklist

- [ ] PostgreSQL created and running
- [ ] DATABASE_URL copied and saved
- [ ] API service deployed
- [ ] Frontend service deployed  
- [ ] API URLs obtained
- [ ] Frontend URL obtained
- [ ] Database variables set in API service
- [ ] Frontend API URL set in Frontend service
- [ ] Services restarted successfully
- [ ] API health check works
- [ ] Frontend loads in browser
- [ ] Can log in with test credentials

---

## 🆘 If Something Fails

### API won't deploy?
1. Check Logs tab for error
2. Look for red error messages
3. Most common: DATABASE_URL format wrong
4. Try deleting and recreating service

### Frontend shows blank page?
1. Check browser console for errors
2. Verify VITE_API_URL is set correctly
3. Make sure API URL includes `/api` at end
4. Try hard refresh: Cmd+Shift+R (Mac) or Ctrl+Shift+R (Windows)

### Can't log in?
1. Check API logs for database connection errors
2. Verify DATABASE_URL is correct
3. Check API health endpoint works

### Still stuck?
1. Check logs in each service
2. Copy error message
3. Google: error message + "Railway"
4. Contact Railway Discord: https://discord.gg/railway

---

## ✨ Key Reminders

- ✅ Build Context = directory (e.g., `src/frontend`)
- ✅ Dockerfile = just the filename (`Dockerfile`)
- ✅ Ports: API=8080, Frontend=3000, Database=5432
- ✅ Variables are CASE SENSITIVE
- ✅ `$DATABASE_URL` means use the variable value
- ✅ Service names matter: "api", "frontend", "PostgreSQL"
- ✅ After changing variables, services auto-restart

---

**That's it! Follow these steps exactly and you'll be live! 🎉**
