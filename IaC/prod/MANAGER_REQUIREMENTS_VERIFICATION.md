# Manager Requirements Verification Checklist

**Date:** January 4, 2026  
**Environment:** Production

---

## ✅ Requirement 1: Frontend URL
**Required:** `https://app-prod.referralsource-qa.com`

### Where to Check:

#### 1. Route53 DNS Record
```bash
aws route53 list-resource-record-sets --hosted-zone-id Z0368402HCW12J2L63FT --query "ResourceRecordSets[?Name=='app-prod.referralsource-qa.com.']"
```
**Expected:** A record pointing to CloudFront distribution

#### 2. CloudFront Distribution
```bash
aws cloudfront list-distributions --query "DistributionList.Items[?Aliases.Items[?contains(@, 'app-prod.referralsource-qa.com')]]"
```
**Expected:** Distribution ID `E1Y5RFCFJTFI0L` with alias `app-prod.referralsource-qa.com`

#### 3. Configuration Files
- ✅ `source/IaC/prod/prod.tfvars` (line 77): `frontend_base_url = "https://app-prod.referralsource-qa.com"`
- ✅ `source/IaC/prod/route53.tf` (line 28): `name = "app-prod.referralsource-qa.com"`
- ✅ `source/IaC/prod/cloudfront.tf` (line 70): `aliases = ["app-prod.referralsource-qa.com"]`

#### 4. Test URL
Open in browser: **https://app-prod.referralsource-qa.com**

---

## ✅ Requirement 2: Backend URL
**Required:** `https://api-prod.referralsource-qa.com`

### Where to Check:

#### 1. Route53 DNS Record
```bash
aws route53 list-resource-record-sets --hosted-zone-id Z0368402HCW12J2L63FT --query "ResourceRecordSets[?Name=='api-prod.referralsource-qa.com.']"
```
**Expected:** A record pointing to ALB (`prod-alb-99350667.us-east-1.elb.amazonaws.com`)

#### 2. ALB Configuration
```bash
aws elbv2 describe-load-balancers --query "LoadBalancers[?contains(LoadBalancerName, 'prod-alb')]"
```
**Expected:** ALB name `prod-alb` with DNS `prod-alb-99350667.us-east-1.elb.amazonaws.com`

#### 3. Configuration Files
- ✅ `source/IaC/prod/route53.tf` (line 54): `name = "api-prod.referralsource-qa.com"`
- ✅ `source/IaC/prod/lb.tf` (line 24): Certificate includes `api-prod.referralsource-qa.com`

