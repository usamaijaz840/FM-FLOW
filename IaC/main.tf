# Existing ALB security group - imported from manually created resource
resource "aws_security_group" "alb" {
  name        = "${var.environment}-alb-sg"
  description = "Security group for ALB"
  vpc_id      = aws_vpc.existing.id

  tags = {
    Name        = "${var.environment}-alb-sg"
    Environment = var.environment
    ManagedBy   = "tofu"
  }

  lifecycle {
    ignore_changes = [ingress, egress]
  }
}

# Output the S3 bucket name for the React app
output "react_app_bucket_name" {
  value = aws_s3_bucket.react_app.bucket
}
