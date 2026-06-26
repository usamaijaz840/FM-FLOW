# prod.tfvars
# Production Environment Configuration
# This file contains the production environment values for your variables.
# DO NOT commit this file to version control as it may contain sensitive information.

# Environment and AWS Configuration
environment         = "prod"
aws_region         = "us-east-1"
aws_profile        = "fmflow"  # Specify the AWS profile to use

# Network Configuration
# Using different CIDR blocks to avoid conflicts with dev environment
vpc_cidr           = "10.1.0.0/16"
public_subnet_cidr = "10.1.1.0/24"
public_subnet_cidr_2 = "10.1.2.0/24"
availability_zone  = "us-east-1a"
availability_zone_2 = "us-east-1b"

# RDS Configuration - Production settings
db_name                = "FmFlowProd"
db_username           = "dbadmin"
db_password           = "YOUR_DB_PASSWORD" 
db_instance_class     = "db.t3.small" 
db_engine_version     = "17.4"
db_port               = 5432
db_allocated_storage  = 20 
db_backup_retention_period = 1
db_multi_az          = false  # Multi-AZ for high availability
db_skip_final_snapshot = true  # Always create snapshot before deletion

# File Upload Configuration
file_upload_max_size = 10485760  # 10 MB
s3_bucket_name      = "prod-fm-flow-app-files"  # Production S3 bucket name

# Email Settings - Production
email_api_key        = "YOUR_PROD_SENDGRID_API_KEY"  # Update with production SendGrid key!
email_from_address  = "no-reply@prod.referralsource-qa.com"
email_from_name     = "ReferralSource"
email_service_enabled = true

# Keycloak Configuration - Production
keycloak_auth_server_url = "https://identity.1upti.me"  # External/existing Keycloak URL
aws_keycloak_auth_server_url = "https://identity.prod.referralsource-qa.com"  # AWS Route53 Keycloak URL (TEMPORARY - using dev Route53 zone)
keycloak_realm          = "fmflow-prod"
keycloak_resource       = "fm-flow-api-prod"
keycloak_ssl_required   = "external"  # Require SSL in production
keycloak_verify_token_audience = true  # Verify token audience in production
keycloak_secret         = "YOUR_PROD_KEYCLOAK_SECRET"  # Update with production secret!
keycloak_confidential_port = 0
keycloak_require_https_metadata = true  # Require HTTPS in production
keycloak_registration_token = "YOUR_PROD_REGISTRATION_TOKEN"  # Update with production token!

# Keycloak Admin Configuration - Production
keycloak_admin_username = "admin"
keycloak_admin_password = "CHANGE_THIS_PRODUCTION_PASSWORD"  # MUST CHANGE - Use strong password!
keycloak_db_name        = "keycloak"

# MX Connect Configuration - Production (use production API, not sandbox)
mx_connect_api_key    = "YOUR_PROD_MX_API_KEY"  # Update with production MX API key!
mx_connect_api_secret = "YOUR_PROD_MX_API_SECRET"  # Update with production MX API secret!
mx_connect_base_url   = "https://api.mxmerchant.com"  # Production API URL
mx_connect_merchant_id = "YOUR_PROD_MERCHANT_ID"  # Update with production merchant ID!

# Google reCAPTCHA - Production
google_recaptcha_site_id = "6Lf7-D8sAAAAAJgCOQFgEUb-9PKTh2P7mBGnViYd"  # From Tofu state

# CORS Configuration - Production
allowed_origins = "*"

# ALB Configuration
certificate_arn = null  # Set this after creating the certificate for production domain

# Domain Configuration - Production (TEMPORARY - Using dev Route53 zone, will update when actual production domain is available)
domain_name = "prod.referralsource-qa.com"  # TEMPORARY - Using dev Route53 zone for now

# Frontend Base Url - Production
frontend_base_url = "https://app-prod.referralsource-qa.com"

# Integration Redirect URI - Production
integrations_redirect_uri = "https://app-prod.referralsource-qa.com/app/pro/integrations"

# Swagger UI - Disabled in production for security
show_swagger_ui = false

# Custom JWT Configuration - Production (use different keys than dev!)
jwt_signing_key = "CHANGE_THIS_PRODUCTION_JWT_SIGNING_KEY"  # MUST CHANGE - Use strong random key!
jwt_audience = "fmflow-app-prod"
jwt_issuer = "fmflow-app-prod"

# Google Places API Configuration - Production
google_places_api_key = "YOUR_GOOGLE_PLACES_API_KEY"  # From GCP Console
use_mock_google_places_api = false
google_places_api_url = "https://places.googleapis.com/v1"

# Google Cloud Configuration
gcp_project_id       = "fmflow-guru"  # Same GCP project, but may need separate keys
gcp_region           = "us-central1"

# Google Calendar OAuth - Production
google_client_id     = "YOUR_GOOGLE_OAUTH_CLIENT_ID"
google_client_secret = "YOUR_GOOGLE_OAUTH_CLIENT_SECRET_2"