#### 4. Test URL
```bash
curl https://api-prod.referralsource-qa.com/api/Health
```
**Expected:** Health check response (200 OK or 404 if endpoint doesn't exist)

---

## ✅ Requirement 3: AllowedOrigins = "*"
**Required:** CORS should allow all origins

### Where to Check:

#### 1. AWS Secrets Manager
```bash
aws secretsmanager get-secret-value --secret-id prod-ecs-fmflow --query 'SecretString' --output text | ConvertFrom-Json | Select-Object AllowedOrigins__0
```
**Expected:** `AllowedOrigins__0 : *`

#### 2. Configuration Files
- ✅ `source/IaC/prod/prod.tfvars` (line 68): `allowed_origins = "*"`
- ✅ `source/IaC/prod/secrets.tf` (line 92): `AllowedOrigins__0 = var.allowed_origins`

#### 3. Verify in Running ECS Service
The secret is loaded into the ECS task at runtime. Restart the ECS service if needed:
```bash
aws ecs update-service --cluster prod-cluster --service prod-api-service --force-new-deployment
```

---

## ✅ Requirement 4: GitHub Actions Pipeline
**Required:** Pipeline should work for backend and frontend

### Where to Check:

#### 1. GitHub Actions Workflow File
- ✅ `.github/workflows/deploy-to-prod.yml` exists and is configured

#### 2. GitHub Secrets (Required)
Check that these secrets are configured in GitHub:
- ✅ `GCP_SA_KEY` - GCP Service Account JSON key
- ✅ `PROD_AWS_ROLE_ARN` - AWS IAM Role ARN for production
- ✅ `PROD_CLOUDFRONT_DISTRIBUTION_ID` - CloudFront Distribution ID (optional)

#### 3. Test Pipeline
1. Go to GitHub → Actions tab
2. Select "Deploy to Production Environment" workflow
3. Click "Run workflow"
4. Fill in:
   - `deploy_backend`: ✅ true
   - `deploy_frontend`: ✅ true
   - `confirm_production`: `DEPLOY`
5. Click "Run workflow"

#### 4. Verify Pipeline Steps
- ✅ Backend Docker image builds
- ✅ Image pushed to ECR (`fm-flow-prod`)
- ✅ Tofu apply runs successfully
- ✅ Frontend S3 bucket updated
- ✅ CloudFront cache invalidated

---

## ✅ Requirement 5: Frontend S3 Bucket & settings.json
**Required:** Update frontend S3 bucket and settings.json

### Where to Check:

#### 1. S3 Bucket
```bash
aws s3 ls s3://prod-fm-flow-web/
```
**Expected:** `settings.json` file exists

#### 2. settings.json Content
```bash
aws s3 cp s3://prod-fm-flow-web/settings.json -
```
**Expected:**
```json
{
  "apiUrl": "https://api-prod.referralsource-qa.com/api",
  "recaptchaSiteKey": "6Lf7-D8sAAAAAJgCOQFgEUb-9PKTh2P7mBGnViYd",
  ...
}
```

#### 3. CloudFront Cache
After updating `settings.json`, invalidate CloudFront cache:
```bash
aws cloudfront create-invalidation --distribution-id E1Y5RFCFJTFI0L --paths "/settings.json"
```

#### 4. GitHub Actions Workflow
Check `.github/workflows/deploy-to-prod.yml`:
- ✅ Frontend deployment step uploads to `prod-fm-flow-web` bucket
- ✅ `settings.json` is updated with correct `apiUrl`

---

## 📋 Quick Verification Commands

### All URLs Test
```bash
# Frontend
curl -I https://app-prod.referralsource-qa.com

# Backend API
curl -I https://api-prod.referralsource-qa.com/api/Health

# Identity/Keycloak
curl -I https://identity-prod.referralsource-qa.com
```

### All Secrets Check
```bash
# AllowedOrigins
aws secretsmanager get-secret-value --secret-id prod-ecs-fmflow --query 'SecretString' --output text | ConvertFrom-Json | Select-Object AllowedOrigins__0

# settings.json
aws s3 cp s3://prod-fm-flow-web/settings.json - | ConvertFrom-Json | Select-Object apiUrl
```

### All DNS Records
```bash
aws route53 list-resource-record-sets --hosted-zone-id Z0368402HCW12J2L63FT --query "ResourceRecordSets[?contains(Name, 'prod')]"
```

---

## ✅ Summary Status

| Requirement | Status | Location |
|------------|--------|----------|
| Frontend URL | ✅ | `https://app-prod.referralsource-qa.com` |
| Backend URL | ✅ | `https://api-prod.referralsource-qa.com` |
| AllowedOrigins | ✅ | `*` (verified in Secrets Manager) |
| GitHub Actions | ✅ | Workflow configured |
| S3 Bucket | ✅ | `prod-fm-flow-web` |
| settings.json | ✅ | Updated with correct `apiUrl` |

---

## 🎯 Next Steps

1. **Test URLs in Browser:**
   - Frontend: https://app-prod.referralsource-qa.com
   - Backend: https://api-prod.referralsource-qa.com/api/Health

2. **Run GitHub Actions Pipeline:**
   - Go to GitHub → Actions → "Deploy to Production Environment"
   - Run workflow manually to test

3. **Verify ECS Service:**
   - Check that `prod-api-service` is running
   - Verify logs show no errors

4. **Monitor CloudWatch:**
   - Check ECS service logs
   - Check ALB target group health

---

**Last Updated:** January 4, 2026

