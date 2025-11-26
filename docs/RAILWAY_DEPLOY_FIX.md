# 🚀 Railway Deployment - Step by Step (WORKING)

## The Problem You're Experiencing

```
[Region: europe-west4]
Dockerfile `Dockerfile` does not exist
```

**Why this happens:** Railway can't find the Dockerfile because the path is wrong.

**Solution:** Configure the service build context correctly.

---

## ✅ CORRECT WAY TO DEPLOY

### Step 1: Go to Railway Dashboard
```
URL: https://railway.app/dashboard
Already logged in? ✓
```

### Step 2: Create New Project
```
1. Click: "New Project"
2. Select: "Deploy from GitHub repo"
3. Choose: Lubs1984/HoHemaLoans
4. Region: Select "europe-west4" (or your preferred region)
5. Create Project
```

### Step 3: Add PostgreSQL Database
```
1. Click: "+ New"
2. Select: "Database"
3. Choose: "PostgreSQL"
4. Click: "Create"
5. Wait: 2-3 minutes for database to initialize
```

Once PostgreSQL is running:
- Click the PostgreSQL service
- Go to: "Variables" tab
- Copy: DATABASE_URL (you'll need this)

### Step 4: Add API Service (CORRECT SETTINGS)

**VERY IMPORTANT - Follow exactly:**

```
1. Click: "+ New"
2. Select: "GitHub Repo"
3. Repository: HoHemaLoans
4. IMPORTANT - Configuration page shows:
   
   ┌─────────────────────────────────┐
   │ Build Context: src/api/...Api   │ ← SET THIS
   │ Dockerfile: Dockerfile          │ ← JUST "Dockerfile"
   │ Port: 8080                      │ ← SET THIS
   └─────────────────────────────────┘
   
5. Click: "Deploy"
```

**Critical Points:**
- ✅ Build Context = `src/api/HoHemaLoans.Api`
- ✅ Dockerfile = `Dockerfile` (just the name, not a path)
- ✅ Port = `8080`
- ❌ Do NOT include `../../Dockerfile.api`
- ❌ Do NOT use root directory for Dockerfile path

### Step 5: Add Frontend Service (CORRECT SETTINGS)

```
1. Click: "+ New"
2. Select: "GitHub Repo"
3. Repository: HoHemaLoans
4. Configuration:
   
   ┌──────────────────────────────┐
   │ Build Context: src/frontend  │ ← SET THIS
   │ Dockerfile: Dockerfile       │ ← JUST "Dockerfile"
   │ Port: 3000                   │ ← SET THIS
   └──────────────────────────────┘
   
5. Click: "Deploy"
```

**Critical Points:**
- ✅ Build Context = `src/frontend`
- ✅ Dockerfile = `Dockerfile` (just the name)
- ✅ Port = `3000`

### Step 6: Wait for Builds (5-10 minutes)
```
PostgreSQL: Healthy ✓
API: Building... → Healthy ✓
Frontend: Building... → Healthy ✓
```

Watch the logs to see build progress.

### Step 7: Set Environment Variables

**For API Service:**

Click on API service → Variables tab → Add these:

```
DATABASE_URL             = <PostgreSQL CONNECTION_URL>
ASPNETCORE_ENVIRONMENT  = Production
ConnectionStrings__DefaultConnection = $DATABASE_URL
JwtSettings__SecretKey  = YourVeryLongAndComplexSecretKeyThatIsAtLeast32CharactersLong!
JwtSettings__Issuer     = HoHemaLoans
JwtSettings__Audience   = HoHemaLoans
ALLOWED_ORIGINS         = https://<your-frontend-url>.railway.app
```

**For Frontend Service:**

Click on Frontend service → Variables tab → Add:

```
VITE_API_URL = https://<your-api-url>.railway.app/api
```

### Step 8: Get Your URLs

After deployment completes:

**API Service:**
- Click "API" service
- Look for: URL (in the right panel or "Deployments" tab)
- Looks like: `https://hohema-api-prod-xyz.railway.app`

**Frontend Service:**
- Click "Frontend" service  
- Look for: URL
- Looks like: `https://hohema-frontend-xyz.railway.app`

### Step 9: Test Everything

```bash
# Test API Health
curl https://your-api-url.railway.app/api/health

# Open Frontend
https://your-frontend-url.railway.app

# Try Login
Email: john.doe@example.com
Password: TestPassword123!
```

---

## 🔧 If You Get the Dockerfile Error Again

### Error Message:
```
Dockerfile `Dockerfile` does not exist
```

### Fix Checklist:

1. **Check Build Context is Set**
   ```
   API:      src/api/HoHemaLoans.Api
   Frontend: src/frontend
   ```

2. **Check Dockerfile Name**
   ```
   MUST be: Dockerfile
   NOT:     Dockerfile.api
   NOT:     Dockerfile.frontend
   NOT:     ../../Dockerfile.api
   ```

3. **Check Both Dockerfiles Exist**
   ```bash
   # Verify files exist
   ls -la src/api/HoHemaLoans.Api/Dockerfile
   ls -la src/frontend/Dockerfile
   ```

4. **Delete & Recreate Service**
   ```
   1. Click service
   2. Settings tab
   3. Delete service
   4. Create new one with CORRECT settings
   ```

5. **Check Logs**
   ```
   Click service → Logs tab
   Look for error messages
   Red error = problem
   ```

---

## 📋 Complete Checklist Before Deploy

- [ ] GitHub repo up to date: `git push origin main`
- [ ] Dockerfiles exist:
  - [ ] `src/api/HoHemaLoans.Api/Dockerfile` ✓
  - [ ] `src/frontend/Dockerfile` ✓
  - [ ] `src/frontend/nginx.conf` ✓
- [ ] railway.json exists and valid ✓
- [ ] Region selected: europe-west4 (or your choice)
- [ ] PostgreSQL created and DATABASE_URL copied
- [ ] API service configured with correct paths
- [ ] Frontend service configured with correct paths
- [ ] Environment variables set
- [ ] Services deploying successfully

---

## 🎯 Quick Reference

```
When Railway asks:

Build Context?
→ API: src/api/HoHemaLoans.Api
→ Frontend: src/frontend

Dockerfile?
→ JUST type: Dockerfile

Port?
→ API: 8080
→ Frontend: 3000
```

---

## ✨ What Should Happen

```
You click Deploy
  ↓
Railway pulls GitHub repo
  ↓
Railway looks in Build Context
  ↓
Railway finds Dockerfile in that directory
  ↓
Railway builds Docker image
  ↓
Railway runs container
  ↓
Your app is LIVE! 🎉
```

---

## 🆘 Still Getting Error?

1. **Delete the problematic service**
   - Settings → Delete Service

2. **Create it again with EXACT settings from above**
   - Copy-paste the Build Context and Dockerfile values

3. **Check logs immediately**
   - Logs tab will show if build fails

4. **Contact Railway Support**
   - Discord: https://discord.gg/railway
   - Include: Full error message + Build Context settings

---

**Key takeaway: Build Context = directory, Dockerfile = just the filename!**
