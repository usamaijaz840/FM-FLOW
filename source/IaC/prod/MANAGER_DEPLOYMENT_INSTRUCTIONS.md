# Manager Deployment Instructions

**Date:** January 4, 2026  
**Manager Requirements:** Deploy frontend first, then backend

---

## ✅ Requirements Summary

1. ✅ **Health Endpoint Verified:**
   - Dev Health: `https://api.referralsource-qa.com/api/Health` → `200 OK`
   - Dev ALB: `dev-alb-674134508.us-east-1.elb.amazonaws.com`

2. ✅ **Frontend Deploy First:**
   - Workflow updated: Frontend now deploys **BEFORE** backend
   - S3 bucket: `prod-fm-flow-web`
   - CloudFront Distribution: `E1Y5RFCFJTFI0L`

3. ✅ **S3 settings.json Download:**
   - Workflow automatically downloads `settings.json` from S3
   - Location: `s3://prod-fm-flow-web/settings.json`
   - Deploys with frontend build

4. ✅ **Pipeline Ready:**
   - GitHub Actions workflow configured
   - Frontend → Backend deployment order

---

## 🚀 How to Run Pipeline

### Step 1: Go to GitHub Actions

1. Open GitHub repository
2. Click on **"Actions"** tab
3. Select **"Deploy to Production Environment"** workflow

### Step 2: Run Workflow Manually

1. Click **"Run workflow"** button (top right)
2. Fill in the inputs:
   - ✅ `deploy_backend`: `true`
   - ✅ `deploy_frontend`: `true`
   - ✅ `confirm_production`: Type `DEPLOY`
3. Click **"Run workflow"**

### Step 3: Monitor Deployment

The workflow will:
1. ✅ **Deploy Frontend FIRST** (downloads `settings.json` from S3)
2. ✅ **Deploy Backend SECOND** (after frontend completes)

---

## 📋 What Happens During Deployment

### Frontend Deployment (Runs First):

1. ✅ Checkout code
2. ✅ Setup Node.js
3. ✅ Install dependencies
4. ✅ Build frontend (`npx nx build FMFlowWeb`)
5. ✅ Configure AWS credentials
6. ✅ **Download `settings.json` from S3** (`s3://prod-fm-flow-web/settings.json`)
7. ✅ Deploy to S3 with optimized caching
8. ✅ Upload `settings.json` with `no-cache` control
9. ✅ Invalidate CloudFront cache

### Backend Deployment (Runs After Frontend):

1. ✅ Checkout code
2. ✅ Configure AWS credentials
3. ✅ Login to ECR
4. ✅ Build Docker image
5. ✅ Push to ECR (`fm-flow-prod`)
6. ✅ Setup OpenTofu
7. ✅ Authenticate to GCP
8. ✅ Run `tofu apply` (update infrastructure)
9. ✅ Wait for ECS deployment

---

## ✅ Verification After Deployment

### 1. Check Frontend:
```bash
# Check if frontend is accessible
Invoke-WebRequest -Uri "https://app-prod.referralsource-qa.com" -UseBasicParsing

# Check settings.json in S3
aws s3 cp s3://prod-fm-flow-web/settings.json -
```

### 2. Check Backend:
```bash
# Check backend health (using dev endpoint as reference)
Invoke-WebRequest -Uri "https://api-prod.referralsource-qa.com/api/Health" -UseBasicParsing

# Or check root endpoint
Invoke-WebRequest -Uri "https://api-prod.referralsource-qa.com/" -UseBasicParsing
```

### 3. Check CloudFront:
```bash
# Get CloudFront distribution ID
aws cloudfront list-distributions --query "DistributionList.Items[?contains(Aliases.Items[0], 'app-prod')].[Id]"
```

---

## 📝 Current Configuration

| Item | Value | Status |
|------|-------|--------|
| **Frontend URL** | `https://app-prod.referralsource-qa.com` | ✅ Configured |
| **Backend URL** | `https://api-prod.referralsource-qa.com` | ✅ Configured |
| **S3 Bucket** | `prod-fm-flow-web` | ✅ Configured |
| **CloudFront ID** | `E1Y5RFCFJTFI0L` | ✅ Configured |
| **Dev Health Endpoint** | `https://api.referralsource-qa.com/api/Health` | ✅ Working (200 OK) |
| **Deployment Order** | Frontend → Backend | ✅ Updated |

---

## 🎯 Summary

**All requirements met:**

1. ✅ Health endpoint verified (dev endpoint working)
2. ✅ Frontend deploys FIRST (workflow updated)
3. ✅ Pipeline ready to run
4. ✅ `settings.json` downloads from S3 and deploys with frontend

**Next Step:** Run the GitHub Actions workflow!

---

**Last Updated:** January 4, 2026

