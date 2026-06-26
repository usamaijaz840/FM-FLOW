# How to Find Production AWS Role ARN

## Step-by-Step Guide

### Step 1: Open AWS Console
1. Go to: https://console.aws.amazon.com/
2. Login with your AWS credentials
3. Make sure you're in the **correct AWS account** (where production resources are)

---

### Step 2: Navigate to IAM Roles
1. In the AWS Console search bar (top), type: **"IAM"**
2. Click on **"IAM"** service
3. In the left sidebar, click on **"Roles"**

---

### Step 3: Find GitHub Actions Role

Look for roles with names like:
- `github-actions-prod-role`
- `github-actions-production-role`
- `prod-github-actions-role`
- `fmflow-prod-github-role`
- Or any role that mentions "github", "actions", "prod", "production"

**If you don't see a production-specific role:**
- Check if there's a **dev role** (like `github-actions-dev-role` or `dev-github-actions-role`)
- You can use the same role ARN if it has production permissions
- Or check with your manager which role to use

---

### Step 4: Copy the Role ARN

1. **Click on the role name** to open it
2. At the top of the page, you'll see **"Role ARN"**
3. It will look like: `arn:aws:iam::324037278482:role/github-actions-prod-role`
4. **Copy the entire ARN** (from `arn:` to the end)

---

### Step 5: Verify Role Permissions (Optional but Recommended)

While you're on the role page:
1. Check the **"Permissions"** tab
2. Make sure it has permissions for:
   - ECS (Elastic Container Service)
   - ECR (Elastic Container Registry)
   - S3
   - CloudFront
   - Secrets Manager
   - Or at least `AmazonEC2ContainerServiceFullAccess` and `AmazonEC2ContainerRegistryFullAccess`

---

## Alternative: Check Existing Dev Role

If you can't find a production role:

1. **Go to IAM → Roles**
2. **Search for "dev"** or "github"
3. **Find the dev role** (e.g., `github-actions-dev-role`)
4. **Copy its ARN**
5. **Check with your manager** if this role can be used for production, or if you need to create a new one

---

## Role ARN Format

The ARN will look like this:
```
arn:aws:iam::ACCOUNT_ID:role/ROLE_NAME
```

Example:
```
arn:aws:iam::324037278482:role/github-actions-prod-role
```

---

## Quick Visual Guide

```
AWS Console
  ↓
Search: "IAM"
  ↓
Left Sidebar: "Roles"
  ↓
Find role with "github" or "prod" in name
  ↓
Click on role name
  ↓
Copy "Role ARN" from top of page
```

---

## If No Role Exists

If you don't find any GitHub Actions role:

1. **Check with your manager** - they might need to create one
2. **Or use an existing role** that has the required permissions
3. **Or create a new role** (requires IAM permissions)

---

## Next Step

After you get the Role ARN:
1. Go to GitHub → Repository → Settings → Secrets → Actions
2. Add new secret: `PROD_AWS_ROLE_ARN`
3. Paste the ARN value
4. Save

