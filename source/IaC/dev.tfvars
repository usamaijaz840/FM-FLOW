# dev.tfvars
# This file contains the development environment values for your variables.
# DO NOT commit this file to version control as it may contain sensitive information.

# Environment and AWS Configuration
environment         = "dev"
aws_region         = "us-east-1"
aws_profile        = "fmflow"  # Specify the AWS profile to use

# Network Configuration
vpc_cidr           = "10.0.0.0/16"
public_subnet_cidr = "10.0.1.0/24"
public_subnet_cidr_2 = "10.0.2.0/24"
availability_zone  = "us-east-1a"
availability_zone_2 = "us-east-1b"

# RDS Configuration
db_name                = "FmFlowDev"
db_username           = "dbadmin"
db_password           = "YOUR_DB_PASSWORD"  # Change this!
db_instance_class     = "db.t3.micro"
db_engine_version     = "17.4"
db_port               = 5432
db_allocated_storage  = 20
db_backup_retention_period = 1
db_multi_az          = false
db_skip_final_snapshot = true

# File Upload Configuration
file_upload_max_size = 10485760  # 10 MB
s3_bucket_name      = "fm-flow-app-files-dev"  # Update this!

# Email Settings
email_api_key        = "YOUR_DEV_SENDGRID_API_KEY"  # Update this!
email_from_address  = "dev@fmflow.com"
email_from_name     = "FMFlow Dev"
email_service_enabled = false

# Keycloak Configuration
keycloak_auth_server_url = "https://identity.1upti.me"
keycloak_realm          = "fmflow-dev"
keycloak_resource       = "fm-flow-api-dev"
keycloak_ssl_required   = "none"
keycloak_verify_token_audience = false
keycloak_secret         = "YOUR_DEV_KEYCLOAK_SECRET"  # Update this!
keycloak_confidential_port = 0
keycloak_require_https_metadata = false
keycloak_registration_token = "YOUR_DEV_REGISTRATION_TOKEN"  # Update this!

# MX Connect Configuration
mx_connect_api_key    = "YOUR_DEV_MX_API_KEY"  # Update this!
mx_connect_api_secret = "YOUR_DEV_MX_API_SECRET"  # Update this!
mx_connect_base_url   = "https://sandbox.api.mxmerchant.com"
mx_connect_merchant_id = "1000020594"

# CORS Configuration
allowed_origins = ["*"]

# ALB Configuration
certificate_arn = null  # Set this after creating the certificate

# Domain Configuration
domain_name = "dev.referralsource-qa.com"  # Replace with your actual dev domain name

