# Environment and AWS Configuration
variable "environment" {
  description = "Environment name (e.g., dev, prod)"
  type        = string
}

variable "aws_region" {
  description = "AWS region to deploy resources"
  type        = string
}

variable "aws_profile" {
  description = "AWS profile to use for authentication"
  type        = string
}

# Network Configuration
variable "vpc_cidr" {
  description = "CIDR block for the VPC"
  type        = string
}

variable "public_subnet_cidr" {
  description = "CIDR block for the first public subnet"
  type        = string
}

variable "public_subnet_cidr_2" {
  description = "CIDR block for the second public subnet"
  type        = string
}

variable "availability_zone" {
  description = "First availability zone"
  type        = string
}

variable "availability_zone_2" {
  description = "Second availability zone"
  type        = string
}

# RDS Configuration
variable "db_name" {
  description = "Name of the database"
  type        = string
}

variable "db_username" {
  description = "Username for the database"
  type        = string
}

variable "db_password" {
  description = "Password for the database"
  type        = string
  sensitive   = true
}

variable "db_instance_class" {
  description = "Instance class for the RDS instance"
  type        = string
}

variable "db_engine_version" {
  description = "PostgreSQL engine version"
  type        = string
}

variable "db_port" {
  description = "Port for the database"
  type        = number
}

variable "db_allocated_storage" {
  description = "Allocated storage for the database in GB"
  type        = number
}

variable "db_backup_retention_period" {
  description = "Number of days to retain backups"
  type        = number
}

variable "db_multi_az" {
  description = "Whether to enable multi-AZ deployment"
  type        = bool
}

variable "db_skip_final_snapshot" {
  description = "Whether to skip final snapshot when destroying the database"
  type        = bool
}

# File Upload Configuration
variable "file_upload_max_size" {
  description = "Maximum file upload size in bytes"
  type        = number
}

variable "s3_bucket_name" {
  description = "Name of the S3 bucket for file uploads"
  type        = string
}

# Email Settings
variable "email_api_key" {
  description = "SendGrid API key"
  type        = string
  sensitive   = true
}

variable "email_from_address" {
  description = "Email address to send from"
  type        = string
}

variable "email_from_name" {
  description = "Name to send emails from"
  type        = string
}

variable "email_service_enabled" {
  description = "Whether to enable email service"
  type        = bool
}

# Keycloak Configuration
variable "keycloak_auth_server_url" {
  description = "Keycloak authentication server URL (external/existing)"
  type        = string
}

variable "aws_keycloak_auth_server_url" {
  description = "AWS-hosted Keycloak authentication server URL (Route53)"
  type        = string
  default     = ""
}

variable "keycloak_realm" {
  description = "Keycloak realm name"
  type        = string
}

variable "keycloak_resource" {
  description = "Keycloak resource name"
  type        = string
}

variable "keycloak_ssl_required" {
  description = "Keycloak SSL requirement"
  type        = string
}

variable "keycloak_verify_token_audience" {
  description = "Whether to verify token audience"
  type        = bool
}

variable "keycloak_secret" {
  description = "Keycloak client secret"
  type        = string
  sensitive   = true
}

variable "keycloak_confidential_port" {
  description = "Keycloak confidential port"
  type        = number
}

variable "keycloak_require_https_metadata" {
  description = "Whether to require HTTPS for metadata"
  type        = bool
}

variable "keycloak_registration_token" {
  description = "Keycloak registration token"
  type        = string
  sensitive   = true
}

# MX Connect Configuration
variable "mx_connect_api_key" {
  description = "MX Connect API key"
  type        = string
  sensitive   = true
}

variable "mx_connect_api_secret" {
  description = "MX Connect API secret"
  type        = string
  sensitive   = true
}

variable "mx_connect_base_url" {
  description = "MX Connect base URL"
  type        = string
}

variable "mx_connect_merchant_id" {
  description = "MX Connect merchant ID"
  type        = string
}

variable "google_recaptcha_site_id" {
	description = "Google reCAPTCHA site ID"
	type        = string
}

# CORS Configuration
variable "allowed_origins" {
  description = "List of allowed origins for CORS"
  type        = string
}

# ALB Configuration
variable "certificate_arn" {
  description = "ARN of the SSL certificate"
  type        = string
  default     = null
}

# Domain Configuration
variable "domain_name" {
  description = "Domain name for the application"
  type        = string
}

# Frontend Base Url
variable "frontend_base_url" {
  description = "Base URL for the frontend application"
  type        = string
}

# Integration Redirect URI
variable "integrations_redirect_uri" {
  description = "Redirect URL for integrations"
  type        = string
}

variable "show_swagger_ui" {
  description = "Whether to show Swagger UI"
  type        = bool
  default     = false
}

variable "jwt_signing_key" {
  description = "JWT signing key for the application"
  type        = string
  sensitive   = true
}

variable "jwt_audience" {
  description = "JWT audience for tokens issued by the application"
  type = string
  sensitive = true
}

variable "jwt_issuer" {
  description = "JWT issuer for tokens issued by the application"
  type = string
  sensitive = true
}

variable "google_places_api_url" {
  description = "Google Places API URL"
  type        = string
}

variable "google_places_api_key" {
  description = "Google Places API Key"
  type        = string
  sensitive   = true
}

variable "use_mock_google_places_api" {
  description = "Whether to use mock Google Places API"
  type        = bool
  default     = true
}

variable "docker_image" {
  description = "Docker image URL for ECS task definition"
  type        = string
  default     = "324037278482.dkr.ecr.us-east-1.amazonaws.com/fm-flow:latest"
}

# EC2 Bastion Host Configuration
variable "bastion_key_pair_name" {
  description = "Name of the AWS key pair to use for the bastion host (leave empty to not use a key pair)"
  type        = string
  default     = "bastionhost"
}

# Keycloak Configuration
variable "keycloak_admin_username" {
  description = "Keycloak admin username"
  type        = string
  default     = "admin"
}

variable "keycloak_admin_password" {
  description = "Keycloak admin password"
  type        = string
  sensitive   = true
}

variable "keycloak_db_name" {
  description = "Database name for Keycloak"
  type        = string
  default     = "keycloak"
}

# --- ADDED: Google Cloud Configuration ---

variable "gcp_project_id" {
  description = "Google Cloud Project ID"
  type        = string
}

variable "gcp_region" {
  description = "Google Cloud Region"
  type        = string
  default     = "us-central1"
}

# Google Integration Variables (For Manual Details)
variable "google_client_id" {
  description = "Google OAuth Client ID (for Calendar)"
  type        = string
}

variable "google_client_secret" {
  description = "Google OAuth Client Secret (for Calendar)"
  type        = string
  sensitive   = true
}

