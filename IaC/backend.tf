terraform {
  backend "s3" {
    bucket         = "fmflow-terraform-state"
    key            = "terraform.tfstate"
    region         = "us-east-1"
    encrypt        = true
    dynamodb_table = "fmflow-terraform-locks"
#    profile        = "fmflow"
  }
}

