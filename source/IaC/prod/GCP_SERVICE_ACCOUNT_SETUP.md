# GCP Service Account Setup for GitHub Actions

## Overview
This guide explains how to create a GCP Service Account and generate a JSON key for use in GitHub Actions.

---

## Step 1: Create Service Account in GCP Console

1. **Go to GCP Console**: https://console.cloud.google.com/
2. **Select Project**: `fmflow-guru`
3. **Navigate to**: **IAM & Admin** → **Service Accounts**
4. **Click**: **+ CREATE SERVICE ACCOUNT**

### Service Account Details:
- **Service account name**: `github-actions-terraform`
- **Service account ID**: `github-actions-terraform` (auto-generated)
- **Description**: `Service account for GitHub Actions to manage GCP resources via Terraform/OpenTofu`
- **Click**: **CREATE AND CONTINUE**

---

## Step 2: Grant Required Permissions

Add the following roles to the service account:

1. **Project Editor** (or more specific roles):
   - `roles/editor` - For managing GCP resources
   - OR more granular:
     - `roles/serviceusage.serviceUsageAdmin` - Enable/disable APIs
     - `roles/apikeys.admin` - Manage API keys
     - `roles/recaptchaenterprise.admin` - Manage reCAPTCHA keys

2. **Click**: **CONTINUE** → **DONE**

---

## Step 3: Create JSON Key

1. **Click on the service account** you just created
2. **Go to**: **KEYS** tab
3. **Click**: **ADD KEY** → **Create new key**
4. **Select**: **JSON**
5. **Click**: **CREATE**

A JSON file will be downloaded automatically.

---

## Step 4: JSON Key Structure

The downloaded JSON file will look like this:

```json
{
  "type": "service_account",
  "project_id": "fmflow-guru",
  "private_key_id": "abc123...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "github-actions-terraform@fmflow-guru.iam.gserviceaccount.com",
  "client_id": "123456789012345678901",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/..."
}
```

---

## Step 5: Add to GitHub Secrets

1. **Open the downloaded JSON file** in a text editor
2. **Copy the ENTIRE JSON content** (all lines, including braces)
3. **Go to GitHub**: Repository → **Settings** → **Secrets and variables** → **Actions**
4. **Click**: **New repository secret**
5. **Name**: `GCP_SA_KEY`
6. **Value**: Paste the **ENTIRE JSON content** (not just parts of it)
7. **Click**: **Add secret**

---

## Step 6: Verify in GitHub Actions

After adding the secret, the workflow will:
1. Authenticate to GCP using the service account key
2. Allow OpenTofu to manage GCP resources (Places API, reCAPTCHA, etc.)

---

## Security Best Practices

⚠️ **Important**:
- **Never commit** the JSON key file to Git
- **Never share** the key publicly
- **Rotate keys** periodically (every 90 days recommended)
- **Use least privilege** - only grant necessary permissions
- **Delete unused keys** from GCP Console

---

## Troubleshooting

### Error: "Could not load credentials"
- Verify the JSON is complete (all braces, no truncation)
- Check that the secret name is exactly `GCP_SA_KEY`
- Ensure the service account has required permissions

### Error: "Permission denied"
- Verify the service account has `roles/editor` or specific resource permissions
- Check that required APIs are enabled in the project

### Error: "Invalid JSON"
- Make sure you copied the ENTIRE JSON file content
- No extra spaces or line breaks at the beginning/end
- All quotes are properly escaped

---

## Alternative: Use Workload Identity Federation (More Secure)

For better security, consider using **Workload Identity Federation** instead of service account keys:

1. **More secure** - No long-lived keys
2. **Automatic rotation** - No manual key management
3. **Better audit trail** - Clear identity mapping

See: https://github.com/google-github-actions/auth#setting-up-workload-identity-federation

---

## Quick Reference

**GCP Project**: `fmflow-guru`  
**Service Account Name**: `github-actions-terraform`  
**GitHub Secret Name**: `GCP_SA_KEY`  
**Required Permissions**: Project Editor or API-specific roles

