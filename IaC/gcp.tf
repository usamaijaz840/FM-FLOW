# Enable Services
resource "google_project_service" "calendar" {
  service = "calendar-json.googleapis.com"
  disable_on_destroy = false
}

resource "google_project_service" "places" {
  project            = var.gcp_project_id
  service            = "places.googleapis.com"
  disable_on_destroy = false
}

# Enable API Keys API (required before creating API keys)
resource "google_project_service" "apikeys" {
  project            = var.gcp_project_id
  service            = "apikeys.googleapis.com"
  disable_on_destroy = false
}

resource "google_project_service" "recaptcha" {
  service            = "recaptchaenterprise.googleapis.com"
  disable_on_destroy = false
}

# Google Places API Key
resource "google_apikeys_key" "places" {
  project      = var.gcp_project_id
  name         = "${var.environment}-places-api-key"
  display_name = "Places API Key (${var.environment})"

  restrictions {
    api_targets {
      service = "places.googleapis.com"
    }
  }

  depends_on = [
    google_project_service.places,
    google_project_service.apikeys
  ]
}

# reCAPTCHA Enterprise Key
resource "google_recaptcha_enterprise_key" "main" {
  display_name = "${var.environment}-recaptcha-key"

  web_settings {
    integration_type  = "SCORE"
    allow_all_domains = true # For development. In prod, restrict to allowed_domains
    # allowed_domains = [var.domain_name] 
  }

  depends_on = [google_project_service.recaptcha]
}