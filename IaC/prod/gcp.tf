# Production Google Cloud Services
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

# Production Google Places API Key
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

# Production reCAPTCHA Enterprise Key
resource "google_recaptcha_enterprise_key" "main" {
  display_name = "${var.environment}-recaptcha-key"

  web_settings {
    integration_type  = "SCORE"
    allow_all_domains = false # Production: restrict to specific domains
    allowed_domains   = ["prod.referralsource-qa.com", "app-prod.referralsource-qa.com"]
  }

  depends_on = [google_project_service.recaptcha]
}

# Outputs for Google resources
output "google_places_api_key" {
  description = "Google Places API Key"
  value       = google_apikeys_key.places.key_string
  sensitive   = true
}

output "google_recaptcha_site_key" {
  description = "Google reCAPTCHA Site Key"
  value       = google_recaptcha_enterprise_key.main.name
  sensitive   = false
}

