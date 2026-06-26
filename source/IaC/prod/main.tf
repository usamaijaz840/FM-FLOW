# Production ALB security group
resource "aws_security_group" "alb" {
  name        = "${var.environment}-alb-sg"
  description = "Security group for ALB"
  vpc_id      = aws_vpc.main.id

  tags = {
    Name        = "${var.environment}-alb-sg"
    Environment = var.environment
    ManagedBy   = "tofu"
  }

  lifecycle {
    ignore_changes = [ingress, egress]
  }
}

# Allow HTTP (port 80) inbound to ALB
resource "aws_security_group_rule" "alb_http_ingress" {
  type              = "ingress"
  from_port         = 80
  to_port           = 80
  protocol          = "tcp"
  cidr_blocks       = ["0.0.0.0/0"]
  description       = "Allow HTTP traffic from internet"
  security_group_id = aws_security_group.alb.id
}

# Allow HTTPS (port 443) inbound to ALB
resource "aws_security_group_rule" "alb_https_ingress" {
  type              = "ingress"
  from_port         = 443
  to_port           = 443
  protocol          = "tcp"
  cidr_blocks       = ["0.0.0.0/0"]
  description       = "Allow HTTPS traffic from internet"
  security_group_id = aws_security_group.alb.id
}

# Allow all outbound traffic from ALB (for health checks, reaching ECS tasks, etc.)
resource "aws_security_group_rule" "alb_all_egress" {
  type              = "egress"
  from_port         = 0
  to_port           = 65535
  protocol          = "-1"
  cidr_blocks       = ["0.0.0.0/0"]
  description       = "Allow all outbound traffic from ALB"
  security_group_id = aws_security_group.alb.id
}

# Output the S3 bucket name for the React app
output "react_app_bucket_name" {
  value = aws_s3_bucket.react_app.bucket
}

