# 🐳 Dockerfile Structure for Railway

## Current Setup ✅

Your project has the correct Dockerfile structure:

```
HoHemaLoans/
├── src/
│   ├── api/
│   │   └── HoHemaLoans.Api/
│   │       └── Dockerfile ← Railway looks here for API
│   └── frontend/
│       ├── Dockerfile ← Railway looks here for Frontend
│       └── nginx.conf
├── railway.json ← Configuration file
└── railway.toml ← Services configuration
```

---

## 🚀 How Railway Deploys

### Step 1: You Push to GitHub
```bash
git push origin main
```

### Step 2: Railway Detects Changes
```
Railway webhook notified
Builds started...
```

### Step 3: Railway Finds Dockerfiles

**For API Service:**
```
1. Go to: src/api/HoHemaLoans.Api/
2. Find: Dockerfile
3. Build: .NET application
4. Port: 8080
5. Deploy: API service
```

**For Frontend Service:**
```
1. Go to: src/frontend/
2. Find: Dockerfile
3. Find: nginx.conf (in same directory)
4. Build: React + Nginx
5. Port: 3000
6. Deploy: Frontend service
```

**For Database:**
```
1. Use: Railway's managed PostgreSQL
2. Port: 5432
3. Auto-create: CONNECTION_URL env var
```

---

## ✅ Files Already in Place

| File | Location | Purpose |
|------|----------|---------|
| Dockerfile | `src/api/HoHemaLoans.Api/Dockerfile` | API container |
| Dockerfile | `src/frontend/Dockerfile` | Frontend container |
| nginx.conf | `src/frontend/nginx.conf` | Nginx configuration |
| railway.json | `HoHemaLoans/railway.json` | Project config |
| railway.toml | `HoHemaLoans/railway.toml` | Services config |

---

## 🎯 How to Deploy on Railway

### Method 1: Visual Dashboard (Recommended)

1. **Go to Railway**
   - URL: https://railway.app/dashboard

2. **Create New Project**
   - Click "New Project"
   - "Deploy from GitHub"
   - Select: `Lubs1984/HoHemaLoans`

3. **Add Services One by One**

   **A. PostgreSQL Database:**
   ```
   Click "+ New"
   Select: Database → PostgreSQL
   Railway creates it automatically
   ```

   **B. API Service:**
   ```
   Click "+ New"
   Select: GitHub Repo
   Repository: HoHemaLoans
   
   Configure:
   - Root Directory: src/api/HoHemaLoans.Api
   - Dockerfile: Dockerfile (auto-detected)
   - Port: 8080
   ```

   **C. Frontend Service:**
   ```
   Click "+ New"
   Select: GitHub Repo
   Repository: HoHemaLoans
   
   Configure:
   - Root Directory: src/frontend
   - Dockerfile: Dockerfile (auto-detected)
   - Port: 3000
   ```

4. **Add Environment Variables**
   - See RAILWAY_QUICKSTART.md for all variables

5. **Deploy**
   - Railway auto-builds and deploys
   - Takes ~5-10 minutes first time

### Method 2: Railway CLI

```bash
# Install CLI
npm install -g @railway/cli

# Login
railway login

# Initialize project
railway init

# Deploy
railway up
```

---

## 🔍 Verify Everything is Ready

### Check API Dockerfile
```bash
cat src/api/HoHemaLoans.Api/Dockerfile
```
Should show: Dotnet SDK build + Runtime

### Check Frontend Dockerfile
```bash
cat src/frontend/Dockerfile
```
Should show: Node build + Nginx runtime

### Check nginx.conf
```bash
cat src/frontend/nginx.conf
```
Should show: Nginx configuration for React SPA

### Check railway.json
```bash
cat railway.json
```
Should show: Service configurations

---

## 🚨 Common Issues & Fixes

### Issue: "Dockerfile not found"
```
CAUSE: Railway looking in wrong directory
FIX:   Set Root Directory to correct path
       API: src/api/HoHemaLoans.Api
       Frontend: src/frontend
```

### Issue: "nginx.conf not found"
```
CAUSE: nginx.conf in wrong location
FIX:   Ensure it's in src/frontend/nginx.conf
STATUS: ✅ Already correct in your repo
```

### Issue: "Port already in use"
```
CAUSE: Wrong port number
FIX:   API: 8080 (not 5000)
       Frontend: 3000 (not 5173)
STATUS: ✅ Already correct in Dockerfiles
```

### Issue: "Build fails"
```
Check logs:
1. Go to Railway Dashboard
2. Select Service
3. Click "Logs" tab
4. Look for error message
5. Google the error + "Railway"
```

---

## 📝 Docker Build Process

### For API Service:
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
- Copy project file
- Restore packages
- Build project
- Publish to /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
- Copy published files
- Run as non-root user
- Expose port 8080
- Start .NET application
```

### For Frontend Service:
```dockerfile
# Stage 1: Build
FROM node:20-alpine AS build
- Copy package files
- npm ci (install deps)
- npm run build (create dist/)

# Stage 2: Runtime
FROM nginx:alpine
- Copy dist/ to nginx
- Copy nginx.conf
- Run as non-root user
- Expose port 3000
- Start nginx
```

---

## ✨ What Happens During Deployment

```
1. You push code to GitHub
   ↓
2. Railway webhook triggered
   ↓
3. Railway pulls your repo
   ↓
4. Railway detects Dockerfiles
   ↓
5. For API service:
   - Reads: src/api/HoHemaLoans.Api/Dockerfile
   - Builds: Docker image
   - Runs: Container on port 8080
   ↓
6. For Frontend service:
   - Reads: src/frontend/Dockerfile
   - Finds: src/frontend/nginx.conf
   - Builds: Docker image
   - Runs: Container on port 3000
   ↓
7. Railway creates URLs
   ↓
8. Services are LIVE! 🎉
```

---

## 🎯 Your Next Steps

1. ✅ Verify Dockerfiles exist (they do!)
2. ✅ Commit any changes: `git push`
3. ✅ Go to Railway.app
4. ✅ Create new project
5. ✅ Deploy services
6. ✅ Test!

---

## 📞 Need Help?

- Railway Dockerfile Guide: https://docs.railway.app/deploy/dockerfile
- Docker Documentation: https://docs.docker.com/
- Your project Dockerfiles: Already correct! ✅

---

**You're all set! The Dockerfiles are in the right places. Time to deploy! 🚀**
