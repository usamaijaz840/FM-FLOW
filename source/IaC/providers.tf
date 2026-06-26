# providers.tf
# This file configures the providers used in your Tofu configuration.
# Use this file for:
# - Declaring and configuring providers (AWS, Azure, etc.)
# - Setting provider-specific settings
# - Configuring provider authentication
# - Setting provider versions
#
# DO NOT put resource definitions, variables, or outputs here.
# Only provider configurations should be in this file.

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    google = {
      source  = "hashicorp/google"
      version = "~> 6.0"
    }
  }

  # Uncomment and configure this block when you're ready to use remote state
  # backend "s3" {
  #   bucket         = "your-terraform-state-bucket"
  #   key            = "path/to/state/file"
  #   region         = "us-east-1"
  #   dynamodb_table = "terraform-state-lock"
  #   encrypt        = true
  # }
}

provider "aws" {
  region  = var.aws_region
#  profile = var.aws_profile  # Use the profile specified in variables

  default_tags {
    tags = {
      Environment = var.environment
      ManagedBy   = "tofu"
    }
  }
}

provider "google" {
  project = var.gcp_project_id
  region  = var.gcp_region
}