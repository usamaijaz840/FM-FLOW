# Google Secrets Setup for Production

## Overview
This document explains how Google secrets are configured in AWS Secrets Manager for production.

---

## Google Resources Created by Tofu

### 1. Google Places API Key
- **Resource**: `google_apikeys_key.places`
- **Name**: `prod-places-api-key`
- **Status**: ✅ Created by Tofu
- **Key Value**: Retrieved from Tofu output or GCP Console
- **Location in Secrets**: `prod-ecs-fmflow` → `Google__PlacesApiKey`

### 2. Google reCAPTCHA Enterprise Key
- **Resource**: `google_recaptcha_enterprise_key.main`
- **Name**: `prod-recaptcha-key`
- **Status**: ✅ Created by Tofu
- **Site Key**: Retrieved from Tofu output
- **Location in Secrets**: `prod-ecs-fmflow` → `ReCaptcha__SecretKey`

### 3. Google Calendar OAuth Credentials
- **Status**: ⚠️ Manually created in GCP Console
- **Client ID**: `YOUR_GOOGLE_OAUTH_CLIENT_ID`
- **Client Secret**: `YOUR_GOOGLE_OAUTH_CLIENT_SECRET_2`
- **Location in Secrets**: `prod-ecs-fmflow` → `Google__ClientId` and `Google__ClientSecret`

---

## Secrets Manager Configuration

### Secret Name: `prod-ecs-fmflow`

Contains the following Google-related keys:

| Secret Key | Value Source | Status |
|------------|--------------|--------|
| `Google__PlacesApiKey` | `var.google_places_api_key` | ✅ Configured |
| `Google__PlacesApiUrl` | `var.google_places_api_url` | ✅ Configured |
| `Google__UseMockPlacesService` | `var.use_mock_google_places_api` | ✅ Configured |
| `Google__ClientId` | `var.google_client_id` | ✅ Configured |
| `Google__ClientSecret` | `var.google_client_secret` | ✅ Configured |
| `ReCaptcha__SecretKey` | `var.google_recaptcha_site_id` | ⏳ Need to update |

---

## Steps to Complete Setup

### Step 1: Get reCAPTCHA Site Key

After running `tofu apply`, get the reCAPTCHA site key:

```bash
tofu output google_recaptcha_site_key
```

Or from GCP Console:
1. Go to: **Security** → **reCAPTCHA Enterprise**
2. Click on: `prod-recaptcha-key`
3. Copy the **Site Key** (starts with `6L...`)

### Step 2: Update prod.tfvars

Add the reCAPTCHA site key to `prod.tfvars`:

```terraform
google_recaptcha_site_id = "6Lf7-D8sAAAAAJgCOQFgEUb-9PKTh2P7mBGnViYd"  # From Tofu output or GCP Console
```

### Step 3: Apply Tofu Changes

```bash
tofu apply -var-file="prod.tfvars"
```

This will:
- Update the `prod-ecs-fmflow` secret with all Google credentials
- ECS service will automatically pick up the new secrets on next deployment

---

## Current Configuration Status

### ✅ Completed:
- [x] Google OAuth credentials added to `prod.tfvars`
- [x] Places API key added to `prod.tfvars`
- [x] Secrets.tf updated to include `Google__ClientId` and `Google__ClientSecret`
- [x] Variables defined in `variables.tf`

### ⏳ Pending:
- [ ] Get reCAPTCHA site key from Tofu output or GCP Console
- [ ] Update `google_recaptcha_site_id` in `prod.tfvars`
- [ ] Run `tofu apply` to update secrets

---

## Verification

After applying, verify secrets are updated:

```bash
# Check if Google credentials are in the secret
aws secretsmanager get-secret-value \
  --secret-id prod-ecs-fmflow \
  --query 'SecretString' \
  --output text | jq '.Google__ClientId, .Google__PlacesApiKey'
```

---

## Notes

1. **Settings.json**: ❌ NO, you don't need to update settings.json again. These Google secrets are for **backend API only**, not frontend.

2. **Redirect URIs**: Make sure the Google OAuth redirect URIs include:
   - `https://api-prod.referralsource-qa.com/api/Integrations/GoogleCalendar`

3. **Secret Updates**: After updating `prod.tfvars`, run `tofu apply` to update the secret. ECS service will automatically restart and pick up new secrets.


