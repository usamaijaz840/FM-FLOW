# providers.tf
# This file configures the providers used in your Tofu configuration.

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
}

provider "aws" {
  region  = var.aws_region
#  profile = var.aws_profile

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

